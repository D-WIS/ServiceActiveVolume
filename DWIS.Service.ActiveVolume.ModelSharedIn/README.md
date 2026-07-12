# DWIS.Service.ActiveVolume.ModelSharedIn

`DWIS.Service.ActiveVolume.ModelSharedIn` owns the generated input model used by the online ActiveVolume server when it talks to upstream context microservices.

It follows the same shared-model pattern used by the OSDC microservices: source OpenAPI schemas are copied into `json-schemas/`, merged into one OpenAPI document, and used to generate a strongly typed C# client and DTO set.

## Source Schemas

The project currently includes schemas for:

- Field
- Cluster
- Well
- WellBore
- WellBoreArchitecture
- DrillString

The schemas are stored under `json-schemas/`.

## Generated Outputs

- `MergedModel.json`: merged OpenAPI document.
- `MergedModel.cs`: generated C# client and DTOs.

The generated namespace is `NORCE.Drilling.ActiveVolume.ModelShared`.

## Regenerate

From the solution root:

```bash
dotnet run --project DWIS.Service.ActiveVolume.ModelSharedIn/DWIS.Service.ActiveVolume.ModelSharedIn.csproj
```

Regenerate this project whenever one of the upstream schema inputs changes.

## Used By

- `DWIS.Service.ActiveVolume.Server`

The server uses this project for context-service client wiring and DTO compatibility with field, cluster, well, wellbore, wellbore architecture, and drill-string services.
