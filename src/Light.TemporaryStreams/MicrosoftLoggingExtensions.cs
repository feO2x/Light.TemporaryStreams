using System;
using Microsoft.Extensions.Logging;

namespace Light.TemporaryStreams;

/// <summary>
/// Provides extension methods for <see cref="ILogger" /> that are specifically designed for use with the
/// <see cref="TemporaryStreamService" />.
/// </summary>
public static partial class MicrosoftLoggingExtensions
{
    /// <summary>
    /// Logs an error message when an exception occurs while deleting a temporary stream.
    /// </summary>
    /// <param name="logger">The logger to write the error message to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="temporaryStreamFilePath">The file path of the temporary stream.</param>
    [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Error,
            Message = "An error occurred while deleting the temporary stream '{TemporaryStreamFilePath}'"
        )
    ]
    public static partial void LogErrorDeletingTemporaryStream(
        this ILogger logger,
        Exception exception,
        string temporaryStreamFilePath
    );
}
