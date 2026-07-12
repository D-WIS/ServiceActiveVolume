using DWIS.Service.ActiveVolume.CalibrationWebApp;
using DWIS.Service.ActiveVolume.CalibrationWebPages;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

WebPagesHostConfiguration webPagesConfiguration = new()
{
    ActiveVolumeCalibrationHostURL = builder.Configuration["ActiveVolumeCalibrationHostURL"] ?? "http://localhost:5000/",
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    WellHostURL = builder.Configuration["WellHostURL"] ?? string.Empty,
    WellBoreHostURL = builder.Configuration["WellBoreHostURL"] ?? string.Empty,
    WellBoreArchitectureHostURL = builder.Configuration["WellBoreArchitectureHostURL"] ?? string.Empty,
    DrillStringHostURL = builder.Configuration["DrillStringHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"] ?? string.Empty,
    CartographicProjectionHostURL = builder.Configuration["CartographicProjectionHostURL"] ?? string.Empty,
    VerticalDatumHostURL = builder.Configuration["VerticalDatumHostURL"] ?? string.Empty
};

OSDC.UnitConversion.WebPages.Configuration.UnitConversionHostURL = webPagesConfiguration.UnitConversionHostURL;

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddActiveVolumeCalibrationWebPages(webPagesConfiguration);
builder.Services.AddExternalWebPages(webPagesConfiguration);
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase(builder.Configuration["BasePath"] ?? "/activevolumecalibration/webapp");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
