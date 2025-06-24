using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TemporaryStreams.Tests.CallTracking;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public static class TemporaryStreamTests
{
    public static readonly TheoryData<int> NumberOfDisposeCallsData = [1, 3, 7];

    [Fact]
    public static void Constructor_ShouldThrowArgumentNullException_IfUnderlyingStreamIsNull()
    {
        var act = () => new TemporaryStream(null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("underlyingStream");
    }

    [Fact]
    public static void Constructor_ShouldThrowArgumentException_IfUnderlyingStreamIsNotSeekable()
    {
        var act = () => new TemporaryStream(new StreamMock { CanSeekReturnValue = false });

        act.Should()
           .Throw<ArgumentException>()
           .Where(
                exception => exception.ParamName == "underlyingStream" &&
                             exception.Message.StartsWith("The underlying stream must be seekable.")
            );
    }

    [Theory]
    [MemberData(nameof(NumberOfDisposeCallsData))]
    public static void Dispose_ShouldDeleteFile_WhenUnderlyingStreamIsFileStream(int numberOfCalls)
    {
        var (temporaryStream, fileStream) = SetUpTemporaryFileBackedStream();

        for (var i = 0; i < numberOfCalls; i++)
        {
            temporaryStream.Dispose();
        }

        fileStream.SafeFileHandle.IsClosed.Should().BeTrue();
        File.Exists(fileStream.Name).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(NumberOfDisposeCallsData))]
    public static void Close_ShouldDeleteFile_WhenUnderlyingStreamIsFileStream(int numberOfCalls)
    {
        var (temporaryStream, fileStream) = SetUpTemporaryFileBackedStream();

        for (var i = 0; i < numberOfCalls; i++)
        {
            temporaryStream.Close();
        }

        fileStream.SafeFileHandle.IsClosed.Should().BeTrue();
        File.Exists(fileStream.Name).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(NumberOfDisposeCallsData))]
    public static async Task DisposeAsync_ShouldDeleteFile_WhenUnderlyingStreamIsFileStream(int numberOfCalls)
    {
        var (temporaryStream, fileStream) = SetUpTemporaryFileBackedStream();

        for (var i = 0; i < numberOfCalls; i++)
        {
            await temporaryStream.DisposeAsync();
        }

        fileStream.SafeFileHandle.IsClosed.Should().BeTrue();
        File.Exists(fileStream.Name).Should().BeFalse();
    }

    [Fact]
    public static void IsFileBased_ShouldReturnTrue_WhenUnderlyingStreamIsFileStream()
    {
        var (temporaryStream, _) = SetUpTemporaryFileBackedStream();
        using (temporaryStream)
        {
            temporaryStream.IsFileBased.Should().BeTrue();
        }
    }

    [Fact]
    public static void IsFileBased_ShouldReturnFalse_WhenUnderlyingStreamIsMemoryBased()
    {
        using var temporaryStream = new TemporaryStream(new MemoryStream());

        temporaryStream.IsFileBased.Should().BeFalse();
    }

    [Fact]
    public static void OnFileDeletionError_ShouldBeInvoked_WhenFileDeletionFails()
    {
        var callTracker = new CallTracker<TemporaryStream, Exception>("OnFileDeletionError");

        // Create a genuine temporary file
        var realPath = Path.GetTempFileName();

        // Create our special FileStream wrapper that will make file deletion fail
        var fileStream = new TestFileStream(realPath);

        // Create the TemporaryStream with our special FileStream
        var temporaryStream = new TemporaryStream(
            fileStream,
            onFileDeletionError: (stream, exception) => callTracker.TrackCall(stream, exception)
        );

        try
        {
            // This should trigger the error callback
            temporaryStream.Dispose();

            // Verify the error handler was called with the correct exception
            var (capturedTemporaryStream, capturedException) =
                callTracker.CapturedParameters.Should().ContainSingle().Which;
            capturedTemporaryStream.Should().BeSameAs(temporaryStream);
            capturedException
               .Should().BeOfType<DirectoryNotFoundException>()
               .Which.Message.Should().StartWith("Could not find a part of the path");
        }
        finally
        {
            // Clean up the real file
            File.Delete(realPath);
        }
    }

    [Fact]
    public static void FileDeletionError_ShouldBeWrittenToTrace_WhenNoErrorHandlerIsProvided()
    {
        // Create a trace listener to capture the trace output
        var traceListener = new TestTraceListener();
        Trace.Listeners.Add(traceListener);

        // Create a genuine temporary file
        var realPath = Path.GetTempFileName();

        try
        {
            // Create our special FileStream wrapper that will make file deletion fail
            var fileStream = new TestFileStream(realPath);

            // Create the TemporaryStream with no onFileDeletionError delegate
            var temporaryStream = new TemporaryStream(fileStream);

            // This should trigger the trace error
            temporaryStream.Dispose();

            // Verify that trace contains the error message
            traceListener.ErrorOutput.Should().NotBeNullOrEmpty();
            traceListener.ErrorOutput.Should().Contain("Failed to delete underlying file of temporary stream");
            traceListener.ErrorOutput.Should().Contain("DirectoryNotFoundException");
            traceListener.ErrorOutput.Should().Contain("Could not find a part of the path");
        }
        finally
        {
            // Remove our test trace listener
            Trace.Listeners.Remove(traceListener);

            // Clean up the real file
            File.Delete(realPath);
        }
    }

    private static (TemporaryStream temporaryStream, FileStream fileStream) SetUpTemporaryFileBackedStream(
        Action<TemporaryStream, Exception>? onFileDeletionError = null
    )
    {
        var fileStream = GetFileStream();
        var temporaryStream = new TemporaryStream(fileStream, onFileDeletionError: onFileDeletionError);
        return (temporaryStream, fileStream);
    }

    private static FileStream GetFileStream()
    {
        var temporaryFilePath = Path.GetTempFileName();
        return File.OpenRead(temporaryFilePath);
    }

    private sealed class TestFileStream : FileStream
    {
        public TestFileStream(string path)
            : base(path, FileMode.Open, FileAccess.ReadWrite) { }

        // This property is what TemporaryStream uses for File.Delete
        public override string Name => "/invalid_path_that_cannot_exist/test.tmp";
    }

    private sealed class TestTraceListener : TraceListener
    {
        private readonly StringBuilder _errorOutput = new ();

        public string ErrorOutput => _errorOutput.ToString();

        public override void Write(string? message) { }

        public override void WriteLine(string? message) { }

        public override void TraceEvent(
            TraceEventCache? eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string? message
        )
        {
            if (eventType == TraceEventType.Error)
            {
                _errorOutput.AppendLine(message);
            }

            base.TraceEvent(eventCache, source, eventType, id, message);
        }
    }
}
