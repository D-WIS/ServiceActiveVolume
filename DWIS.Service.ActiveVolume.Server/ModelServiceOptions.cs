namespace DWIS.Service.ActiveVolume.Server
{
    public sealed class ModelServiceOptions
    {
        public const string SectionName = "ModelServices";

        public string Field { get; set; } = "http://localhost:5000/Field/api/";
        public string Cluster { get; set; } = "http://localhost:5000/Cluster/api/";
        public string Well { get; set; } = "http://localhost:5000/Well/api/";
        public string WellBore { get; set; } = "http://localhost:5000/WellBore/api/";
        public string WellBoreArchitecture { get; set; } = "http://localhost:5000/WellBoreArchitecture/api/";
        public string DrillString { get; set; } = "http://localhost:5000/DrillString/api/";
    }
}
