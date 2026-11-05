using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunTastyTreat.cs).
///     The Caddellite buff (<c>Caddellite.UpdateBuff</c>) is dropped: that type does not exist here. The pet
///     influence table itself is kept and persisted.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunTastyTreat : Item
{
    public const int Duration = 1; // hours

    public static Dictionary<BaseCreature, DateTime> Table { get; set; }

    public override int LabelNumber => 1158680; // khaldun tasty treat

    [Constructible]
    public KhaldunTastyTreat() : this(1)
    {
    }

    [Constructible]
    public KhaldunTastyTreat(int amount) : base(2424)
    {
        Stackable = true;
        Amount = amount;
    }

    public static bool UnderInfluence(BaseCreature bc) => Table != null && Table.ContainsKey(bc);

    public static BaseCreature GetPetUnderEffects(Mobile m)
    {
        if (Table == null || m is not PlayerMobile pm)
        {
            return null;
        }

        foreach (var follower in pm.AllFollowers)
        {
            if (follower is BaseCreature bc && UnderInfluence(bc))
            {
                return bc;
            }
        }

        return null;
    }

    public static void RemoveInfluence(BaseCreature bc)
    {
        if (Table == null || !Table.ContainsKey(bc))
        {
            return;
        }

        Table.Remove(bc);

        bc.ControlMaster?.SendLocalizedMessage(1158687); // Your pet is no longer Caddellite infused.

        if (Table.Count == 0)
        {
            Table = null;
        }
    }

    public static void SerializeTable(IGenericWriter writer)
    {
        writer.Write(0);
        writer.Write(Table?.Count ?? 0);

        if (Table == null)
        {
            return;
        }

        foreach (var (creature, expiry) in Table)
        {
            writer.Write(creature);
            writer.Write(expiry);
        }
    }

    public static void DeserializeTable(IGenericReader reader)
    {
        reader.ReadInt();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var bc = reader.ReadEntity<Mobile>() as BaseCreature;
            var expiry = reader.ReadDateTime();

            if (bc == null || expiry <= Core.Now)
            {
                continue;
            }

            Table ??= new Dictionary<BaseCreature, DateTime>();
            Table[bc] = expiry;

            Timer.StartTimer(expiry - Core.Now, () => RemoveInfluence(bc));
        }
    }

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
    {
        TryFeed(from, target);

        return false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        m.BeginTarget(2, false, Targeting.TargetFlags.Beneficial, (from, targeted) =>
        {
            if (targeted is Mobile mobile)
            {
                TryFeed(from, mobile);
            }
        });
    }

    private void TryFeed(Mobile from, Mobile target)
    {
        if (target is not BaseCreature { IsDeadBondedPet: false } bc || bc.ControlMaster != from)
        {
            return;
        }

        if (UnderInfluence(bc))
        {
            from.SendLocalizedMessage(1113051); // Your pet is still enjoying the last tasty treat!
            return;
        }

        DoEffects(from, bc);
    }

    public bool DoEffects(Mobile owner, BaseCreature bc)
    {
        owner.SendLocalizedMessage(1158685); // Your pet is now Caddellite infused by this treat.

        bc.PlaySound(0x1EA);
        bc.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
        bc.Loyalty = BaseCreature.MaxLoyalty;

        Table ??= new Dictionary<BaseCreature, DateTime>();
        Table[bc] = Core.Now + TimeSpan.FromHours(Duration);

        Timer.StartTimer(TimeSpan.FromHours(Duration), () => RemoveInfluence(bc));

        Consume();

        return true;
    }
}

public class KhaldunTastyTreatPersistence : GenericPersistence
{
    public KhaldunTastyTreatPersistence() : base("KhaldunTastyTreat", 20)
    {
    }

    public override void Serialize(IGenericWriter writer) => KhaldunTastyTreat.SerializeTable(writer);

    public override void Deserialize(IGenericReader reader) => KhaldunTastyTreat.DeserializeTable(reader);
}
