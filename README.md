# DWIS Service Active Volume

This solution provides an end-to-end active-volume processing pipeline for DWIS blackboard environments.

It includes:

- a **data source** that publishes synthetic realtime inputs
- a **fusion server** that runs the active-volume EKF
- a **data sink** that reads and logs fused outputs
- a shared **model** project that defines contracts and sensor-fusion logic

## Solution Projects

- `DWIS.Service.ActiveVolume.Model`
  - Shared contracts and logic.
  - Contains `RealtimeInputsData`, `RealtimeOutputsData`, `ConfigurationForActiveVolume`, and `SensorFusion`.
- `DWIS.Service.ActiveVolume.Server`
  - Main processing service.
  - Reads input signals, executes fusion, publishes outputs.
- `DWIS.Service.ActiveVolume.DataSource`
  - Synthetic producer.
  - Publishes test input signals to the blackboard.
- `DWIS.Service.ActiveVolume.DataSink`
  - Consumer/observer.
  - Reads fused outputs and logs them.

## Data Flow

```text
DataSource  -->  Blackboard Inputs  -->  Server (EKF Fusion)  -->  Blackboard Outputs  -->  DataSink
```

## Core Signals

Inputs (published by DataSource / read by Server):

- active pit volume (`m^3`)
- inlet flow rate (`m^3/s`)
- shaker load estimates (dimensionless return proxy)
- cuttings recovery rates (`m^3/s`)

Outputs (published by Server / read by DataSink):

- corrected active volume (`m^3`)
- estimated pit volume flow bias (`m^3/s`)
- return flow capacity scale (`m^3/s`)

## Build

```bash
dotnet build DWIS.Service.ActiveVolume.sln
```

## Run (Local)

Start services in separate terminals, typically in this order:

1. DataSource
2. Server
3. DataSink

```bash
dotnet run --project DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
dotnet run --project DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
```

## Configuration

- Each runtime project has `appsettings.json` and `appsettings.Development.json`.
- OPC UA / client connection setup is under each project `config/Quickstarts.ReferenceClient.Config.xml`.
- EKF tuning and numerical settings are exposed via `ConfigurationForActiveVolume` in the model project and consumed by Server.

### Realtime Input/Output Dumping (Server)

`DWIS.Service.ActiveVolume.Server` now supports periodic dumping of realtime input/output snapshots to JSON files.

- Default dump directory: `/home` (shared Docker volume location)
- Default dump interval: `01:00:00` (every plain UTC hour)
- In-memory samples are reset after each successful dump to prevent memory growth.

Configuration keys (in the same runtime configuration source used by Server, e.g. `home/config.json`):

- `EnableRealtimeDataDump` (`true`/`false`, default `true`)
- `RealtimeDataDumpDirectory` (default `"/home"`)
- `RealtimeDataDumpInterval` (`TimeSpan`, default `"01:00:00"`)

## Containers

Each runtime project (`DataSource`, `Server`, `DataSink`) includes a Dockerfile for containerized execution.

## Documentation

Detailed project-level documentation is available in:

- `DWIS.Service.ActiveVolume.Model/README.md`
- `DWIS.Service.ActiveVolume.Server/README.md`
- `DWIS.Service.ActiveVolume.DataSource/README.md`
- `DWIS.Service.ActiveVolume.DataSink/README.md`
