using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.DummyVisualizer
{
    class DummyVisualizerDevice : ICitpVisualizerDevice
    {
        public CitpPeerType DeviceType { get; }
        public string PeerName { get; }
        public string State { get; }
        public CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request)
        {
            return null;
        }

        public Guid Uuid { get; }
        public ImmutableHashSet<MsexImageFormat> SupportedStreamFormats { get; }
        public ImmutableDictionary<int, VideoSourceInformation> VideoSourceInformation { get; }
    }
}
