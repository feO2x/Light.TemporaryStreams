using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Light.TemporaryStreams;

/// <summary>
/// <para>
/// Represents a stream to a temporary local file. If the file is small enough for the Small Object Heap, it will
/// be stored in a <see cref="MemoryStream" />, otherwise a local file will be created. All calls like reading from or
/// writing to the stream are forwarded to the underlying stream. You can access the underlying stream via the
/// <see cref="UnderlyingStream" /> property.
/// </para>
/// <para>
/// When the underlying stream is a <see cref="FileStream" />, the temporary file will be deleted when this stream
/// is disposed of. You can change this behavior by providing a different <see cref="TemporaryStreamDisposeBehavior" />
/// to the constructor.
/// </para>
/// <para>
/// This class is not thread-safe. Only use it in contexts where one thread accesses it at a time or synchronize access
/// to it manually.
/// </para>
/// </summary>
public class TemporaryStream : Stream
{
    /// <summary>
    /// Initializes a new instance of <see cref="TemporaryStream" />.
    /// </summary>
    /// <param name="underlyingStream">
    /// The stream that is encapsulated by this instance. This stream must be seekable.
    /// </param>
    /// <param name="disposeBehavior">
    /// The value indicating how the underlying stream and file should be treated during disposal of the temporary
    /// stream. Defaults to <see cref="TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile" />.
    /// </param>
    /// <param name="onFileDeletionError">
    /// The delegate that is executed when an exception occurs while deleting the underlying file.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="underlyingStream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="underlyingStream" /> is not seekable.
    /// </exception>
    public TemporaryStream(
        Stream underlyingStream,
        TemporaryStreamDisposeBehavior disposeBehavior =
            TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile,
        Action<TemporaryStream, Exception>? onFileDeletionError = null
    )
    {
        UnderlyingStream = underlyingStream.MustNotBeNull();
        if (!underlyingStream.CanSeek)
        {
            throw new ArgumentException("The underlying stream must be seekable.", nameof(underlyingStream));
        }

        DisposeBehavior = disposeBehavior;
        OnFileDeletionError = onFileDeletionError;
    }

    /// <summary>
    /// Gets the stream that is encapsulated by this instance.
    /// </summary>
    public Stream UnderlyingStream { get; }

    /// <summary>
    /// Gets the value indicating how the underlying stream and file should be treated during disposal of the
    /// temporary stream.
    /// </summary>
    public TemporaryStreamDisposeBehavior DisposeBehavior { get; }

    /// <inheritdoc />
    public override bool CanRead => UnderlyingStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => UnderlyingStream.CanWrite;

    /// <inheritdoc />
    public override bool CanTimeout => UnderlyingStream.CanTimeout;

    /// <inheritdoc />
    public override long Length => UnderlyingStream.Length;

    /// <summary>
    /// Gets the value indicating whether the underlying stream is a <see cref="FileStream" />.
    /// </summary>
    public bool IsFileBased => UnderlyingStream is FileStream;

    /// <summary>
    /// Gets the delegate that is executed when an exception occurs while deleting the underlying file.
    /// </summary>
    public Action<TemporaryStream, Exception>? OnFileDeletionError { get; }

    /// <inheritdoc />
    public override long Position
    {
        get => UnderlyingStream.Position;
        set => UnderlyingStream.Position = value;
    }

    /// <inheritdoc />
    public override int ReadTimeout
    {
        get => UnderlyingStream.ReadTimeout;
        set => UnderlyingStream.ReadTimeout = value;
    }

    /// <inheritdoc />
    public override int WriteTimeout
    {
        get => UnderlyingStream.WriteTimeout;
        set => UnderlyingStream.WriteTimeout = value;
    }

