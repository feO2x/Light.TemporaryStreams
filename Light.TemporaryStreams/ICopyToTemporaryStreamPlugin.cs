using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Light.TemporaryStreams;

/// <summary>
/// Represents the abstraction of a plugin that can be injected during the copying of a source stream to a temporary
/// stream.
/// </summary>
public interface ICopyToTemporaryStreamPlugin : IAsyncDisposable
{
    /// <summary>
    /// Sets up the plugin to be used during the copying of a source stream to a temporary stream.
    /// </summary>
    /// <param name="innerStream">The stream that can be wrapped by the plugin.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <returns>The wrapped stream.</returns>
    public ValueTask<Stream> SetUpAsync(Stream innerStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executed after the contents of the source stream have been successfully copied to the temporary stream.
    /// </summary>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask AfterCopyAsync(CancellationToken cancellationToken = default);
}
