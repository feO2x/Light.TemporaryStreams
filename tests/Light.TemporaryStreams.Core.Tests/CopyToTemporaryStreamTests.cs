using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TemporaryStreams.Hashing;
using Xunit;

namespace Light.TemporaryStreams;

public static class CopyToTemporaryStreamTests
{
    [Theory]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes - 1)]
    [InlineData(12_000)]
    public static async Task CopyToTemporaryStreamAsync_ShouldCreateMemoryStream_WhenSourceIsSmall(int bufferSize)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(bufferSize);
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(sourceData);
    }

    [Theory]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes)]
    [InlineData(100_000)]
    public static async Task CopyToTemporaryStreamAsync_ShouldCreateFileStream_WhenSourceIsLarge(int bufferSize)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(bufferSize); // More than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(sourceData);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCopyBufferSize_WhenSpecified()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000);
        using var sourceStream = new MemoryStream(sourceData);
        var filePath = Path.GetFullPath("test.txt");

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            filePath: filePath,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.GetUnderlyingFilePath().Should().Be(filePath);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCustomOptions_WhenProvided()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(50_000); // Would normally use MemoryStream
        using var sourceStream = new MemoryStream(sourceData);

        var customOptions = new TemporaryStreamServiceOptions
        {
            FileThresholdInBytes = 30_000 // Force FileStream usage
        };

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            options: customOptions,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue(); // Should use file due to custom threshold
        temporaryStream.Length.Should().Be(sourceData.Length);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldForwardFilePath()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000);
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            filePath: "test.txt",
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(sourceData);
    }

    [Theory]
    [InlineData(HashConversionMethod.Base64)]
    [InlineData(HashConversionMethod.UpperHexadecimal)]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldCalculateCorrectHash_WhenSourceIsSmall(
        HashConversionMethod hashConversionMethod
    )
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(40_000); // Less than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);
        await using var hashingPlugin =
            new HashingPlugin([new CopyToHashCalculator(SHA1.Create(), hashConversionMethod)]);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.Position = 0;
        var expectedHash = HashConverter.ConvertHashToString(SHA1.HashData(sourceData), hashConversionMethod);
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(sourceData);
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedHash);
    }

    [Theory]
    [InlineData(HashConversionMethod.Base64)]
    [InlineData(HashConversionMethod.UpperHexadecimal)]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldCalculateCorrectHash_WhenSourceIsLarge(
        HashConversionMethod hashConversionMethod
    )
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000); // More than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);
        await using var hashingPlugin =
            new HashingPlugin([new CopyToHashCalculator(MD5.Create(), hashConversionMethod)]);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(sourceData);
        var expectedHash = HashConverter.ConvertHashToString(MD5.HashData(sourceData), hashConversionMethod);
        hashingPlugin.GetHash(nameof(MD5)).Should().Be(expectedHash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldSupportMultipleHashAlgorithms()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(50_000);
        using var sourceStream = new MemoryStream(sourceData);

        await using var hashingPlugin = new HashingPlugin(
            [
                new CopyToHashCalculator(SHA1.Create(), HashConversionMethod.UpperHexadecimal),
                new CopyToHashCalculator(SHA256.Create(), HashConversionMethod.Base64)
            ]
        );

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            [hashingPlugin],
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        var expectedSha1Hash = HashConverter.ConvertHashToString(
            SHA1.HashData(sourceData),
            HashConversionMethod.UpperHexadecimal
        );
        var expectedSha256Hash = HashConverter.ConvertHashToString(
            SHA256.HashData(sourceData),
            HashConversionMethod.Base64
        );
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedSha1Hash);
        hashingPlugin.GetHash(nameof(SHA256)).Should().Be(expectedSha256Hash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldUseCustomBufferSize()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(60_000);
        using var sourceStream = new MemoryStream(sourceData);
        await using var hashingPlugin = new HashingPlugin([SHA1.Create()]);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            copyBufferSize: 4096,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        var expectedHash = HashConverter.ConvertHashToString(SHA1.HashData(sourceData), HashConversionMethod.Base64);
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedHash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldForwardFilePath()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000);
        using var sourceStream = new MemoryStream(sourceData);
        await using var hashingPlugin = new HashingPlugin([SHA1.Create()]);
        var filePath = Path.GetFullPath("test.txt");

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            [hashingPlugin],
            filePath: filePath,
            cancellationToken: cancellationToken
        );

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        temporaryStream.GetUnderlyingFilePath().Should().Be(filePath);
        var expectedHash = HashConverter.ConvertHashToString(SHA1.HashData(sourceData), HashConversionMethod.Base64);
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedHash);
    }

    private static TemporaryStreamService CreateDefaultService() =>
        new (
            new TemporaryStreamServiceOptions(),
            new TemporaryStreamErrorHandlerProvider(
                (_, exception) => { TestContext.Current.TestOutputHelper?.WriteLine(exception.ToString()); }
            )
        );

    private static byte[] CreateTestData(int size)
    {
        var data = new byte[size];
        var random = new Random(42); // Fixed seed for reproducible tests
        random.NextBytes(data);
        return data;
    }
}
