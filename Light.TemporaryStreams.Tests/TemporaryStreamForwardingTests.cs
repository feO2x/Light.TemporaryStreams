using System;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public sealed class TemporaryStreamForwardingTests
{
    private static readonly AsyncCallback AsyncCallback = asyncResult => { };
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
}
