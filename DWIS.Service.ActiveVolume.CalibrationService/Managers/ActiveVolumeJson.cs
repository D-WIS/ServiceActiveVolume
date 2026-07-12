using System.Text.Json;
using System.Text.Json.Serialization;

namespace DWIS.Service.ActiveVolume.CalibrationService.Managers
{
    public static class ActiveVolumeJson
    {
        public static readonly JsonSerializerOptions Options = Create();

        public static void ApplyTo(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.WriteIndented = true;
            options.Converters.Add(new JsonStringEnumConverter());
        }

        private static JsonSerializerOptions Create()
        {
            JsonSerializerOptions options = new();
            ApplyTo(options);
            return options;
        }
    }
}
