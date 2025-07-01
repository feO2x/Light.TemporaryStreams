using System;
using System.IO;
using Light.GuardClauses;
using Range = Light.GuardClauses.Range;

namespace Light.TemporaryStreams;

/// <summary>
/// Represents options for the <see cref="TemporaryStreamService" />.
/// </summary>
public record TemporaryStreamServiceOptions
{
    /// <summary>
    /// Gets the default file threshold in bytes, which is 80 KB (81920 bytes).
    /// </summary>
    public const int DefaultFileThresholdInBytes = 80 * 1024;

    private readonly int _fileThresholdInBytes = DefaultFileThresholdInBytes;

    /// <summary>
    /// <para>
    /// Gets or inits the file threshold in bytes. If an incoming stream's length is smaller than this value, it will be
    /// stored in a <see cref="MemoryStream" />. Set this value to 0 to always store the incoming stream in a
    /// <see cref="FileStream" />.
    /// </para>
    /// <para>
    /// When setting this value, keep in mind that, by default, byte[] arrays only stay in the Small Object Heap
    /// when their size is less than 85,000 bytes. We strongly recommend to stay under this size as it has
    /// performance implications. The default value chosen for Light.TemporaryStreams is 80 KB (81920 bytes), which
    /// is a power of 2 to accomodate for virtual page alignment while just staying within the limits of the Small
    /// Object Heap. Learn more about the Large Object Heap here:
    /// https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when setting this property to a value less than 0 or greater than <see cref="Array.MaxLength" />.
    /// </exception>
    public int FileThresholdInBytes
    {
        get => _fileThresholdInBytes;
        init => _fileThresholdInBytes = value.MustBeIn(Range.InclusiveBetween(0, Array.MaxLength));
    }

    /// <summary>
    /// Gets or inits the <see cref="FileStreamOptions" /> to use when creating a <see cref="FileStream" />.
    /// The default value is <see cref="FileStreamFactory.DefaultOptions" />.
    /// </summary>
    public FileStreamOptions FileStreamOptions { get; init; } = FileStreamFactory.DefaultOptions;

    /// <summary>
    /// Gets or inits the value determining how a temporary stream handles disposal of its underlying stream and file.
    /// The default value is <see cref="TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile" />.
    /// </summary>
    public TemporaryStreamDisposeBehavior DisposeBehavior { get; init; } =
        TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile;
}
