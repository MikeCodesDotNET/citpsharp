using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    [PublicAPI]
    public interface ICitpServerDevice : ICitpDevice
    {
        /// <summary>
        ///     The unique identifier of the stream provider
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        ///     An hashset of supported image formats for CITP peers making streaming requests from this media server
        /// </summary>
        ImmutableHashSet<MsexImageFormat> SupportedStreamFormats { get; }

        /// <summary>
        ///     An dictionary containing information on available streaming video sources, where the key is the unique video source
        ///     ID.
        /// </summary>
        ImmutableDictionary<int, CitpVideoSourceInformation> VideoSourceInformation { get; }

        /// <summary>
        ///     Requests a frame from a streaming video source
        /// </summary>
        /// <param name="sourceId">ID of the video source</param>
        /// <param name="request">Image request parameters</param>
        /// <returns>A <see cref="CitpImage" />, or null if the request was unsuccessful</returns>
        [CanBeNull]
        CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request);
    }
}