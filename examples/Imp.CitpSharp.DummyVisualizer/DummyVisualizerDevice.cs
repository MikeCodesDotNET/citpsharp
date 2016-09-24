using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Imp.CitpSharp.DummyVisualizer
{
	class DummyVisualizerDevice : ICitpVisualizerDevice
	{
		public DummyVisualizerDevice(Guid uuid, string peerName, string state)
		{
			Uuid = uuid;
			PeerName = peerName;
			State = state;
		}

		public Guid Uuid { get; }
		public string PeerName { get; }
		public string State { get; set; }

		[CanBeNull]
		public CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request)
		{
			return null;
		}

		public IImmutableSet<MsexImageFormat> SupportedStreamFormats =>
			new []
			{
				MsexImageFormat.Rgb8,
				MsexImageFormat.Jpeg,
				MsexImageFormat.Png,
				MsexImageFormat.FragmentedJpeg,
				MsexImageFormat.FragmentedPng
			}.ToImmutableHashSet();

		public IImmutableDictionary<int, VideoSourceInformation> VideoSourceInformation =>
			new Dictionary<int, VideoSourceInformation>
			{
				{ 1, new VideoSourceInformation(1, "Red", MsexVideoSourcesFlags.None, 1920, 1080)}
			}.ToImmutableDictionary();
	}
}
