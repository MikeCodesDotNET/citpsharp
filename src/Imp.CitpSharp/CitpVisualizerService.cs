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
            bool isUseLegacyMulticastIp, NetworkInterface networkInterface = null)
            : base(logger, device, isUseLegacyMulticastIp, networkInterface)
        {
            _device = device;
        }
    }
}