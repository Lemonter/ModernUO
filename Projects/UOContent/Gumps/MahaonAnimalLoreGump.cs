using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class MahaonAnimalLoreGump : StaticGump<MahaonAnimalLoreGump>
{
    private readonly Mobile _creature;

    public override bool Singleton => true;

    public MahaonAnimalLoreGump(Mobile creature) : base(50, 50) => _creature = creature;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 340, 5054);
        builder.AddAlphaRegion(10, 10, 280, 320);

        builder.AddHtml(15, 15, 270, 20, $"{_creature.Name}");

        var y = 45;

        builder.AddLabel(15, y, 0x480, "Сила:");
        builder.AddLabel(180, y, 0x59, _creature.RawStr.ToString());
        y += 22;

        builder.AddLabel(15, y, 0x480, "Ловкость:");
        builder.AddLabel(180, y, 0x59, _creature.RawDex.ToString());
        y += 22;

        builder.AddLabel(15, y, 0x480, "Интеллект:");
        builder.AddLabel(180, y, 0x59, _creature.RawInt.ToString());
        y += 22;

        builder.AddLabel(15, y, 0x480, "Хиты:");
        builder.AddLabel(180, y, 0x59, $"{_creature.Hits}/{_creature.HitsMax}");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Стамина:");
        builder.AddLabel(180, y, 0x59, $"{_creature.Stam}/{_creature.StamMax}");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Мана:");
        builder.AddLabel(180, y, 0x59, $"{_creature.Mana}/{_creature.ManaMax}");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Физ. резист:");
        builder.AddLabel(180, y, 0x59, $"{_creature.PhysicalResistance}%");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Огонь резист:");
        builder.AddLabel(180, y, 0x59, $"{_creature.FireResistance}%");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Холод резист:");
        builder.AddLabel(180, y, 0x59, $"{_creature.ColdResistance}%");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Яд резист:");
        builder.AddLabel(180, y, 0x59, $"{_creature.PoisonResistance}%");
        y += 22;

        builder.AddLabel(15, y, 0x480, "Энергия резист:");
        builder.AddLabel(180, y, 0x59, $"{_creature.EnergyResistance}%");
        y += 22;

        if (_creature is BaseCreature bc)
        {
            builder.AddLabel(15, y, 0x480, "Урон:");
            builder.AddLabel(180, y, 0x59, $"{bc.DamageMin}-{bc.DamageMax}");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Слава:");
            builder.AddLabel(180, y, 0x59, bc.Fame.ToString());
            y += 22;

            builder.AddLabel(15, y, 0x480, "Приручён:");
            builder.AddLabel(180, y, 0x59, bc.Controlled ? "да" : "нет");
        }
    }
}
