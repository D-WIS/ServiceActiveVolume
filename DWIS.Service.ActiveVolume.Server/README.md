# DWIS.Service.ActiveVolume.Server

`DWIS.Service.ActiveVolume.Server` is a .NET 8 worker service that reads realtime drilling signals from the DWIS blackboard, runs active-volume sensor fusion, and publishes corrected outputs back to the blackboard.

## Purpose

The service hosts the active-volume fusion model (`DWIS.Service.ActiveVolume.Model`) and executes it continuously to estimate:

- corrected active pit volume
- net flow bias affecting pit volume balance
- return flow capacity scale used to convert return proportion into volumetric return flow

## Project Role in the Solution

This project is the runtime host for the model layer:

- `DWIS.Service.ActiveVolume.Server`: process orchestration, blackboard connectivity, periodic loop, publishing
- `DWIS.Service.ActiveVolume.Model`: DTO definitions (`RealtimeInputsData`, `RealtimeOutputsData`), configuration (`ConfigurationForActiveVolume`), and EKF logic (`SensorFusion`)

## Main Components

- `Program.cs`: builds a generic host and registers `Worker` as a hosted service.
- `Worker.cs`: derives from `DWISWorker<ConfigurationForActiveVolume>` and owns runtime behavior.
- `config/Quickstarts.ReferenceClient.Config.xml`: OPC UA client configuration used by the DWIS client stack.
- `appsettings*.json`: standard .NET logging settings (and optional config override source when provided by host/environment).

## Runtime Flow

At startup (`Worker.ExecuteAsync`):

1. Connect to blackboard (`ConnectToBlackboard`).
2. Register input queries for `RealtimeInputsData`.
3. Register output variables for `RealtimeOutputsData`.
4. Enter periodic loop (`LoopSpan` controlled by base worker configuration).

Each loop iteration (`Worker.Loop`):

1. Read current inputs from blackboard.
2. Run `SensorFusion.FuseData(Configuration, RealtimeInputsData, RealtimeOutputsData)`.
3. Publish updated outputs to blackboard.
4. Log key input/output values.
5. Refresh live configuration via `ConfigurationUpdater<ConfigurationForActiveVolume>.Instance.UpdateConfiguration(this)`.

Any exception in the loop is caught and logged; the loop continues on the next tick.

## Input Signals (Consumed)

From `RealtimeInputsData`:

- `ActiveVolume` (`m^3`): direct measured active pit volume.
- `FlowrateIn` (`m^3/s`): flow from pits into well.
- `ShakerLoadEstimates` (dimensionless, per shaker): converted to average return proportion by dividing each shaker mean by `10.0`, then averaging.
- `CuttingsRecoveryRates` (`m^3/s`, per shaker): summed to total cuttings removal flow.

## Output Signals (Published)

From `RealtimeOutputsData`:

- `CorrectedActiveVolume` (`m^3`)
- `EstimatedPitVolumeFlowBias` (`m^3/s`)
- `ReturnFlowCapacityScale` (`m^3/s`)

## EKF Configuration (`ConfigurationForActiveVolume`)

The service consumes the following model/runtime tuning properties each cycle:

- Time handling: `MinDtSeconds`, `MaxDtSeconds`, `DefaultDtSeconds`
- Return proportion bounds/init: `MinReturnProportion`, `MaxReturnProportion`, `InitReturnProportionEpsilon`
- Capacity scaling/init: `MinCapacityScale`, `InitCapacityScaleWhenNoReturn`
- Measurement/process variances: `MeasurementVarianceR`, `ProcessVarianceBiasQb`, `ProcessVarianceCapacityQc`, `ProcessVarianceModelQmodel`
- Input noise sigmas: `SigmaReturnProportion`, `SigmaCuttingsFlow`, `SigmaInletFlow`
- Outlier gating: `MaxNis`
- Numerical floors: `InnovationCovarianceFloor`, `MinStateVarianceFloor`
- Initial covariance: `InitVolumeVariance`, `InitBiasVariance`, `InitCapacityVariance`

These values are hot-reloadable through the base worker configuration updater mechanism.

## Logging

The worker emits informational diagnostics each cycle when `Information` logging is enabled:

- inlet flow (`L/min`)
- measured active volume (`m^3`)
- derived return proportion (`%`)
- cuttings flow (`L/min`)
- corrected active volume (`m^3`)
- estimated bias (`L/min`)
- return flow capacity scale (`L/min`)

## Build and Run

### Local

```bash
dotnet build DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
dotnet run --project DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
```

### Container

A multi-stage Dockerfile is included:

- base: `mcr.microsoft.com/dotnet/runtime:8.0`
- build/publish: `mcr.microsoft.com/dotnet/sdk:8.0`

The container entrypoint is:

```bash
dotnet DWIS.Service.ActiveVolume.Server.dll
```

Run container:

```sh
docker run -dit --name DWISActiveVolume -v c:\Volumes\DWISServiceActiveVolume:/home digiwells/dwisserviceactivevolumeserver:stable
```


## External Dependencies

- DWIS worker/runtime framework (`DWIS.RigOS.Common.Worker` via model dependency)
- DWIS OPC Foundation client integration (`DWIS.Client.ReferenceImplementation.OPCFoundation`)
- Model project reference: `DWIS.Service.ActiveVolume.Model`

## Operational Notes

- The service requires successful blackboard connectivity to start processing.
- If input signals are missing, fusion still runs with available defaults/clamping logic in the model.
- Service behavior is deterministic per cycle for a given configuration and input snapshot.
