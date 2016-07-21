using System;
using System.Collections.Generic;
using FluentAssertions;
using Imp.CitpSharp;
using Xunit;

namespace Imp.CitpSharp.Tests
{
	public class CitpDmxConnectionStringTests
	{
		[Fact, MemberData(nameof(DmxConnectionStrings))]
		public void CanParse(CitpDmxConnectionString input)
		{
			var output = CitpDmxConnectionString.Parse(input.ToString());
			output.Should().Be(input, "because parsing the string representation of this object should produce an equal value");
		}

	    public static IEnumerable<object> DmxConnectionStrings =>
	        new object[]
	        {
	            CitpDmxConnectionString.FromArtNet(0, 0, 1),
	            CitpDmxConnectionString.FromBsre131(1, 1),
	            CitpDmxConnectionString.FromEtcNet2(1),
	            CitpDmxConnectionString.FromMaNet(2, 0, 1),
	            CitpDmxConnectionString.FromMaNet(2, 0, 1, Guid.Parse("{3C980270-3900-47CC-9F5B-8FB2AFDC04D7}"))
	        };
	}
}
