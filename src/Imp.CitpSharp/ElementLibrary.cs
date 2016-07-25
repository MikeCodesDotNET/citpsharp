using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
    public class ElementLibrary
    {
		public ElementLibrary(ElementLibraryInformation libraryInformation, IEnumerable<ElementInformation> elements)
		{
			LibraryInformation = libraryInformation;
			Elements = elements.ToImmutableDictionary(e => e.ElementNumber);
		}

		public ElementLibraryInformation LibraryInformation { get; }

		public ImmutableDictionary<byte, ElementInformation> Elements { get; }
    }
}
