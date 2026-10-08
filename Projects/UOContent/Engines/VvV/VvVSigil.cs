using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/VvVSigil.cs).
[SerializationGenerator(0, false)]
public partial class VvVSigil : Item, IRevealableItem
{
    public const int OwnershipHue = 0xB;

    // Not a [SerializableField]: VvVBattle is a [PropertyObject], not an Item/Mobile entity,
    // so codegen can't write/read it directly. It's persisted separately (as part of
    // ViceVsVirtueSystem's own save via VvVPersistence) and reattached in AfterDeserialization
    // — there's always exactly one live battle while a sigil instance exists.
    public VvVBattle Battle { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _homeLocation;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _lastStolen;

    public override int LabelNumber => 1123391; // Sigil
    public override bool HandlesOnMovement => !Visible;
    public bool CheckWhenHidden => true;

    [Constructible]
    public VvVSigil() : this(null, Point3D.Zero)
    {
    }

    public VvVSigil(VvVBattle battle, Point3D home) : base(0x99C7)
    {
        Battle = battle;
        Visible = false;
        Hue = 2721;
        LootType = LootType.Cursed;
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Battle = ViceVsVirtueSystem.Instance?.Battle;

    public static bool ExistsOn(Mobile mob, bool vvvOnly = false)
    {
        if (mob?.Backpack == null)
        {
            return false;
        }

        return ViceVsVirtueSystem.Enabled && vvvOnly && mob.Backpack.FindItemByType(typeof(VvVSigil)) != null;
    }

    public void OnStolen(VvVPlayerEntry entry)
    {
        if (Battle != null && RootParent == null)
        {
            Battle.SpawnPriests();
            Battle.Update(null, entry, UpdateType.Steal);

            LastStolen = Core.Now;
            HomeLocation = Location;

            Movable = true;
        }
    }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
        if (LastStolen == DateTime.MinValue)
        {
            from.SendLocalizedMessage(1005225); // You must use the stealing skill to pick up the sigil
            return false;
        }

        return base.CheckLift(from, item, ref reject);
    }

    public void ReturnToHome()
    {
        MoveToWorld(HomeLocation, Map.Felucca);
        Visible = false;
        Movable = false;
    }

    public static bool CheckMovement(PlayerMobile pm, Direction d)
    {
        if (!ViceVsVirtueSystem.Enabled)
        {
            return true;
        }

        var x = pm.X;
        var y = pm.Y;

        Movement.Movement.Offset(d, ref x, ref y);

        var r = Region.Find(new Point3D(x, y, pm.Map.GetAverageZ(x, y)), pm.Map);

        return ViceVsVirtueSystem.IsBattleRegion(r);
    }

    public bool CheckReveal(Mobile m)
    {
        if (!ViceVsVirtueSystem.IsVvV(m))
        {
            return false;
        }

        return Utility.Random(100) <= m.Skills[SkillName.DetectHidden].Value;
    }

    public void OnRevealed(Mobile m) => Visible = true;

    public bool CheckPassiveDetect(Mobile m)
    {
        if (m.InRange(Location, 4))
        {
            var skill = (int)m.Skills[SkillName.DetectHidden].Value;

            if (skill >= 80 && Utility.Random(300) < skill)
            {
                return true;
            }
        }

        return false;
    }
}
