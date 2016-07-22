using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
// ReSharper disable UnusedMember.Global

namespace Imp.CitpSharp
{
	internal class CitpId : Attribute
	{
		public CitpId(string id)
		{
			Id = Encoding.UTF8.GetBytes(id.Substring(0, 4));
		}

		public byte[] Id { get; }

		public string IdString => Encoding.UTF8.GetString(Id, 0, Id.Length);
	}



	internal static class CitpEnumHelper
	{
		private static readonly Dictionary<Type, Dictionary<string, Enum>> CitpIdMaps =
			new Dictionary<Type, Dictionary<string, Enum>>();

		/// <summary>
		///     Gets an attribute on an enum field value
		/// </summary>
		/// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
		/// <param name="enumVal">The enum value</param>
		/// <returns>The attribute of type T that exists on the enum value</returns>
		public static T GetCustomAttribute<T>(this Enum enumVal) where T : Attribute
		{
			return enumVal
				.GetType()
				.GetRuntimeField(enumVal.ToString())
				.GetCustomAttribute<T>(false);
		}

		public static T GetEnumFromIdString<T>(string s) where T : struct
		{
			var typeT = typeof(T);

			Dictionary<string, Enum> map;

			if (CitpIdMaps.TryGetValue(typeT, out map))
				return (T)(object)map[s];

			var values = Enum.GetValues(typeT).Cast<Enum>();

			map = values.ToDictionary(v => v.GetCustomAttribute<CitpId>().IdString);

			CitpIdMaps.Add(typeT, map);

			return (T)(object)map[s];
		}
	}



	internal enum CitpLayerType : uint
	{
		[CitpId("PINF")] PeerInformationLayer,
		[CitpId("SDMX")] SendDmxLayer,
		[CitpId("FPTC")] FixturePatchLayer,
		[CitpId("FSEL")] FixtureSelectionLayer,
		[CitpId("FINF")] FixtureInformationLayer,
		[CitpId("MSEX")] MediaServerExtensionsLayer
	}



	internal enum PinfMessageType : uint
	{
		[CitpId("PNam")] PeerNameMessage,
		[CitpId("PLoc")] PeerLocationMessage
	}



	internal enum SdmxMessageType : uint
	{
		[CitpId("Capa")] CapabilitiesMessage,
		[CitpId("UNam")] UniverseNameMessage,
		[CitpId("EnId")] EncryptionIdentifierMessage,
		[CitpId("ChBk")] ChannelBlockMessage,
		[CitpId("ChLs")] ChannelListMessage,
		[CitpId("SXSr")] SetExternalSourceMessage,
		[CitpId("SXUS")] SetExternalUniverseSourceMessage
	}



	internal enum FptcMessageType : uint
	{
		[CitpId("Ptch")] PatchMessage,
		[CitpId("UPtc")] UnpatchMessage,
		[CitpId("SPtc")] SendPatchMessage
	}



	internal enum FselMessageType : uint
	{
		[CitpId("Sele")] SelectMessage,
		[CitpId("DeSe")] DeselectMessage
	}



	internal enum FinfMessageType : uint
	{
		[CitpId("SFra")] SendFramesMessage,
		[CitpId("Fram")] FramesMessage,
		[CitpId("SPos")] SendPositionMessage,
		[CitpId("Posi")] PositionMessage,
		[CitpId("LSta")] LiveStatusMessage
	}



	internal enum MsexMessageType : uint
	{
		[CitpId("CInf")] ClientInformationMessage,
		[CitpId("SInf")] ServerInformationMessage,
		[CitpId("Nack")] NegativeAcknowledgeMessage,
		[CitpId("LSta")] LayerStatusMessage,
		[CitpId("GELI")] GetElementLibraryInformationMessage,
		[CitpId("ELIn")] ElementLibraryInformationMessage,
		[CitpId("ELUp")] ElementLibraryUpdatedMessage,
		[CitpId("GEIn")] GetElementInformationMessage,
		[CitpId("MEIn")] MediaElementInformationMessage,
		[CitpId("EEIn")] EffectElementInformationMessage,
		[CitpId("GLEI")] GenericElementInformationMessage,
		[CitpId("GELT")] GetElementLibraryThumbnailMessage,
		[CitpId("ELTh")] ElementLibraryThumbnailMessage,
		[CitpId("GETh")] GetElementThumbnailMessage,
		[CitpId("EThn")] ElementThumbnailMessage,
		[CitpId("GVSr")] GetVideoSourcesMessage,
		[CitpId("VSrc")] VideoSourcesMessage,
		[CitpId("RqSt")] RequestStreamMessage,
		[CitpId("StFr")] StreamFrameMessage
	}


    [PublicAPI]
	public enum PeerKind
	{
		LightingConsole,
		MediaServer,
		Visualizer,
		OperationHub,
		Unknown
	}



	internal enum SdmxCapability : ushort
	{
		ChannelList = 1,
		ExternalSource = 2,
		PerUniverseExternalSources = 3,
		ArtNetExternalSources = 101,
		Bsre131ExternalSources = 102,
		EtcNet2ExternalSources = 103,
		MaNetExternalSources = 104
	}


	[PublicAPI]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum MsexVersion : ushort
	{
		UnsupportedVersion = 0,
		Version1_0,
		Version1_1,
		Version1_2
	}



	public enum MsexLibraryType : byte
	{
		Media = 1,
		Effects = 2,
		Cues = 3,
		Crossfades = 4,
		Mask = 5,
		BlendPresets = 6,
		EffectPresets = 7,
		ImagePresets = 8,
		Meshes = 9
	}


	[PublicAPI]
	public enum MsexImageFormat : uint
	{
		[CitpId("RGB8")] Rgb8,
		[CitpId("PNG ")] Png,
		[CitpId("JPEG")] Jpeg,
		[CitpId("fJPG")] FragmentedJpeg,
		[CitpId("fPNG")] FragmentedPng
	}


	[PublicAPI]
	[Flags]
	public enum MsexLayerStatusFlags : uint
	{
		None = 0x0000,
		MediaPlaying = 0x0001,
		MediaPlaybackReverse = 0x0002,
		MediaPlaybackLooping = 0x0004,
		MediaPlaybackBouncing = 0x0008,
		MediaPlaybackRandom = 0x0010,
		MediaPaused = 0x0020
	}


	[PublicAPI]
	[Flags]
	public enum MsexElementLibraryUpdatedFlags : byte
	{
		None = 0x00,
		ExistingElementsUpdated = 0x01,
		ElementsAddedOrRemoved = 0x02,
		SubLibrariesUpdated = 0x04,
		SubLibrariesAddedOrRemoved = 0x08
	}


	[PublicAPI]
	[Flags]
	public enum MsexThumbnailFlags : byte
	{
		None = 0x00,
		PreserveAspectRatio = 0x01
	}


	[PublicAPI]
	[Flags]
	public enum MsexVideoSourcesFlags : ushort
	{
		None = 0x0000,
		WithoutEffects = 0x0001
	}
}