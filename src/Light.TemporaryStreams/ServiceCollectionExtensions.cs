using System;
using Light.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Light.TemporaryStreams;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection" /> to register <see cref="TemporaryStreamService" />
/// and corresponding dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="TemporaryStreamService" /> to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
    /// <param name="createOptions">An optional delegate to create the <see cref="TemporaryStreamServiceOptions" />.</param>
    /// <param name="integrateIntoMicrosoftExtensionsLogging">
    /// When true, the <see cref="TemporaryStreamErrorHandlerProvider" /> returns a delegate which logs exceptions
    /// that occur during the deletion of temporary streams to Microsoft.Extensions.Logging. If false, the provider
    /// will simply return null and logging will occur against the .NET Trace.
    /// </param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddTemporaryStreamService(
        this IServiceCollection services,
        Func<IServiceProvider, TemporaryStreamServiceOptions>? createOptions = null,
        bool integrateIntoMicrosoftExtensionsLogging = true
    )
    {
        services.MustNotBeNull();
        if (createOptions is null)
        {
            services.TryAddSingleton<TemporaryStreamServiceOptions>();
        }
        else
        {
            services.TryAddSingleton(createOptions);
        }

        if (integrateIntoMicrosoftExtensionsLogging)
        {
            services.TryAddSingleton<TemporaryStreamErrorHandlerProvider>(
                sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(nameof(TemporaryStreamService));
                    return new TemporaryStreamErrorHandlerProvider(
                        (stream, exception) =>
                            logger.LogErrorDeletingTemporaryStream(
                                exception,
                                stream.GetUnderlyingFilePath()
                            )
                    );
                }
            );
        }
        else
        {
            services.TryAddSingleton<TemporaryStreamErrorHandlerProvider>(
                _ => new TemporaryStreamErrorHandlerProvider(null)
            );
        }

        services.TryAddSingleton<TemporaryStreamService>();
        services.TryAddSingleton<ITemporaryStreamService>(sp => sp.GetRequiredService<TemporaryStreamService>());
        return services;
    }
}
