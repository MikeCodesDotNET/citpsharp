using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace Imp.CitpSharp.DummyVisualizer
{
	internal class DummyMediaServerDevice : ICitpMediaServerDevice
	{
		public DummyMediaServerDevice(Guid uuid, string peerName, string state, string productName,
			int productVersionMajor, int productVersionMinor, int productversionBugfix)
		{
			Uuid = uuid;
			PeerName = peerName;
			State = state;

			ProductName = productName;
			ProductVersionMajor = productVersionMajor;
			ProductVersionMinor = productVersionMinor;
			ProductVersionBugfix = productversionBugfix;
		}

		public Guid Uuid { get; }
		public string PeerName { get; }
		public string State { get; set; }

		public string ProductName { get; }
		public int ProductVersionMajor { get; }
		public int ProductVersionMinor { get; }
		public int ProductVersionBugfix { get; }

		public IEnumerable<MsexVersion> SupportedMsexVersions =>
			new[]
			{
				MsexVersion.Version1_0,
				MsexVersion.Version1_1,
				MsexVersion.Version1_2
			};


		public IEnumerable<MsexLibraryType> SupportedLibraryTypes =>
			new[]
			{
				MsexLibraryType.Media
			};

		public IEnumerable<MsexImageFormat> SupportedThumbnailFormats =>
			new[]
			{
				MsexImageFormat.Rgb8,
				MsexImageFormat.Jpeg,
				MsexImageFormat.Png
			};

		public IEnumerable<ICitpMediaServerLayer> Layers { get; } = Enumerable.Empty<ICitpMediaServerLayer>();

		public IReadOnlyDictionary<MsexLibraryId, ElementLibrary> ElementLibraries { get; } = new Dictionary<MsexLibraryId, ElementLibrary>();

		public bool HasLibraryBeenUpdated { get; set; }



		[CanBeNull]
		public CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request)
		{
			var buffer = new byte[request.FrameWidth * request.FrameHeight * 3];

			for (int i = 0; i < buffer.Length; i += 3)
				buffer[i] = 255;

			return new CitpImage(request, buffer, request.FrameWidth, request.FrameHeight);
		}

		public ImmutableHashSet<MsexImageFormat> SupportedStreamFormats =>
			new[]
			{
				MsexImageFormat.Rgb8,
				MsexImageFormat.Jpeg,
				MsexImageFormat.Png,
				MsexImageFormat.FragmentedJpeg,
				MsexImageFormat.FragmentedPng
			}.ToImmutableHashSet();

		public ImmutableDictionary<int, VideoSourceInformation> VideoSourceInformation =>
			new Dictionary<int, VideoSourceInformation>
			{
				{1, new VideoSourceInformation(1, "Red", MsexVideoSourcesFlags.None, 1920, 1080)}
			}.ToImmutableDictionary();



		public IEnumerable<ElementLibraryUpdatedInformation> GetLibraryUpdateMessages()
		{
			throw new NotImplementedException();
		}

		public Tuple<MsexId, CitpImage> GetElementLibraryThumbnail(CitpImageRequest request,
			ElementLibraryInformation elementLibrary)
		{
			throw new NotImplementedException();
		}

		public Tuple<byte, CitpImage> GetElementThumbnail(CitpImageRequest request, ElementLibraryInformation elementLibrary,
			ElementInformation element)
		{
			throw new NotImplementedException();
		}
	}
}