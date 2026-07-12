# DWIS.Service.ActiveVolume.CalibrationWebApp

`DWIS.Service.ActiveVolume.CalibrationWebApp` is the Blazor Server host for the reusable ActiveVolume web pages and selected reusable contextual-data pages.

The project stays intentionally thin: layout, host configuration, endpoint wiring, and static shell assets live here, while feature pages live in `DWIS.Service.ActiveVolume.CalibrationWebPages`.

## Responsibilities

- Host the reusable ActiveVolume Razor class library.
- Compose contextual data and calculator pages from their NuGet packages, following the Trajectory web app pattern.
- Provide endpoint configuration for the CalibrationService, context data services, and calculator services.
- Render the left-side ActiveVolume navigation.
- Package the UI as a deployable Docker image.

## Main Files

- `Program.cs`: Blazor Server setup and ActiveVolume/external web page registrations.
- `App.razor`: route discovery for both host and reusable page assemblies.
- `ExternalRazorAssemblies.cs`: external Razor class libraries discovered by the router.
- `ExternalWebPagesServiceCollectionExtensions.cs`: DI registrations for external page API helpers.
- `WebPagesHostConfiguration.cs`: host configuration shared across ActiveVolume and external page packages.
- `Shared/MainLayout.razor`: application layout and navigation host.
- `Pages/_Host.cshtml`: Blazor Server host page.
- `wwwroot/css/site.css`: host-level CSS.
- `Dockerfile`: Linux container build.
- `charts/`: Helm chart for Kubernetes deployment.

## Configuration

`appsettings.json` contains endpoint URLs used by `DWIS.Service.ActiveVolume.CalibrationWebPages`.
The `*HostURL` values are service roots used for server-side API calls. In Kubernetes they can be internal
service names such as `http://norcedrillingfieldservice/`, as in the Trajectory web app.

```json
{
  "BasePath": "/activevolumecalibration/webapp",
  "ActiveVolumeCalibrationHostURL": "http://localhost:5000/",
  "FieldHostURL": "http://norcedrillingfieldservice/",
  "ClusterHostURL": "http://norcedrillingclusterservice/",
  "WellHostURL": "http://norcedrillingwellservice/",
  "WellBoreHostURL": "http://norcedrillingwellboreservice/",
  "WellBoreArchitectureHostURL": "http://norcedrillingwellborearchitectureservice/",
  "DrillStringHostURL": "http://norcedrillingdrillstringservice/",
  "UnitConversionHostURL": "http://osdcunitconversionservice/",
  "RigHostURL": "http://norcedrillingrigservice/",
  "TrajectoryHostURL": "http://norcedrillingtrajectoryservice/",
  "CartographicProjectionHostURL": "http://norcedrillingcartographicprojectionservice/",
  "VerticalDatumHostURL": "http://norcedrillingverticaldatumservice/"
}
```

Environment variables use the same names, for example:

```text
BasePath=/activevolumecalibration/webapp
ActiveVolumeCalibrationHostURL=http://calibration/
FieldHostURL=http://field/
UnitConversionHostURL=http://unit-conversion/
VerticalDatumHostURL=http://vertical-datum/
```

The context service URLs mirror the services represented in `DWIS.Service.ActiveVolume.ModelSharedOut` and the services required by the embedded contextual pages. As in Trajectory, host URL settings are service roots. The reusable page API helper appends `activevolumecalibration/api` for CalibrationService calls.

## Build and Run

```bash
dotnet build DWIS.Service.ActiveVolume.CalibrationWebApp/DWIS.Service.ActiveVolume.CalibrationWebApp.csproj
dotnet run --project DWIS.Service.ActiveVolume.CalibrationWebApp/DWIS.Service.ActiveVolume.CalibrationWebApp.csproj
```

## Docker

```bash
docker build --file DWIS.Service.ActiveVolume.CalibrationWebApp/Dockerfile --tag digiwells/norcedrillingactivevolumecalibrationwebappclient:stable .
docker run --rm -p 8080:8080 digiwells/norcedrillingactivevolumecalibrationwebappclient:stable
```

## Kubernetes

A Helm chart is available at:

```text
DWIS.Service.ActiveVolume.CalibrationWebApp/charts/norcedrillingactivevolumecalibrationwebappclient
```

Render it with:

```bash
helm template activevolume-webapp DWIS.Service.ActiveVolume.CalibrationWebApp/charts/norcedrillingactivevolumecalibrationwebappclient
```

Endpoint URLs are configured through chart values under `env`.

## Dependencies

- `DWIS.Service.ActiveVolume.CalibrationWebPages`
- `DWIS.Service.ActiveVolume.ModelSharedOut`
- CalibrationService REST API
- contextual data and calculator web page NuGet packages
