using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Server
{
    public sealed class OnlineCaseSpool
    {
        private const string CurrentCaseFileName = "current-case.json";
        private const string UploadStateFileName = "upload-state.json";
        private readonly string rootDirectory_;
        private readonly string chunksDirectory_;
        private readonly JsonSerializerOptions jsonOptions_ = new() { WriteIndented = true };
        private readonly List<ActiveVolumeSample> buffer_ = new();
        private readonly int chunkSize_;
        private int nextChunkIndex_;
        private int uploadedChunkIndex_ = -1;
        private DateTimeOffset lastFlushUtc_ = DateTimeOffset.UtcNow;

        public OnlineCaseSpool(string rootDirectory, int chunkSize)
        {
            rootDirectory_ = rootDirectory;
            chunksDirectory_ = Path.Combine(rootDirectory_, "chunks");
            chunkSize_ = Math.Max(1, chunkSize);
            Directory.CreateDirectory(chunksDirectory_);
            LoadUploadState();
            nextChunkIndex_ = ResolveNextChunkIndex();
        }

        public ActiveVolumeCase LoadOrCreateCase(ActiveVolumeCase template)
        {
            string path = Path.Combine(rootDirectory_, CurrentCaseFileName);
            if (File.Exists(path))
            {
                ActiveVolumeCase? existing = JsonSerializer.Deserialize<ActiveVolumeCase>(File.ReadAllText(path), jsonOptions_);
                if (existing is not null)
                {
                    return existing;
                }
            }

            SaveCase(template);
            return template;
        }

        public void SaveCase(ActiveVolumeCase activeCase)
        {
            Directory.CreateDirectory(rootDirectory_);
            File.WriteAllText(Path.Combine(rootDirectory_, CurrentCaseFileName), JsonSerializer.Serialize(activeCase, jsonOptions_));
        }

        public void AppendSample(Guid caseId, ActiveVolumeSample sample, TimeSpan flushInterval)
        {
            buffer_.Add(sample);
            if (buffer_.Count >= chunkSize_ || DateTimeOffset.UtcNow - lastFlushUtc_ >= flushInterval)
            {
                Flush(caseId);
            }
        }

        public void Flush(Guid caseId)
        {
            if (buffer_.Count == 0)
            {
                return;
            }

            ActiveVolumeCaseChunk chunk = new()
            {
                CaseID = caseId,
                ChunkIndex = nextChunkIndex_,
                Samples = buffer_.OrderBy(x => x.TimestampUtc).ToList()
            };
            chunk.StartTimeUtc = chunk.Samples.First().TimestampUtc;
            chunk.EndTimeUtc = chunk.Samples.Last().TimestampUtc;
            chunk.Checksum = ComputeChecksum(chunk.Samples);

            File.WriteAllText(GetChunkPath(chunk.ChunkIndex), JsonSerializer.Serialize(chunk, jsonOptions_));
            buffer_.Clear();
            nextChunkIndex_++;
            lastFlushUtc_ = DateTimeOffset.UtcNow;
        }

        public IEnumerable<ActiveVolumeCaseChunk> ReadPendingChunks()
        {
            foreach (string file in Directory.EnumerateFiles(chunksDirectory_, "*.json").OrderBy(x => x))
            {
                ActiveVolumeCaseChunk? chunk = JsonSerializer.Deserialize<ActiveVolumeCaseChunk>(File.ReadAllText(file), jsonOptions_);
                if (chunk is not null && chunk.ChunkIndex > uploadedChunkIndex_)
                {
                    yield return chunk;
                }
            }
        }

        public void MarkUploaded(int chunkIndex)
        {
            uploadedChunkIndex_ = Math.Max(uploadedChunkIndex_, chunkIndex);
            SaveUploadState();
        }

        private string GetChunkPath(int chunkIndex)
        {
            return Path.Combine(chunksDirectory_, $"{chunkIndex:D8}.json");
        }

        private int ResolveNextChunkIndex()
        {
            int max = Directory.EnumerateFiles(chunksDirectory_, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(x => int.TryParse(x, out int value) ? value : -1)
                .DefaultIfEmpty(-1)
                .Max();
            return max + 1;
        }

        private void LoadUploadState()
        {
            string path = Path.Combine(rootDirectory_, UploadStateFileName);
            if (!File.Exists(path))
            {
                return;
            }

            UploadState? state = JsonSerializer.Deserialize<UploadState>(File.ReadAllText(path), jsonOptions_);
            uploadedChunkIndex_ = state?.UploadedChunkIndex ?? -1;
        }

        private void SaveUploadState()
        {
            File.WriteAllText(
                Path.Combine(rootDirectory_, UploadStateFileName),
                JsonSerializer.Serialize(new UploadState { UploadedChunkIndex = uploadedChunkIndex_ }, jsonOptions_));
        }

        private static string ComputeChecksum(IReadOnlyList<ActiveVolumeSample> samples)
        {
            string json = JsonSerializer.Serialize(samples);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(hash);
        }

        private sealed class UploadState
        {
            public int UploadedChunkIndex { get; set; } = -1;
        }
    }
}
