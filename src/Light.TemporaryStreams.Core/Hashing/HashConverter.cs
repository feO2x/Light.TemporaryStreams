using System;
using System.IO;

namespace Light.TemporaryStreams.Hashing;

/// <summary>
/// Converts hash byte arrays to strings.
/// </summary>
public static class HashConverter
{
    /// <summary>
    /// Converts the hash byte array to a string based on the specified <see cref="HashConversionMethod" />.
    /// </summary>
    /// <param name="hashArray">The hash byte array to convert.</param>
    /// <param name="conversionMethod">The enum value specifying how the hash byte array should be converted to a string.</param>
    /// <returns>The hash as a string.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown if <paramref name="conversionMethod" /> has an invalid value.
    /// </exception>
    public static string ConvertHashToString(byte[] hashArray, HashConversionMethod conversionMethod) =>
        conversionMethod switch
        {
            HashConversionMethod.Base64 => Convert.ToBase64String(hashArray),
            HashConversionMethod.UpperHexadecimal => Convert.ToHexString(hashArray),
            HashConversionMethod.None => "",
            _ => throw new ArgumentOutOfRangeException(
                nameof(conversionMethod),
                $"{nameof(conversionMethod)} has an invalid value '{conversionMethod}'"
            )
        };
}
