using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Blackthorn;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Creatures/AgentoftheCrown.cs) — the officer who
///     takes Minax-crested artifacts off your hands for Blackthorn points and sells the Blackthorn set
///     back for them.
///
///     Three ServUO pieces do not exist here and were replaced rather than faked:
///       - <c>BaseTurnInMobile</c> / <c>ShrineHealer</c> (its collector NPC base, with the tile-button
///         turn-in gump). The agent is a stationary vendor-like creature with two context entries: turn an
///         artifact in (target it) and claim rewards (opens <see cref="BlackthornRewardGump" />).
///       - <c>ReforgedSuffix.Minax</c> — there is no reforging engine, so invasion artifacts carry no crest
///         property. The invasion controller stamps its drops with the marker name below instead, which is
///         what <see cref="IsRedeemableItem" /> checks.
///       - <c>PointsSystem.Blackthorn</c> — points live on the invasion controller's own tally.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AgentOfTheCrown : BaseCreature
{
    /// <summary>Stamped on invasion drops so they can be identified without a reforging engine.</summary>
    public const string MinaxArtifactName = "артефакт Минакса";

    public override bool IsInvulnerable => true;
    public override bool DisallowAllMoves => true;
    public override bool ClickTitle => true;
    public override bool CanTeach => false;

    [Constructible]
    public AgentOfTheCrown() : base(AIType.AI_Vendor, FightMode.None, 2, 1)
    {
        Name = RussianNames.RandomMale();
        Title = "агент короны";
        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();
        HairItemID = 0x2047;
        HairHue = 0x46D;

        AddItem(new ChainChest { Hue = 2106 });
        AddItem(new ThighBoots { Hue = 2106 });
        AddItem(new Obi { Hue = 1775 });
        AddItem(new BodySash { Hue = 1775 });
        AddItem(new GoldRing());

        // ServUO also dresses him in an Epaulette; that base clothing item does not exist here (only the
        // crest variants do), so the shoulder piece is omitted rather than substituted.

        PackDisplay(new CloakBearingTheCrestOfBlackthorn());
        PackDisplay(new GargishClothWingArmorBearingTheCrestOfBlackthorn());
        PackDisplay(new DoubletBearingTheCrestOfBlackthorn());
        PackDisplay(new GargishSashBearingTheCrestOfBlackthorn());
        PackDisplay(new SurcoatBearingTheCrestOfBlackthorn());
        PackDisplay(new TunicBearingTheCrestOfBlackthorn());
    }

    private void PackDisplay(Item item)
    {
        item.Movable = false;
        PackItem(item);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1154517); // Minax Artifact Turn in Officer
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from is PlayerMobile)
        {
            list.Add(new TurnInEntry(this));
            list.Add(new ClaimEntry(this));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile && from.InRange(Location, 5))
        {
            SendRewardGump(from);
        }
    }

    public void SendRewardGump(Mobile m)
    {
        if (m.Player && m.CheckAlive())
        {
            m.SendGump(new BlackthornRewardGump(this, (PlayerMobile)m));
        }
    }

    /// <summary>ServUO accepted any reforged Minax artifact; here the marker name stands in for the crest.</summary>
    public static bool IsRedeemableItem(Item item) =>
        item?.Name == MinaxArtifactName && item is BaseWeapon or BaseArmor or BaseJewel or BaseClothing;

    public static void AwardPoints(Mobile pm, int amount)
    {
        var controller = pm.Map == Map.Felucca ? InvasionController.FelInstance : InvasionController.TramInstance;

        controller?.AwardPoints(pm, amount);
    }

    private sealed class TurnInEntry : ContextMenuEntry
    {
        private readonly AgentOfTheCrown _agent;

        public TurnInEntry(AgentOfTheCrown agent) : base(1154571, 2) // Turn In Minax Artifacts
        {
            _agent = agent;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_agent?.Deleted != false)
            {
                return;
            }

            from.SendLocalizedMessage(1154520); // Click a minor artifact to turn in for reward points.
            from.Target = new TurnInTarget(_agent);
        }
    }
    private sealed class TurnInTarget : Target
    {
        private readonly AgentOfTheCrown _agent;

        public TurnInTarget(AgentOfTheCrown agent) : base(2, false, TargetFlags.None)
        {
            _agent = agent;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_agent?.Deleted != false || from.Backpack == null)
            {
                return;
            }

            if (targeted is not Item item || !item.IsChildOf(from.Backpack))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1154519); // Bring me items bearing the crest...
                return;
            }

            if (!IsRedeemableItem(item))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1154519);
                return;
            }

            AwardPoints(from, 1);
            item.Delete();
        }
    }

    private sealed class ClaimEntry : ContextMenuEntry
    {
        private readonly AgentOfTheCrown _agent;

        public ClaimEntry(AgentOfTheCrown agent) : base(1154572, 2) // Claim Blackthorn Artifacts
        {
            _agent = agent;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_agent?.Deleted == false)
            {
                _agent.SendRewardGump(from);
            }
        }
    }
}