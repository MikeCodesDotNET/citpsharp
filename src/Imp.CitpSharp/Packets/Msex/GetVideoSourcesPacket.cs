namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetVideoSourcesPacket : MsexPacket
	{
		public GetVideoSourcesPacket()
			: base(MsexMessageType.GetVideoSourcesMessage) { }
	}
}