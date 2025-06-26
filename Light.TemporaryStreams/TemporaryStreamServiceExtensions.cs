using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Light.TemporaryStreams;

/// <summary>
/// Provides extension methods for <see cref="TemporaryStreamService" />
/// </summary>
public static class TemporaryStreamServiceExtensions
{
    /// <summary>
    /// Copies the contents of the specified <paramref name="source" /> stream to a temporary stream.
    /// </summary>
    /// <param name="temporaryStreamService">The temporary stream service.</param>
    /// <param name="source">The source stream whose contents will be copied to a new temporary stream.</param>
    /// <param name="filePath">
    /// The optional file path. If not provided, the service will use <see cref="Path.GetTempFileName" /> to create a temporary file.
    /// Please ensure that you have appropriate options configured if you want to open an existing file instead of creating a new one or overwriting an
    /// existing one.
    /// </param>
    /// <param name="options">
    /// The optional options. If not provided, the default options of the temporary stream service will be used. This object allows
    /// you to deviate from the default options which are optimized for creating new temporary files. By adapting an options object, you can open existing
    /// files, change the buffer size, or the file options. You can also specify the threshold when a file stream is used instead of a memory stream.
    /// </param>
    /// <param name="copyBufferSize">
    /// The size of the buffer to use when copying the contents of the source stream to the temporary stream. Defaults to
    /// null. If null, the Stream.CopyToAsync method will determine the optimal buffer size for copying.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The temporary stream.</returns>
    public static async Task<TemporaryStream> CopyToTemporaryStreamAsync(
        this TemporaryStreamService temporaryStreamService,
        Stream source,
        string? filePath = null,
        TemporaryStreamServiceOptions? options = null,
        int? copyBufferSize = null,
        CancellationToken cancellationToken = default
    )
    {
        temporaryStreamService.MustNotBeNull();
        source.MustNotBeNull();
        copyBufferSize?.MustNotBeLessThan(0);

        var temporaryStream = temporaryStreamService.CreateTemporaryStream(source.Length, filePath, options);
        try
        {
            if (copyBufferSize.HasValue)
            {
                await source
                   .CopyToAsync(temporaryStream, copyBufferSize.Value, cancellationToken)
                   .ConfigureAwait(false);
            }
            else
            {
                await source.CopyToAsync(temporaryStream, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await temporaryStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return temporaryStream;
    }
}
