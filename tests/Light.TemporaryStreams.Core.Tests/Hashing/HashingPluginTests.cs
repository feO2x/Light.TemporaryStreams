using System;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Light.GuardClauses.Exceptions;
using Xunit;

namespace Light.TemporaryStreams.Hashing;

public static class HashingPluginTests
{
    [Fact]
    public static void Constructor_ThrowsEmptyCollectionException_WhenImmutableArrayIsEmpty()
    {
        var hashCalculators = ImmutableArray<CopyToHashCalculator>.Empty;

        var act = () => new HashingPlugin(hashCalculators);

        act.Should().Throw<EmptyCollectionException>().Which.ParamName.Should().Be("hashCalculators");
    }

    [Fact]
    public static void Constructor_ThrowsEmptyCollectionException_WhenImmutableArrayIsDefault()
    {
        var hashCalculators = default(ImmutableArray<CopyToHashCalculator>);

        var act = () => new HashingPlugin(hashCalculators);

        act.Should().Throw<EmptyCollectionException>().Which.ParamName.Should().Be("hashCalculators");
    }

    [Fact]
    public static async Task DisposeAsync_DoesNotDisposeCalculators_WhenDisposeCalculatorsIsFalse()
    {
        using var sha1 = SHA1.Create();
        await using var hashingPlugin = new HashingPlugin([sha1], disposeCalculators: false);
    }

    [Fact]
    public static async Task AfterCopyAsync_ShouldThrow_WhenSetUpAsyncWasNotCalledBeforehand()
    {
        await using var hashingPlugin = new HashingPlugin([SHA1.Create()]);

        // ReSharper disable once AccessToDisposedClosure -- delegate called before hashingPlugin is disposed of
        var act = () => hashingPlugin.AfterCopyAsync().AsTask();

        (await act.Should().ThrowAsync<InvalidOperationException>())
           .Which.Message.Should().Be("SetUpAsync must be called before AfterCopyAsync");
    }
}
