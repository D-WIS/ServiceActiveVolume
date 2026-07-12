# DWIS.Service.ActiveVolume.CalibrationWebPages

`DWIS.Service.ActiveVolume.CalibrationWebPages` is a reusable Razor class library for the ActiveVolume user interface.

It is intentionally separated from the host CalibrationWebApp so the pages can be packaged as a NuGet and reused in another Blazor host.

## Contents

- `Pages/ActiveVolumeCalibrationCases.razor`: lists light case records from the CalibrationService.
- `Pages/ActiveVolumeCalibrationBatchImports.razor`: lists batch import definitions.
- `Pages/ActiveVolumeCalibrationRecords.razor`: lists stored calibration records.
- `ActiveVolumeCalibrationNavMenu.razor`: left-side navigation with ActiveVolume, context data, and calculator links.
- `ActiveVolumeCalibrationAPIUtils.cs`: HTTP helper for CalibrationService API calls.
- `ActiveVolumeCalibrationWebPagesConfiguration.cs`: endpoint configuration for the pages.
- `wwwroot/activevolumecalibration.css`: styles for the reusable pages.

## Configuration Contract

Hosts register the pages with `AddActiveVolumeCalibrationWebPages` and provide an `IActiveVolumeCalibrationWebPagesConfiguration`.

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

The `*HostURL` values are service roots for server-side API clients. Browser navigation uses fixed public web app
route paths in `ActiveVolumeCalibrationNavMenu.razor`, following the pattern used by the contextual-data web apps.

## Routes

The library currently contributes:

- `/activevolumecalibration/cases`
- `/activevolumecalibration/batch-imports`
- `/activevolumecalibration/calibrations`

The navigation menu also includes placeholders or links for online cases, jobs, contextual data pages, unit conversion, and vertical depth calculators.

## Build

```bash
dotnet build DWIS.Service.ActiveVolume.CalibrationWebPages/DWIS.Service.ActiveVolume.CalibrationWebPages.csproj
```

## Package

```bash
dotnet pack DWIS.Service.ActiveVolume.CalibrationWebPages/DWIS.Service.ActiveVolume.CalibrationWebPages.csproj --configuration Release --output artifacts
```

The package ID is `DWIS.Service.ActiveVolume.CalibrationWebPages`.

## Publishing

The solution workflow `.github/workflows/publish-webpages-nuget.yml` publishes this project to NuGet.org. It requires the repository secret `NUGET_API_KEY` and can be triggered manually with a version or by pushing a tag like `webpages-v1.0.0`.

## Dependencies

This project links selected DTO source files from `DWIS.Service.ActiveVolume.Model` so the NuGet package can be consumed without a local project reference.
