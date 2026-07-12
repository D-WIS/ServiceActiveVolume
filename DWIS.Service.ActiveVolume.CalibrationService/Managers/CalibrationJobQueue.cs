using System.Threading.Channels;

namespace DWIS.Service.ActiveVolume.CalibrationService.Managers
{
    public sealed class CalibrationJobQueue
    {
        private readonly Channel<Guid> channel_ = Channel.CreateUnbounded<Guid>();

        public ValueTask QueueAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            return channel_.Writer.WriteAsync(jobId, cancellationToken);
        }

        public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return channel_.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
