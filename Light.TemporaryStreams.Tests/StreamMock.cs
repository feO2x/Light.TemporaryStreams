using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Light.TemporaryStreams.Tests.CallTracking;

namespace Light.TemporaryStreams.Tests;

public sealed class StreamMock : Stream
{
    public const int EndReadReturnValue = 42;
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

    public void BeginReadMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    )
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(BeginRead), buffer, offset, count, callback, state);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(BeginRead));
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

    public void BeginWriteMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    )
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(BeginWrite), buffer, offset, count, callback, state);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(BeginWrite));
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
        return EndReadReturnValue;
    }

    public override void EndWrite(IAsyncResult asyncResult) => _callTrackers.TrackCall(asyncResult);

    public override void Flush() => _callTrackers.TrackCall();

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        _callTrackers.TrackCall();
        return Task.CompletedTask;
    }


    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override int Read(Span<byte> buffer) => base.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        base.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new ()) =>
        base.ReadAsync(buffer, cancellationToken);

    public override int ReadByte() => base.ReadByte();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override void Write(ReadOnlySpan<byte> buffer) => base.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        base.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new ()) =>
        base.WriteAsync(buffer, cancellationToken);

    public override void WriteByte(byte value) => base.WriteByte(value);

    public void CopyToMustHaveBeenCalledWith(Stream targetStream)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(CopyTo), targetStream);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(CopyTo));
    }

    public void CopyToAsyncMustHaveBeenCalledWith(Stream targetStream)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(CopyToAsync), targetStream);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(CopyToAsync));
    }

    public void DisposeMustHaveBeenCalled()
    {
        _callTrackers.MustHaveBeenCalled(nameof(Dispose));
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(Dispose));
    }

    public void DisposeAsyncMustHaveBeenCalled()
    {
        _callTrackers.MustHaveBeenCalled(nameof(DisposeAsync));
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(DisposeAsync));
    }

    public void FlushMustHaveBeenCalled()
    {
        _callTrackers.MustHaveBeenCalled(nameof(Flush));
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(Flush));
    }

    public void FlushAsyncMustHaveBeenCalled()
    {
        _callTrackers.MustHaveBeenCalled(nameof(FlushAsync));
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(FlushAsync));
    }

    public void EndReadMustHaveBeenCalledWith(IAsyncResult asyncResult)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(EndRead), asyncResult);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(EndRead));
    }

    public void EndWriteMustHaveBeenCalledWith(IAsyncResult asyncResult)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(EndWrite), asyncResult);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(EndWrite));
    }
}
