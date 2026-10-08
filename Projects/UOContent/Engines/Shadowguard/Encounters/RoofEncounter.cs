using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class RoofEncounter : ShadowguardEncounter
{
    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardBoss CurrentBoss { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public LadyMinax Minax { get; set; }

    public List<Type> Bosses { get; set; }

    private static readonly Type[] _bossRoster = { typeof(Anon), typeof(Virtuebane), typeof(Ozymandias), typeof(Juonar) };

    public override TimeSpan EncounterDuration => TimeSpan.MaxValue;
    public override TimeSpan ResetDuration => TimeSpan.FromMinutes(5);

    public override Type AddonType => null;

    public override void Setup()
    {
        Bosses = new List<Type>(_bossRoster);

        for (var i = 0; i < 15; i++)
        {
            var t = Bosses[Utility.Random(Bosses.Count)];
            Bosses.Remove(t);
            Bosses.Insert(0, t);
        }

        var p = SpawnPoints[0];
        ConvertOffset(ref p);

        Minax = new LadyMinax();
        Minax.MoveToWorld(p, Map.TerMur);
        Minax.Home = p;
        Minax.RangeHome = 5;

        Timer.DelayCall(TimeSpan.FromSeconds(35), () =>
        {
            // Minax Says: Well Well! You've managed to get through my fortress but alas you
            // shall never foil my plans! Perhaps you need some friends to play with? Muwahhahah!
            SendPartyMessage(1156255);
        });

        Timer.DelayCall(TimeSpan.FromSeconds(40), SpawnBoss);
    }

    public RoofEncounter() : base(EncounterType.Roof)
    {
    }

    public RoofEncounter(ShadowguardInstance instance) : base(EncounterType.Roof, instance)
    {
    }

    public override void CheckEncounter()
    {
    }

    public override void CompleteEncounter()
    {
        base.CompleteEncounter();

        using var mobiles = Region.GetMobilesPooled();
        foreach (var m in mobiles)
        {
            if (m is PlayerMobile pm)
            {
                Controller.CompleteRoof(pm);
            }
        }
    }

    public override void OnCreatureKilled(BaseCreature bc)
    {
        if (Bosses == null || bc is not ShadowguardBoss || bc != CurrentBoss)
        {
            return;
        }

        if (Bosses.Count > 0)
        {
            SpawnBoss();
            return;
        }

        if (Minax is { Alive: true })
        {
            Minax.Say(1156257); // How...How could this happen! I shall nay be bested by mere mortals! We shall meet again vile heroes!
        }

        GiveRewardTitle();

        CompleteEncounter();
    }

    // ServUO's reward-title system (PlayerMobile.AddRewardTitle) has no equivalent in this
    // codebase — no title/achievement system exists to hang "Destroyer of the Time Rift" on.
    private void GiveRewardTitle()
    {
    }

    public override void ClearItems()
    {
        Minax?.Delete();

        Bosses = null;

        if (CurrentBoss != null)
        {
            if (!CurrentBoss.Deleted)
            {
                CurrentBoss.Delete();
            }

            CurrentBoss = null;
        }
    }

    private void SpawnBoss()
    {
        if (Bosses == null)
        {
            return;
        }

        var p = SpawnPoints[0];
        ConvertOffset(ref p);

        CurrentBoss = Activator.CreateInstance(Bosses[0]) as ShadowguardBoss;
        Bosses.RemoveAt(0);

        CurrentBoss?.MoveToWorld(p, Map.TerMur);

        if (Bosses.Count == 0 && CurrentBoss != null)
        {
            CurrentBoss.IsLastBoss = true;
        }

        if (Minax is not { Alive: true })
        {
            return;
        }

        if (CurrentBoss is Juonar)
        {
            Minax.Say(1156258); // You shall burn as Trinsic burned at the hands of the Vile Lich Juo'nar!
        }
        else if (CurrentBoss is Anon)
        {
            Minax.Say(1156259); // Oh Anon my dear! Deal with these pesky intruders will you? Burn them to ASH!
        }
        else if (CurrentBoss is Virtuebane)
        {
            Minax.Say(1156260); // You didn't think that ridiculous pie trick would work twice in a row? Virtuebane I command thee destroy these vile creatures!
        }
        else
        {
            Minax.Say(1156261); // And now you shall bow to the King of Kings! Suffer at the hands of the Feudal Lord Ozymandias!
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.Write(CurrentBoss);
        writer.Write(Minax);
        writer.WriteEncodedInt(Bosses?.Count ?? 0);

        if (Bosses != null)
        {
            foreach (var b in Bosses)
            {
                writer.Write(b.Name);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Bosses = new List<Type>();

        CurrentBoss = reader.ReadEntity<Mobile>() as ShadowguardBoss;
        Minax = reader.ReadEntity<Mobile>() as LadyMinax;

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (AssemblyHandler.FindTypeByName(reader.ReadString()) is { } boss)
            {
                Bosses.Add(boss);
            }
        }

        if (CurrentBoss == null && !Completed)
        {
            Completed = true;
        }
    }
}
