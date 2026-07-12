# DWIS.Service.ActiveVolume.Server

`DWIS.Service.ActiveVolume.Server` is the realtime online ActiveVolume worker.

It connects to the DWIS blackboard, reads realtime drilling signals, runs the shared ActiveVolume fusion model, publishes corrected outputs back to the blackboard, and keeps an append-only local spool that is uploaded to the CalibrationService.

## Responsibilities

- Read ActiveVolume input signals from the blackboard.
- Resolve contextual service endpoints for field, cluster, well, wellbore, wellbore architecture, and drill-string.
- Run the shared online fusion engine from `DWIS.Service.ActiveVolume.Model`.
- Publish fused ActiveVolume outputs to the blackboard.
- Create and maintain one online case in the CalibrationService.
- Persist realtime samples locally before upload so restarts lose as little information as possible.
- Periodically request best-match background calibration data from the CalibrationService.

## Main Files

- `Program.cs`: worker host setup, options binding, HTTP clients, and hosted service registration.
- `Worker.cs`: blackboard loop, model execution, output publishing, chunking, and calibration-service calls.
- `ActiveVolumeOnlineOptions.cs`: online case, spool, chunking, and calibration polling options.
- `CalibrationServiceClient.cs`: REST client for online case/chunk/calibration operations.
- `OnlineCaseSpool.cs`: local durable chunk spool.
- `ModelServiceOptions.cs` and `ModelServiceClients.cs`: generated client setup for context microservices.
- `appsettings*.json`: logging, context service URLs, and online calibration settings.
- `Dockerfile`: Linux container build for the worker.

## Configuration

The main configuration sections are `ModelServices` and `ActiveVolumeOnline`.

```json
{
  "ModelServices": {
    "Field": "http://localhost:5000/Field/api/",
    "Cluster": "http://localhost:5000/Cluster/api/",
    "Well": "http://localhost:5000/Well/api/",
    "WellBore": "http://localhost:5000/WellBore/api/",
    "WellBoreArchitecture": "http://localhost:5000/WellBoreArchitecture/api/",
    "DrillString": "http://localhost:5000/DrillString/api/"
  },
  "ActiveVolumeOnline": {
    "CalibrationServiceUrl": "http://localhost:5000/activevolume/api/",
    "SpoolDirectory": "/home/activevolume-online",
    "ChunkSize": 600,
    "ChunkFlushInterval": "00:01:00",
    "CalibrationRequestInterval": "00:10:00",
    "CaseName": "Online ActiveVolume Case",
    "ReturnFlowMeasurementMode": "FlowPaddle"
  }
}
```

Environment variables use the standard .NET double-underscore form, for example:

```text
ModelServices__Field=http://field/api/
ActiveVolumeOnline__CalibrationServiceUrl=http://calibration/activevolume/api/
```

## Input Signals

The worker consumes `RealtimeInputsData` from the blackboard, including:

- active pit volume;
- inlet flow;
- flow-paddle indication and/or Coriolis return flow;
- cuttings recovery rate;
- bit or bottom-of-string depth;
- bottom-hole depth;
- optional return mud density and standpipe pressure;
- context IDs for the relevant field, cluster, well, wellbore, wellbore architecture, and drill-string.

## Output Signals

The worker publishes `RealtimeOutputsData`, including:

- corrected active volume;
- estimated pit-volume flow bias;
- return-flow capacity scale.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
dotnet run --project DWIS.Service.ActiveVolume.Server/DWIS.Service.ActiveVolume.Server.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.Server/Dockerfile --tag digiwells/dwisserviceactivevolumeserver:stable .
docker run --rm -v c:/Volumes/DWISServiceActiveVolume:/home digiwells/dwisserviceactivevolumeserver:stable
```

Mount `/home` in production so the online spool survives container restarts.

## Dependencies

- `DWIS.Service.ActiveVolume.Model`
- `DWIS.Service.ActiveVolume.ModelSharedIn`
- DWIS worker and OPC UA client packages
- CalibrationService REST API
- upstream context microservices
