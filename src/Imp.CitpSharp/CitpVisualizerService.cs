using System.Net;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Runs CITP services for a visualizer device.
    /// </summary>
    public class CitpVisualizerService : CitpServerService
    {
        private readonly ICitpVisualizerDevice _device;

        public CitpVisualizerService(ICitpLogService logger, ICitpVisualizerDevice device, CitpServiceFlags flags, int preferredTcpListenPort, IPAddress localIp = null)
            : base(logger, device, flags, preferredTcpListenPort, localIp)
        {
            _device = device;
        }

        public override CitpPeerType DeviceType => CitpPeerType.Visualizer;
    }
}