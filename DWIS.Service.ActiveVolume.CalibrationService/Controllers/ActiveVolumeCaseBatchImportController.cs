using DWIS.Service.ActiveVolume.CalibrationService.Managers;
using DWIS.Service.ActiveVolume.Model.Import;
using Microsoft.AspNetCore.Mvc;

namespace DWIS.Service.ActiveVolume.CalibrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public sealed class ActiveVolumeCaseBatchImportController : ControllerBase
    {
        private readonly ActiveVolumeSqliteStore store_;

        public ActiveVolumeCaseBatchImportController(ActiveVolumeSqliteStore store)
        {
            store_ = store;
        }

        [HttpGet(Name = "GetAllActiveVolumeCaseBatchImport")]
        public ActionResult<IEnumerable<ActiveVolumeCaseBatchImport>> GetAllActiveVolumeCaseBatchImport()
        {
            return Ok(store_.GetAllBatchImports());
        }

        [HttpGet("LightData", Name = "GetAllActiveVolumeCaseBatchImportLight")]
        public ActionResult<IEnumerable<ActiveVolumeCaseBatchImportLight>> GetAllActiveVolumeCaseBatchImportLight()
        {
            return Ok(store_.GetAllBatchImports().Select(batch => batch.Light));
        }

        [HttpGet("{id}", Name = "GetActiveVolumeCaseBatchImportById")]
        public ActionResult<ActiveVolumeCaseBatchImport> GetActiveVolumeCaseBatchImportById(Guid id)
        {
            ActiveVolumeCaseBatchImport? data = store_.GetBatchImport(id);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost(Name = "PostActiveVolumeCaseBatchImport")]
        public ActionResult PostActiveVolumeCaseBatchImport([FromBody] ActiveVolumeCaseBatchImport? data)
        {
            if (data is null || data.ID == Guid.Empty)
            {
                return BadRequest();
            }

            store_.SaveBatchImport(data);
            return Ok();
        }
    }
}
