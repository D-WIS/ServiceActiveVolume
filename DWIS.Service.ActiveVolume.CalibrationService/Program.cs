using DWIS.Service.ActiveVolume.CalibrationService.Managers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ActiveVolumeSqliteStore>();
builder.Services.AddSingleton<CalibrationJobQueue>();
builder.Services.AddHostedService<CalibrationJobWorker>();
builder.Services.AddControllers().AddJsonOptions(options => ActiveVolumeJson.ApplyTo(options.JsonSerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config => config.CustomSchemaIds(type => type.FullName));

var app = builder.Build();

var basePath = builder.Configuration["BasePath"] ?? "/activevolume/api";
app.UsePathBase(basePath);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "ActiveVolume Calibration API v1"));
app.UseCors(cors => cors.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());
app.MapControllers();
app.MapGet("/", () => Results.Ok("DWIS ActiveVolume Calibration Service"));
app.Run();
