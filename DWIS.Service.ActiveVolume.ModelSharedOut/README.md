# DWIS.Service.ActiveVolume.ModelSharedOut

`DWIS.Service.ActiveVolume.ModelSharedOut` owns the generated output/client model intended for ActiveVolume clients such as the WebApp and external UI hosts.

The current schema inputs mirror `ModelSharedIn`, giving client applications the same contextual contracts for field, cluster, well, wellbore, wellbore architecture, and drill-string. ActiveVolume-specific REST API schemas can be added later when the CalibrationService Swagger contract is exported and versioned.

## Source Schemas

The project includes source schemas in `json-schemas/` for:

- Field
- Cluster
- Well
- WellBore
- WellBoreArchitecture
- DrillString

## Generated Outputs

- `ActiveVolumeMergedModel.json`: merged OpenAPI document.
- `ActiveVolumeMergedModel.cs`: generated C# client and DTOs.

## Regenerate

From the solution root:

```bash
dotnet run --project DWIS.Service.ActiveVolume.ModelSharedOut/DWIS.Service.ActiveVolume.ModelSharedOut.csproj
```

Regenerate this project whenever the client-facing schema set changes.

## Used By

- `DWIS.Service.ActiveVolume.WebApp`

The reusable `WebPages` project currently uses ActiveVolume DTOs directly; this project remains the place for generated client-facing shared models.
