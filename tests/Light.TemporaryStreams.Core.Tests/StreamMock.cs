using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TemporaryStreams.CallTracking;

namespace Light.TemporaryStreams;

public sealed class StreamMock : Stream
{
    public const int ReadReturnValue = 42;
    public const int SeekReturnValue = 123;
    private const string ReadSpanName = "Read(Span<byte> buffer)";
    private const string ReadAsyncMemoryName = "ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)";
    private const string WriteSpanName = "Write(ReadOnlySpan<byte> buffer)";

    private const string WriteAsyncMemoryName =
        "WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)";

    private readonly CallTrackers _callTrackers = new ();

    public AsyncResultNullObject AsyncResult { get; } = new ();

    public bool CanReadReturnValue { get; set; } = true;
    public override bool CanRead => CanReadReturnValue;

    public bool CanSeekReturnValue { get; init; } = true;
    public override bool CanSeek => CanSeekReturnValue;

    public bool CanTimeoutReturnValue { get; set; }
    public override bool CanTimeout => CanTimeoutReturnValue;

    public bool CanWriteReturnValue { get; set; }
    public override bool CanWrite => CanWriteReturnValue;

    public long LengthReturnValue { get; set; } = 4321;
    public override long Length => LengthReturnValue;

    public override long Position { get; set; }
    public override int ReadTimeout { get; set; }
    public override int WriteTimeout { get; set; }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        _callTrackers.TrackCall(buffer, offset, count, callback, state);
        return AsyncResult;
    }

    public override IAsyncResult BeginWrite(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    )
    {
        _callTrackers.TrackCall(buffer, offset, count, callback, state);
        return AsyncResult;
    }

    public override void CopyTo(Stream destination, int bufferSize) => _callTrackers.TrackCall(destination);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        _callTrackers.TrackCall(destination);
        return Task.CompletedTask;
    }


    protected override void Dispose(bool disposing) => _callTrackers.TrackCall();

    public override ValueTask DisposeAsync()
    {
        _callTrackers.TrackCall();
        return ValueTask.CompletedTask;
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        _callTrackers.TrackCall(asyncResult);
        return ReadReturnValue;
    }

    public override void EndWrite(IAsyncResult asyncResult) => _callTrackers.TrackCall(asyncResult);

    public override void Flush() => _callTrackers.TrackCall();

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        _callTrackers.TrackCall();
        return Task.CompletedTask;
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        _callTrackers.TrackCall(buffer, offset, count);
        return ReadReturnValue;
    }

    public override int Read(Span<byte> buffer)
    {
        // ReSharper disable once ExplicitCallerInfoArgument -- Read is already used by another method
        _callTrackers.TrackCall(buffer.ToImmutableArray(), ReadSpanName);
        return ReadReturnValue;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _callTrackers.TrackCall(buffer, offset, count, cancellationToken);
        return Task.FromResult(ReadReturnValue);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ExplicitCallerInfoArgument -- ReadAsync is already used by another method
        _callTrackers.TrackCall(buffer, cancellationToken, ReadAsyncMemoryName);
        return ValueTask.FromResult(ReadReturnValue);
    }

    public override int ReadByte()
    {
        _callTrackers.TrackCall();
        return ReadReturnValue;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _callTrackers.TrackCall(offset, origin);
        return SeekReturnValue;
    }

    public override void SetLength(long value) => _callTrackers.TrackCall(value);

    public override void Write(byte[] buffer, int offset, int count) => _callTrackers.TrackCall(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) =>
        // ReSharper disable once ExplicitCallerInfoArgument -- Write is already used by another method
        _callTrackers.TrackCall(buffer.ToImmutableArray(), WriteSpanName);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _callTrackers.TrackCall(buffer, offset, count, cancellationToken);
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ExplicitCallerInfoArgument -- WriteAsync is already used by another method
        _callTrackers.TrackCall(buffer, cancellationToken, WriteAsyncMemoryName);
        return ValueTask.CompletedTask;
    }

    public override void WriteByte(byte value) => _callTrackers.TrackCall(value);

    public void BeginReadMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(BeginRead), buffer, offset, count, callback, state);

    public void BeginWriteMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(BeginWrite), buffer, offset, count, callback, state);

    public void CopyToMustHaveBeenCalledWith(Stream targetStream) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(CopyTo), targetStream);

    public void CopyToAsyncMustHaveBeenCalledWith(Stream targetStream) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(CopyToAsync), targetStream);

    public void DisposeMustHaveBeenCalled() => _callTrackers.MustHaveBeenOnlyMethodCalled(nameof(Dispose));

    public void DisposeAsyncMustHaveBeenCalled() => _callTrackers.MustHaveBeenOnlyMethodCalled(nameof(DisposeAsync));

    public void FlushMustHaveBeenCalled() => _callTrackers.MustHaveBeenOnlyMethodCalled(nameof(Flush));

    public void FlushAsyncMustHaveBeenCalled() => _callTrackers.MustHaveBeenOnlyMethodCalled(nameof(FlushAsync));

    public void EndReadMustHaveBeenCalledWith(IAsyncResult asyncResult) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(EndRead), asyncResult);

    public void EndWriteMustHaveBeenCalledWith(IAsyncResult asyncResult) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(EndWrite), asyncResult);

    public void ReadMustHaveBeenCalledWith(byte[] buffer, int offset, int count) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(Read), buffer, offset, count);

    public void ReadMustHaveCalledWith(byte[] buffer)
    {
        var callTracker = _callTrackers.GetRequiredCallTracker<CallTracker<ImmutableArray<byte>>>(ReadSpanName);
        callTracker.CapturedParameters.Should().ContainSingle().Which.Should().Equal(buffer);
        _callTrackers.MustHaveNoOtherCallsExcept(ReadSpanName);
    }

    public void ReadAsyncMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(ReadAsync), buffer, offset, count, cancellationToken);

    public void ReadAsyncMustHaveBeenCalledWith(Memory<byte> buffer, CancellationToken cancellationToken) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(ReadAsyncMemoryName, buffer, cancellationToken);

    public void ReadByteMustHaveBeenCalled() => _callTrackers.MustHaveBeenOnlyMethodCalled(nameof(ReadByte));

    public void SeekMustHaveBeenCalledWith(long offset, SeekOrigin origin) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(Seek), offset, origin);

    public void SetLengthMustHaveBeenCalledWith(long value) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(SetLength), value);

    public void WriteMustHaveBeenCalledWith(byte[] buffer, int offset, int bufferLength) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(Write), buffer, offset, bufferLength);

    public void WriteMustHaveBeenCalledWith(byte[] buffer)
    {
        var callTracker = _callTrackers.GetRequiredCallTracker<CallTracker<ImmutableArray<byte>>>(WriteSpanName);
        callTracker.CapturedParameters.Should().ContainSingle().Which.Should().Equal(buffer);
        _callTrackers.MustHaveNoOtherCallsExcept(WriteSpanName);
    }

    public void WriteAsyncMustHaveBeenCalledWith(byte[] buffer, int offset, int bufferLength, CancellationToken none) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(WriteAsync), buffer, offset, bufferLength, none);

    public void WriteAsyncMustHaveBeenCalledWith(ReadOnlyMemory<byte> buffer, CancellationToken none) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(WriteAsyncMemoryName, buffer, none);

    public void WriteByteMustHaveBeenCalledWith(byte value) =>
        _callTrackers.MustHaveBeenOnlyMethodCalledWith(nameof(WriteByte), value);
}
