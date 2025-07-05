using System;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Hashing;

public static class HashConverterTests
{
    [Fact]
    public static void ConvertHashToString_Base64()
    {
        byte[] hash = [0x01, 0x02, 0x03, 0x04, 0x05];

        var result = HashConverter.ConvertHashToString(hash, HashConversionMethod.Base64);

        result.Should().Be("AQIDBAU=");
    }

    [Fact]
    public static void ConvertHashToString_UpperHexadecimal()
    {
        byte[] hash = [0x01, 0x02, 0x03, 0x04, 0x05];

        var result = HashConverter.ConvertHashToString(hash, HashConversionMethod.UpperHexadecimal);

        result.Should().Be("0102030405");
    }

    [Fact]
    public static void ConvertHashToString_None_ReturnsEmptyString()
    {
        byte[] hash = [0x01, 0x02, 0x03, 0x04, 0x05];

        var result = HashConverter.ConvertHashToString(hash, HashConversionMethod.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public static void ConvertHashToString_InvalidMethod_ThrowsArgumentOutOfRangeException()
    {
        byte[] hash = [0x01, 0x02, 0x03, 0x04, 0x05];
        const HashConversionMethod method = (HashConversionMethod) 999;

        Action act = () => HashConverter.ConvertHashToString(hash, method);

        act.Should()
           .Throw<ArgumentOutOfRangeException>()
           .Where(
                x => x.ParamName == "conversionMethod" &&
                     x.Message == $"conversionMethod has an invalid value '{method}' (Parameter 'conversionMethod')"
            );
    }

    [Fact]
    public static void ConvertHashToString_EmptyArray_ReturnsCorrectResult()
    {
        var emptyHash = Array.Empty<byte>();

        HashConverter.ConvertHashToString(emptyHash, HashConversionMethod.Base64).Should().BeEmpty();
        HashConverter.ConvertHashToString(emptyHash, HashConversionMethod.UpperHexadecimal).Should().BeEmpty();
        HashConverter.ConvertHashToString(emptyHash, HashConversionMethod.None).Should().BeEmpty();
    }

    [Fact]
    public static void ConvertHashToString_NullArray_ThrowsArgumentNullException()
    {
        Action act = () => HashConverter.ConvertHashToString(null!, HashConversionMethod.Base64);

        act.Should().Throw<ArgumentNullException>();
    }
}
