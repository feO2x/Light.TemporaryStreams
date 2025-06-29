using System;
using System.IO;
using Light.GuardClauses;

namespace Light.TemporaryStreams;

/// <summary>
/// Represents a service for creating temporary streams.
/// </summary>
public sealed class TemporaryStreamService : ITemporaryStreamService
{
    /// <summary>
    /// Initializes a new instance of <see cref="TemporaryStreamService" />.
    /// </summary>
    /// <param name="options">The options used to initialize temporary stream objects.</param>
    /// <param name="errorHandlerProvider">The object that provides delegates to handle file errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public TemporaryStreamService(
        TemporaryStreamServiceOptions options,
        TemporaryStreamErrorHandlerProvider errorHandlerProvider
    )
    {
        Options = options.MustNotBeNull();
        ErrorHandlerProvider = errorHandlerProvider.MustNotBeNull();
    }

    /// <summary>
    /// Gets the options used to initialize temporary stream objects.
    /// </summary>
    public TemporaryStreamServiceOptions Options { get; }

    /// <summary>
    /// Gets the object that provides delegates to handle file errors.
    /// </summary>
    public TemporaryStreamErrorHandlerProvider ErrorHandlerProvider { get; }

    /// <inheritdoc />
    public TemporaryStream CreateTemporaryStream(
        long expectedLengthInBytes,
        string? filePath = null,
        TemporaryStreamServiceOptions? options = null
    )
    {
        expectedLengthInBytes.MustNotBeLessThan(0);
        options ??= Options;
        if (expectedLengthInBytes < options.FileThresholdInBytes)
        {
            return new TemporaryStream(new MemoryStream((int) expectedLengthInBytes), options.DisposeBehavior);
        }

        filePath ??= Path.GetTempFileName();
        return new TemporaryStream(
            FileStreamFactory.Create(filePath, options.FileStreamOptions),
            options.DisposeBehavior,
            ErrorHandlerProvider.GetErrorHandlerDelegate()
        );
    }
}
