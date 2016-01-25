//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
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

		List<Tuple<MsexId, CitpImage>> GetElementLibraryThumbnails(MsexLibraryType libraryType, List<MsexId> libraryIds);
		List<Tuple<byte, CitpImage>> GetElementThumbnails(MsexLibraryType libraryType, MsexId libraryId, List<byte> elementNumbers);

		[CanBeNull]
		IEnumerable<CitpImage> GetVideoSourceFrame(int sourceId, IEnumerable<CitpImageRequest> requests);
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