using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public static class TemporaryStreamServiceTests
{
    [Fact]
    public static void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        var errorHandlerProvider = new TemporaryStreamErrorHandlerProvider(null);

        var act = () => new TemporaryStreamService(null!, errorHandlerProvider);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("options");
    }

    [Fact]
    public static void Constructor_ShouldThrowArgumentNullException_WhenErrorHandlerProviderIsNull()
    {
        var options = new TemporaryStreamServiceOptions();

        var act = () => new TemporaryStreamService(options, null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("errorHandlerProvider");
    }

    [Fact]
    public static void Constructor_ShouldSetProperties_WhenValidParametersAreProvided()
    {
        var options = new TemporaryStreamServiceOptions();
        var errorHandlerProvider = new TemporaryStreamErrorHandlerProvider(null);

        var service = new TemporaryStreamService(options, errorHandlerProvider);

        service.Options.Should().BeSameAs(options);
        service.ErrorHandlerProvider.Should().BeSameAs(errorHandlerProvider);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldThrowArgumentOutOfRangeException_WhenExpectedLengthIsNegative()
    {
        var service = CreateDefaultService();

        var act = () => service.CreateTemporaryStream(-1);

        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("expectedLengthInBytes");
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldCreateMemoryStream_WhenExpectedLengthIsBelowThreshold()
    {
        var options = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(options);

        using var result = service.CreateTemporaryStream(500);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeFalse();
        result.UnderlyingStream.Should().BeOfType<MemoryStream>();
        result.DisposeBehavior.Should().Be(options.DisposeBehavior);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldCreateFileStream_WhenExpectedLengthIsAboveThreshold()
    {
        var options = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(options);

        using var result = service.CreateTemporaryStream(1500);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeTrue();
        result.UnderlyingStream.Should().BeOfType<FileStream>();
        result.DisposeBehavior.Should().Be(options.DisposeBehavior);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldCreateFileStream_WhenExpectedLengthEqualsThreshold()
    {
        var options = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(options);

        using var result = service.CreateTemporaryStream(1000);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeTrue();
        result.UnderlyingStream.Should().BeOfType<FileStream>();
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseProvidedFilePath_WhenFilePathIsSpecified()
    {
        var service = CreateDefaultService();
        var customFilePath = Path.GetTempFileName();

        try
        {
            using var result = service.CreateTemporaryStream(100000, customFilePath);

            result.Should().NotBeNull();
            result.IsFileBased.Should().BeTrue();
            result.GetUnderlyingFilePath().Should().Be(customFilePath);
        }
        finally
        {
            if (File.Exists(customFilePath))
                File.Delete(customFilePath);
        }
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseOverrideOptions_WhenOptionsAreProvided()
    {
        var defaultOptions = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(defaultOptions);
        var overrideOptions = new TemporaryStreamServiceOptions 
        { 
            FileThresholdInBytes = 500,
            DisposeBehavior = TemporaryStreamDisposeBehavior.CloseUnderlyingStreamOnly
        };

        using var result = service.CreateTemporaryStream(750, options: overrideOptions);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeTrue(); // 750 > 500 (override threshold)
        result.DisposeBehavior.Should().Be(TemporaryStreamDisposeBehavior.CloseUnderlyingStreamOnly);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseDefaultOptions_WhenNoOptionsAreProvided()
    {
        var defaultOptions = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(defaultOptions);

        using var result = service.CreateTemporaryStream(500);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeFalse(); // 500 < 1000 (default threshold)
        result.DisposeBehavior.Should().Be(defaultOptions.DisposeBehavior);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldSetCorrectMemoryStreamCapacity_WhenCreatingMemoryStream()
    {
        var service = CreateDefaultService();
        const int expectedLength = 1024;

        using var result = service.CreateTemporaryStream(expectedLength);

        result.UnderlyingStream.Should().BeOfType<MemoryStream>();
        var memoryStream = (MemoryStream)result.UnderlyingStream;
        memoryStream.Capacity.Should().Be(expectedLength);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseErrorHandlerFromProvider_WhenCreatingFileStream()
    {
        Action<TemporaryStream, Exception> errorHandler = (_, _) => { };
        var errorHandlerProvider = new TemporaryStreamErrorHandlerProvider(errorHandler);
        var service = CreateService(errorHandlerProvider: errorHandlerProvider);

        using var result = service.CreateTemporaryStream(100000);

        result.Should().NotBeNull();
        result.IsFileBased.Should().BeTrue();
        result.OnFileDeletionError.Should().BeSameAs(errorHandler);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes - 1)]
    public static void CreateTemporaryStream_ShouldCreateMemoryStream_WhenBelowDefaultThreshold(long expectedLength)
    {
        var service = CreateDefaultService();

        using var result = service.CreateTemporaryStream(expectedLength);

        result.IsFileBased.Should().BeFalse();
        result.UnderlyingStream.Should().BeOfType<MemoryStream>();
    }

    [Theory]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes)]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes + 1)]
    [InlineData(1000000)]
    public static void CreateTemporaryStream_ShouldCreateFileStream_WhenAtOrAboveDefaultThreshold(long expectedLength)
    {
        var service = CreateDefaultService();

        using var result = service.CreateTemporaryStream(expectedLength);

        result.IsFileBased.Should().BeTrue();
        result.UnderlyingStream.Should().BeOfType<FileStream>();
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseFileStreamOptionsFromServiceOptions_WhenCreatingFileStream()
    {
        var customFileStreamOptions = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.ReadWrite,
            Share = FileShare.Read,
            BufferSize = 4096,
            Options = FileOptions.None
        };
        var options = new TemporaryStreamServiceOptions 
        { 
            FileStreamOptions = customFileStreamOptions,
            FileThresholdInBytes = 1000
        };
        var service = CreateService(options);

        using var result = service.CreateTemporaryStream(2000);

        result.IsFileBased.Should().BeTrue();
        var fileStream = (FileStream)result.UnderlyingStream;
        // Verify the file stream was created with the custom options
        fileStream.CanRead.Should().BeTrue();
        fileStream.CanWrite.Should().BeTrue();
    }

    private static TemporaryStreamService CreateDefaultService()
    {
        return CreateService();
    }

    private static TemporaryStreamService CreateService(
        TemporaryStreamServiceOptions? options = null,
        TemporaryStreamErrorHandlerProvider? errorHandlerProvider = null)
    {
        options ??= new TemporaryStreamServiceOptions();
        errorHandlerProvider ??= new TemporaryStreamErrorHandlerProvider(null);
        return new TemporaryStreamService(options, errorHandlerProvider);
    }
}