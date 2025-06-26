using System;

namespace Light.TemporaryStreams;

/// <summary>
/// Provides an optional delegate to handle errors when deleting temporary files.
/// </summary>
public sealed class TemporaryStreamErrorHandlerProvider
{
    private readonly Action<TemporaryStream, Exception>? _errorHandler;


    /// <summary>
    /// Initializes a new instance of <see cref="TemporaryStreamErrorHandlerProvider" />.
    /// </summary>
    /// <param name="errorHandler">
    /// An optional delegate that is executed when an exception occurs while deleting a temporary file. The first
    /// parameter is the <see cref="TemporaryStream" /> that triggered the deletion attempt, and the second parameter
    /// is the exception that was thrown.
    /// </param>
    public TemporaryStreamErrorHandlerProvider(Action<TemporaryStream, Exception>? errorHandler) =>
        _errorHandler = errorHandler;


    /// <summary>
    /// Gets the delegate that is executed when an exception occurs while deleting a temporary file.
    /// </summary>
    public Action<TemporaryStream, Exception>? GetErrorHandlerDelegate() => _errorHandler;
}
