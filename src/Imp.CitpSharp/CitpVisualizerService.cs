using System.Net.NetworkInformation;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Runs CITP services for a visualizer device.
    /// </summary>
    public class CitpVisualizerService : CitpServerService
    {
        private readonly ICitpVisualizerDevice _device;

        public CitpVisualizerService(ICitpLogService logger, ICitpVisualizerDevice device,
            bool isUseLegacyMulticastIp, bool isRunStreamTimer, NetworkInterface networkInterface = null)
            : base(logger, device, isUseLegacyMulticastIp, isRunStreamTimer, networkInterface)
        {
            _device = device;
        }

        public override CitpPeerType DeviceType => CitpPeerType.Visualizer;
    }
}