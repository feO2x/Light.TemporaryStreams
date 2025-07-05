using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.GuardClauses.ExceptionFactory;
using Light.GuardClauses.Exceptions;

namespace Light.TemporaryStreams.Hashing;

/// <summary>
/// Represents a plugin that calculates one or more hashes during a copy operation to a temporary stream.
/// </summary>
public sealed class HashingPlugin : ICopyToTemporaryStreamPlugin
{
    private CryptoStream? _outermostCryptoStream;

    /// <summary>
    /// Initializes a new instance of <see cref="HashingPlugin" />.
    /// </summary>
    /// <param name="hashCalculators">The hash calculators to use.</param>
    /// <param name="disposeCalculators">
    /// The value indicating whether the hash calculators should be disposed of when the plugin is disposed of.
    /// </param>
    /// <exception cref="EmptyCollectionException">
    /// Thrown when <paramref name="hashCalculators" /> is empty or the default instance.
    /// </exception>
    public HashingPlugin(ImmutableArray<CopyToHashCalculator> hashCalculators, bool disposeCalculators = true)
    {
        if (hashCalculators.IsDefaultOrEmpty)
        {
            Throw.EmptyCollection(nameof(hashCalculators));
        }

        HashCalculators = hashCalculators;
        DisposeCalculators = disposeCalculators;
    }

    /// <summary>
    /// Gets the value indicating whether the hash calculators are disposed by this plugin instance.
    /// </summary>
    public bool DisposeCalculators { get; }

    /// <summary>
    /// Gets the hash calculators that are used during the copy operation.
    /// </summary>
    public ImmutableArray<CopyToHashCalculator> HashCalculators { get; }

    /// <summary>
    /// Disposes of all hash calculators in reverse order when <see cref="DisposeCalculators" /> is true.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (DisposeCalculators)
        {
            for (var i = HashCalculators.Length - 1; i >= 0; i--)
            {
                await HashCalculators[i].DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Sets up the hash calculators by wrapping the specified inner stream with a CryptoStream for each hash
    /// calculator. The CryptoStream is configured to write to the inner stream and calculate the hash.
    /// <para>
    /// The outermost stream is returned, which is the CryptoStream wrapping the original inner stream.
    /// </para>
    /// </summary>
    /// <param name="innerStream">The inner stream to be wrapped by the hash calculators.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <returns>The outermost CryptoStream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerStream" /> is null.</exception>
    public ValueTask<Stream> SetUpAsync(Stream innerStream, CancellationToken cancellationToken = default)
    {
        innerStream.MustNotBeNull();
        Stream currentStream = innerStream;
        for (var i = 0; i < HashCalculators.Length; i++)
        {
            currentStream = HashCalculators[i].CreateWrappingCryptoStream(currentStream, leaveWrappedStreamOpen: true);
        }

        _outermostCryptoStream = (CryptoStream)currentStream;
        return new ValueTask<Stream>(currentStream);
    }

    /// <summary>
    /// Finalizes the hash calculation for all hash calculators. This method must be called after all data has been
    /// written to the stream returned by <see cref="SetUpAsync" />.
    /// </summary>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <returns>A value task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="SetUpAsync" /> has not been called before this method.
    /// </exception>
    public async ValueTask AfterCopyAsync(CancellationToken cancellationToken = default)
    {
        // Flushing must only be called on the outermost stream
        await _outermostCryptoStream
           .MustNotBeNull(
                () => new InvalidOperationException(
                    $"{nameof(SetUpAsync)} must be called before {nameof(AfterCopyAsync)}"
                )
            )
           .FlushFinalBlockAsync(cancellationToken);

        foreach (var hashCalculator in HashCalculators)
        {
            hashCalculator.ObtainHashFromAlgorithm();
        }
    }

    /// <summary>
    /// Retrieves the hash as a string calculated by the hash calculator with the specified name.
    /// </summary>
    /// <param name="calculatorName">The name of the hash calculator.</param>
    /// <returns>The hash as a string.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the hash calculator with the specified name does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the CopyTo operation is not complete.</exception>
    public string GetHash(string calculatorName) => FindCalculator(calculatorName).Hash;

    /// <summary>
    /// Retrieves the hash as a byte array calculated by the hash calculator with the specified name.
    /// </summary>
    /// <param name="calculatorName">The name of the hash calculator.</param>
    /// <returns>The hash as a byte array.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the hash calculator with the specified name does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the CopyTo operation is not complete.</exception>
    public byte[] GetHashArray(string calculatorName) => FindCalculator(calculatorName).HashArray;

    private CopyToHashCalculator FindCalculator(string calculatorName)
    {
        for (var i = 0; i < HashCalculators.Length; i++)
        {
            if (HashCalculators[i].Name == calculatorName)
            {
                return HashCalculators[i];
            }
        }

        var stringBuilder = new StringBuilder().AppendLine(
            $"There is no hash calculator with the name '{calculatorName}' - available calculators are:"
        );
        foreach (var calculator in HashCalculators)
        {
            stringBuilder.Append(calculator.Name).AppendLine();
        }

        throw new KeyNotFoundException(stringBuilder.ToString());
    }
}

#pragma warning disable CS8524
