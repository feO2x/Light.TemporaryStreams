using System;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public sealed class TemporaryStreamTests
{
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
}
