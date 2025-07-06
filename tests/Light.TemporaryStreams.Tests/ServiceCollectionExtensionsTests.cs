using System;
using System.Threading.Tasks;
using FluentAssertions;
using Light.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neovolve.Logging.Xunit;
using Xunit;
using Xunit.Sdk;

namespace Light.TemporaryStreams;

public sealed class ServiceCollectionExtensionsTests
{
    private readonly ITestOutputHelper _output;

    public ServiceCollectionExtensionsTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task AddTemporaryStreamService_ShouldRegisterAllNecessaryTypes_WhenNoConfigurationProvided()
    {
        await using var serviceProvider = new ServiceCollection()
           .AddLogging(builder => builder.AddXunit(_output))
           .AddTemporaryStreamService()
           .BuildServiceProvider();

        serviceProvider.GetService<TemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<ITemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<TemporaryStreamServiceOptions>().Should().NotBeNull()
           .And.Be(new TemporaryStreamServiceOptions());
        var errorHandlerProvider = serviceProvider.GetRequiredService<TemporaryStreamErrorHandlerProvider>();
        var errorHandler = errorHandlerProvider.GetErrorHandlerDelegate();
        errorHandler.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTemporaryStreamService_ShouldNotIncorporateMicrosoftLogging_When()
    {
        await using var serviceProvider = new ServiceCollection()
           .AddTemporaryStreamService(integrateIntoMicrosoftExtensionsLogging: false)
           .BuildServiceProvider();

        serviceProvider.GetService<TemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<ITemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<TemporaryStreamServiceOptions>().Should().NotBeNull()
           .And.Be(new TemporaryStreamServiceOptions());
        var errorHandlerProvider = serviceProvider.GetRequiredService<TemporaryStreamErrorHandlerProvider>();
        var errorHandler = errorHandlerProvider.GetErrorHandlerDelegate();
        errorHandler.Should().BeNull();
    }

    [Fact]
    public async Task AddTemporaryStreamService_ShouldNotRegisterDefaultOptions_WhenDelegateIsProvided()
    {
        await using var serviceProvider = new ServiceCollection()
           .AddLogging(builder => builder.AddXunit(_output))
           .AddTemporaryStreamService(_ => new TemporaryStreamServiceOptions { FileThresholdInBytes = 0 })
           .BuildServiceProvider();

        serviceProvider.GetService<TemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<ITemporaryStreamService>().Should().NotBeNull();
        serviceProvider.GetService<TemporaryStreamServiceOptions>().Should().NotBeNull()
           .And.NotBe(new TemporaryStreamServiceOptions());
    }

    [Fact]
    public async Task MicrosoftErrorHandler_ShouldLogErrorMessage_WhenExceptionIsReported()
    {
        using var logger = _output.BuildLogger();
        await using var serviceProvider = new ServiceCollection()
           .AddSingleton<ILoggerFactory>(new LoggerFactoryStub(logger))
           .AddTemporaryStreamService()
           .BuildServiceProvider();

        var errorHandler =
            serviceProvider.GetRequiredService<TemporaryStreamErrorHandlerProvider>().GetErrorHandlerDelegate();
        await using var temporaryStream =
            serviceProvider.GetRequiredService<TemporaryStreamService>().CreateTemporaryStream(100_000);
        var exception = new XunitException("Just for testing purposes");
        errorHandler.MustNotBeNull().Invoke(temporaryStream, exception);
        logger.Entries.Should().HaveCount(1);
    }

    private sealed class LoggerFactoryStub(ICacheLogger logger) : ILoggerFactory
    {
        public void Dispose() { }

        public ILogger CreateLogger(string categoryName) => logger;

        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
    }
}
