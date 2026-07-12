# DWIS.Service.ActiveVolume.Model

`DWIS.Service.ActiveVolume.Model` is the shared domain and computation library for the ActiveVolume solution.

It contains the data contracts and algorithms used by both the realtime online worker and the background calibration service. Code that must behave identically online and during historical replay belongs here.

## Contents

- `RealtimeInputsData.cs` and `RealtimeOutputsData.cs`: blackboard input/output contracts used by the online worker and development source/sink services.
- `ConfigurationForActiveVolume.cs`: model and runtime tuning parameters.
- `SensorFusion.cs`: legacy-compatible fusion entry point used by the blackboard worker surface.
- `Case/`: active-volume case, chunk, light-case, and status DTOs.
- `Calibration/`: calibration records, matching requests, job DTOs, and related metadata.
- `Fusion/`: online fusion engine and return-flow models shared by online and background processing.
- `Geometry/`: wellbore/drill-string volume calculation helpers.
- `Import/`: ASCII import and batch import DTOs.
- `NextModel.md`: design notes for the next-generation model.

## Design Boundary

This project should stay independent of hosting concerns:

- no ASP.NET controllers;
- no database access;
- no blackboard connection code;
- no Blazor components.

The model layer assumes SI units internally. Host projects are responsible for converting external units and normalizing coordinate/reference systems before creating model DTOs.

## Model Responsibilities

The shared model supports:

- realtime fusion of active pit volume with inlet flow, return-flow information, cuttings recovery, bit depth, and bottom-hole depth;
- flow-paddle and Coriolis return-flow modes;
- reusable calculations for total mud volume based on wellbore architecture and drill-string geometry;
- chunked historical case data so large time series can be stored and processed incrementally;
- calibration records and matching requests that can be saved by the REST API and reused by the online worker.

## Build

From the solution root:

```bash
dotnet build DWIS.Service.ActiveVolume.Model/DWIS.Service.ActiveVolume.Model.csproj
```

## Used By

- `DWIS.Service.ActiveVolume.Server`
- `DWIS.Service.ActiveVolume.CalibrationService`
- `DWIS.Service.ActiveVolume.DataSource`
- `DWIS.Service.ActiveVolume.DataSink`
- `DWIS.Service.ActiveVolume.CalibrationWebPages` through linked DTO source files for NuGet packaging
