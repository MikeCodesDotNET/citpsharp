using System.Collections.Generic;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class EffectElementInformationPacket : MsexPacket
	{
		public EffectElementInformationPacket()
			: base(MsexMessageType.EffectElementInformationMessage) { }

	    public EffectElementInformationPacket(MsexVersion version, MsexId library, IEnumerable<EffectInformation> effects,
	        ushort requestResponseIndex = 0)
	        : base(MsexMessageType.EffectElementInformationMessage, version, requestResponseIndex)
	    {
	        Library = library;
	        Effects = effects.ToImmutableSortedSet();
	    }

		public MsexId Library { get; private set; }

		public ImmutableSortedSet<EffectInformation> Effects { get; private set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

            writer.Write(Library, Version);
            writer.Write(Effects, GetCollectionLengthType(), e => e.Serialize(writer, Version));
        }

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

            Library = reader.ReadMsexId(Version);
            Effects = reader.ReadCollection(GetCollectionLengthType(),
                () => EffectInformation.Deserialize(reader, Version))
                .ToImmutableSortedSet();
		}
	}
}