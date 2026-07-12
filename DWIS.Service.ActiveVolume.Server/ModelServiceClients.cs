using Microsoft.Extensions.Options;
using ModelSharedClient = NORCE.Drilling.ActiveVolume.ModelShared.Client;

namespace DWIS.Service.ActiveVolume.Server
{
    public interface IModelServiceClients
    {
        ModelSharedClient Field { get; }
        ModelSharedClient Cluster { get; }
        ModelSharedClient Well { get; }
        ModelSharedClient WellBore { get; }
        ModelSharedClient WellBoreArchitecture { get; }
        ModelSharedClient DrillString { get; }
    }

    public sealed class ModelServiceClients : IModelServiceClients
    {
        private readonly IHttpClientFactory httpClientFactory_;
        private readonly ModelServiceOptions options_;

        public ModelServiceClients(IHttpClientFactory httpClientFactory, IOptions<ModelServiceOptions> options)
        {
            httpClientFactory_ = httpClientFactory;
            options_ = options.Value;
        }

        public ModelSharedClient Field => CreateClient(options_.Field);
        public ModelSharedClient Cluster => CreateClient(options_.Cluster);
        public ModelSharedClient Well => CreateClient(options_.Well);
        public ModelSharedClient WellBore => CreateClient(options_.WellBore);
        public ModelSharedClient WellBoreArchitecture => CreateClient(options_.WellBoreArchitecture);
        public ModelSharedClient DrillString => CreateClient(options_.DrillString);

        private ModelSharedClient CreateClient(string baseUrl)
        {
            return new ModelSharedClient(NormalizeBaseUrl(baseUrl), httpClientFactory_.CreateClient(nameof(ModelServiceClients)));
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException($"{ModelServiceOptions.SectionName} contains an empty service URL.");
            }

            return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        }
    }
}
