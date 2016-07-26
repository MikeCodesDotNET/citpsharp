using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
    public class ElementLibrary
    {
		public ElementLibrary(MsexLibraryType libraryType, ElementLibraryInformation libraryInformation, IEnumerable<ElementInformation> elements)
		{
			LibraryType = libraryType;
			LibraryInformation = libraryInformation;
			Elements = elements.ToImmutableDictionary(e => e.ElementNumber);
		}

		public MsexLibraryType LibraryType { get; }

		public ElementLibraryInformation LibraryInformation { get; }

		public ImmutableDictionary<byte, ElementInformation> Elements { get; }
    }
}
