using System.IO;

namespace Light.TemporaryStreams;

/// <summary>
/// Represents the abstraction of a service for creating temporary streams.
/// </summary>
public interface ITemporaryStreamService
{
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
    /// The optional options. If not provided, the <see cref="TemporaryStream" /> passed via the constructor will be used. This
    /// object allows you to deviate from the default options which are optimized for creating new temporary files. By
    /// adapting an options object, you can open existing files, change the buffer size, or the file options. You can
    /// also specify the threshold when a file stream is used instead of a memory stream.
    /// </param>
    /// <returns>The created <see cref="TemporaryStreamService" /> instance.</returns>
    TemporaryStream CreateTemporaryStream(
        long expectedLengthInBytes,
        string? filePath = null,
        TemporaryStreamServiceOptions? options = null
    );
}
