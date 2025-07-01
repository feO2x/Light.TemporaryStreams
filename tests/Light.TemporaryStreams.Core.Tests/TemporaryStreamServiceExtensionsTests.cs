using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TemporaryStreams.Hashing;
using Xunit;

namespace Light.TemporaryStreams;

public static class TemporaryStreamServiceExtensionsTests
{
    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldCreateMemoryStream_WhenSourceIsSmall()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(40_000); // Less than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(sourceStream);

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify content was copied correctly
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData);
        copiedData.Should().BeEquivalentTo(sourceData);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldCreateFileStream_WhenSourceIsLarge()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000); // More than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(sourceStream);

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify content was copied correctly
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData);
        copiedData.Should().BeEquivalentTo(sourceData);

        // Clean up file
        var filePath = temporaryStream.GetUnderlyingFilePath();
        await temporaryStream.DisposeAsync();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCopyBufferSize_WhenSpecified()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(50_000);
        using var sourceStream = new MemoryStream(sourceData);

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream, 
            copyBufferSize: 8192);

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify content was copied correctly
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData);
        copiedData.Should().BeEquivalentTo(sourceData);
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_ShouldUseCustomOptions_WhenProvided()
    {
        // Arrange
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
            options: customOptions);

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue(); // Should use file due to custom threshold
        temporaryStream.Length.Should().Be(sourceData.Length);

        // Clean up file
        var filePath = temporaryStream.GetUnderlyingFilePath();
        await temporaryStream.DisposeAsync();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldCalculateCorrectHash_WhenSourceIsSmall()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(40_000); // Less than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);
        
        using var sha1 = SHA1.Create();
        var hashCalculator = new CopyToHashCalculator(sha1, HashConversionMethod.UpperHexadecimal, "SHA1");
        var hashingPlugin = new HashingPlugin(ImmutableArray.Create(hashCalculator));
        
        // Calculate expected hash for comparison
        var expectedHash = Convert.ToHexString(SHA1.HashData(sourceData));

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin));

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify content was copied correctly
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData);
        copiedData.Should().BeEquivalentTo(sourceData);
        
        // Verify hash was calculated correctly
        hashCalculator.Hash.Should().Be(expectedHash);
        
        await hashingPlugin.DisposeAsync();
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldCalculateCorrectHash_WhenSourceIsLarge()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(100_000); // More than 80KB threshold
        using var sourceStream = new MemoryStream(sourceData);
        
        using var sha1 = SHA1.Create();
        var hashCalculator = new CopyToHashCalculator(sha1, HashConversionMethod.UpperHexadecimal, "SHA1");
        var hashingPlugin = new HashingPlugin(ImmutableArray.Create(hashCalculator));
        
        // Calculate expected hash for comparison
        var expectedHash = Convert.ToHexString(SHA1.HashData(sourceData));

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin));

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify content was copied correctly
        temporaryStream.Position = 0;
        var copiedData = new byte[sourceData.Length];
        await temporaryStream.ReadExactlyAsync(copiedData);
        copiedData.Should().BeEquivalentTo(sourceData);
        
        // Verify hash was calculated correctly
        hashCalculator.Hash.Should().Be(expectedHash);
        
        await hashingPlugin.DisposeAsync();
        
        // Clean up file
        var filePath = temporaryStream.GetUnderlyingFilePath();
        await temporaryStream.DisposeAsync();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldSupportMultipleHashAlgorithms()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(50_000);
        using var sourceStream = new MemoryStream(sourceData);
        
        using var sha1 = SHA1.Create();
        using var sha256 = SHA256.Create();
        var sha1Calculator = new CopyToHashCalculator(sha1, HashConversionMethod.UpperHexadecimal, "SHA1");
        var sha256Calculator = new CopyToHashCalculator(sha256, HashConversionMethod.UpperHexadecimal, "SHA256");
        var hashingPlugin = new HashingPlugin(ImmutableArray.Create(sha1Calculator, sha256Calculator));
        
        // Calculate expected hashes for comparison
        var expectedSha1Hash = Convert.ToHexString(SHA1.HashData(sourceData));
        var expectedSha256Hash = Convert.ToHexString(SHA256.HashData(sourceData));

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin));

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        
        // Verify hashes were calculated correctly
        sha1Calculator.Hash.Should().Be(expectedSha1Hash);
        sha256Calculator.Hash.Should().Be(expectedSha256Hash);
        
        await hashingPlugin.DisposeAsync();
        
        // Clean up file if needed
        if (temporaryStream.IsFileBased)
        {
            var filePath = temporaryStream.GetUnderlyingFilePath();
            await temporaryStream.DisposeAsync();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public static async Task CopyToTemporaryStreamAsync_WithHashingPlugin_ShouldUseCustomBufferSize()
    {
        // Arrange
        var service = CreateDefaultService();
        var sourceData = CreateTestData(60_000);
        using var sourceStream = new MemoryStream(sourceData);
        
        using var sha1 = SHA1.Create();
        var hashCalculator = new CopyToHashCalculator(sha1, HashConversionMethod.UpperHexadecimal, "SHA1");
        var hashingPlugin = new HashingPlugin(ImmutableArray.Create(hashCalculator));
        
        var expectedHash = Convert.ToHexString(SHA1.HashData(sourceData));

        // Act
        await using var temporaryStream = await service.CopyToTemporaryStreamAsync(
            sourceStream,
            ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin),
            copyBufferSize: 4096);

        // Assert
        temporaryStream.Should().NotBeNull();
        temporaryStream.Length.Should().Be(sourceData.Length);
        hashCalculator.Hash.Should().Be(expectedHash);
        
        await hashingPlugin.DisposeAsync();
    }

    private static TemporaryStreamService CreateDefaultService() =>
        new(
            new TemporaryStreamServiceOptions(),
            new TemporaryStreamErrorHandlerProvider(null)
        );

    private static byte[] CreateTestData(int size)
    {
        var data = new byte[size];
        var random = new Random(42); // Fixed seed for reproducible tests
        random.NextBytes(data);
        return data;
    }
}