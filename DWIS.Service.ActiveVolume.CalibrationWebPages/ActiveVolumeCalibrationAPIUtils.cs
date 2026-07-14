using NORCE.Drilling.ActiveVolume.ModelSharedOut;

namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public interface IActiveVolumeCalibrationAPIUtils
    {
        Task<List<ActiveVolumeCaseLight>> GetCasesAsync(CancellationToken cancellationToken = default);
        Task<ActiveVolumeCase?> GetCaseAsync(Guid id, bool includeChunks = false, CancellationToken cancellationToken = default);
        Task<bool> SaveCaseAsync(ActiveVolumeCase activeCase, CancellationToken cancellationToken = default);
        Task<bool> DeleteCaseAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid?> ProcessCaseAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CalibrationRecord?> GetCalibrationAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<ActiveVolumeCaseBatchImportLight>> GetBatchImportsAsync(CancellationToken cancellationToken = default);
        Task<ActiveVolumeCaseBatchImport?> GetBatchImportAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> SaveBatchImportAsync(ActiveVolumeCaseBatchImport batchImport, CancellationToken cancellationToken = default);
        Task<bool> DeleteBatchImportAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<FieldLight>> GetFieldsAsync(CancellationToken cancellationToken = default);
        Task<List<ClusterLight>> GetClustersAsync(CancellationToken cancellationToken = default);
        Task<List<Well>> GetWellsAsync(CancellationToken cancellationToken = default);
        Task<List<WellBore>> GetWellBoresAsync(CancellationToken cancellationToken = default);
        Task<List<WellBoreArchitecture>> GetWellBoreArchitecturesAsync(CancellationToken cancellationToken = default);
        Task<List<DrillStringLight>> GetDrillStringsAsync(CancellationToken cancellationToken = default);
        Task<List<Rig>> GetRigsAsync(CancellationToken cancellationToken = default);
        string HostNameUnitConversion { get; }
        string HostBasePathUnitConversion { get; }
    }

    public sealed class ActiveVolumeCalibrationAPIUtils : IActiveVolumeCalibrationAPIUtils
    {
        private const string ActiveVolumeCalibrationHostBasePath = "activevolumecalibration/api";
        private const string UnitConversionHostBasePath = "UnitConversion/api/";

        private readonly HttpClient httpClient_;
        private readonly IActiveVolumeCalibrationWebPagesConfiguration configuration_;

        public ActiveVolumeCalibrationAPIUtils(HttpClient httpClient, IActiveVolumeCalibrationWebPagesConfiguration configuration)
        {
            httpClient_ = httpClient;
            configuration_ = configuration;
        }

        public string HostNameUnitConversion => configuration_.UnitConversionHostURL;

        public string HostBasePathUnitConversion => UnitConversionHostBasePath;

        public async Task<List<ActiveVolumeCaseLight>> GetCasesAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return new List<ActiveVolumeCaseLight>();
            }

            try
            {
                return (await client.GetAllActiveVolumeCaseLightAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<ActiveVolumeCaseLight>();
            }
        }

        public async Task<ActiveVolumeCase?> GetCaseAsync(Guid id, bool includeChunks = false, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return null;
            }

            try
            {
                return await client.GetActiveVolumeCaseByIdAsync(id, includeChunks, cancellationToken);
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return null;
            }
        }

        public async Task<bool> SaveCaseAsync(ActiveVolumeCase activeCase, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return false;
            }

            bool isNew = activeCase.Id == Guid.Empty;
            activeCase.Id = isNew ? Guid.NewGuid() : activeCase.Id;
            try
            {
                if (isNew)
                {
                    await client.PostActiveVolumeCaseAsync(activeCase, cancellationToken);
                }
                else
                {
                    await client.PutActiveVolumeCaseByIdAsync(activeCase.Id, activeCase, cancellationToken);
                }

                return true;
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return false;
            }
        }

        public async Task<bool> DeleteCaseAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return false;
            }

            try
            {
                await client.DeleteActiveVolumeCaseByIdAsync(id, cancellationToken);
                return true;
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return false;
            }
        }

        public async Task<Guid?> ProcessCaseAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return null;
            }

            try
            {
                return await client.PostActiveVolumeCaseProcessAsync(id, cancellationToken);
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return null;
            }
        }

        public async Task<CalibrationRecord?> GetCalibrationAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return null;
            }

            try
            {
                return await client.GetCalibrationByIdAsync(id, cancellationToken);
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return null;
            }
        }

        public async Task<List<ActiveVolumeCaseBatchImportLight>> GetBatchImportsAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return new List<ActiveVolumeCaseBatchImportLight>();
            }

            try
            {
                return (await client.GetAllActiveVolumeCaseBatchImportLightAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<ActiveVolumeCaseBatchImportLight>();
            }
        }

        public async Task<ActiveVolumeCaseBatchImport?> GetBatchImportAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return null;
            }

            try
            {
                return await client.GetActiveVolumeCaseBatchImportByIdAsync(id, cancellationToken);
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return null;
            }
        }

        public async Task<bool> SaveBatchImportAsync(ActiveVolumeCaseBatchImport batchImport, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return false;
            }

            if (batchImport.Id == Guid.Empty)
            {
                batchImport.Id = Guid.NewGuid();
            }

            try
            {
                await client.PostActiveVolumeCaseBatchImportAsync(batchImport, cancellationToken);
                return true;
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return false;
            }
        }

        public async Task<bool> DeleteBatchImportAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient();
            if (client is null)
            {
                return false;
            }

            try
            {
                await client.DeleteActiveVolumeCaseBatchImportByIdAsync(id, cancellationToken);
                return true;
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return false;
            }
        }

        public async Task<List<FieldLight>> GetFieldsAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.FieldHostURL, "Field/api");
            if (client is null)
            {
                return new List<FieldLight>();
            }

            try
            {
                return (await client.GetAllFieldLightAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<FieldLight>();
            }
        }

        public async Task<List<ClusterLight>> GetClustersAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.ClusterHostURL, "Cluster/api");
            if (client is null)
            {
                return new List<ClusterLight>();
            }

            try
            {
                return (await client.GetAllClusterLightAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<ClusterLight>();
            }
        }

        public async Task<List<Well>> GetWellsAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.WellHostURL, "Well/api");
            if (client is null)
            {
                return new List<Well>();
            }

            try
            {
                return (await client.GetAllWellAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<Well>();
            }
        }

        public async Task<List<WellBore>> GetWellBoresAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.WellBoreHostURL, "WellBore/api");
            if (client is null)
            {
                return new List<WellBore>();
            }

            try
            {
                return (await client.GetAllWellBoreAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<WellBore>();
            }
        }

        public async Task<List<WellBoreArchitecture>> GetWellBoreArchitecturesAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.WellBoreArchitectureHostURL, "WellBoreArchitecture/api");
            if (client is null)
            {
                return new List<WellBoreArchitecture>();
            }

            try
            {
                return (await client.GetAllWellBoreArchitectureAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<WellBoreArchitecture>();
            }
        }

        public async Task<List<DrillStringLight>> GetDrillStringsAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.DrillStringHostURL, "DrillString/api");
            if (client is null)
            {
                return new List<DrillStringLight>();
            }

            try
            {
                return (await client.GetAllDrillStringLightAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<DrillStringLight>();
            }
        }

        public async Task<List<Rig>> GetRigsAsync(CancellationToken cancellationToken = default)
        {
            Client? client = CreateClient(configuration_.RigHostURL, "Rig/api");
            if (client is null)
            {
                return new List<Rig>();
            }

            try
            {
                return (await client.GetAllRigAsync(cancellationToken)).ToList();
            }
            catch (Exception exception) when (IsClientException(exception))
            {
                return new List<Rig>();
            }
        }

        private Client? CreateClient()
        {
            return CreateClient(configuration_.ActiveVolumeCalibrationHostURL, ActiveVolumeCalibrationHostBasePath);
        }

        private Client? CreateClient(string hostUrl, string defaultBasePath)
        {
            string normalizedHost = hostUrl.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(normalizedHost))
            {
                return null;
            }

            return new Client(BuildApiBaseUrl(normalizedHost, defaultBasePath), httpClient_);
        }

        private static string BuildCalibrationApiBaseUrl(string hostUrl)
        {
            return BuildApiBaseUrl(hostUrl, ActiveVolumeCalibrationHostBasePath);
        }

        private static string BuildApiBaseUrl(string hostUrl, string defaultBasePath)
        {
            string normalizedHost = hostUrl.TrimEnd('/');
            string normalizedBasePath = defaultBasePath.Trim('/');
            if (normalizedHost.EndsWith($"/{normalizedBasePath}", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedHost;
            }

            return $"{normalizedHost}/{normalizedBasePath}";
        }

        private static bool IsClientException(Exception exception)
        {
            return exception is ApiException or HttpRequestException or TaskCanceledException;
        }
    }
}
