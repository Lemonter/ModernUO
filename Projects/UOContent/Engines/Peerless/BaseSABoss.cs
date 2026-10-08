using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/BaseSABosses.cs). The Stygian Abyss
/// bosses reuse the peerless creature base but pay out differently: instead of the ML special
/// they roll once on death for an artifact — 5 % from the boss's own unique list, another 10 %
/// from the list it shares with the other SA bosses — and hand it to one participant chosen by
/// weighted damage.
///
/// Two things the original leaves half-finished, kept in mind rather than copied:
/// its `AwardArtifact` builds a filtered `validEntries` dictionary and then never reads it,
/// drawing from the unfiltered table instead, so an ineligible player can win the roll and the
/// artifact is silently deleted; here the draw runs over the eligible entries it just
/// collected, which is what the filtering was plainly for. And its `OnDeath` collects a list of
/// players with looting rights and does nothing with it at all — dropped.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseSABoss : BasePeerless
{
    public BaseSABoss(
        AIType aiType, FightMode fightMode = FightMode.Closest, int rangePerception = 10,
        int rangeFight = 1
    ) : base(aiType, fightMode, rangePerception, rangeFight)
    {
    }

    /// <summary>Artifacts only this boss drops.</summary>
    public abstract Type[] UniqueSAList { get; }

    /// <summary>Artifacts every Stygian Abyss boss can drop.</summary>
    public abstract Type[] SharedSAList { get; }

    public virtual bool NoGoodies => false;

    public override bool GiveMLSpecial => false;
    public override bool DropPrimer => false;

    public override bool OnBeforeDeath()
    {
        if (!NoKillAwards)
        {
            AwardArtifact(GetArtifact());
        }

        return base.OnBeforeDeath();
    }

    public Item GetArtifact()
    {
        var random = Utility.RandomDouble();

        if (random <= 0.05)
        {
            return CreateArtifact(UniqueSAList);
        }

        return random <= 0.15 ? CreateArtifact(SharedSAList) : null;
    }

    public static Item CreateArtifact(Type[] list) =>
        list.Length == 0 ? null : Loot.Construct(list.RandomElement());

    public void AwardArtifact(Item artifact)
    {
        if (artifact == null)
        {
            return;
        }

        var damageEntries = new Dictionary<Mobile, int>();
        RegisterDamageTo(damageEntries, this);

        var totalDamage = 0;

        using var eligible = PooledRefList<Mobile>.Create();
        using var weights = PooledRefList<int>.Create();

        foreach (var (mobile, damage) in damageEntries)
        {
            if (IsEligible(mobile, artifact))
            {
                eligible.Add(mobile);
                weights.Add(damage);
                totalDamage += damage;
            }
        }

        if (totalDamage <= 0)
        {
            artifact.Delete();
            return;
        }

        var roll = Utility.RandomMinMax(1, totalDamage);
        var running = 0;

        for (var i = 0; i < eligible.Count; i++)
        {
            running += weights[i];

            if (running >= roll)
            {
                GiveArtifact(eligible[i], artifact);
                return;
            }
        }

        artifact.Delete();
    }

    public static void GiveArtifact(Mobile to, Item artifact)
    {
        if (to == null || artifact == null)
        {
            return;
        }

        to.PlaySound(0x5B4);

        if (to.Backpack?.TryDropItem(to, artifact, false) != true)
        {
            artifact.Delete();
            return;
        }

        // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
        to.SendLocalizedMessage(1062317);
    }

    public bool IsEligible(Mobile m, Item artifact) =>
        m.Player && m.Alive && m.InRange(Location, 32) && m.Backpack?.CheckHold(m, artifact, false) == true;

    /// <summary>Rolls the boss's damage table up into per-player totals, crediting a pet's
    /// damage to its master.</summary>
    private static void RegisterDamageTo(Dictionary<Mobile, int> entries, Mobile m)
    {
        if (m == null)
        {
            return;
        }

        foreach (var de in m.DamageEntries)
        {
            var damager = de.Damager;

            if (damager == null)
            {
                continue;
            }

            damager = damager.GetDamageMaster(m) ?? damager;

            if (!damager.Player)
            {
                continue;
            }

            entries[damager] = entries.GetValueOrDefault(damager) + de.DamageGiven;
        }
    }
}
