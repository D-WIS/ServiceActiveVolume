using DWIS.Service.ActiveVolume.CalibrationService.Managers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ActiveVolumeSqliteStore>();
builder.Services.AddSingleton<CalibrationJobQueue>();
builder.Services.AddHostedService<CalibrationJobWorker>();
builder.Services.AddControllers().AddJsonOptions(options => ActiveVolumeJson.ApplyTo(options.JsonSerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config => config.CustomSchemaIds(type => type.FullName));

var app = builder.Build();

var basePath = builder.Configuration["BasePath"] ?? "/activevolumecalibration/api";

app.Use((context, next) =>
{
    if (context.Request.Path.StartsWithSegments(basePath, out PathString remaining))
    {
        context.Request.PathBase = basePath;
        context.Request.Path = remaining;
    }

    return next();
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "ActiveVolume Calibration API v1"));
app.UseCors(cors => cors.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());
app.MapControllers();
app.MapGet("/", () => Results.Ok("DWIS ActiveVolume Calibration Service"));
app.MapGet("/ActiveVolumeCalibration", () => Results.Ok(new
{
    Service = "DWIS ActiveVolume Calibration Service",
    PrimaryBasePath = basePath,
    Routes = new[]
    {
        "ActiveVolumeCase",
        "ActiveVolumeCase/LightData",
        "ActiveVolumeCaseBatchImport",
        "Calibration",
        "Calibration/BestMatch",
        "CalibrationJob/{id}"
    }
}));
app.Run();
