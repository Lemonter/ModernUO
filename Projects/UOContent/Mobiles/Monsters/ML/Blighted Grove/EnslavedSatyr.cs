using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EnslavedSatyr : Satyr
    {
        [Constructible]
        public EnslavedSatyr()
        {
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public override string CorpseName => "труп порабощённого сатира";
        public override string DefaultName => "порабощённый сатир";
    }
}
