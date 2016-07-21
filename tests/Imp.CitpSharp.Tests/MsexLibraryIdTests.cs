using System.Linq;
using FluentAssertions;
using Imp.CitpSharp;
using Xunit;

namespace Imp.CitpSharp.Tests
{
	public class MsexLibraryIdTests
	{
		[Fact]
		public void CanSort()
		{
			var libraryIds = new[]
			{
				new MsexLibraryId(0),
				new MsexLibraryId(1),
				new MsexLibraryId(2),
				new MsexLibraryId(2, 0, 1),
				new MsexLibraryId(2, 0, 2),
				new MsexLibraryId(1, 1),
				new MsexLibraryId(2, 1),
				new MsexLibraryId(2, 1, 1),
				new MsexLibraryId(2, 1, 2),
				new MsexLibraryId(1, 2),
				new MsexLibraryId(1, 3),
				new MsexLibraryId(1, 4),
				new MsexLibraryId(2, 4),
				new MsexLibraryId(2, 4, 1)
			};

			var orderedLibraryIds = libraryIds.OrderBy(id => id).ToList();

			orderedLibraryIds.Should().ContainInOrder(libraryIds, "because this is the correct order");
		}
	}
}
