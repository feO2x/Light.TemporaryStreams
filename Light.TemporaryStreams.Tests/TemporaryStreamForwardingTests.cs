using System;
using System.IO;
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
    public void Close_MustNotForwardToUnderlyingStream()
    {
        _temporaryStream.Close();

        _temporaryStream.IsDisposed.Should().BeTrue();
        _streamMock.CloseMustNotHaveBeenCalled();
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
}
