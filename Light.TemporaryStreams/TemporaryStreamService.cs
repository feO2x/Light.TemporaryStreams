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

    /// <summary>
    /// <para>
    /// Creates a new <see cref="TemporaryStream" /> instance.
    /// </para>
    /// </summary>
    /// <param name="expectedLengthInBytes">
    /// The length of the stream. Will be used to determine whether a file stream or a memory stream should be used.
    /// </param>
    /// <param name="filePath">
    /// The optional file path. If not provided, <see cref="Path.GetTempFileName" /> will be used to create a temporary
    /// file. Please ensure that you have appropriate options configured if you want to open an existing file instead
    /// of creating a new one or overwriting an existing one.
    /// </param>
    /// <param name="options">
    /// The optional options. If not provided, the <see cref="Options" /> passed via the constructor will be used. This
    /// object allows you to deviate from the default options which are optimized for creating new temporary files. By
    /// adapting an options object, you can open existing files, change the buffer size, or the file options. You can
    /// also specify the threshold when a file stream is used instead of a memory stream.
    /// </param>
    /// <returns>The created <see cref="TemporaryStream" /> instance.</returns>
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
