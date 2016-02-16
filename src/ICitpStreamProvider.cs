using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	/// Interface allowing <see cref="CitpMediaServerService"/> to publish available streaming sources and fulfill streaming frame requests.
	/// </summary>
	[PublicAPI]
	public interface ICitpStreamProvider
	{
		/// <summary>
		/// The unique identifier of the stream provider
		/// </summary>
		Guid Uuid { get; }

		/// <summary>
		/// An dictionary containing information on available streaming video sources, where the key is the unique video source ID.
		/// </summary>
		IReadOnlyDictionary<int, CitpVideoSourceInformation> VideoSources { get; }

		/// <summary>
		/// Requests a frame from a streaming video source
		/// </summary>
		/// <param name="sourceId">ID of the video source</param>
		/// <param name="request">Image request parameters</param>
		/// <returns>A <see cref="CitpImage"/>, or null if the request was unsuccessful</returns>
		[CanBeNull]
		CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request);
	}
}
