using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace Light.TemporaryStreams.Tests;

public sealed class StreamMock : Stream
{
    private readonly List<(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)>
        _capturedBeginReadParameters = [];

    private readonly List<(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)>
        _capturedBeginWriteParameters = [];

    public AsyncResultNullObject AsyncResult { get; } = new ();
    public bool CanSeekReturnValue { get; set; } = true;
    public override bool CanRead { get; }
    public override bool CanSeek => CanSeekReturnValue;
    public override bool CanTimeout { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
    public override int ReadTimeout { get; set; }
    public override int WriteTimeout { get; set; }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        _capturedBeginReadParameters.Add((buffer, offset, count, callback, state));
        return AsyncResult;
    }

    public void BeginReadMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        _capturedBeginReadParameters
           .Should().ContainSingle()
           .Which.Should().Be((buffer, offset, count, callback, state));

    public override IAsyncResult BeginWrite(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    )
    {
        _capturedBeginWriteParameters.Add((buffer, offset, count, callback, state));
        return AsyncResult;
    }

    public void BeginWriteMustHaveBeenCalledWith(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        _capturedBeginWriteParameters
           .Should().ContainSingle()
           .Which.Should().Be((buffer, offset, count, callback, state));

    public override void Close() => base.Close();

    public override void CopyTo(Stream destination, int bufferSize) => base.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        base.CopyToAsync(destination, bufferSize, cancellationToken);


    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    public override ValueTask DisposeAsync() => base.DisposeAsync();

    public override int EndRead(IAsyncResult asyncResult) => base.EndRead(asyncResult);

    public override void EndWrite(IAsyncResult asyncResult) => base.EndWrite(asyncResult);

    public override void Flush() => throw new NotImplementedException();

    public override Task FlushAsync(CancellationToken cancellationToken) => base.FlushAsync(cancellationToken);


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
}

public sealed class AsyncResultNullObject : IAsyncResult
{
    public object? AsyncState => null;
    public WaitHandle AsyncWaitHandle => null!;
    public bool CompletedSynchronously => true;
    public bool IsCompleted => true;
}
