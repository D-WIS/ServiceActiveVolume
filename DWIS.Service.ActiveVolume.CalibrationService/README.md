# DWIS.Service.ActiveVolume.CalibrationService

`DWIS.Service.ActiveVolume.CalibrationService` is the ActiveVolume background calibration REST API.

It stores online and historical cases in SQLite, accepts chunked time-series data, tracks calibration jobs, runs background processing, and serves calibration records back to the online worker and web UI.

## Responsibilities

- Store full and light ActiveVolume case data.
- Store chunked time-series samples for large historical or online cases.
- Store batch import definitions for groups of historical cases.
- Queue and execute calibration jobs with configurable concurrency.
- Persist generated calibration records.
- Return the best matching calibration for a requested context.

## Main Files

- `Program.cs`: ASP.NET Core host setup, Swagger, SQLite store, job queue, and background worker registration.
- `Controllers/ActiveVolumeCaseController.cs`: case CRUD, chunk endpoints, and case processing endpoint.
- `Controllers/ActiveVolumeCaseBatchImportController.cs`: batch import endpoints.
- `Controllers/CalibrationController.cs`: calibration list/detail and best-match endpoint.
- `Controllers/CalibrationJobController.cs`: calibration job status endpoint.
- `Managers/ActiveVolumeSqliteStore.cs`: SQLite persistence.
- `Managers/CalibrationJobQueue.cs`: in-process job queue.
- `Managers/CalibrationJobWorker.cs`: background calibration processor.
- `Managers/ActiveVolumeJson.cs`: JSON serialization options.
- `Dockerfile`: Linux container build.
- `charts/`: Helm chart for Kubernetes deployment.

## API Surface

The service exposes controller routes under the application root:

- `GET ActiveVolumeCase`
- `GET ActiveVolumeCase/LightData`
- `GET ActiveVolumeCase/{id}`
- `POST ActiveVolumeCase`
- `PUT ActiveVolumeCase/{id}`
- `GET ActiveVolumeCase/{id}/Chunks/ChunkCount`
- `GET ActiveVolumeCase/{id}/Chunks/{chunkIndex}`
- `PUT ActiveVolumeCase/{id}/Chunks/{chunkIndex}`
- `POST ActiveVolumeCase/{id}/Process`
- `GET ActiveVolumeCaseBatchImport`
- `GET ActiveVolumeCaseBatchImport/{id}`
- `POST ActiveVolumeCaseBatchImport`
- `GET Calibration`
- `GET Calibration/{id}`
- `POST Calibration/BestMatch`
- `GET CalibrationJob/{id}`

Swagger is enabled by the project and can be used during development to inspect the live API contract.

## Configuration

`appsettings.json` contains:

```json
{
  "BasePath": "/activevolume/api",
  "DatabasePath": "/home/activevolume-calibration.db",
  "MaxConcurrentCalibrationJobs": 2,
  "FieldHostURL": "http://norcedrillingfieldservice/",
  "ClusterHostURL": "http://norcedrillingclusterservice/",
  "WellHostURL": "http://norcedrillingwellservice/",
  "WellBoreHostURL": "http://norcedrillingwellboreservice/",
  "WellBoreArchitectureHostURL": "http://norcedrillingwellborearchitectureservice/",
  "DrillStringHostURL": "http://norcedrillingdrillstringservice/"
}
```

Environment variable equivalents:

```text
BasePath=/activevolume/api
DatabasePath=/home/activevolume-calibration.db
MaxConcurrentCalibrationJobs=2
FieldHostURL=http://norcedrillingfieldservice/
DrillStringHostURL=http://norcedrillingdrillstringservice/
```

Mount `/home` in containers so the SQLite database survives restarts.

The context service URLs mirror the services represented in `DWIS.Service.ActiveVolume.ModelSharedIn`: field, cluster, well, wellbore, wellbore architecture, and drill-string.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj
dotnet run --project DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.CalibrationService/Dockerfile --tag digiwells/norcedrillingactivevolumecalibrationservice:stable .
docker run --rm -p 5000:8080 -v c:/Volumes/DWISActiveVolumeCalibration:/home digiwells/norcedrillingactivevolumecalibrationservice:stable
```

## Kubernetes

A Helm chart is available at:

```text
DWIS.Service.ActiveVolume.CalibrationService/charts/norcedrillingactivevolumecalibrationservice
```

Render it with:

```bash
helm template activevolume-calibration DWIS.Service.ActiveVolume.CalibrationService/charts/norcedrillingactivevolumecalibrationservice
```

The chart includes a persistent volume claim mounted at `/home`.

## Dependencies

- `DWIS.Service.ActiveVolume.Model`
- `Microsoft.Data.Sqlite`
- `Swashbuckle.AspNetCore`
