using System;
using System.Collections.Generic;
using FluentAssertions;
using Imp.CitpSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Imp.CitpSharp.Tests
{
	[TestClass]
	public class CitpDmxConnectionStringTests
	{
		[TestMethod]
		public void CanParse(DmxPatchInfo input)
		{
			var output = DmxPatchInfo.Parse(input.ToString());
			output.Should().Be(input, "because parsing the string representation of this object should produce an equal value");
		}

		public static IEnumerable<object[]> PatchInfo =>
			new[]
			{
				new object[] {DmxPatchInfo.FromArtNet(0, 0, 1)},
				new object[] {DmxPatchInfo.FromBsre131(1, 1)},
				new object[] {DmxPatchInfo.FromEtcNet2(1)},
				new object[] {DmxPatchInfo.FromMaNet(2, 0, 1)},
				new object[] {DmxPatchInfo.FromMaNet(2, 0, 1, Guid.Parse("{3C980270-3900-47CC-9F5B-8FB2AFDC04D7}"))}
			};
	}
}
