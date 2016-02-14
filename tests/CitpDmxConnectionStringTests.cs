using System;
using FluentAssertions;
using Imp.CitpSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CitpSharp.Tests
{
	[TestClass]
	public class CitpDmxConnectionStringTests
	{
		[TestMethod]
		public void CanParse()
		{
			Action<CitpDmxConnectionString> test = input =>
			{
				var output = CitpDmxConnectionString.Parse(input.ToString());
				output.Should().Be(input, "because parsing the string representation of this object should produce an equal value");
			};

			test(CitpDmxConnectionString.FromArtNet(0, 0, 1));
			test(CitpDmxConnectionString.FromBsre131(1, 1));
			test(CitpDmxConnectionString.FromEtcNet2(1));
			test(CitpDmxConnectionString.FromMaNet(2, 0, 1));
			test(CitpDmxConnectionString.FromMaNet(2, 0, 1, Guid.Parse("{3C980270-3900-47CC-9F5B-8FB2AFDC04D7}")));
		}
	}
}
