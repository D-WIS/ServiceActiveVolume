# DWIS.Service.ActiveVolume.DataSink

`DWIS.Service.ActiveVolume.DataSink` is a .NET 8 worker service that subscribes to fused active-volume outputs on the DWIS blackboard and logs them.

## Purpose

This project is the output consumer in the active-volume pipeline. It does not run fusion logic; it observes and reports the outputs produced by `DWIS.Service.ActiveVolume.Server`.

## Project Role in the Solution

- `DWIS.Service.ActiveVolume.DataSource`: publishes synthetic input signals.
- `DWIS.Service.ActiveVolume.Server`: computes fused outputs.
- `DWIS.Service.ActiveVolume.DataSink`: reads and logs fused outputs.

## Main Components

- `Program.cs`: generic host bootstrap and registration of `Worker`.
- `Worker.cs`: blackboard query registration, periodic read loop, and output logging.
- `config/Quickstarts.ReferenceClient.Config.xml`: OPC UA client configuration used by the DWIS client stack.
- `appsettings*.json`: logging settings.

## Runtime Flow

At startup (`Worker.ExecuteAsync`):

1. Connect to blackboard (`ConnectToBlackboard`).
2. Register read queries for `RealtimeOutputsData`.
3. Enter periodic loop (`LoopSpan`).

Each loop iteration (`Worker.Loop`):

1. Read `RealtimeOutputsData` from blackboard.
2. Log any available output values.

This service does not publish data to blackboard.

## Consumed Signals

From `RealtimeOutputsData`:

- `CorrectedActiveVolume` (`m^3`)
- `EstimatedPitVolumeFlowBias` (`m^3/s`)
- `ReturnFlowCapacityScale` (`m^3/s`)

## Logging Behavior

At `Information` level, DataSink logs:

- corrected active volume (`m^3`)
- estimated pit volume flow bias (`L/min`, converted from `m^3/s`)
- return flow capacity scale (`L/min`, converted from `m^3/s`)

Each value is logged only if present in the latest blackboard read.

## Build and Run

### Local

```bash
dotnet build DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSink/DWIS.Service.ActiveVolume.DataSink.csproj
```

### Container

A multi-stage Dockerfile is included. Runtime entrypoint:

```bash
dotnet DWIS.Service.ActiveVolume.DataSink.dll
```

## Dependencies

- DWIS worker runtime (`DWIS.RigOS.Common.Worker` via project references)
- DWIS OPC Foundation client integration (`DWIS.Client.ReferenceImplementation.OPCFoundation`)
- Model project reference: `DWIS.Service.ActiveVolume.Model` (for `RealtimeOutputsData` contract)

## Operational Notes

- DataSink starts reading only after successful blackboard connection.
- It is a passive observer intended for validation, diagnostics, and demonstration.
- Missing values are handled gracefully (no log line for absent fields).
