using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public sealed class TemporaryStreamForwardingTests
{
    private static readonly AsyncCallback AsyncCallback = _ => { };
    private readonly StreamMock _streamMock = new ();
    private readonly TemporaryStream _temporaryStream;

    public TemporaryStreamForwardingTests() => _temporaryStream = new TemporaryStream(_streamMock);

    [Fact]
    public void BeginRead_MustForwardToUnderlyingStream()
    {
        byte[] array = [];
        const int offset = 0;
        var state = new object();

        var result = _temporaryStream.BeginRead(array, offset, array.Length, AsyncCallback, state);

        result.Should().BeSameAs(_streamMock.AsyncResult);
        _streamMock.BeginReadMustHaveBeenCalledWith(array, offset, array.Length, AsyncCallback, state);
    }

    [Fact]
    public void BeginWrite_MustForwardToUnderlyingStream()
    {
        byte[] array = [];
        const int offset = 0;
        var state = new object();

        var result = _temporaryStream.BeginWrite(array, offset, array.Length, AsyncCallback, state);

        result.Should().BeSameAs(_streamMock.AsyncResult);
        _streamMock.BeginWriteMustHaveBeenCalledWith(array, offset, array.Length, AsyncCallback, state);
    }

    [Fact]
    public void Close_MustNotForwardToUnderlyingStream_ButSimplyCallDispose()
    {
        _temporaryStream.Close();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.DisposeMustHaveBeenCalled();
    }

    [Fact]
    public void CopyTo_MustForwardToUnderlyingStream()
    {
        using var targetStream = new MemoryStream();

        _temporaryStream.CopyTo(targetStream);

        _streamMock.CopyToMustHaveBeenCalledWith(targetStream);
    }

    [Fact]
    public async Task CopyToAsync_MustForwardToUnderlyingStream()
    {
        using var targetStream = new MemoryStream();

        await _temporaryStream.CopyToAsync(targetStream, TestContext.Current.CancellationToken);

        _streamMock.CopyToAsyncMustHaveBeenCalledWith(targetStream);
    }

    [Fact]
    public void Dispose_MustForwardToUnderlyingStream()
    {
        _temporaryStream.Dispose();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.DisposeMustHaveBeenCalled();
    }

    [Fact]
    public async Task DisposeAsync_MustForwardToUnderlyingStream()
    {
        await _temporaryStream.DisposeAsync();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.DisposeAsyncMustHaveBeenCalled();
    }

    [Fact]
    public void Dispose_MustOnlyForwardOnce_WhenCalledMultipleTimes()
    {
        _temporaryStream.Dispose();
        _temporaryStream.Dispose();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.DisposeMustHaveBeenCalled();
    }

    [Fact]
    public async Task DisposeAsync_MustOnlyForwardOnce_WhenCalledMultipleTimes()
    {
        await _temporaryStream.DisposeAsync();
        await _temporaryStream.DisposeAsync();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.DisposeAsyncMustHaveBeenCalled();
    }

    [Fact]
    public void Flush_MustForwardToUnderlyingStream()
    {
        _temporaryStream.Flush();

        _streamMock.FlushMustHaveBeenCalled();
    }

    [Fact]
    public async Task FlushAsync_MustForwardToUnderlyingStream()
    {
        await _temporaryStream.FlushAsync(TestContext.Current.CancellationToken);

        _streamMock.FlushAsyncMustHaveBeenCalled();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRead_MustForwardToUnderlyingStream(bool value)
    {
        _streamMock.CanReadReturnValue = value;

        var result = _temporaryStream.CanRead;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanTimeout_MustForwardToUnderlyingStream(bool value)
    {
        _streamMock.CanTimeoutReturnValue = value;

        var result = _temporaryStream.CanTimeout;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanWrite_MustForwardToUnderlyingStream(bool value)
    {
        _streamMock.CanWriteReturnValue = value;

        var result = _temporaryStream.CanWrite;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(4321)]
    [InlineData(84291)]
    [InlineData(2049)]
    public void Length_MustForwardToUnderlyingStream(long value)
    {
        _streamMock.LengthReturnValue = value;

        var result = _temporaryStream.Length;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(3)]
    public void SetPosition_MustForwardToUnderlyingStream(long value)
    {
        _temporaryStream.Position = value;

        _streamMock.Position.Should().Be(value);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(123)]
    public void GetPosition_MustForwardToUnderlyingStream(long value)
    {
        _streamMock.Position = value;

        var result = _temporaryStream.Position;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(5000)]
    [InlineData(3566)]
    public void SetReadTimeout_MustForwardToUnderlyingStream(int value)
    {
        _temporaryStream.ReadTimeout = value;

        _streamMock.ReadTimeout.Should().Be(value);
    }

    [Theory]
    [InlineData(7400)]
    [InlineData(2300)]
    public void GetReadTimeout_MustForwardToUnderlyingStream(int value)
    {
        _streamMock.ReadTimeout = value;

        var result = _temporaryStream.ReadTimeout;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(5000)]
    [InlineData(20000)]
    public void SetWriteTimeout_MustForwardToUnderlyingStream(int value)
    {
        _temporaryStream.WriteTimeout = value;

        _streamMock.WriteTimeout.Should().Be(value);
    }

    [Theory]
    [InlineData(7800)]
    [InlineData(9500)]
    public void GetWriteTimeout_MustForwardToUnderlyingStream(int value)
    {
        _streamMock.WriteTimeout = value;

        var result = _temporaryStream.WriteTimeout;

        result.Should().Be(value);
    }

    [Fact]
    public void EndRead_MustForwardToUnderlyingStream()
    {
        var result = _temporaryStream.EndRead(_streamMock.AsyncResult);

        result.Should().Be(StreamMock.ReadReturnValue);
        _streamMock.EndReadMustHaveBeenCalledWith(_streamMock.AsyncResult);
    }

    [Fact]
    public void EndWrite_MustForwardToUnderlyingStream()
    {
        _temporaryStream.EndWrite(_streamMock.AsyncResult);

        _streamMock.EndWriteMustHaveBeenCalledWith(_streamMock.AsyncResult);
    }

    [Fact]
    public void Read_MustForwardToUnderlyingStream()
    {
        var buffer = new byte[10];
        var result = _temporaryStream.Read(buffer, 0, buffer.Length);

        result.Should().Be(StreamMock.ReadReturnValue);
        _streamMock.ReadMustHaveBeenCalledWith(buffer, 0, buffer.Length);
    }

    [Fact]
    public void ReadSpan_MustForwardToUnderlyingStream()
    {
        var buffer = new byte[10];
        new Random(42).NextBytes(buffer);
        var result = _temporaryStream.Read(buffer.AsSpan());

        result.Should().Be(StreamMock.ReadReturnValue);
        _streamMock.ReadMustHaveCalledWith(buffer);
    }

    [Fact]
    public async Task ReadAsync_MustForwardToUnderlyingStream()
    {
        var buffer = new byte[3];

        var result = await _temporaryStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        result.Should().Be(StreamMock.ReadReturnValue);
        _streamMock.ReadAsyncMustHaveBeenCalledWith(buffer, 0, buffer.Length, CancellationToken.None);
    }
}
