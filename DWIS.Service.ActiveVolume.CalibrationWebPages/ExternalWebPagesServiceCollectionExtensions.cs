using Microsoft.Extensions.DependencyInjection;

namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public static class ExternalWebPagesServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveVolumeCalibrationWebPages(this IServiceCollection services, IActiveVolumeCalibrationWebPagesConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddHttpClient<IActiveVolumeCalibrationAPIUtils, ActiveVolumeCalibrationAPIUtils>();
            return services;
        }
    }
}
