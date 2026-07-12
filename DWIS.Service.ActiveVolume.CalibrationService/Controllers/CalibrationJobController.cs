using DWIS.Service.ActiveVolume.CalibrationService.Managers;
using DWIS.Service.ActiveVolume.Model.Calibration;
using Microsoft.AspNetCore.Mvc;

namespace DWIS.Service.ActiveVolume.CalibrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public sealed class CalibrationJobController : ControllerBase
    {
        private readonly ActiveVolumeSqliteStore store_;

        public CalibrationJobController(ActiveVolumeSqliteStore store)
        {
            store_ = store;
        }

        [HttpGet("{id}", Name = "GetCalibrationJobById")]
        public ActionResult<CalibrationJob> GetCalibrationJobById(Guid id)
        {
            CalibrationJob? job = store_.GetJob(id);
            return job is null ? NotFound() : Ok(job);
        }
    }
}
