# DWIS Service Active Volume

`DWIS.Service.ActiveVolume` provides the ActiveVolume online fusion, historical calibration, and inspection tooling for DWIS blackboard environments.

The solution is organized around three runtime surfaces:

- a realtime worker that reads drilling signals from the blackboard, runs the shared active-volume model, publishes fused values, and spools live data durably;
- a calibration REST API that stores historical and online cases in SQLite, processes calibration jobs, and serves best-match calibration records;
- a Blazor web app, backed by reusable web pages, for importing historical cases and inspecting cases, jobs, and calibrations.

All internal engineering values are represented in SI units. Imported spatial/contextual data is expected to be normalized to WGS84 at the import boundary.

## Projects

| Project | Role |
| --- | --- |
| `DWIS.Service.ActiveVolume.Model` | Shared domain model, DTOs, calibration records, import definitions, online fusion engine, and geometry calculators. |
| `DWIS.Service.ActiveVolume.Server` | Realtime online worker connected to the DWIS blackboard and the CalibrationService. |
| `DWIS.Service.ActiveVolume.CalibrationService` | ASP.NET Core REST API backed by SQLite for cases, chunks, batch imports, jobs, and calibration records. |
| `DWIS.Service.ActiveVolume.WebPages` | Reusable Razor class library with ActiveVolume pages and API helpers; packaged as a NuGet. |
| `DWIS.Service.ActiveVolume.WebApp` | Thin Blazor Server host for the reusable WebPages library. |
| `DWIS.Service.ActiveVolume.ModelSharedIn` | Generated merged OpenAPI client/DTO project for upstream context services used by the online server. |
| `DWIS.Service.ActiveVolume.ModelSharedOut` | Generated merged OpenAPI client/DTO project for client applications. |
| `DWIS.Service.ActiveVolume.DataSource` | Development worker that publishes synthetic ActiveVolume inputs to the blackboard. |
| `DWIS.Service.ActiveVolume.DataSink` | Development worker that reads and logs fused ActiveVolume outputs from the blackboard. |

## Runtime Flow

```text
DataSource -> Blackboard -> Server -> Blackboard outputs
                         \-> local online spool -> CalibrationService -> SQLite
                                                       ^
                                                       |
                                                  WebApp/WebPages
```

The online server owns one active online case. It persists realtime samples to an append-only local spool before uploading chunks to the CalibrationService, which minimizes data loss across process restarts.

## Core Data

Realtime inputs include:

- active pit volume;
- inlet flow;
- flow-paddle return indication and/or Coriolis return-flow measurement;
- cuttings recovery rate;
- bit or bottom-of-string depth;
- bottom-hole depth;
- optional return mud density and standpipe pressure;
- context IDs for field, cluster, well, wellbore, wellbore architecture, and drill-string.

Historical cases use chunked time-series storage so large imported datasets can be uploaded, stored, and processed incrementally. Each case carries context IDs so calibrations can later be matched against comparable field, well, wellbore architecture, and drill-string conditions.

## Build

```bash
dotnet build DWIS.Service.ActiveVolume.sln
```

## Run Locally

Start the Calibration REST API:

```bash
dotnet run --project DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj
```

Start the WebApp:

```bash
dotnet run --project DWIS.Service.ActiveVolume.WebApp/DWIS.Service.ActiveVolume.WebApp.csproj
```

Start the Blackboard development services in separate terminals:

```bash
dotnet run --project DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
dotnet run --project DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
```

## Docker

Dockerfiles are provided for:

- `DWIS.Service.ActiveVolume.Server`
- `DWIS.Service.ActiveVolume.CalibrationService`
- `DWIS.Service.ActiveVolume.WebApp`
- `DWIS.Service.ActiveVolume.DataSource`
- `DWIS.Service.ActiveVolume.DataSink`

Use mounted volumes for `/home` so the online spool and SQLite calibration database persist across restarts.

The GitHub Actions Docker workflow publishes:

- `digiwells/dwisserviceactivevolumeserver:stable`
- `digiwells/norcedrillingactivevolumecalibrationservice:stable`
- `digiwells/norcedrillingactivevolumewebappclient:stable`

## Kubernetes

Helm charts are provided for the deployable REST API and WebApp:

- `DWIS.Service.ActiveVolume.CalibrationService/charts/norcedrillingactivevolumecalibrationservice`
- `DWIS.Service.ActiveVolume.WebApp/charts/norcedrillingactivevolumewebappclient`

Render them with:

```bash
helm template activevolume-calibration DWIS.Service.ActiveVolume.CalibrationService/charts/norcedrillingactivevolumecalibrationservice
helm template activevolume-webapp DWIS.Service.ActiveVolume.WebApp/charts/norcedrillingactivevolumewebappclient
```

The CalibrationService chart includes a persistent volume claim mounted at `/home` for SQLite storage. The WebApp chart exposes endpoint URLs through chart values under `env`.

## Publishing

GitHub Actions workflows are available for release artifacts:

- `.github/workflows/docker-build-push.yml`
  - Builds and pushes the Server, CalibrationService, and WebApp Docker images to DockerHub.
  - Requires repository secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_PASSWORD`.
- `.github/workflows/publish-webpages-nuget.yml`
  - Packs `DWIS.Service.ActiveVolume.WebPages` and publishes it to NuGet.org.
  - Requires repository secret `NUGET_API_KEY`.
  - Can be run manually with a version input or by pushing a tag like `webpages-v1.0.0`.

## Project READMEs

Each project folder contains a project-specific README with runtime, configuration, and build details.
