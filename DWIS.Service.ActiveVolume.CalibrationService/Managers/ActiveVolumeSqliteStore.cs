using System.Text.Json;
using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;
using DWIS.Service.ActiveVolume.Model.Import;
using Microsoft.Data.Sqlite;

namespace DWIS.Service.ActiveVolume.CalibrationService.Managers
{
    public sealed class ActiveVolumeSqliteStore
    {
        private readonly ILogger<ActiveVolumeSqliteStore> logger_;
        private readonly string databasePath_;
        private readonly object lock_ = new();

        public ActiveVolumeSqliteStore(IConfiguration configuration, ILogger<ActiveVolumeSqliteStore> logger)
        {
            logger_ = logger;
            databasePath_ = configuration["DatabasePath"] ?? "/home/activevolume-calibration.db";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath_)) ?? ".");
            Initialize();
        }

        public List<Guid> GetAllCaseIds()
        {
            return QueryIds("SELECT ID FROM ActiveVolumeCaseTable ORDER BY LastModificationDate DESC");
        }

        public List<ActiveVolumeCaseLight> GetAllCaseLight()
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT CaseJson FROM ActiveVolumeCaseTable ORDER BY LastModificationDate DESC";
                using SqliteDataReader reader = command.ExecuteReader();
                List<ActiveVolumeCaseLight> result = new();
                while (reader.Read())
                {
                    ActiveVolumeCase? data = Deserialize<ActiveVolumeCase>(reader.GetString(0));
                    if (data is not null)
                    {
                        result.Add(data.Light);
                    }
                }

                return result;
            }
        }

        public ActiveVolumeCase? GetCase(Guid id, bool includeChunks = false)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                ActiveVolumeCase? data = GetCase(connection, id);
                if (data is not null && includeChunks)
                {
                    data.Chunks = GetChunks(connection, id);
                }

                return data;
            }
        }

        public bool UpsertCase(ActiveVolumeCase data)
        {
            lock (lock_)
            {
                data.LastModificationDate = DateTimeOffset.UtcNow;
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO ActiveVolumeCaseTable
                    (ID, FieldID, ClusterID, WellID, WellBoreID, WellBoreArchitectureID, DrillStringID, ReturnFlowMeasurementMode, State, Progress, LastModificationDate, CaseJson)
                    VALUES ($id, $fieldId, $clusterId, $wellId, $wellBoreId, $architectureId, $drillStringId, $mode, $state, $progress, $lastModificationDate, $json)
                    ON CONFLICT(ID) DO UPDATE SET
                        FieldID = excluded.FieldID,
                        ClusterID = excluded.ClusterID,
                        WellID = excluded.WellID,
                        WellBoreID = excluded.WellBoreID,
                        WellBoreArchitectureID = excluded.WellBoreArchitectureID,
                        DrillStringID = excluded.DrillStringID,
                        ReturnFlowMeasurementMode = excluded.ReturnFlowMeasurementMode,
                        State = excluded.State,
                        Progress = excluded.Progress,
                        LastModificationDate = excluded.LastModificationDate,
                        CaseJson = excluded.CaseJson
                    """;
                AddCaseParameters(command, data);
                return command.ExecuteNonQuery() > 0;
            }
        }

        public ChunkSaveResult SaveChunk(Guid caseId, ActiveVolumeCaseChunk chunk)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                ActiveVolumeCase? data = GetCase(connection, caseId);
                if (data is null)
                {
                    return ChunkSaveResult.CaseNotFound;
                }

                chunk.CaseID = caseId;
                chunk.Samples = chunk.Samples.OrderBy(x => x.TimestampUtc).ToList();
                if (chunk.Samples.Count > 0)
                {
                    chunk.StartTimeUtc = chunk.Samples.First().TimestampUtc;
                    chunk.EndTimeUtc = chunk.Samples.Last().TimestampUtc;
                }

                using SqliteCommand existing = connection.CreateCommand();
                existing.CommandText = "SELECT Checksum FROM ActiveVolumeCaseChunkTable WHERE CaseID = $caseId AND ChunkIndex = $chunkIndex";
                existing.Parameters.AddWithValue("$caseId", caseId.ToString());
                existing.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
                object? existingChecksum = existing.ExecuteScalar();
                if (existingChecksum is string storedChecksum)
                {
                    return storedChecksum == chunk.Checksum ? ChunkSaveResult.AlreadyStored : ChunkSaveResult.Conflict;
                }

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO ActiveVolumeCaseChunkTable
                    (CaseID, ChunkIndex, StartTimeUtc, EndTimeUtc, SampleCount, Checksum, ChunkJson)
                    VALUES ($caseId, $chunkIndex, $startTimeUtc, $endTimeUtc, $sampleCount, $checksum, $json)
                    """;
                command.Parameters.AddWithValue("$caseId", caseId.ToString());
                command.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
                command.Parameters.AddWithValue("$startTimeUtc", chunk.StartTimeUtc.ToString("O"));
                command.Parameters.AddWithValue("$endTimeUtc", chunk.EndTimeUtc.ToString("O"));
                command.Parameters.AddWithValue("$sampleCount", chunk.Samples.Count);
                command.Parameters.AddWithValue("$checksum", chunk.Checksum);
                command.Parameters.AddWithValue("$json", Serialize(chunk));
                command.ExecuteNonQuery();

                data.ProcessingState = ActiveVolumeCaseProcessingState.Uploading;
                data.ChunkCount = Math.Max(data.ChunkCount, chunk.ChunkIndex + 1);
                data.SampleCount += chunk.Samples.Count;
                UpsertCase(data);
                return ChunkSaveResult.Stored;
            }
        }

        public int GetChunkCount(Guid caseId)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM ActiveVolumeCaseChunkTable WHERE CaseID = $caseId";
                command.Parameters.AddWithValue("$caseId", caseId.ToString());
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public ActiveVolumeCaseChunk? GetChunk(Guid caseId, int chunkIndex)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT ChunkJson FROM ActiveVolumeCaseChunkTable WHERE CaseID = $caseId AND ChunkIndex = $chunkIndex";
                command.Parameters.AddWithValue("$caseId", caseId.ToString());
                command.Parameters.AddWithValue("$chunkIndex", chunkIndex);
                object? result = command.ExecuteScalar();
                return result is string json ? Deserialize<ActiveVolumeCaseChunk>(json) : null;
            }
        }

        public List<ActiveVolumeCaseChunk> GetChunks(Guid caseId)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                return GetChunks(connection, caseId);
            }
        }

        public CalibrationJob CreateJob(Guid caseId)
        {
            lock (lock_)
            {
                CalibrationJob job = new() { CaseID = caseId };
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO CalibrationJobTable (ID, CaseID, State, Progress, Message, CreatedUtc, JobJson)
                    VALUES ($id, $caseId, $state, $progress, $message, $createdUtc, $json)
                    """;
                command.Parameters.AddWithValue("$id", job.ID.ToString());
                command.Parameters.AddWithValue("$caseId", job.CaseID.ToString());
                command.Parameters.AddWithValue("$state", job.State.ToString());
                command.Parameters.AddWithValue("$progress", job.Progress);
                command.Parameters.AddWithValue("$message", job.Message);
                command.Parameters.AddWithValue("$createdUtc", job.CreatedUtc.ToString("O"));
                command.Parameters.AddWithValue("$json", Serialize(job));
                command.ExecuteNonQuery();

                ActiveVolumeCase? data = GetCase(connection, caseId);
                if (data is not null)
                {
                    data.ActiveJobID = job.ID;
                    data.ProcessingState = ActiveVolumeCaseProcessingState.Queued;
                    data.ProcessingProgress = 0.0;
                    data.ProcessingMessage = "Queued for background calibration.";
                    UpsertCase(data);
                }

                return job;
            }
        }

        public CalibrationJob? GetJob(Guid jobId)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT JobJson FROM CalibrationJobTable WHERE ID = $id";
                command.Parameters.AddWithValue("$id", jobId.ToString());
                object? result = command.ExecuteScalar();
                return result is string json ? Deserialize<CalibrationJob>(json) : null;
            }
        }

        public void UpdateJob(CalibrationJob job)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    UPDATE CalibrationJobTable
                    SET State = $state, Progress = $progress, Message = $message, JobJson = $json
                    WHERE ID = $id
                    """;
                command.Parameters.AddWithValue("$id", job.ID.ToString());
                command.Parameters.AddWithValue("$state", job.State.ToString());
                command.Parameters.AddWithValue("$progress", job.Progress);
                command.Parameters.AddWithValue("$message", job.Message);
                command.Parameters.AddWithValue("$json", Serialize(job));
                command.ExecuteNonQuery();

                ActiveVolumeCase? data = GetCase(connection, job.CaseID);
                if (data is not null)
                {
                    data.ActiveJobID = job.ID;
                    data.ProcessingProgress = job.Progress;
                    data.ProcessingMessage = job.Message;
                    data.ProcessingState = job.State switch
                    {
                        CalibrationJobState.Queued => ActiveVolumeCaseProcessingState.Queued,
                        CalibrationJobState.Running => ActiveVolumeCaseProcessingState.Processing,
                        CalibrationJobState.Succeeded => ActiveVolumeCaseProcessingState.Processed,
                        CalibrationJobState.Failed => ActiveVolumeCaseProcessingState.Failed,
                        _ => data.ProcessingState
                    };
                    data.LastProcessedChunkIndex = Math.Max(data.LastProcessedChunkIndex, (int)Math.Floor(job.Progress * Math.Max(data.ChunkCount - 1, 0)));
                    data.LastCalibrationDate = job.CompletedUtc;
                    data.BestCalibrationID = job.CalibrationRecordID ?? data.BestCalibrationID;
                    UpsertCase(data);
                }
            }
        }

        public void SaveCalibration(CalibrationRecord calibration)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO CalibrationRecordTable (ID, SourceCaseID, FieldID, ClusterID, WellID, WellBoreID, WellBoreArchitectureID, DrillStringID, Mode, Quality, CreationDate, CalibrationJson)
                    VALUES ($id, $sourceCaseId, $fieldId, $clusterId, $wellId, $wellBoreId, $architectureId, $drillStringId, $mode, $quality, $creationDate, $json)
                    ON CONFLICT(ID) DO UPDATE SET CalibrationJson = excluded.CalibrationJson
                    """;
                command.Parameters.AddWithValue("$id", calibration.ID.ToString());
                command.Parameters.AddWithValue("$sourceCaseId", calibration.SourceCaseID?.ToString() ?? string.Empty);
                command.Parameters.AddWithValue("$fieldId", calibration.Context.FieldID.ToString());
                command.Parameters.AddWithValue("$clusterId", calibration.Context.ClusterID.ToString());
                command.Parameters.AddWithValue("$wellId", calibration.Context.WellID.ToString());
                command.Parameters.AddWithValue("$wellBoreId", calibration.Context.WellBoreID.ToString());
                command.Parameters.AddWithValue("$architectureId", calibration.Context.WellBoreArchitectureID.ToString());
                command.Parameters.AddWithValue("$drillStringId", calibration.Context.DrillStringID.ToString());
                command.Parameters.AddWithValue("$mode", calibration.Context.ReturnFlowMeasurementMode.ToString());
                command.Parameters.AddWithValue("$quality", calibration.Quality);
                command.Parameters.AddWithValue("$creationDate", calibration.CreationDate.ToString("O"));
                command.Parameters.AddWithValue("$json", Serialize(calibration));
                command.ExecuteNonQuery();
            }
        }

        public CalibrationRecord? GetCalibration(Guid id)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT CalibrationJson FROM CalibrationRecordTable WHERE ID = $id";
                command.Parameters.AddWithValue("$id", id.ToString());
                object? result = command.ExecuteScalar();
                return result is string json ? Deserialize<CalibrationRecord>(json) : null;
            }
        }

        public List<CalibrationRecord> GetAllCalibrations()
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT CalibrationJson FROM CalibrationRecordTable ORDER BY CreationDate DESC";
                using SqliteDataReader reader = command.ExecuteReader();
                List<CalibrationRecord> result = new();
                while (reader.Read())
                {
                    CalibrationRecord? data = Deserialize<CalibrationRecord>(reader.GetString(0));
                    if (data is not null)
                    {
                        result.Add(data);
                    }
                }

                return result;
            }
        }

        public void SaveBatchImport(ActiveVolumeCaseBatchImport batch)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO ActiveVolumeCaseBatchImportTable (ID, State, Progress, BatchJson)
                    VALUES ($id, $state, $progress, $json)
                    ON CONFLICT(ID) DO UPDATE SET State = excluded.State, Progress = excluded.Progress, BatchJson = excluded.BatchJson
                    """;
                command.Parameters.AddWithValue("$id", batch.ID.ToString());
                command.Parameters.AddWithValue("$state", batch.ProcessingState.ToString());
                command.Parameters.AddWithValue("$progress", batch.ProcessingProgress);
                command.Parameters.AddWithValue("$json", Serialize(batch));
                command.ExecuteNonQuery();
            }
        }

        public ActiveVolumeCaseBatchImport? GetBatchImport(Guid id)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT BatchJson FROM ActiveVolumeCaseBatchImportTable WHERE ID = $id";
                command.Parameters.AddWithValue("$id", id.ToString());
                object? result = command.ExecuteScalar();
                return result is string json ? Deserialize<ActiveVolumeCaseBatchImport>(json) : null;
            }
        }

        public List<ActiveVolumeCaseBatchImport> GetAllBatchImports()
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT BatchJson FROM ActiveVolumeCaseBatchImportTable ORDER BY ID DESC";
                using SqliteDataReader reader = command.ExecuteReader();
                List<ActiveVolumeCaseBatchImport> result = new();
                while (reader.Read())
                {
                    ActiveVolumeCaseBatchImport? data = Deserialize<ActiveVolumeCaseBatchImport>(reader.GetString(0));
                    if (data is not null)
                    {
                        result.Add(data);
                    }
                }

                return result;
            }
        }

        private void Initialize()
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                ExecuteNonQuery(connection,
                    """
                    CREATE TABLE IF NOT EXISTS ActiveVolumeCaseTable (
                        ID TEXT PRIMARY KEY,
                        FieldID TEXT,
                        ClusterID TEXT,
                        WellID TEXT,
                        WellBoreID TEXT,
                        WellBoreArchitectureID TEXT,
                        DrillStringID TEXT,
                        ReturnFlowMeasurementMode TEXT,
                        State TEXT,
                        Progress REAL,
                        LastModificationDate TEXT,
                        CaseJson TEXT NOT NULL
                    )
                    """);
                ExecuteNonQuery(connection,
                    """
                    CREATE TABLE IF NOT EXISTS ActiveVolumeCaseChunkTable (
                        CaseID TEXT NOT NULL,
                        ChunkIndex INTEGER NOT NULL,
                        StartTimeUtc TEXT,
                        EndTimeUtc TEXT,
                        SampleCount INTEGER,
                        Checksum TEXT,
                        ChunkJson TEXT NOT NULL,
                        PRIMARY KEY (CaseID, ChunkIndex)
                    )
                    """);
                ExecuteNonQuery(connection,
                    """
                    CREATE TABLE IF NOT EXISTS CalibrationRecordTable (
                        ID TEXT PRIMARY KEY,
                        SourceCaseID TEXT,
                        FieldID TEXT,
                        ClusterID TEXT,
                        WellID TEXT,
                        WellBoreID TEXT,
                        WellBoreArchitectureID TEXT,
                        DrillStringID TEXT,
                        Mode TEXT,
                        Quality REAL,
                        CreationDate TEXT,
                        CalibrationJson TEXT NOT NULL
                    )
                    """);
                ExecuteNonQuery(connection,
                    """
                    CREATE TABLE IF NOT EXISTS CalibrationJobTable (
                        ID TEXT PRIMARY KEY,
                        CaseID TEXT NOT NULL,
                        State TEXT,
                        Progress REAL,
                        Message TEXT,
                        CreatedUtc TEXT,
                        JobJson TEXT NOT NULL
                    )
                    """);
                ExecuteNonQuery(connection,
                    """
                    CREATE TABLE IF NOT EXISTS ActiveVolumeCaseBatchImportTable (
                        ID TEXT PRIMARY KEY,
                        State TEXT,
                        Progress REAL,
                        BatchJson TEXT NOT NULL
                    )
                    """);
            }
        }

        private SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new($"Data Source={databasePath_}");
            connection.Open();
            return connection;
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private ActiveVolumeCase? GetCase(SqliteConnection connection, Guid id)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT CaseJson FROM ActiveVolumeCaseTable WHERE ID = $id";
            command.Parameters.AddWithValue("$id", id.ToString());
            object? result = command.ExecuteScalar();
            return result is string json ? Deserialize<ActiveVolumeCase>(json) : null;
        }

        private static List<ActiveVolumeCaseChunk> GetChunks(SqliteConnection connection, Guid caseId)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ChunkJson FROM ActiveVolumeCaseChunkTable WHERE CaseID = $caseId ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("$caseId", caseId.ToString());
            using SqliteDataReader reader = command.ExecuteReader();
            List<ActiveVolumeCaseChunk> chunks = new();
            while (reader.Read())
            {
                ActiveVolumeCaseChunk? chunk = Deserialize<ActiveVolumeCaseChunk>(reader.GetString(0));
                if (chunk is not null)
                {
                    chunks.Add(chunk);
                }
            }

            return chunks;
        }

        private static void AddCaseParameters(SqliteCommand command, ActiveVolumeCase data)
        {
            command.Parameters.AddWithValue("$id", data.ID.ToString());
            command.Parameters.AddWithValue("$fieldId", data.FieldID.ToString());
            command.Parameters.AddWithValue("$clusterId", data.ClusterID.ToString());
            command.Parameters.AddWithValue("$wellId", data.WellID.ToString());
            command.Parameters.AddWithValue("$wellBoreId", data.WellBoreID.ToString());
            command.Parameters.AddWithValue("$architectureId", data.WellBoreArchitectureID.ToString());
            command.Parameters.AddWithValue("$drillStringId", data.DrillStringID.ToString());
            command.Parameters.AddWithValue("$mode", data.ReturnFlowMeasurementMode.ToString());
            command.Parameters.AddWithValue("$state", data.ProcessingState.ToString());
            command.Parameters.AddWithValue("$progress", data.ProcessingProgress);
            command.Parameters.AddWithValue("$lastModificationDate", data.LastModificationDate.ToString("O"));
            command.Parameters.AddWithValue("$json", Serialize(data));
        }

        private List<Guid> QueryIds(string sql)
        {
            lock (lock_)
            {
                using SqliteConnection connection = OpenConnection();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = sql;
                using SqliteDataReader reader = command.ExecuteReader();
                List<Guid> result = new();
                while (reader.Read())
                {
                    if (Guid.TryParse(reader.GetString(0), out Guid id))
                    {
                        result.Add(id);
                    }
                }

                return result;
            }
        }

        private static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, ActiveVolumeJson.Options);
        }

        private static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, ActiveVolumeJson.Options);
        }
    }

    public enum ChunkSaveResult
    {
        Stored,
        AlreadyStored,
        Conflict,
        CaseNotFound
    }
}
