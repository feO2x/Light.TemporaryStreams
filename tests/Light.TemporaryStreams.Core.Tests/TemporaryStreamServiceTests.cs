using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams;

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

        using var temporaryStream = service.CreateTemporaryStream(500);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.UnderlyingStream.Should().BeOfType<MemoryStream>();
        temporaryStream.DisposeBehavior.Should().Be(options.DisposeBehavior);
    }

    [Fact]
    public static async Task CreateTemporaryStream_ShouldCreateFileStream_WhenExpectedLengthIsAboveThreshold()
    {
        var options = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(options);

        await using var temporaryStream = service.CreateTemporaryStream(1500);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.UnderlyingStream.Should().BeOfType<FileStream>();
        temporaryStream.DisposeBehavior.Should().Be(options.DisposeBehavior);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldCreateFileStream_WhenExpectedLengthEqualsThreshold()
    {
        var options = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(options);

        using var temporaryStream = service.CreateTemporaryStream(1000);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.UnderlyingStream.Should().BeOfType<FileStream>();
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseProvidedFilePath_WhenFilePathIsSpecified()
    {
        var service = CreateDefaultService();
        var customFilePath = Path.GetTempFileName();

        try
        {
            using var temporaryStream = service.CreateTemporaryStream(100000, customFilePath);

            temporaryStream.Should().NotBeNull();
            temporaryStream.IsFileBased.Should().BeTrue();
            temporaryStream.GetUnderlyingFilePath().Should().Be(customFilePath);
        }
        finally
        {
            if (File.Exists(customFilePath))
            {
                File.Delete(customFilePath);
            }
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

        using var temporaryStream = service.CreateTemporaryStream(750, options: overrideOptions);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue(); // 750 > 500 (override threshold)
        temporaryStream.DisposeBehavior.Should().Be(TemporaryStreamDisposeBehavior.CloseUnderlyingStreamOnly);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseDefaultOptions_WhenNoOptionsAreProvided()
    {
        var defaultOptions = new TemporaryStreamServiceOptions { FileThresholdInBytes = 1000 };
        var service = CreateService(defaultOptions);

        using var temporaryStream = service.CreateTemporaryStream(500);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeFalse(); // 500 < 1000 (default threshold)
        temporaryStream.DisposeBehavior.Should().Be(defaultOptions.DisposeBehavior);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldSetCorrectMemoryStreamCapacity_WhenCreatingMemoryStream()
    {
        var service = CreateDefaultService();
        const int expectedLength = 1024;

        using var temporaryStream = service.CreateTemporaryStream(expectedLength);

        temporaryStream.UnderlyingStream.Should().BeOfType<MemoryStream>();
        var memoryStream = (MemoryStream) temporaryStream.UnderlyingStream;
        memoryStream.Capacity.Should().Be(expectedLength);
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldUseErrorHandlerFromProvider_WhenCreatingFileStream()
    {
        Action<TemporaryStream, Exception> errorHandler = (_, _) => { };
        var errorHandlerProvider = new TemporaryStreamErrorHandlerProvider(errorHandler);
        var service = CreateService(errorHandlerProvider: errorHandlerProvider);

        using var temporaryStream = service.CreateTemporaryStream(100000);

        temporaryStream.Should().NotBeNull();
        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.OnFileDeletionError.Should().BeSameAs(errorHandler);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes - 1)]
    public static void CreateTemporaryStream_ShouldCreateMemoryStream_WhenBelowDefaultThreshold(long expectedLength)
    {
        var service = CreateDefaultService();

        using var temporaryStream = service.CreateTemporaryStream(expectedLength);

        temporaryStream.IsFileBased.Should().BeFalse();
        temporaryStream.UnderlyingStream.Should().BeOfType<MemoryStream>();
    }

    [Theory]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes)]
    [InlineData(TemporaryStreamServiceOptions.DefaultFileThresholdInBytes + 1)]
    [InlineData(1000000)]
    public static void CreateTemporaryStream_ShouldCreateFileStream_WhenAtOrAboveDefaultThreshold(long expectedLength)
    {
        var service = CreateDefaultService();

        using var temporaryStream = service.CreateTemporaryStream(expectedLength);

        temporaryStream.IsFileBased.Should().BeTrue();
        temporaryStream.UnderlyingStream.Should().BeOfType<FileStream>();
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

        using var temporaryStream = service.CreateTemporaryStream(2000);

        temporaryStream.IsFileBased.Should().BeTrue();
        var fileStream = (FileStream) temporaryStream.UnderlyingStream;
        // Verify the file stream was created with the custom options
        fileStream.CanRead.Should().BeTrue();
        fileStream.CanWrite.Should().BeTrue();
    }

    [Fact]
    public static void CreateTemporaryStream_ShouldOpenExistingFile_WhenFilePathAndOptionsAreProvided()
    {
        var customFilePath = Path.GetTempFileName();
        try
        {
            var customFileStreamOptions = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.ReadWrite,
                Share = FileShare.Read,
                BufferSize = 4096,
                Options = FileOptions.None
            };
            var options = new TemporaryStreamServiceOptions
            {
                FileStreamOptions = customFileStreamOptions,
                FileThresholdInBytes = 0
            };
            var service = CreateService(options);

            using var temporaryStream = service.CreateTemporaryStream(0, customFilePath);

            temporaryStream.IsFileBased.Should().BeTrue();
            ((FileStream) temporaryStream.UnderlyingStream).Name.Should().Be(customFilePath);
        }
        finally
        {
            if (File.Exists(customFilePath))
            {
                File.Delete(customFilePath);
            }
        }
    }

    private static TemporaryStreamService CreateDefaultService()
    {
        return CreateService();
    }

    private static TemporaryStreamService CreateService(
        TemporaryStreamServiceOptions? options = null,
        TemporaryStreamErrorHandlerProvider? errorHandlerProvider = null
    )
    {
        options ??= new TemporaryStreamServiceOptions();
        errorHandlerProvider ??= new TemporaryStreamErrorHandlerProvider(null);
        return new TemporaryStreamService(options, errorHandlerProvider);
    }
}
