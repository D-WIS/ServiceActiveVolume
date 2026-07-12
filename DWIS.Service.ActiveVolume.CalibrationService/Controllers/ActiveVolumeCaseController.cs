using DWIS.Service.ActiveVolume.CalibrationService.Managers;
using DWIS.Service.ActiveVolume.Model.Case;
using Microsoft.AspNetCore.Mvc;

namespace DWIS.Service.ActiveVolume.CalibrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public sealed class ActiveVolumeCaseController : ControllerBase
    {
        private readonly ActiveVolumeSqliteStore store_;
        private readonly CalibrationJobQueue queue_;

        public ActiveVolumeCaseController(ActiveVolumeSqliteStore store, CalibrationJobQueue queue)
        {
            store_ = store;
            queue_ = queue;
        }

        [HttpGet(Name = "GetAllActiveVolumeCaseId")]
        public ActionResult<IEnumerable<Guid>> GetAllActiveVolumeCaseId()
        {
            return Ok(store_.GetAllCaseIds());
        }

        [HttpGet("LightData", Name = "GetAllActiveVolumeCaseLight")]
        public ActionResult<IEnumerable<ActiveVolumeCaseLight>> GetAllActiveVolumeCaseLight()
        {
            return Ok(store_.GetAllCaseLight());
        }

        [HttpGet("{id}", Name = "GetActiveVolumeCaseById")]
        public ActionResult<ActiveVolumeCase> GetActiveVolumeCaseById(Guid id, [FromQuery] bool includeChunks = false)
        {
            ActiveVolumeCase? data = store_.GetCase(id, includeChunks);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost(Name = "PostActiveVolumeCase")]
        public ActionResult PostActiveVolumeCase([FromBody] ActiveVolumeCase? data)
        {
            if (data is null || data.ID == Guid.Empty)
            {
                return BadRequest();
            }

            return store_.UpsertCase(data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutActiveVolumeCaseById")]
        public ActionResult PutActiveVolumeCaseById(Guid id, [FromBody] ActiveVolumeCase? data)
        {
            if (data is null || data.ID != id)
            {
                return BadRequest();
            }

            return store_.UpsertCase(data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}/Chunks/ChunkCount", Name = "GetActiveVolumeCaseChunkCount")]
        public ActionResult<int> GetActiveVolumeCaseChunkCount(Guid id)
        {
            if (store_.GetCase(id) is null)
            {
                return NotFound();
            }

            return Ok(store_.GetChunkCount(id));
        }

        [HttpGet("{id}/Chunks/{chunkIndex}", Name = "GetActiveVolumeCaseChunk")]
        public ActionResult<ActiveVolumeCaseChunk> GetActiveVolumeCaseChunk(Guid id, int chunkIndex)
        {
            ActiveVolumeCaseChunk? chunk = store_.GetChunk(id, chunkIndex);
            return chunk is null ? NotFound() : Ok(chunk);
        }

        [HttpPut("{id}/Chunks/{chunkIndex}", Name = "PutActiveVolumeCaseChunk")]
        public ActionResult PutActiveVolumeCaseChunk(Guid id, int chunkIndex, [FromBody] ActiveVolumeCaseChunk? chunk)
        {
            if (chunk is null || chunkIndex < 0)
            {
                return BadRequest();
            }

            chunk.ChunkIndex = chunkIndex;
            ChunkSaveResult result = store_.SaveChunk(id, chunk);
            return result switch
            {
                ChunkSaveResult.Stored => Ok(),
                ChunkSaveResult.AlreadyStored => Ok(),
                ChunkSaveResult.Conflict => Conflict(),
                ChunkSaveResult.CaseNotFound => NotFound(),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpPost("{id}/Process", Name = "PostActiveVolumeCaseProcess")]
        public async Task<ActionResult<Guid>> PostActiveVolumeCaseProcess(Guid id, CancellationToken cancellationToken)
        {
            if (store_.GetCase(id) is null)
            {
                return NotFound();
            }

            var job = store_.CreateJob(id);
            await queue_.QueueAsync(job.ID, cancellationToken);
            return Ok(job.ID);
        }
    }
}
