# DWIS.Service.ActiveVolume.DataSource

`DWIS.Service.ActiveVolume.DataSource` is a .NET 8 worker service that publishes synthetic realtime input signals for the active-volume workflow to the DWIS blackboard.

## Purpose

This project is a signal generator used to drive and test downstream services (notably `DWIS.Service.ActiveVolume.Server`).

It continuously publishes `RealtimeInputsData` with internally generated values for:

- active pit volume
- inlet flow rate
- shaker load estimates (return proportion proxy)
- cuttings recovery rates

## Project Role in the Solution

- `DWIS.Service.ActiveVolume.DataSource`: publishes synthetic **inputs** to blackboard.
- `DWIS.Service.ActiveVolume.Server`: consumes those inputs and computes fused outputs.
- `DWIS.Service.ActiveVolume.DataSink`: reads/logs fused outputs.

## Main Components

- `Program.cs`: generic host bootstrap and hosted `Worker` registration.
- `Worker.cs`: blackboard registration and periodic synthetic data generation.
- `config/Quickstarts.ReferenceClient.Config.xml`: OPC UA client configuration used by the DWIS client stack.
- `appsettings*.json`: logging configuration.

## Runtime Flow

At startup (`Worker.ExecuteAsync`):

1. Connect to blackboard (`ConnectToBlackboard`).
2. Register `RealtimeInputsData` as writable variables on blackboard.
3. Enter periodic loop with cadence `LoopSpan`.

Each loop iteration (`Worker.Loop`):

1. Populate `RealtimeInputsData` fields.
2. Advance internal simulation time `t += LoopSpan.TotalSeconds`.
3. Publish current sample to blackboard (`PublishBlackboardAsync`).
4. Log emitted values.

## Synthetic Signal Model

The generator uses fixed internal constants:

- `flowrateIn = 2000 / 60000 m^3/s` (2000 L/min)
- `flowrateOut = flowrateIn`
- `activeVolume = 30.0 m^3` initial
- `activeVolumeAmplitude = 1.0 m^3`
- `activeVolumePeriod = 15.0 s`
- `cuttingsFlowrate = 30 / 60000 m^3/s` (30 L/min)
- `scalingFactor = 3000 / 60000 m^3/s`
- cuttings std dev per shaker: `1 / 60000 m^3/s`
- shaker load std dev per shaker: `0.1`

### Active volume evolution

Per loop:

```text
activeVolume += dt * (-flowrateIn + flowrateOut - cuttingsFlowrate)
```

Published `ActiveVolume` adds sinusoidal variation:

```text
ActiveVolume = activeVolume + activeVolumeAmplitude * sin(2*pi*t/activeVolumePeriod)
```

### Cuttings recovery rates

Published as two Gaussian values (representing two shakers), each with half of total cuttings flow:

```text
Mean = 0.5 * cuttingsFlowrate
Std  = cuttingsFlowrateStandardDeviation
```

### Shaker load estimates

`totalShakerLoadEstimates = flowrateOut / scalingFactor`

Published as two Gaussian values, each with:

```text
Mean = 0.5 * totalShakerLoadEstimates * 10.0
Std  = shakerLoadStandardDeviation
```

Note: the downstream fusion logic divides shaker means by `10.0` and averages them, so this encoding maps back to return proportion.

## Published Blackboard Variables

From `RealtimeInputsData`:

- `ActiveVolume` (`m^3`)
- `FlowrateIn` (`m^3/s`)
- `CuttingsRecoveryRates` (`GaussianValuesProperty`, `m^3/s` means)
- `ShakerLoadEstimates` (`GaussianValuesProperty`, dimensionless means)

## Logging

At `Information` level, each loop logs:

- inlet flow (`L/min`)
- active volume (`m^3`)
- derived flow-out proportion (`%`)
- total cuttings flow (`L/min`)

## Build and Run

### Local

```bash
dotnet build DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
dotnet run --project DWIS.Service.ActiveVolume.DataSource/DWIS.Service.ActiveVolume.DataSource.csproj
```

### Container

A multi-stage Dockerfile is provided and starts with:

```bash
dotnet DWIS.Service.ActiveVolume.DataSource.dll
```

## Dependencies

- DWIS worker runtime (`DWIS.RigOS.Common.Worker` via project references)
- DWIS OPC Foundation client integration (`DWIS.Client.ReferenceImplementation.OPCFoundation`)
- Model project reference: `DWIS.Service.ActiveVolume.Model`

## Operational Notes

- This service is deterministic given `LoopSpan` and startup state.
- It is intended as a development/test data producer rather than a production sensor adapter.
- The process starts publishing only when blackboard connection is established.
