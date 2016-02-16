using JetBrains.Annotations;

namespace Imp.CitpSharp
{

	[PublicAPI]
	public interface ICitpDevice
	{
		/// <summary>
		/// The name of this CITP peer to be broadcast to other CITP peers
		/// </summary>
		string PeerName { get; }
	}
}
