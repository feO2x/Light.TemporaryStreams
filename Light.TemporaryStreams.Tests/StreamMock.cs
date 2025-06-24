using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TemporaryStreams.Tests.CallTracking;

namespace Light.TemporaryStreams.Tests;

public sealed class StreamMock : Stream
{
    public const int ReadReturnValue = 42;
    public const int SeekReturnValue = 123;
    private const string ReadSpanName = "Read(Span<byte> buffer)";
    private const string ReadAsyncMemoryName = "ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)";
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

    public void ReadMustHaveBeenCalledWith(byte[] buffer, int offset, int count)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(Read), buffer, offset, count);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(Read));
    }

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
    )
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(ReadAsync), buffer, offset, count, cancellationToken);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(ReadAsync));
    }

    public void ReadAsyncMustHaveBeenCalledWith(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        _callTrackers.MustHaveBeenCalledWith(ReadAsyncMemoryName, buffer, cancellationToken);
        _callTrackers.MustHaveNoOtherCallsExcept(ReadAsyncMemoryName);
    }

    public void ReadByteMustHaveBeenCalled()
    {
        _callTrackers.MustHaveBeenCalled(nameof(ReadByte));
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(ReadByte));
    }

    public void SeekMustHaveBeenCalledWith(long offset, SeekOrigin origin)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(Seek), offset, origin);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(Seek));
    }

    public void SetLengthMustHaveBeenCalledWith(long value)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(SetLength), value);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(SetLength));
    }

    public void WriteMustHaveBeenCalledWith(byte[] buffer, int offset, int bufferLength)
    {
        _callTrackers.MustHaveBeenCalledWith(nameof(Write), buffer, offset, bufferLength);
        _callTrackers.MustHaveNoOtherCallsExcept(nameof(Write));
    }
}
