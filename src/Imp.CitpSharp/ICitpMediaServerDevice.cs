using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Interface allowing <see cref="CitpMediaServerService" /> access to properties of a host media server and ability to
    ///     satisfy thumbnail/streaming requests.
    /// </summary>
    [PublicAPI]
    public interface ICitpMediaServerDevice : ICitpServerDevice
    {
        /// <summary>
        ///     The name of the media server product
        /// </summary>
        string ProductName { get; }

        /// <summary>
        ///     The major version of the media server product
        /// </summary>
        int ProductVersionMajor { get; }

        /// <summary>
        ///     The minor version of the media server product
        /// </summary>
        int ProductVersionMinor { get; }

        /// <summary>
        ///     The bugfix version of the media server product
        /// </summary>
        int ProductVersionBugfix { get; }

        /// <summary>
        ///     An enumerable of MSEX versions supported by this media server
        /// </summary>
        IEnumerable<MsexVersion> SupportedMsexVersions { get; }

        /// <summary>
        ///     An enumerable of library types available on this media server
        /// </summary>
        IEnumerable<MsexLibraryType> SupportedLibraryTypes { get; }

        /// <summary>
        ///     An enumerable of supported image formats for CITP peers making thumbnail requests from this media server
        /// </summary>
        IEnumerable<MsexImageFormat> SupportedThumbnailFormats { get; }

        /// <summary>
        ///     An enumerable of available layers on this media server
        /// </summary>
        IEnumerable<ICitpMediaServerLayer> Layers { get; }

        /// <summary>
        ///     When true, indicates to <see cref="CitpMediaServerService" /> that the contents of one of the media server
        ///     libraries has been updated
        /// </summary>
        bool HasLibraryBeenUpdated { get; }

        /// <summary>
        ///     Requests information from the media server on which libraries have been updated.
        /// </summary>
        /// <returns>An enumerable of library update information objects</returns>
        IEnumerable<ElementLibraryUpdatedInformation> GetLibraryUpdateMessages();

        /// <summary>
        ///     Requests information from the media server about specific element libraries.
        /// </summary>
        /// <param name="libraryType">The type of the libraries</param>
        /// <param name="parentLibraryId">The ID of the parent library, or the root ID if there is no parent</param>
        /// <param name="libraryIndices">The indices of the libraries to request information on</param>
        /// <returns>An enumerable of library information for the library indices requested</returns>
        IEnumerable<ElementLibraryInformation> GetElementLibraryInformation(MsexLibraryType libraryType,
            MsexLibraryId? parentLibraryId, IEnumerable<byte> libraryIndices);

        /// <summary>
        ///     Requests information from the media server about specific media elements in a library
        /// </summary>
        /// <param name="libraryId">ID of the library</param>
        /// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
        /// <returns>An enumerable of media element information for the element numbers requested</returns>
        IEnumerable<MediaInformation> GetMediaElementInformation(MsexId libraryId, IEnumerable<byte> elementNumbers);

        /// <summary>
        ///     Requests information from the media server about specific effect elements in a library
        /// </summary>
        /// <param name="libraryId">ID of the library</param>
        /// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
        /// <returns>An enumerable of effect element information for the element numbers requested</returns>
        IEnumerable<EffectInformation> GetEffectElementInformation(MsexId libraryId, IEnumerable<byte> elementNumbers);

        /// <summary>
        ///     Requests information from the media server about specific generic elements in a library
        /// </summary>
        /// <param name="libraryType">Type of the library</param>
        /// <param name="libraryId">ID of the library</param>
        /// <param name="elementNumbers">Element numbers of items in the library to request information on</param>
        /// <returns>An enumerable of generic element information for the element numbers requested</returns>
        IEnumerable<GenericInformation> GetGenericElementInformation(MsexLibraryType libraryType,
            MsexLibraryId libraryId,
            IEnumerable<byte> elementNumbers);

        /// <summary>
        ///     Requests library thumbnails from the media server for specific libraries
        /// </summary>
        /// <param name="request">Image request parameters to be used for all requested thumbnails</param>
        /// <param name="libraryType">Type of the libraries to request thumbnails for</param>
        /// <param name="libraryIds">IDs of the libraries to request thumbnails for</param>
        /// <returns>An enumerable of Tuples with the <see cref="MsexId" /> and <see cref="CitpImage" /> for each requested library</returns>
        IEnumerable<Tuple<MsexId, CitpImage>> GetElementLibraryThumbnails(CitpImageRequest request,
            MsexLibraryType libraryType,
            IEnumerable<MsexId> libraryIds);

        /// <summary>
        ///     Requests element thumbnails from the media server for specific elements
        /// </summary>
        /// <param name="request">Image request parameters to be used for all requested thumbnails</param>
        /// <param name="libraryType">Type of the library containing elements to request thumbnails for</param>
        /// <param name="libraryId">ID of the library containing elements to request thumbnails for</param>
        /// <param name="elementNumbers">Element numbers of the elements to request thumbnails for</param>
        /// <returns>An enumerable of Tuples with the element number and <see cref="CitpImage" /> for each requested library</returns>
        IEnumerable<Tuple<byte, CitpImage>> GetElementThumbnails(CitpImageRequest request, MsexLibraryType libraryType,
            MsexId libraryId, IEnumerable<byte> elementNumbers);
    }



    /// <summary>
    ///     Interface allowing <see cref="CitpMediaServerService" /> access to properties of individual layers on a host media
    ///     server.
    /// </summary>
    /// <seealso cref="ICitpMediaServerDevice" />
    [PublicAPI]
    public interface ICitpMediaServerLayer
    {
        /// <summary>
        ///     DMX patching information for this layer
        /// </summary>
        DmxPatchInfo DmxSource { get; }

        /// <summary>
        ///     Zero-based index indicating the physical output on the media server this layer is linked to
        /// </summary>
        int PhysicalOutput { get; }

        /// <summary>
        ///     The library type for elements which can be loaded to this layer (for MSEX 1.0)
        /// </summary>
        MsexLibraryType MediaLibraryType { get; }

        /// <summary>
        ///     The index of the library containing elements which can be loaded to this layer (for MSEX 1.1+)
        /// </summary>
        int MediaLibraryIndex { get; }

        /// <summary>
        ///     The ID of the library containing elements which can be loaded to this layer
        /// </summary>
        MsexLibraryId MediaLibraryId { get; }

        /// <summary>
        ///     Index of the media loaded to this layer
        /// </summary>
        int MediaIndex { get; }

        /// <summary>
        ///     Name of the media loaded to this layer
        /// </summary>
        string MediaName { get; }

        /// <summary>
        ///     Current frame of the media loaded to this layer
        /// </summary>
        uint MediaFrame { get; }

        /// <summary>
        ///     Frame count of the media loaded to this layer
        /// </summary>
        uint MediaNumFrames { get; }

        /// <summary>
        ///     Frames per second of the media loaded to this layer
        /// </summary>
        int MediaFps { get; }

        /// <summary>
        ///     Flags indicating media playback status information
        /// </summary>
        MsexLayerStatusFlags LayerStatusFlags { get; }
    }
}