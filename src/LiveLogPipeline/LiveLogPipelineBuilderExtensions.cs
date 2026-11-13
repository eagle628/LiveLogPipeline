using Microsoft.Extensions.Logging;
using LiveLogPipeline.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LiveLogPipeline;

public static class LiveLogPipelineBuilderExtensions
{
    public static ILoggingBuilder AddLiveLogPipeline(this ILoggingBuilder builder)
        {
            builder.Services
                .AddSingleton<LiveLogPipelineCore>()
                .AddSingleton<ILiveLogPipeline>(provider => provider.GetRequiredService<LiveLogPipelineCore>())
                .AddSingleton<LiveLogRouter>()
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LiveLogLoggerProvider>(x =>
                {
                    var router = x.GetRequiredService<LiveLogRouter>();
                    return new LiveLogLoggerProvider(router);
                }));
            return builder;
        }
}