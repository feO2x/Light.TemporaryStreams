using System.IO;

namespace Light.TemporaryStreams;

/// <summary>
/// Determines how the <see cref="TemporaryStream" /> behaves when it is being disposed of.
/// </summary>
public enum TemporaryStreamDisposeBehavior
{
    /// <summary>
    /// When the <see cref="TemporaryStream" /> is disposed of, close the underlying stream and delete the file, if
    /// the underlying stream is a <see cref="FileStream" />.
    /// </summary>
    CloseUnderlyingStreamAndDeleteFile = 0,

    /// <summary>
    /// When the <see cref="TemporaryStream" /> is disposed of, close the underlying stream. A correlating file will not
    /// be deleted.
    /// </summary>
    CloseUnderlyingStreamOnly = 1,

    /// <summary>
    /// When the <see cref="TemporaryStream" /> is disposed of, do not close the underlying stream.
    /// </summary>
    LeaveUnderlyingStreamOpen = 2
}
