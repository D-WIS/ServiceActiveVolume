# DWIS.Service.ActiveVolume.CalibrationWebPages

`DWIS.Service.ActiveVolume.CalibrationWebPages` is a reusable Razor class library for the ActiveVolume user interface.

It is intentionally separated from the host CalibrationWebApp so the pages can be packaged as a NuGet and reused in another Blazor host.

## Contents

- `Pages/ActiveVolumeCalibrationCases.razor`: lists light case records from the CalibrationService and loads the heavy case only when a row is selected.
- `Pages/ActiveVolumeCalibrationBatchImports.razor`: lists light batch import records and loads the heavy batch import only when a row is selected.
- `ActiveVolumeCalibrationNavMenu.razor`: left-side navigation with ActiveVolume, context data, and calculator links.
- `ActiveVolumeCalibrationAPIUtils.cs`: thin wrapper around the generated ModelSharedOut client for CalibrationService API calls.
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
- `RigHostURL`
- `UnitConversionHostURL`
- `VerticalDatumHostURL`

The `*HostURL` values are service roots for server-side API clients. Browser navigation points to routes hosted by
the consuming web app; in `CalibrationWebApp`, those context and calculator routes are provided by the respective
NuGet web page packages.

## Routes

The library currently contributes:

- `/activevolumecalibration/cases`
- `/activevolumecalibration/batch-imports`

Case and batch import tables use light objects for listing and show processing progress directly in the table. They support creating new rows, selecting one or all rows, per-row deletion, and deletion of all selected rows. Detail/edit mode downloads the heavy object from the CalibrationService. Calibration results are displayed from the selected case instead of through a separate calibration page.

The active volume case editor selects contextual data by name and stores the associated IDs. Rig, mud system, and hole section are read-only derived values: rig and mud system are resolved through the selected wellbore or fixed-platform cluster rig, while the hole section is resolved from the selected wellbore architecture. The editor is wrapped in the OSDC unit/reference component, and open-hole diameters and lengths are displayed in the selected unit system.

The navigation menu also includes contextual data pages, unit conversion, and vertical datum/depth calculators supplied by the consuming web app through external web page packages.

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

This project includes `DWIS.Service.ActiveVolume.ModelSharedOut/ActiveVolumeMergedModel.cs` as generated source so the NuGet package can be consumed without a local project reference. It also depends on `OSDC.UnitConversion.DrillingRazorMudComponents` for unit/reference selection and converted value display. Regenerate `ModelSharedOut` whenever the CalibrationService API changes before packing this project.
