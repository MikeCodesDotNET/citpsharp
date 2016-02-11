using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public interface ICitpMediaServerInfo
	{
		string PeerName { get; }

		string ProductName { get; }
		int ProductVersionMajor { get; }
		int ProductVersionMinor { get; }
		int ProductVersionBugfix { get; }
		Guid Uuid { get; }

		IReadOnlyList<MsexVersion> SupportedMsexVersions { get; }

		IReadOnlyList<MsexLibraryType> SupportedLibraryTypes { get; }

		IReadOnlyList<MsexImageFormat> SupportedThumbnailFormats { get; }
		IReadOnlyList<MsexImageFormat> SupportedStreamFormats { get; }

		IReadOnlyList<ICitpLayer> Layers { get; }

		IReadOnlyDictionary<int, CitpVideoSourceInformation> VideoSources { get; }

		bool HasLibraryBeenUpdated { get; }

		List<CitpElementLibraryUpdatedInformation> GetLibraryUpdateMessages();

		List<CitpElementLibraryInformation> GetElementLibraryInformation(MsexLibraryType libraryType,
			MsexLibraryId? parentLibraryId, List<byte> libraryIndices);

		List<CitpMediaInformation> GetMediaElementInformation(MsexId libraryId, List<byte> elementNumbers);
		List<CitpEffectInformation> GetEffectElementInformation(MsexId libraryId, List<byte> elementNumbers);

		List<CitpGenericInformation> GetGenericElementInformation(MsexLibraryType libraryType, MsexLibraryId libraryId,
			List<byte> elementNumbers);

		List<Tuple<MsexId, CitpImage>> GetElementLibraryThumbnails(CitpImageRequest request, MsexLibraryType libraryType,
			List<MsexId> libraryIds);

		List<Tuple<byte, CitpImage>> GetElementThumbnails(CitpImageRequest request, MsexLibraryType libraryType,
			MsexId libraryId, List<byte> elementNumbers);

		[CanBeNull]
		CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request);
	}



	[PublicAPI]
	public interface ICitpLayer
	{
		CitpDmxConnectionString DmxSource { get; }

		int PhysicalOutput { get; }
		MsexLibraryType MediaLibraryType { get; }
		int MediaLibraryIndex { get; }
		MsexLibraryId MediaLibraryId { get; }
		int MediaIndex { get; }
		string MediaName { get; }
		uint MediaFrame { get; }
		uint MediaNumFrames { get; }
		int MediaFps { get; }
		MsexLayerStatusFlags LayerStatusFlags { get; }
	}
}