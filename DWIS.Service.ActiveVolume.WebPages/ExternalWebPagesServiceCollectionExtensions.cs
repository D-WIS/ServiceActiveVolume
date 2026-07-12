using Microsoft.Extensions.DependencyInjection;

namespace DWIS.Service.ActiveVolume.WebPages
{
    public static class ExternalWebPagesServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveVolumeWebPages(this IServiceCollection services, IActiveVolumeWebPagesConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddHttpClient<IActiveVolumeAPIUtils, ActiveVolumeAPIUtils>();
            return services;
        }
    }
}
