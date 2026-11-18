using Microsoft.Extensions.Logging;
using LiveLogPipeline.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace LiveLogPipeline;

public static class LiveLogPipelineBuilderExtensions
{
    public static ILoggingBuilder AddLiveLogPipeline(this ILoggingBuilder builder)
    {
        // Add configuration support for the logging builder
        builder.AddConfiguration();

        // Register the core services for the live log pipeline
        builder.Services.TryAddSingleton<LiveLogPipelineCore>();
        builder.Services.TryAddSingleton<ILiveLogPipeline>(provider => provider.GetRequiredService<LiveLogPipelineCore>());
        builder.Services.TryAddSingleton<LiveLogRouter>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LiveLogLoggerProvider>());
        
        // Register the provider options for LiveLogLoggerProvider
        LoggerProviderOptions.RegisterProviderOptions<LiveLogPipelineOptions, LiveLogLoggerProvider>(builder.Services);
        
        // Coinfguration validation for LiveLogOptions
        _ = builder.Services.AddOptions<LiveLogPipelineOptions>()
        // TODO: Consider grouping option validation in the options class or creating a dedicated class for it.
                        .Validate(options => options.MaxCapacity > 0, "MaxCapacity must be greater than 0")
                        .Validate(options => options.FlushInterval > TimeSpan.Zero, "FlushInterval must be greater than 0")
                        .Validate(options => options.MaxBatchSize > 0, "MaxBatchSize must be greater than 0");

        return builder;
    }

    public static ILoggingBuilder AddLiveLogPipeline(this ILoggingBuilder builder, Action<LiveLogPipelineOptions> configure)
    {
        _ = builder.AddLiveLogPipeline();
        _ = builder.Services.Configure(configure);
        return builder;
    }
}