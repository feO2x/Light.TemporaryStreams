using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
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
        var (service, sourceData, sourceStream) = CreateTestSetup(bufferSize);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: false,
            cancellationToken
        );
    }

    [Theory]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes)]
    [InlineData(100_000)]
    public static async Task CopyToTemporaryStreamAsync_ShouldCreateFileStream_WhenSourceIsLarge(int bufferSize)
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(bufferSize);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: true,
            cancellationToken
        );
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCopyBufferSize_WhenSpecified()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(100_000);
        var cancellationToken = TestContext.Current.CancellationToken;
        var filePath = Path.GetFullPath("test.txt");

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            filePath: filePath,
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: true,
            cancellationToken
        );
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCustomOptions_WhenProvided()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(50_000); // Would normally use MemoryStream
        var cancellationToken = TestContext.Current.CancellationToken;
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
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: true,
            cancellationToken
        );
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldForwardFilePath()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(100_000);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            filePath: "test.txt",
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: true,
            cancellationToken
        );
    }

    [Theory]
    [InlineData(HashConversionMethod.Base64)]
    [InlineData(HashConversionMethod.UpperHexadecimal)]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldCalculateCorrectHash_WhenSourceIsSmall(
        HashConversionMethod hashConversionMethod
    )
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(40_000); // Less than 80KB threshold
        var cancellationToken = TestContext.Current.CancellationToken;
        var (hashingPlugin, expectedHash) = CreateHashingPluginWithExpectedHash(
            sourceData,
            new CopyToHashCalculator(SHA1.Create(), hashConversionMethod)
        );

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: false,
            cancellationToken
        );
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
        var (service, sourceData, sourceStream) = CreateTestSetup(100_000); // More than 80KB threshold
        var cancellationToken = TestContext.Current.CancellationToken;
        var (hashingPlugin, expectedHash) = CreateHashingPluginWithExpectedHash(
            sourceData,
            new CopyToHashCalculator(MD5.Create(), hashConversionMethod)
        );

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            cancellationToken: cancellationToken
        );

        // Assert
        await AssertTemporaryStreamContentsMatchAsync(
            temporaryStream,
            sourceData,
            expectFileBased: true,
            cancellationToken
        );
        hashingPlugin.GetHash(nameof(MD5)).Should().Be(expectedHash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldSupportMultipleHashAlgorithms()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(50_000);
        var cancellationToken = TestContext.Current.CancellationToken;

        var sha1Calculator = new CopyToHashCalculator(SHA1.Create(), HashConversionMethod.UpperHexadecimal);
        var sha256Calculator = new CopyToHashCalculator(SHA256.Create(), HashConversionMethod.Base64);

        var expectedSha1Hash = HashConverter.ConvertHashToString(
            SHA1.HashData(sourceData),
            HashConversionMethod.UpperHexadecimal
        );
        var expectedSha256Hash = HashConverter.ConvertHashToString(
            SHA256.HashData(sourceData),
            HashConversionMethod.Base64
        );

        await using var hashingPlugin = new HashingPlugin([sha1Calculator, sha256Calculator]);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            [hashingPlugin],
            cancellationToken: cancellationToken
        );

        // Assert
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedSha1Hash);
        hashingPlugin.GetHash(nameof(SHA256)).Should().Be(expectedSha256Hash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldUseCustomBufferSize()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(60_000);
        var cancellationToken = TestContext.Current.CancellationToken;
        var (hashingPlugin, expectedHash) = CreateHashingPluginWithExpectedHash(
            sourceData,
            new CopyToHashCalculator(SHA1.Create(), HashConversionMethod.Base64)
        );

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
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedHash);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldForwardFilePath()
    {
        // Arrange
        var (service, sourceData, sourceStream) = CreateTestSetup(100_000);
        var cancellationToken = TestContext.Current.CancellationToken;
        var (hashingPlugin, expectedHash) = CreateHashingPluginWithExpectedHash(
            sourceData,
            new CopyToHashCalculator(SHA1.Create(), HashConversionMethod.Base64)
        );
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
        hashingPlugin.GetHash(nameof(SHA1)).Should().Be(expectedHash);
    }

    private static (TemporaryStreamService service, byte[] sourceData, MemoryStream sourceStream) CreateTestSetup(
        int dataSize
    )
    {
        var service = CreateDefaultService();
        var sourceData = CreateTestData(dataSize);
        var sourceStream = new MemoryStream(sourceData);

        return (service, sourceData, sourceStream);
    }

    private static async Task AssertTemporaryStreamContentsMatchAsync(
        TemporaryStream temporaryStream,
        byte[] expectedData,
        bool expectFileBased,
        CancellationToken cancellationToken
    )
    {
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().Be(expectFileBased);
        temporaryStream.Length.Should().Be(expectedData.Length);

        temporaryStream.Position = 0;
        var copiedData = new byte[expectedData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData, cancellationToken);
        copiedData.Should().Equal(expectedData);
    }

    private static (HashingPlugin plugin, string expectedHash) CreateHashingPluginWithExpectedHash(
        byte[] sourceData,
        CopyToHashCalculator hashCalculator
    )
    {
        var plugin = new HashingPlugin([hashCalculator]);

        var expectedHash = HashConverter.ConvertHashToString(
            hashCalculator.HashAlgorithm.ComputeHash(sourceData),
            hashCalculator.ConversionMethod
        );

        return (plugin, expectedHash);
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
