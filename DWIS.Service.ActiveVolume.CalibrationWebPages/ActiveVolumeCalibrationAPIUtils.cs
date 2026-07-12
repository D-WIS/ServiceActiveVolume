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
        Task<ActiveVolumeCase?> GetCaseAsync(Guid id, bool includeChunks = false, CancellationToken cancellationToken = default);
        Task<bool> SaveCaseAsync(ActiveVolumeCase activeCase, CancellationToken cancellationToken = default);
        Task<Guid?> ProcessCaseAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CalibrationRecord?> GetCalibrationAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<ActiveVolumeCaseBatchImportLight>> GetBatchImportsAsync(CancellationToken cancellationToken = default);
        Task<ActiveVolumeCaseBatchImport?> GetBatchImportAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> SaveBatchImportAsync(ActiveVolumeCaseBatchImport batchImport, CancellationToken cancellationToken = default);
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

        public async Task<ActiveVolumeCase?> GetCaseAsync(Guid id, bool includeChunks = false, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ActiveVolumeCase>($"ActiveVolumeCase/{id}?includeChunks={includeChunks.ToString().ToLowerInvariant()}", cancellationToken);
        }

        public async Task<bool> SaveCaseAsync(ActiveVolumeCase activeCase, CancellationToken cancellationToken = default)
        {
            bool isNew = activeCase.ID == Guid.Empty;
            activeCase.ID = isNew ? Guid.NewGuid() : activeCase.ID;
            HttpMethod method = isNew ? HttpMethod.Post : HttpMethod.Put;
            string methodRelativeUrl = isNew ? "ActiveVolumeCase" : $"ActiveVolumeCase/{activeCase.ID}";
            HttpResponseMessage? response = await SendJsonAsync(method, methodRelativeUrl, activeCase, cancellationToken);
            return response?.IsSuccessStatusCode == true;
        }

        public async Task<Guid?> ProcessCaseAsync(Guid id, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = await PostAsync($"ActiveVolumeCase/{id}/Process", cancellationToken);
            if (response?.IsSuccessStatusCode != true)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        }

        public async Task<CalibrationRecord?> GetCalibrationAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await GetAsync<CalibrationRecord>($"Calibration/{id}", cancellationToken);
        }

        public async Task<List<ActiveVolumeCaseBatchImportLight>> GetBatchImportsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<ActiveVolumeCaseBatchImportLight>>("ActiveVolumeCaseBatchImport/LightData", cancellationToken) ?? new List<ActiveVolumeCaseBatchImportLight>();
        }

        public async Task<ActiveVolumeCaseBatchImport?> GetBatchImportAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ActiveVolumeCaseBatchImport>($"ActiveVolumeCaseBatchImport/{id}", cancellationToken);
        }

        public async Task<bool> SaveBatchImportAsync(ActiveVolumeCaseBatchImport batchImport, CancellationToken cancellationToken = default)
        {
            if (batchImport.ID == Guid.Empty)
            {
                batchImport.ID = Guid.NewGuid();
            }

            HttpResponseMessage? response = await SendJsonAsync(HttpMethod.Post, "ActiveVolumeCaseBatchImport", batchImport, cancellationToken);
            return response?.IsSuccessStatusCode == true;
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

        private async Task<HttpResponseMessage?> SendJsonAsync<T>(HttpMethod method, string relativeUrl, T value, CancellationToken cancellationToken)
        {
            string? absoluteUrl = BuildAbsoluteUrl(relativeUrl);
            if (absoluteUrl is null)
            {
                return null;
            }

            try
            {
                using HttpRequestMessage request = new(method, absoluteUrl)
                {
                    Content = JsonContent.Create(value)
                };
                return await httpClient_.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private async Task<HttpResponseMessage?> PostAsync(string relativeUrl, CancellationToken cancellationToken)
        {
            string? absoluteUrl = BuildAbsoluteUrl(relativeUrl);
            if (absoluteUrl is null)
            {
                return null;
            }

            try
            {
                return await httpClient_.PostAsync(absoluteUrl, content: null, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private string? BuildAbsoluteUrl(string relativeUrl)
        {
            string hostUrl = configuration_.ActiveVolumeCalibrationHostURL.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                return null;
            }

            return $"{BuildCalibrationApiBaseUrl(hostUrl)}/{relativeUrl}";
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
