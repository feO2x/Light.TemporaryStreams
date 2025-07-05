using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Hashing;

public static class CopyToHashCalculatorTests
{
    [Fact]
    public static async Task Hash_ShouldThrow_WhenObtainHashFromAlgorithmWasNotCalledBeforehand()
    {
        await using CopyToHashCalculator calculator = SHA1.Create();

        // ReSharper disable once AccessToDisposedClosure -- delegate called before calculator is disposed of
        var act = () => calculator.Hash;

        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().Be("ObtainHashFromAlgorithm must be called before accessing the Hash property");
    }

    [Fact]
    public static async Task HashArray_ShouldThrow_WhenObtainHashFromAlgorithmWasNotCalledBeforehand()
    {
        await using CopyToHashCalculator calculator = SHA256.Create();

        // ReSharper disable once AccessToDisposedClosure -- delegate called before calculator is disposed of
        var act = () => calculator.HashArray;

        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().Be("ObtainHashFromAlgorithm must be called before accessing the HashArray property");
    }

    [Fact]
    public static async Task ObtainHashFromAlgorithm_ShouldThrow_WhenNothingWasWrittenToUnderlyingCryptoStream()
    {
        await using CopyToHashCalculator calculator = SHA3_512.Create();

        // ReSharper disable once AccessToDisposedClosure -- delegate called before calculator is disposed of
        var act = () => calculator.ObtainHashFromAlgorithm();

        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().Be("The crypto stream was not written to - no hash was calculated");
    }
}
