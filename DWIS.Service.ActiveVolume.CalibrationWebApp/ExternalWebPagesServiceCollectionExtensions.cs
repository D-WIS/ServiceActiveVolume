using Microsoft.Extensions.DependencyInjection;

namespace DWIS.Service.ActiveVolume.CalibrationWebApp
{
    public static class ExternalWebPagesServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalWebPages(this IServiceCollection services, WebPagesHostConfiguration configuration)
        {
            services.AddSingleton<NORCE.Drilling.Field.WebPages.IFieldWebPagesConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.Field.WebPages.IFieldAPIUtils, NORCE.Drilling.Field.WebPages.FieldAPIUtils>();

            services.AddSingleton<NORCE.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.Cluster.WebPages.IClusterAPIUtils, NORCE.Drilling.Cluster.WebPages.ClusterAPIUtils>();

            services.AddSingleton<NORCE.Drilling.Well.WebPages.IWellWebPagesConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.Well.WebPages.IWellAPIUtils, NORCE.Drilling.Well.WebPages.WellAPIUtils>();

            services.AddSingleton<NORCE.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.WellBore.WebPages.IWellBoreAPIUtils, NORCE.Drilling.WellBore.WebPages.WellBoreAPIUtils>();

            services.AddSingleton<NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureWebPagesConfiguration>(configuration);
            services.AddSingleton<
                NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureAPIUtils,
                NORCE.Drilling.WellBoreArchitecture.WebPages.WellBoreArchitectureAPIUtils>();

            services.AddSingleton<NORCE.Drilling.DrillString.WebPages.IDrillStringWebPagesConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.DrillString.WebPages.IDrillStringAPIUtils, NORCE.Drilling.DrillString.WebPages.DrillStringAPIUtils>();

            services.AddSingleton<NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumWebPageConfiguration>(configuration);
            services.AddSingleton<NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumAPIUtils, NORCE.Drilling.VerticalDatum.WebPage.APIUtils>();

            return services;
        }
    }
}
