using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	/// Interface allowing <see cref="CitpService"/> access to properties of and ability to request thumbnails/streaming frames from a host media server.
	/// </summary>
	[PublicAPI]
	public interface ICitpMediaServer
	{
		/// <summary>
		/// The name of this media server to be broadcast to other CITP peers
		/// </summary>
		string PeerName { get; }

		/// <summary>
		/// The name of the media server product
		/// </summary>
		string ProductName { get; }

		/// <summary>
		/// The major version of the media server product
		/// </summary>
		int ProductVersionMajor { get; }

		/// <summary>
		/// The minor version of the media server product
		/// </summary>
		int ProductVersionMinor { get; }

		/// <summary>
		/// The bugfix version of the media server product
		/// </summary>
		int ProductVersionBugfix { get; }

		/// <summary>
		/// The unique identifier of the media server
		/// </summary>
		Guid Uuid { get; }

		/// <summary>
		/// An enumerable of MSEX versions supported by this media server
		/// </summary>
		IEnumerable<MsexVersion> SupportedMsexVersions { get; }

		/// <summary>
		/// An enumerable of library types available on this media server
		/// </summary>
		IEnumerable<MsexLibraryType> SupportedLibraryTypes { get; }

		/// <summary>
		/// An enumerable of supported image formats for CITP peers making thumbnail requests from this media server
		/// </summary>
		IEnumerable<MsexImageFormat> SupportedThumbnailFormats { get; }

		/// <summary>
		/// An enumerable of supported image formats for CITP peers making streaming requests from this media server
		/// </summary>
		IEnumerable<MsexImageFormat> SupportedStreamFormats { get; }

		/// <summary>
		/// An enumerable of available layers on this media server
		/// </summary>
		IEnumerable<ICitpLayer> Layers { get; }

		/// <summary>
		/// An dictionary containing information on available video sources on this media server, where the key is the unique video source ID.
		/// </summary>
		IReadOnlyDictionary<int, CitpVideoSourceInformation> VideoSources { get; }

		/// <summary>
		/// When true, indicates to <see cref="CitpService"/> that the contents of one of the media server libraries has been updated
		/// </summary>
		bool HasLibraryBeenUpdated { get; }

		/// <summary>
		/// Requests information from the media server on which libraries have been updated.
		/// </summary>
		/// <returns>An enumerable of library update information objects</returns>
		IEnumerable<CitpElementLibraryUpdatedInformation> GetLibraryUpdateMessages();

		/// <summary>
		/// Requests information from the media server about specific element libraries.
		/// </summary>
		/// <param name="libraryType">The type of the libraries</param>
		/// <param name="parentLibraryId">The ID of the parent library, or the root ID if there is no parent</param>
		/// <param name="libraryIndices">The indices of the libraries to request information on</param>
		/// <returns>An enumerable of library information for the library indices requested</returns>
		IEnumerable<CitpElementLibraryInformation> GetElementLibraryInformation(MsexLibraryType libraryType,
			MsexLibraryId? parentLibraryId, IEnumerable<byte> libraryIndices);

		/// <summary>
		/// Requests information from the media server about specific media elements in a library
		/// </summary>
		/// <param name="libraryId">ID of the library</param>
		/// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
		/// <returns>An enumerable of media element information for the element numbers requested</returns>
		IEnumerable<CitpMediaInformation> GetMediaElementInformation(MsexId libraryId, IEnumerable<byte> elementNumbers);

		/// <summary>
		/// Requests information from the media server about specific effect elements in a library
		/// </summary>
		/// <param name="libraryId">ID of the library</param>
		/// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
		/// <returns>An enumerable of effect element information for the element numbers requested</returns>
		IEnumerable<CitpEffectInformation> GetEffectElementInformation(MsexId libraryId, IEnumerable<byte> elementNumbers);

		/// <summary>
		/// Requests information from the media server about specific generic elements in a library
		/// </summary>
		/// <param name="libraryType">Type of the library</param>
		/// <param name="libraryId">ID of the library</param>
		/// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
		/// <returns>An enumerable of generic element information for the element numbers requested</returns>
		IEnumerable<CitpGenericInformation> GetGenericElementInformation(MsexLibraryType libraryType, MsexLibraryId libraryId,
			IEnumerable<byte> elementNumbers);

		/// <summary>
		/// Requests library thumbnails from the media server for specific libraries
		/// </summary>
		/// <param name="request">Image request parameters to be used for all requested thumbnails</param>
		/// <param name="libraryType">Type of the libraries to request thumbnails for</param>
		/// <param name="libraryIds">IDs of the libraries to request thumbnails for</param>
		/// <returns>An enumerable of Tuples with the <see cref="MsexId"/> and <see cref="CitpImage"/> for each requested library</returns>
		IEnumerable<Tuple<MsexId, CitpImage>> GetElementLibraryThumbnails(CitpImageRequest request, MsexLibraryType libraryType,
			IEnumerable<MsexId> libraryIds);

		/// <summary>
		/// Requests element thumbnails from the media server for specific elements
		/// </summary>
		/// <param name="request">Image request parameters to be used for all requested thumbnails</param>
		/// <param name="libraryType">Type of the library containing elements to request thumbnails for</param>
		/// <param name="libraryId">ID of the library containing elements to request thumbnails for</param>
		/// <param name="elementNumbers">Element numbers of the elements to request thumbnails for</param>
		/// <returns>An enumerable of Tuples with the element number and <see cref="CitpImage"/> for each requested library</returns>
		IEnumerable<Tuple<byte, CitpImage>> GetElementThumbnails(CitpImageRequest request, MsexLibraryType libraryType,
			MsexId libraryId, IEnumerable<byte> elementNumbers);

		/// <summary>
		/// Requests a frame from a streaming video source
		/// </summary>
		/// <param name="sourceId">ID of the video source</param>
		/// <param name="request">Image request parameters</param>
		/// <returns>A <see cref="CitpImage"/>, or null if the request was unsuccessful</returns>
		[CanBeNull]
		CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request);
	}


	/// <summary>
	/// Interface allowing <see cref="CitpService"/> access to properties of individual layers on a host media server.
	/// </summary>
	/// <seealso cref="ICitpMediaServer"/>
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