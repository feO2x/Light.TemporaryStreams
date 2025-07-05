using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Light.TemporaryStreams.Hashing;

/// <summary>
/// Represents a hash calculator that can be used when copying a stream to a temporary stream. This class is not
/// thread-safe.
/// </summary>
public sealed class CopyToHashCalculator : IAsyncDisposable
{
    private CryptoStream? _cryptoStream;
    private string? _hash;
    private byte[]? _hashArray;

    /// <summary>
    /// Initializes a new instance of <see cref="CopyToHashCalculator" />.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <param name="conversionMethod">The enum value identifying how hash byte arrays are converted to strings.</param>
    /// <param name="name">A name that uniquely identifies the hash algorithm.</param>
    public CopyToHashCalculator(HashAlgorithm hashAlgorithm, HashConversionMethod conversionMethod, string? name = null)
    {
        HashAlgorithm = hashAlgorithm.MustNotBeNull();
        ConversionMethod = conversionMethod.MustBeValidEnumValue();
        Name = name ?? DetermineDefaultName(hashAlgorithm);
    }

    /// <summary>
    /// Gets the type of conversion method that is applied to obtain a string representation from the hash byte array.
    /// </summary>
    public HashConversionMethod ConversionMethod { get; }

    /// <summary>
    /// Gets the hash algorithm that is used to calculate the hash.
    /// </summary>
    public HashAlgorithm HashAlgorithm { get; }

    /// <summary>
    /// The name that uniquely identifies this calculator instance within the scope of a CopyTo operation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The calculated hash in string representation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ObtainHashFromAlgorithm" /> has not been called yet.
    /// </exception>
    public string Hash
    {
        get
        {
            var hash = _hash;
            if (hash is null)
            {
                throw new InvalidOperationException(
                    $"ObtainHashFromAlgorithm must be called before accessing the {nameof(Hash)} property"
                );
            }

            return hash;
        }
    }

    /// <summary>
    /// The calculated hash in byte array representation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ObtainHashFromAlgorithm" /> has not been called yet.
    /// </exception>
    public byte[] HashArray
    {
        get
        {
            var hashArray = _hashArray;
            if (hashArray is null)
            {
                throw new InvalidOperationException(
                    $"ObtainHashFromAlgorithm must be called before accessing the {nameof(HashArray)} property"
                );
            }

            return hashArray;
        }
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the current instance, including the CryptoStream and the hash algorithm.
    /// Ensures proper cleanup of unmanaged resources and releases memory associated with the instance.
    /// </summary>
    /// <returns>A ValueTask that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_cryptoStream is not null)
        {
            await _cryptoStream.DisposeAsync();
            _cryptoStream = null;
        }

        HashAlgorithm.Dispose();
    }

    private static string DetermineDefaultName(HashAlgorithm hashAlgorithm)
    {
        /* Some of the .NET hash algorithms (like .SHA1, MD5) are actually just abstract base classes. They have a
         * nested type called implementation which can be instantiated, but this type is not publicly visible. GetType()
         * will likely return the implementation type, which is not what we want as callers would want the name of the
         * base type instead. This is why we check the name of the type for ".Implementation" and if found, we return
         * the name of the base type instead.
         *
         * Other types like HMACSHA256 are public non-abstract types and can be used as expected. */
        var type = hashAlgorithm.GetType();
        var name = type.Name;
        var baseTypeName = type.BaseType?.Name;
        return name.Equals("Implementation", StringComparison.Ordinal) && !baseTypeName.IsNullOrWhiteSpace() ?
            baseTypeName :
            name;
    }

    /// <summary>
    /// Creates a CryptoStream wrapped around the specified stream. The CryptoStream
    /// is configured to calculate a hash using the hash algorithm provided by the
    /// implementation of the derived class. This method can only be called once
    /// for the lifetime of this instance; later calls will throw an <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="wrappedStream">The stream to be wrapped by the CryptoStream.</param>
    /// <param name="leaveWrappedStreamOpen">
    /// The value indicating whether the wrapped stream should not be disposed when the Crypt Stream created by this method is disposed.
    /// </param>
    /// <returns>The Crypto Stream configured to calculate the hash on write operations.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this method is called more than once.</exception>
    public CryptoStream CreateWrappingCryptoStream(Stream wrappedStream, bool leaveWrappedStreamOpen)
    {
        return _cryptoStream = new CryptoStream(
            wrappedStream,
            HashAlgorithm,
            CryptoStreamMode.Write,
            leaveWrappedStreamOpen
        );
    }

    /// <summary>
    /// Finalizes the hash calculation and retrieves the hash from the configured hash algorithm.
    /// The resulting hash is stored in Base64 representation in the <see cref="Hash" /> property.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the hash algorithm has not been initialized by calling <see cref="CreateWrappingCryptoStream" /> beforehand
    /// or if no data has been written to the underlying crypto stream, resulting in no hash calculation.
    /// </exception>
    public void ObtainHashFromAlgorithm()
    {
        _hashArray =
            HashAlgorithm
               .Hash
               .MustNotBeNull(
                    () => new InvalidOperationException(
                        "The crypto stream was not written to - no hash was calculated"
                    )
                );

        _hash = HashConverter.ConvertHashToString(_hashArray, ConversionMethod);
    }

    /// <summary>
    /// Converts a <see cref="HashAlgorithm" /> to a <see cref="CopyToHashCalculator" /> using the default settings
    /// (hash array is converted to a Base64 string, name is identical to the type name).
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm that should be used to calculate the hash.</param>
    /// <returns>A new instance of <see cref="CopyToHashCalculator" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hashAlgorithm" /> is null.</exception>
    public static implicit operator CopyToHashCalculator(HashAlgorithm hashAlgorithm) =>
        new (hashAlgorithm, HashConversionMethod.Base64);
}
