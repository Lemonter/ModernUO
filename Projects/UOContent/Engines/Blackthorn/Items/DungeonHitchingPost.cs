using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's dungeon hitching post (Items/Addons/DungeonHitchingPost.cs) — a stable post
///     that works inside a dungeon: 30 gold per pet, a real week of stable time, and "stable"/"claim"
///     speech commands like an animal trainer.
///
///     Adapted: this is a plain <see cref="Item" /> with a context menu (ServUO's version carried the same
///     contract); pets are added with <c>PlayerMobile.AddStabled</c>; the per-pet claim list gump is
///     dropped — "claim" takes every pet back at once, which is the same rule with one less window; the
///     <c>Allured</c> pet flag does not exist here.
/// </summary>
[Flippable(0x14E7, 0x14E8)]
[SerializationGenerator(0, false)]
public partial class DungeonHitchingPost : Item
{
    public override int LabelNumber => 1025351; // hitching post

    [Constructible]
    public DungeonHitchingPost() : base(0x14E7)
    {
        Movable = false;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!from.Alive)
        {
            return;
        }

        list.Add(new StableEntry());

        if (from is PlayerMobile { Stabled.Count: > 0 })
        {
            list.Add(new ClaimEntry());
        }
    }

    private sealed class StableEntry : ContextMenuEntry
    {
        public StableEntry() : base(6126, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            (target as DungeonHitchingPost)?.BeginStable(from);
        }
    }

    private sealed class ClaimEntry : ContextMenuEntry
    {
        public ClaimEntry() : base(6127, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            (target as DungeonHitchingPost)?.Claim(from);
        }
    }

    private sealed class StableTarget : Target
    {
        private readonly DungeonHitchingPost _post;

        public StableTarget(DungeonHitchingPost post) : base(12, false, TargetFlags.None)
        {
            _post = post;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is BaseCreature pet)
            {
                _post.EndStable(from, pet);
            }
            else if (targeted == from)
            {
                from.SendLocalizedMessage(502672); // HA HA HA! Sorry, I am not an inn.
            }
            else
            {
                from.SendLocalizedMessage(1048053); // You can't stable that!
            }
        }
    }

    public void BeginStable(Mobile from)
    {
        if (Deleted || !from.CheckAlive())
        {
            return;
        }

        if (!(from.Backpack?.GetAmount(typeof(Gold)) >= 30) && Banker.GetBalance(from) < 30)
        {
            from.SendLocalizedMessage(502677); // But thou hast not the funds in thy bank account!
            return;
        }

        // I charge 30 gold per pet for a real week's stable time...
        from.SendLocalizedMessage(1042558);

        from.Target = new StableTarget(this);
    }

    public void EndStable(Mobile from, BaseCreature pet)
    {
        if (Deleted || !from.CheckAlive() || from is not PlayerMobile pm)
        {
            return;
        }

        if (pet.Body.IsHuman)
        {
            from.SendLocalizedMessage(502672); // HA HA HA! Sorry, I am not an inn.
        }
        else if (!pet.Controlled)
        {
            from.SendLocalizedMessage(1048053); // You can't stable that!
        }
        else if (pet.ControlMaster != from)
        {
            from.SendLocalizedMessage(1042562); // You do not own that pet!
        }
        else if (pet.IsDeadPet)
        {
            from.SendLocalizedMessage(1049668); // Living pets only, please.
        }
        else if (pet.Summoned)
        {
            from.SendLocalizedMessage(502673); // I can not stable summoned creatures.
        }
        else if (pet is PackLlama or PackHorse or Beetle && pet.Backpack?.Items.Count > 0)
        {
            from.SendLocalizedMessage(1042563); // You need to unload your pet.
        }
        else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
        {
            from.SendLocalizedMessage(1042564); // I'm sorry.  Your pet seems to be busy.
        }
        else if (pm.Stabled.Count >= AnimalTrainer.GetMaxStabled(from))
        {
            from.SendLocalizedMessage(1042565); // You have too many pets in the stables!
        }
        else if (from.Backpack?.ConsumeTotal(typeof(Gold), 30) == true || Banker.Withdraw(from, 30))
        {
            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;
            pet.Internalize();

            pet.SetControlMaster(null);
            pet.SummonMaster = null;

            pet.IsStabled = true;
            pet.StabledBy = from;

            if (Core.SE)
            {
                pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy
            }

            pm.AddStabled(pet);

            from.SendLocalizedMessage(Core.AOS ? 1049677 : 502679);
        }
        else
        {
            from.SendLocalizedMessage(502677); // But thou hast not the funds in thy bank account!
        }
    }

    public void Claim(Mobile from)
    {
        if (Deleted || !from.CheckAlive() || from is not PlayerMobile pm)
        {
            return;
        }

        var claimed = false;
        var stabled = 0;

        using var queue = PooledRefQueue<Mobile>.Create();

        foreach (var m in pm.Stabled)
        {
            var pet = m as BaseCreature;

            if (pet?.Deleted != false)
            {
                if (pet != null)
                {
                    pet.IsStabled = false;
                    pet.StabledBy = null;
                }

                queue.Enqueue(pet);
                continue;
            }

            ++stabled;

            var creature = pet;

            if (from.Followers + creature.ControlSlots <= from.FollowersMax)
            {
                creature.SetControlMaster(from);

                if (creature.Summoned)
                {
                    creature.SummonMaster = from;
                }

                creature.ControlTarget = from;
                creature.ControlOrder = OrderType.Follow;

                creature.MoveToWorld(from.Location, from.Map);

                creature.IsStabled = false;

                if (Core.SE)
                {
                    creature.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy
                }

                queue.Enqueue(creature);
                claimed = true;
            }
            else
            {
                // ~1_NAME~ remained in the stables because you have too many followers.
                from.SendLocalizedMessage(1049612, creature?.Name);
            }
        }

        while (queue.TryDequeue(out var pet))
        {
            pm.RemoveStabled(pet);
        }

        if (claimed)
        {
            from.SendLocalizedMessage(1042559); // Here you go... and good day to you!
        }
        else if (stabled == 0)
        {
            from.SendLocalizedMessage(502671); // But I have no animals stabled with me at the moment!
        }
    }

    public override bool HandlesOnSpeech => true;

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!e.Handled && e.HasKeyword(0x0008)) // *stable*
        {
            e.Handled = true;
            BeginStable(e.Mobile);
        }
        else if (!e.Handled && e.HasKeyword(0x0009)) // *claim*
        {
            e.Handled = true;
            Claim(e.Mobile);
        }
        else
        {
            base.OnSpeech(e);
        }
    }
}
