# DWIS.Service.ActiveVolume.ModelSharedOut

`DWIS.Service.ActiveVolume.ModelSharedOut` owns the generated output/client model intended for ActiveVolume clients such as the CalibrationWebPages package, CalibrationWebApp, and external UI hosts.

The schema inputs include the ActiveVolume CalibrationService Swagger contract plus the contextual contracts for field, cluster, well, wellbore, wellbore architecture, drill-string, and rig.

## Source Schemas

The project includes source schemas in `json-schemas/` for:

- Field
- Cluster
- Well
- WellBore
- WellBoreArchitecture
- DrillString
- Rig
- ActiveVolumeCalibration

## Generated Outputs

- `ActiveVolumeMergedModel.json`: merged OpenAPI document.
- `ActiveVolumeMergedModel.cs`: generated C# client and DTOs.

## Regenerate

From the solution root:

```bash
dotnet build DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj
dotnet run --project DWIS.Service.ActiveVolume.ModelSharedOut/DWIS.Service.ActiveVolume.ModelSharedOut.csproj
```

The CalibrationService Debug build writes `json-schemas/ActiveVolumeCalibrationFullName.json` using the local Swagger CLI tool. Regenerate this project whenever the CalibrationService API or client-facing schema set changes.

## Used By

- `DWIS.Service.ActiveVolume.CalibrationWebPages`, which includes `ActiveVolumeMergedModel.cs` as generated source in the same style as Trajectory WebPages.
- `DWIS.Service.ActiveVolume.CalibrationWebApp`, through the reusable page package.
