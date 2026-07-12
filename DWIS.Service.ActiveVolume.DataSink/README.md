# DWIS.Service.ActiveVolume.DataSink

`DWIS.Service.ActiveVolume.DataSink` is a development worker that reads fused ActiveVolume outputs from the DWIS blackboard and logs them.

It is a passive observer used for local validation of the `DWIS.Service.ActiveVolume.Server` worker.

## Responsibilities

- Connect to the DWIS blackboard.
- Register read queries for `RealtimeOutputsData`.
- Read corrected ActiveVolume outputs on the worker loop cadence.
- Log available values for diagnostics.

## Runtime Flow

```text
DataSource -> Blackboard -> Server -> Blackboard -> DataSink
```

The service does not publish data and does not call the CalibrationService.

## Consumed Outputs

From `RealtimeOutputsData`:

- corrected active volume;
- estimated pit-volume flow bias;
- return-flow capacity scale.

## Main Files

- `Program.cs`: generic worker host setup.
- `Worker.cs`: blackboard connection, output queries, and logging loop.
- `appsettings*.json`: logging configuration.
- `Dockerfile`: Linux container build.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.DataSink/Dockerfile --tag dwis-service-activevolume-datasink:local .
docker run --rm dwis-service-activevolume-datasink:local
```

## Dependencies

- `DWIS.Service.ActiveVolume.Model`
- DWIS worker and OPC UA client packages
- a reachable DWIS blackboard with ActiveVolume outputs

## Notes

Missing output values are tolerated. The worker logs only the values present in the latest blackboard read.
