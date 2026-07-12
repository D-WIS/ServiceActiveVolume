# DWIS.Service.ActiveVolume.WebPages

`DWIS.Service.ActiveVolume.WebPages` is a reusable Razor class library for the ActiveVolume user interface.

It is intentionally separated from the host WebApp so the pages can be packaged as a NuGet and reused in another Blazor host.

## Contents

- `Pages/ActiveVolumeCases.razor`: lists light case records from the CalibrationService.
- `Pages/ActiveVolumeBatchImports.razor`: lists batch import definitions.
- `Pages/ActiveVolumeCalibrations.razor`: lists stored calibration records.
- `ActiveVolumeNavMenu.razor`: left-side navigation with ActiveVolume, context data, and calculator links.
- `ActiveVolumeAPIUtils.cs`: HTTP helper for CalibrationService API calls.
- `ActiveVolumeWebPagesConfiguration.cs`: endpoint configuration for the pages.
- `wwwroot/activevolume.css`: styles for the reusable pages.

## Configuration Contract

Hosts register the pages with `AddActiveVolumeWebPages` and provide an `IActiveVolumeWebPagesConfiguration`.

The configuration includes:

- `ActiveVolumeCalibrationHostURL`
- `FieldHostURL`
- `ClusterHostURL`
- `WellHostURL`
- `WellBoreHostURL`
- `WellBoreArchitectureHostURL`
- `DrillStringHostURL`
- `UnitConversionHostURL`
- `VerticalDepthHostURL`

## Routes

The library currently contributes:

- `/activevolume/cases`
- `/activevolume/batch-imports`
- `/activevolume/calibrations`

The navigation menu also includes placeholders or links for online cases, jobs, contextual data pages, unit conversion, and vertical depth calculators.

## Build

```bash
dotnet build DWIS.Service.ActiveVolume.WebPages/DWIS.Service.ActiveVolume.WebPages.csproj
```

## Package

```bash
dotnet pack DWIS.Service.ActiveVolume.WebPages/DWIS.Service.ActiveVolume.WebPages.csproj --configuration Release --output artifacts
```

The package ID is `DWIS.Service.ActiveVolume.WebPages`.

## Publishing

The solution workflow `.github/workflows/publish-webpages-nuget.yml` publishes this project to NuGet.org. It requires the repository secret `NUGET_API_KEY` and can be triggered manually with a version or by pushing a tag like `webpages-v1.0.0`.

## Dependencies

This project links selected DTO source files from `DWIS.Service.ActiveVolume.Model` so the NuGet package can be consumed without a local project reference.
