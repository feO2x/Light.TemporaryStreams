using System;
using System.IO;
using Light.GuardClauses;

namespace Light.TemporaryStreams;

/// <summary>
/// Represents options for creating a <see cref="FileStream" />.
/// </summary>
public sealed record FileStreamCreationOptions
{
    /// <summary>
    /// Gets the default buffer size. It is 64 KB (65536 bytes).
    /// </summary>
    public const int DefaultBufferSize = 64 * 1024;

    /// <summary>
    /// Represents options for creating a <see cref="FileStream" />.
    /// </summary>
    /// <param name="FileMode">The file mode for the file stream. Defaults to <see cref="FileMode.Create" />.</param>
    /// <param name="FileAccess">The file access mode for the file stream. Defaults to <see cref="FileAccess.ReadWrite" />.</param>
    /// <param name="FileShare">The file share mode for the file stream. Defaults to <see cref="FileShare.None" />.</param>
    /// <param name="BufferSize">The buffer size for the file stream. Defaults to <see cref="DefaultBufferSize" />.</param>
    /// <param name="FileOptions">The file stream options. Defaults to <see cref="FileOptions.Asynchronous" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="BufferSize" /> is less than 1.</exception>
    public FileStreamCreationOptions(
        FileMode FileMode = FileMode.Create,
        FileAccess FileAccess = FileAccess.ReadWrite,
        FileShare FileShare = FileShare.None,
        int BufferSize = DefaultBufferSize,
        FileOptions FileOptions = FileOptions.Asynchronous
    )
    {
        this.FileMode = FileMode;
        this.FileAccess = FileAccess;
        this.FileShare = FileShare;
        this.BufferSize = BufferSize.MustNotBeLessThan(1);
        this.FileOptions = FileOptions;
    }

    /// <summary>
    /// Gets the default <see cref="FileStreamCreationOptions" />.
    /// </summary>
    public static FileStreamCreationOptions Default { get; } = new ();

    /// <summary>
    /// Gets the file mode for the file stream. Defaults to <see cref="FileMode.Create" />.
    /// </summary>
    public FileMode FileMode { get; }

    /// <summary>
    /// Gets the file access mode for the file stream. Defaults to <see cref="FileAccess.ReadWrite" />.
    /// </summary>
    public FileAccess FileAccess { get; }

    /// <summary>
    /// Gets the file share mode for the file stream. Defaults to <see cref="FileShare.None" />.
    /// </summary>
    public FileShare FileShare { get; }

    /// <summary>
    /// Gets the buffer size for the file stream. Defaults to <see cref="DefaultBufferSize" />.
    /// </summary>
    public int BufferSize { get; }

    /// <summary>
    /// Gets the file stream options. Defaults to <see cref="FileOptions.Asynchronous" />.
    /// </summary>
    public FileOptions FileOptions { get; }


    /// <summary>
    /// Creates a <see cref="FileStream" /> with the specified path using the properties of this instance.
    /// </summary>
    /// <param name="path">The path of the file stream.</param>
    /// <returns>The created file stream.</returns>
    public FileStream CreateFileStream(string path) => new (
        path,
        FileMode,
        FileAccess,
        FileShare,
        BufferSize,
        FileOptions
    );
}
