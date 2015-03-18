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

using Imp.CitpSharp.Packets.Msex;
using System;
using System.Collections.Generic;
using System.Drawing;

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

		List<MsexVersion> SupportedMsexVersions { get; }

		List<MsexLibraryType> SupportedLibraryTypes { get; }
		
		List<MsexImageFormat> SupportedThumbnailFormats { get; }
		List<MsexImageFormat> SupportedStreamFormats { get; }

		List<ICitpLayer> Layers { get; }

		bool HasLibraryBeenUpdated { get; }

		List<ElementLibraryUpdatedMessagePacket> GetLibraryUpdateMessages();

		List<ElementLibraryInformation> GetElementLibraryInformation(MsexLibraryType libraryType, MsexLibraryId? parentLibraryId, List<byte> libraryIndices);

		List<MediaInformation> GetMediaElementInformation(MsexId libraryId, List<byte> elementNumbers);
		List<EffectInformation> GetEffectElementInformation(MsexId libraryId, List<byte> elementNumbers);
		List<GenericInformation> GetGenericElementInformation(MsexLibraryType libraryType, MsexLibraryId libraryId, List<byte> elementNumbers);

		List<Tuple<MsexId, Image>> GetElementLibraryThumbnails(MsexLibraryType libraryType, List<MsexId> libraryIds);
		List<Tuple<byte, Image>> GetElementThumbnails(MsexLibraryType libraryType, MsexId libraryId, List<byte> elementNumbers);
	}

	public interface ICitpLayer
	{
		DmxConnectionString DmxSource { get; }

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

	//public class CitpMediaServerRemote
	//{
	//	List<CitpVideoSource> _videoSources = new List<CitpVideoSource>();
	//	List<CitpVideoSource> VideoSources
	//	{
	//		get { return _videoSources; }
	//	}

		
	//}

	//public class CitpVideoSource
	//{
	//	public CitpVideoSource(CitpPeer peer, int id)
	//	{
	//		Peer = peer;
	//		Id = id;
	//	}

	//	public CitpPeer Peer { get; private set; }
	//	public int Id { get; private set; }

	//	public string Name { get; set; }
	//	public int PhysicalOutput { get; set; }
	//	public int LayerNumber { get; set; }
	//	public bool HasFX { get; set; }
	//	public Size Size { get; set; }
	//}

	
}
