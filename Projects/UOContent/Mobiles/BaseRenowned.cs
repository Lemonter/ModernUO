using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Mobiles/Normal/BaseRenowned.cs) — base for "Renowned"
///     tier creatures (Citadel/TerMur content): on death, tracks which players actually
///     damaged it and has a small chance to hand one of them a unique or shared artifact
///     from its own drop lists.
///
///     Trailing speed args dropped from the AIType/FightMode constructor — see
///     DespiseCreature's matching comment (Engines/Despise/DespiseCreature.cs), same reason.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseRenowned : BaseCreature
{
    private Dictionary<Mobile, int> _damageEntries;

    public BaseRenowned(AIType aiType) : this(aiType, FightMode.Closest)
    {
    }

    public BaseRenowned(AIType aiType, FightMode mode) : base(aiType, mode, 18, 1)
    {
    }

    public abstract Type[] UniqueSAList { get; }
    public abstract Type[] SharedSAList { get; }

    public virtual bool NoGoodies => false;

    public virtual void RegisterDamageTo(Mobile m)
    {
        if (m == null)
        {
            return;
        }

        foreach (var de in m.DamageEntries)
        {
            var damager = de.Damager;
            var master = damager.GetDamageMaster(m);

            if (master != null)
            {
                damager = master;
            }

            RegisterDamage(damager, de.DamageGiven);
        }
    }

    public void RegisterDamage(Mobile from, int amount)
    {
        if (from == null || !from.Player)
        {
            return;
        }

        _damageEntries[from] = _damageEntries.GetValueOrDefault(from) + amount;
    }

    public void AwardArtifact(Item artifact)
    {
        if (artifact == null)
        {
            return;
        }

        var totalDamage = 0;
        var validEntries = new Dictionary<Mobile, int>();

        foreach (var (mobile, damage) in _damageEntries)
        {
            if (IsEligible(mobile, artifact))
            {
                validEntries.Add(mobile, damage);
                totalDamage += damage;
            }
        }

        if (totalDamage <= 0)
        {
            artifact.Delete();
            return;
        }

        var randomDamage = Utility.RandomMinMax(1, totalDamage);
        totalDamage = 0;

        foreach (var (mobile, damage) in _damageEntries)
        {
            totalDamage += damage;

            if (totalDamage > randomDamage)
            {
                GiveArtifact(mobile, artifact);
                return;
            }
        }

        artifact.Delete();
    }

    public void GiveArtifact(Mobile to, Item artifact)
    {
        if (to == null || artifact == null)
        {
            return;
        }

        var pack = to.Backpack;

        if (pack == null || !pack.TryDropItem(to, artifact, false))
        {
            artifact.Delete();
        }
        else
        {
            to.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
            to.PlaySound(0x5B4);
        }
    }

    public bool IsEligible(Mobile m, Item artifact) =>
        m.Player && m.Alive && m.InRange(Location, 32) && m.Backpack != null && m.Backpack.CheckHold(m, artifact, false);

    public Item GetArtifact()
    {
        var random = Utility.RandomDouble();

        if (0.05 >= random)
        {
            return CreateArtifact(UniqueSAList);
        }

        if (0.15 >= random)
        {
            return CreateArtifact(SharedSAList);
        }

        return null;
    }

    public Item CreateArtifact(Type[] list)
    {
        if (list.Length == 0)
        {
            return null;
        }

        return Loot.Construct(list[Utility.Random(list.Length)]);
    }

    public override bool OnBeforeDeath()
    {
        if (!NoKillAwards && !NoGoodies)
        {
            _damageEntries = new Dictionary<Mobile, int>();

            RegisterDamageTo(this);
            AwardArtifact(GetArtifact());
        }

        return base.OnBeforeDeath();
    }
}
