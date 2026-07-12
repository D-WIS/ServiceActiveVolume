using System.Net.Http.Json;
using System.Text.Json;
using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;
using DWIS.Service.ActiveVolume.Model.Import;

namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public interface IActiveVolumeCalibrationAPIUtils
    {
        Task<List<ActiveVolumeCaseLight>> GetCasesAsync(CancellationToken cancellationToken = default);
        Task<List<CalibrationRecord>> GetCalibrationsAsync(CancellationToken cancellationToken = default);
        Task<List<ActiveVolumeCaseBatchImport>> GetBatchImportsAsync(CancellationToken cancellationToken = default);
    }

    public sealed class ActiveVolumeCalibrationAPIUtils : IActiveVolumeCalibrationAPIUtils
    {
        private const string ActiveVolumeCalibrationHostBasePath = "activevolumecalibration/api";

        private readonly HttpClient httpClient_;
        private readonly IActiveVolumeCalibrationWebPagesConfiguration configuration_;

        public ActiveVolumeCalibrationAPIUtils(HttpClient httpClient, IActiveVolumeCalibrationWebPagesConfiguration configuration)
        {
            httpClient_ = httpClient;
            configuration_ = configuration;
        }

        public async Task<List<ActiveVolumeCaseLight>> GetCasesAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<ActiveVolumeCaseLight>>("ActiveVolumeCase/LightData", cancellationToken) ?? new List<ActiveVolumeCaseLight>();
        }

        public async Task<List<CalibrationRecord>> GetCalibrationsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<CalibrationRecord>>("Calibration", cancellationToken) ?? new List<CalibrationRecord>();
        }

        public async Task<List<ActiveVolumeCaseBatchImport>> GetBatchImportsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<ActiveVolumeCaseBatchImport>>("ActiveVolumeCaseBatchImport", cancellationToken) ?? new List<ActiveVolumeCaseBatchImport>();
        }

        private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
        {
            string hostUrl = configuration_.ActiveVolumeCalibrationHostURL.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                return default;
            }

            string baseUrl = BuildCalibrationApiBaseUrl(hostUrl);
            try
            {
                return await httpClient_.GetFromJsonAsync<T>($"{baseUrl}/{relativeUrl}", cancellationToken);
            }
            catch (HttpRequestException)
            {
                return default;
            }
            catch (JsonException)
            {
                return default;
            }
            catch (NotSupportedException)
            {
                return default;
            }
        }

        private static string BuildCalibrationApiBaseUrl(string hostUrl)
        {
            string normalizedHost = hostUrl.TrimEnd('/');
            if (normalizedHost.EndsWith($"/{ActiveVolumeCalibrationHostBasePath}", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedHost;
            }

            return $"{normalizedHost}/{ActiveVolumeCalibrationHostBasePath}";
        }
    }
}
