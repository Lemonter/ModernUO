using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Functional/SerpentNest.cs). The CharmMaster/
/// EndCharm-driven OnMoveOver "a charmed snake searches the nest" mechanic dropped — neither
/// exists on BaseCreature here (this codebase has no Charm-taming subsystem, only the
/// standard Tame skill). Everything else — the double-click reach-in-the-nest interaction
/// (find a rare egg / collapse / hatch a swarm of snakes) — ported faithfully.
/// BaseCreature.RemoveOnSave and Map.GetSpawnPosition don't exist here — dropped/simplified
/// to spawning the snakes directly at the nest's own location.</summary>
[SerializationGenerator(0, false)]
public partial class SerpentNest : Item
{
    private static readonly Type[] _snakeTypes =
    {
        typeof(LavaSnake), typeof(Snake), typeof(CoralSnake), typeof(GiantSerpent)
    };

    [Constructible]
    public SerpentNest() : base(0x2233)
    {
        Hue = 0x456;
        Movable = false;
    }

    public override int LabelNumber => 1112582; // a serpent's nest

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(this, 1))
        {
            SendLocalizedMessageTo(from, 1076766); // That is too far away.
            return;
        }

        from.RevealingAction();

        if (0.025 > Utility.RandomDouble())
        {
            from.AddToBackpack(new RareSerpentEgg());
            from.SendLocalizedMessage(1112581); // You reach in and find a rare serpent egg!!
        }
        else
        {
            switch (Utility.Random(3))
            {
                case 0:
                    from.SendLocalizedMessage(1112578); // You try to reach the eggs, but the hole is too deep.
                    break;
                case 1:
                    from.SendLocalizedMessage(1112579); // You reach in but clumsily destroy the eggs inside the nest.
                    Collapse(from);
                    break;
                case 2:
                    from.SendLocalizedMessage(1112580); // Beware! You've hatched the eggs!!
                    HatchEggs(from);

                    from.PrivateOverheadMessage(MessageType.Regular, 33, 1112940, from.NetState); // Your hand remains stuck!!!
                    from.Frozen = true;

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), () =>
                    {
                        from.Frozen = false;
                        from.PrivateOverheadMessage(MessageType.Regular, 65, 1112941, from.NetState); // You manage to free your hand!
                    });
                    break;
            }
        }
    }

    private void Collapse(Mobile from)
    {
        from.SendLocalizedMessage(1112583); // The nest collapses.
        Delete();
    }

    private void HatchEggs(Mobile from)
    {
        from.SendLocalizedMessage(1112577); // A swarm of snakes springs forth from the nest and attacks you!!!

        for (var i = 0; i < 4; i++)
        {
            var snake = (BaseCreature)Activator.CreateInstance(_snakeTypes[Utility.Random(_snakeTypes.Length)]);

            snake.MoveToWorld(Location, Map);
        }

        Collapse(from);
    }
}
