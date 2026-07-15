namespace DWIS.Service.ActiveVolume.Model.Case
{
    public enum ReturnFlowMeasurementMode
    {
        Unknown = 0,
        FlowPaddle = 1,
        CoriolisFlowmeter = 2
    }

    public enum ActiveVolumeCaseProcessingState
    {
        Created = 0,
        Uploading = 1,
        Uploaded = 2,
        Queued = 3,
        Processing = 4,
        Processed = 5,
        Failed = 6,
        Interrupted = 7
    }

    public enum CalibrationJobState
    {
        Queued = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum CalibrationComponent
    {
        ReturnFlow = 0,
        LowFlowSurrogate = 1,
        Compressibility = 2,
        ThermalExpansion = 3,
        SurfaceRetention = 4,
        MudFilm = 5,
        FormationExchangeDiagnostic = 6
    }
}
