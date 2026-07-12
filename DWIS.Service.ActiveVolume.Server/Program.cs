using DWIS.Service.ActiveVolume.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<ModelServiceOptions>(builder.Configuration.GetSection(ModelServiceOptions.SectionName));
builder.Services.Configure<ActiveVolumeOnlineOptions>(builder.Configuration.GetSection(ActiveVolumeOnlineOptions.SectionName));
builder.Services.AddHttpClient(nameof(ModelServiceClients));
builder.Services.AddHttpClient<CalibrationServiceClient>();
builder.Services.AddSingleton<IModelServiceClients, ModelServiceClients>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
