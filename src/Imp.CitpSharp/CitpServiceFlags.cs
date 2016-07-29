using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	[Flags]
    public enum CitpServiceFlags
    {
		None = 0,
		DisableStreaming = 1 << 0,
		DisableLibraryInformation = 1 << 1,
		DisableElementInformation = 1 << 2,
		DisableLibraryThumbnails = 1 << 3,
		DisableElementThumbnails = 1 << 4,
		DisableLayerStatus = 1 << 5,
		RunStreamThread = 1 << 6,
		UseLegacyMulticastIp = 1 << 7
    }
}
