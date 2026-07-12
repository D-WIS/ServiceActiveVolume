# DWIS.Service.ActiveVolume.WebApp

`DWIS.Service.ActiveVolume.WebApp` is the Blazor Server host for the reusable ActiveVolume web pages.

The project stays intentionally thin: layout, host configuration, endpoint wiring, and static shell assets live here, while feature pages live in `DWIS.Service.ActiveVolume.WebPages`.

## Responsibilities

- Host the reusable ActiveVolume Razor class library.
- Provide endpoint configuration for the CalibrationService, context data services, and calculator services.
- Render the left-side ActiveVolume navigation.
- Package the UI as a deployable Docker image.

## Main Files

- `Program.cs`: Blazor Server setup and `AddActiveVolumeWebPages` registration.
- `App.razor`: route discovery for both host and reusable page assemblies.
- `Shared/MainLayout.razor`: application layout and navigation host.
- `Pages/_Host.cshtml`: Blazor Server host page.
- `wwwroot/css/site.css`: host-level CSS.
- `Dockerfile`: Linux container build.
- `charts/`: Helm chart for Kubernetes deployment.

## Configuration

`appsettings.json` contains endpoint URLs used by `DWIS.Service.ActiveVolume.WebPages`:

```json
{
  "BasePath": "/activevolume/webapp",
  "ActiveVolumeCalibrationHostURL": "http://localhost:5000/",
  "FieldHostURL": "http://norcedrillingfieldservice/",
  "ClusterHostURL": "http://norcedrillingclusterservice/",
  "WellHostURL": "http://norcedrillingwellservice/",
  "WellBoreHostURL": "http://norcedrillingwellboreservice/",
  "WellBoreArchitectureHostURL": "http://norcedrillingwellborearchitectureservice/",
  "DrillStringHostURL": "http://norcedrillingdrillstringservice/",
  "UnitConversionHostURL": "http://osdcunitconversionservice/",
  "VerticalDepthHostURL": "http://norcedrillingverticaldepthservice/"
}
```

Environment variables use the same names, for example:

```text
BasePath=/activevolume/webapp
ActiveVolumeCalibrationHostURL=http://calibration/
FieldHostURL=http://field/
UnitConversionHostURL=http://unit-conversion/
```

The context service URLs mirror the services represented in `DWIS.Service.ActiveVolume.ModelSharedOut`; the calculator links are used by the reusable web navigation. As in Trajectory, host URL settings are service roots. The reusable page API helper appends `activevolume/api` for CalibrationService calls.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.WebApp/DWIS.Service.ActiveVolume.WebApp.csproj
dotnet run --project DWIS.Service.ActiveVolume.WebApp/DWIS.Service.ActiveVolume.WebApp.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.WebApp/Dockerfile --tag digiwells/norcedrillingactivevolumewebappclient:stable .
docker run --rm -p 8080:8080 digiwells/norcedrillingactivevolumewebappclient:stable
```

## Kubernetes

A Helm chart is available at:

```text
DWIS.Service.ActiveVolume.WebApp/charts/norcedrillingactivevolumewebappclient
```

Render it with:

```bash
helm template activevolume-webapp DWIS.Service.ActiveVolume.WebApp/charts/norcedrillingactivevolumewebappclient
```

Endpoint URLs are configured through chart values under `env`.

## Dependencies

- `DWIS.Service.ActiveVolume.WebPages`
- `DWIS.Service.ActiveVolume.ModelSharedOut`
- CalibrationService REST API
- optional context and calculator web apps
