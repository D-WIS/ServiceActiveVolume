using DWIS.Service.ActiveVolume.CalibrationService.Managers;
using DWIS.Service.ActiveVolume.Model.Calibration;
using Microsoft.AspNetCore.Mvc;

namespace DWIS.Service.ActiveVolume.CalibrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public sealed class CalibrationController : ControllerBase
    {
        private readonly ActiveVolumeSqliteStore store_;

        public CalibrationController(ActiveVolumeSqliteStore store)
        {
            store_ = store;
        }

        [HttpGet(Name = "GetAllCalibration")]
        public ActionResult<IEnumerable<CalibrationRecord>> GetAllCalibration()
        {
            return Ok(store_.GetAllCalibrations());
        }

        [HttpGet("{id}", Name = "GetCalibrationById")]
        public ActionResult<CalibrationRecord> GetCalibrationById(Guid id)
        {
            CalibrationRecord? data = store_.GetCalibration(id);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost("BestMatch", Name = "PostBestMatchCalibration")]
        public ActionResult<IEnumerable<BestMatchCalibrationResult>> PostBestMatchCalibration([FromBody] BestMatchCalibrationRequest? request)
        {
            if (request is null)
            {
                return BadRequest();
            }

            int maxResults = Math.Max(1, request.MaxResults);
            List<BestMatchCalibrationResult> matches = store_.GetAllCalibrations()
                .Select(x => new BestMatchCalibrationResult
                {
                    Calibration = x,
                    Distance = ComputeDistance(request.Context, x.Context)
                })
                .OrderBy(x => x.Distance)
                .Take(maxResults)
                .ToList();

            foreach (BestMatchCalibrationResult match in matches)
            {
                match.Weight = 1.0 / (1.0 + match.Distance);
            }

            return Ok(matches);
        }

        private static double ComputeDistance(ActiveVolumeContext requested, ActiveVolumeContext candidate)
        {
            double distance = 0.0;
            distance += requested.ReturnFlowMeasurementMode == candidate.ReturnFlowMeasurementMode ? 0.0 : 10.0;
            distance += requested.FieldID == candidate.FieldID ? 0.0 : 1.0;
            distance += requested.ClusterID == candidate.ClusterID ? 0.0 : 1.0;
            distance += requested.WellID == candidate.WellID ? 0.0 : 1.0;
            distance += requested.WellBoreID == candidate.WellBoreID ? 0.0 : 1.0;
            distance += requested.WellBoreArchitectureID == candidate.WellBoreArchitectureID ? 0.0 : 2.0;
            distance += requested.DrillStringID == candidate.DrillStringID ? 0.0 : 2.0;
            foreach ((string key, double requestedValue) in requested.NumericFeatures)
            {
                if (candidate.NumericFeatures.TryGetValue(key, out double candidateValue))
                {
                    distance += Math.Abs(requestedValue - candidateValue);
                }
            }

            return distance;
        }
    }
}
