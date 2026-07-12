# DWIS.Service.ActiveVolume.DataSource

`DWIS.Service.ActiveVolume.DataSource` is a development worker that publishes synthetic ActiveVolume input signals to the DWIS blackboard.

It is useful for local demonstrations, smoke tests, and validating the online worker without connecting to a real rig data source.

## Responsibilities

- Connect to the DWIS blackboard.
- Register writable `RealtimeInputsData` variables.
- Generate synthetic active volume, inlet flow, return-flow proxy, cuttings recovery, and depth-related signals.
- Publish samples on the worker loop cadence.

## Runtime Flow

```text
DataSource -> Blackboard -> Server -> DataSink
```

The service does not run calibration or fusion logic. It only produces inputs for `DWIS.Service.ActiveVolume.Server`.

## Main Files

- `Program.cs`: generic worker host setup.
- `Worker.cs`: blackboard connection, signal generation, and publishing loop.
- `appsettings*.json`: logging configuration.
- `Dockerfile`: Linux container build.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.DataSource/Dockerfile --tag dwis-service-activevolume-datasource:local .
docker run --rm dwis-service-activevolume-datasource:local
```

## Dependencies

- `DWIS.Service.ActiveVolume.Model`
- DWIS worker and OPC UA client packages
- a reachable DWIS blackboard

## Notes

This is not a production sensor adapter. Production ingestion should be implemented by the appropriate rig data bridge or blackboard publisher.
