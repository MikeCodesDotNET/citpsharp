namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetVideoSourcesMessagePacket : MsexPacket
	{
		public GetVideoSourcesMessagePacket()
			: base(MsexMessageType.GetVideoSourcesMessage) { }
	}
}