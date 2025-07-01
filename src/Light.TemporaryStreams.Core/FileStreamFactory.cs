using System.IO;

namespace Light.TemporaryStreams;

/// <summary>
/// A factory for creating <see cref="FileStream" /> instances.
/// </summary>
public static class FileStreamFactory
{
    /// <summary>
    /// The default buffer size used for file stream buffering.
    /// </summary>
    public const int DefaultBufferSize = 80 * 1024;

    /// <summary>
    /// Gets the default options for creating <see cref="FileStream" /> instances.
    /// </summary>
    public static FileStreamOptions DefaultOptions { get; } =
        new ()
        {
            Mode = FileMode.Create,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            BufferSize = DefaultBufferSize,
            Options = FileOptions.Asynchronous
        };

    /// <summary>
    /// Creates a new <see cref="FileStream" /> instance.
    /// </summary>
    /// <param name="path">The target file path.</param>
    /// <param name="options">
    /// The optional file stream options. If no options are provided, the <see cref="DefaultOptions" /> instance is used.
    /// </param>
    /// <returns>The created <see cref="FileStream" /> instance.</returns>
    public static FileStream Create(string path, FileStreamOptions? options = null)
    {
        options ??= DefaultOptions;
        return new FileStream(path, options);
    }
}
