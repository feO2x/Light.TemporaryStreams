namespace Light.TemporaryStreams.Hashing;

/// <summary>
/// Represents methods that <see cref="CopyToHashCalculator" /> supports to convert hash byte arrays to strings.
/// </summary>
public enum HashConversionMethod
{
    /// <summary>
    /// Converts the hash byte array to a Base64 string.
    /// </summary>
    Base64,

    /// <summary>
    /// Converts the hash byte array to an upper hexadecimal string.
    /// </summary>
    UpperHexadecimal,

    /// <summary>
    /// Performs no conversion of the hash byte array.
    /// </summary>
    None
}
