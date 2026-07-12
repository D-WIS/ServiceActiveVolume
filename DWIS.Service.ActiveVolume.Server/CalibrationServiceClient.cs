using System.Net.Http.Json;
using DWIS.Service.ActiveVolume.Model.Case;
using Microsoft.Extensions.Options;

namespace DWIS.Service.ActiveVolume.Server
{
    public sealed class CalibrationServiceClient
    {
        private readonly HttpClient httpClient_;
        private readonly ActiveVolumeOnlineOptions options_;

        public CalibrationServiceClient(HttpClient httpClient, IOptions<ActiveVolumeOnlineOptions> options)
        {
            httpClient_ = httpClient;
            options_ = options.Value;
        }

        public async Task EnsureCaseAsync(ActiveVolumeCase activeCase, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await httpClient_.PostAsJsonAsync(BuildUrl("ActiveVolumeCase"), activeCase, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task UploadChunkAsync(ActiveVolumeCaseChunk chunk, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await httpClient_.PutAsJsonAsync(
                BuildUrl($"ActiveVolumeCase/{chunk.CaseID}/Chunks/{chunk.ChunkIndex}"),
                chunk,
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<Guid?> RequestProcessingAsync(Guid caseId, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await httpClient_.PostAsync(BuildUrl($"ActiveVolumeCase/{caseId}/Process"), null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: cancellationToken);
        }

        private string BuildUrl(string relativeUrl)
        {
            return options_.CalibrationServiceUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
        }
    }
}
