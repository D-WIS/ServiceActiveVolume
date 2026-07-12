using DWIS.Service.ActiveVolume.WebPages;

var builder = WebApplication.CreateBuilder(args);

ActiveVolumeWebPagesConfiguration webPagesConfiguration = new()
{
    ActiveVolumeCalibrationHostURL = builder.Configuration["ActiveVolumeCalibrationHostURL"] ?? "http://localhost:5000/",
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    WellHostURL = builder.Configuration["WellHostURL"] ?? string.Empty,
    WellBoreHostURL = builder.Configuration["WellBoreHostURL"] ?? string.Empty,
    WellBoreArchitectureHostURL = builder.Configuration["WellBoreArchitectureHostURL"] ?? string.Empty,
    DrillStringHostURL = builder.Configuration["DrillStringHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
    VerticalDepthHostURL = builder.Configuration["VerticalDepthHostURL"] ?? string.Empty
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddActiveVolumeWebPages(webPagesConfiguration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase(builder.Configuration["BasePath"] ?? "/activevolume/webapp");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