    /// <summary>
    /// Gets the value indicating whether this instance is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public override IAsyncResult BeginRead(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        UnderlyingStream.BeginRead(buffer, offset, count, callback, state);

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state
    ) =>
        UnderlyingStream.BeginWrite(buffer, offset, count, callback, state);

    /// <summary>
    /// Tries to get the underlying file path of the stream.
    /// </summary>
    /// <param name="filePath">
    /// The file path of the underlying stream, or <see langword="null" /> if the stream is not backed by a file.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the underlying stream is a <see cref="FileStream" />,
    /// otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetUnderlyingFilePath([NotNullWhen(true)] out string? filePath)
    {
        if (UnderlyingStream is FileStream fileStream)
        {
            filePath = fileStream.Name;
            return true;
        }

        filePath = null;
        return false;
    }

    /// <summary>
    /// Gets the underlying file path of the stream, or throws an <see cref="InvalidOperationException" /> if the
    /// stream is not backed by a file.
    /// </summary>
    /// <returns>
    /// The file path of the underlying stream.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the underlying stream is not a <see cref="FileStream" />.
    /// </exception>
    public string GetUnderlyingFilePath()
    {
        if (UnderlyingStream is not FileStream fileStream)
        {
            throw new InvalidOperationException("The underlying stream is not a file stream.");
        }

        return fileStream.Name;
    }

    /// <summary>
    /// <para>
    /// WARNING: do not call this method. Use <see cref="DisposeAsync" /> instead.
    /// </para>
    /// <para>
    /// Disposes of the current instance of the <see cref="TemporaryStream" /> class.
    /// </para>
    /// </summary>
    public override void Close() => Dispose(true);

    /// <inheritdoc />
    public override void CopyTo(Stream destination, int bufferSize) =>
        UnderlyingStream.CopyTo(destination, bufferSize);

    /// <inheritdoc />
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        UnderlyingStream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="TemporaryStream" /> class.
    /// If <see cref="DisposeBehavior" /> is set to
    /// <see cref="TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile" /> and the underlying stream is a
    /// <see cref="FileStream" />, this method deletes the underlying file after disposing the stream.
    /// </summary>
    /// <param name="disposing">This value is ignored.</param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        if (DisposeBehavior != TemporaryStreamDisposeBehavior.LeaveUnderlyingStreamOpen)
        {
            UnderlyingStream.Dispose();
        }

        SetIsDisposedAndDeleteFileIfNecessary();
    }

    /// <summary>
    /// Asynchronously releases all resources used by the current instance of the <see cref="TemporaryStream" /> class.
    /// If <see cref="DisposeBehavior" /> is set to
    /// <see cref="TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile" /> and the underlying stream is a
    /// <see cref="FileStream" />, this method deletes the underlying file after disposing the stream.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        if (DisposeBehavior != TemporaryStreamDisposeBehavior.LeaveUnderlyingStreamOpen)
        {
            await UnderlyingStream.DisposeAsync();
        }

        SetIsDisposedAndDeleteFileIfNecessary();
    }

    private void SetIsDisposedAndDeleteFileIfNecessary()
    {
        if (DisposeBehavior == TemporaryStreamDisposeBehavior.CloseUnderlyingStreamAndDeleteFile &&
            UnderlyingStream is FileStream fileStream)
        {
            try
            {
                File.Delete(fileStream.Name);
            }
            catch (Exception exception)
            {
                if (OnFileDeletionError is null)
                {
                    Trace.TraceError(
                        $"Failed to delete underlying file of temporary stream:{Environment.NewLine}{exception}"
                    );
                }
                else
                {
                    OnFileDeletionError.Invoke(this, exception);
                }
            }
        }

        IsDisposed = true;
    }

    /// <inheritdoc />
    public override void Flush() => UnderlyingStream.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        UnderlyingStream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int EndRead(IAsyncResult asyncResult) => UnderlyingStream.EndRead(asyncResult);

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult) => UnderlyingStream.EndWrite(asyncResult);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => UnderlyingStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) => UnderlyingStream.Read(buffer);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        UnderlyingStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        UnderlyingStream.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override int ReadByte() => UnderlyingStream.ReadByte();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => UnderlyingStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => UnderlyingStream.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => UnderlyingStream.Write(buffer, offset, count);

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) => UnderlyingStream.Write(buffer);

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        UnderlyingStream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        UnderlyingStream.WriteAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override void WriteByte(byte value) => UnderlyingStream.WriteByte(value);
}
