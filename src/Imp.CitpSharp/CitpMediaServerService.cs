using System.Net.NetworkInformation;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Runs CITP services for a media server device.
    /// </summary>
    [PublicAPI]
    public class CitpMediaServerService : CitpServerService
    {
        private readonly ICitpMediaServerDevice _device;

        public CitpMediaServerService(ICitpLogService logger, ICitpMediaServerDevice device, bool isUseLegacyMulticastIp, NetworkInterface networkInterface = null)
            : base(logger, device, isUseLegacyMulticastIp, networkInterface)
        {
            _device = device;
        }
    }
}