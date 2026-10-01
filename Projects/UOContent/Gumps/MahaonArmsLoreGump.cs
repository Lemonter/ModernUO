using Server.Items;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonGems;

namespace Server.Gumps;

public class MahaonArmsLoreGump : DynamicGump
{
    private readonly Item _item;

    public override bool Singleton => true;

    public MahaonArmsLoreGump(Item item) : base(50, 50) => _item = item;

    // Was StaticGump<MahaonArmsLoreGump> with no Cached override — same bug as
    // MahaonAnimalLoreGump had: StaticGump caches its compiled layout per-TYPE (static
    // field), so this bakes in whichever item was FIRST examined after server start and
    // resends those exact durability/socket numbers for every item forever after. Every
    // other Mahaon *LoreGump/*InfoGump sibling already either uses DynamicGump or
    // explicitly sets `Cached => false` — this one was just missed.
    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 300, 5054);
        builder.AddAlphaRegion(10, 10, 280, 280);

        builder.AddHtml(15, 15, 270, 20, $"{_item.Name ?? _item.GetType().Name}");

        var y = 45;

        if (_item is BaseWeapon weapon)
        {
            var profile = DamageTypeSystem.GetWeaponProfile(weapon);

            builder.AddLabel(15, y, 0x480, "Урон:");
            builder.AddLabel(180, y, 0x59, $"{weapon.MinDamage}-{weapon.MaxDamage}");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Колющий:");
            builder.AddLabel(180, y, 0x59, $"{profile.Piercing * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Дробящий:");
            builder.AddLabel(180, y, 0x59, $"{profile.Blunt * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Режущий:");
            builder.AddLabel(180, y, 0x59, $"{profile.Slashing * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Прочность:");
            builder.AddLabel(180, y, 0x59, $"{weapon.HitPoints}/{weapon.MaxHitPoints}");
            y += 22;
        }
        else if (_item is BaseArmor armor)
        {
            var profile = DamageTypeSystem.GetArmorProfile(armor);

            builder.AddLabel(15, y, 0x480, "Рейтинг брони:");
            builder.AddLabel(180, y, 0x59, $"{armor.ArmorRatingScaled:F1}");
            y += 22;

            builder.AddLabel(15, y, 0x480, "От колющего:");
            builder.AddLabel(180, y, 0x59, $"{profile.Piercing * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "От дробящего:");
            builder.AddLabel(180, y, 0x59, $"{profile.Blunt * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "От режущего:");
            builder.AddLabel(180, y, 0x59, $"{profile.Slashing * 100:F0}%");
            y += 22;

            builder.AddLabel(15, y, 0x480, "Прочность:");
            builder.AddLabel(180, y, 0x59, $"{armor.HitPoints}/{armor.MaxHitPoints}");
            y += 22;
        }

        var socketCount = GemSocketingSystem.SocketCount(_item);
        builder.AddLabel(15, y, 0x480, "Камней вставлено:");
        builder.AddLabel(180, y, 0x59, $"{socketCount}/3");
    }
}
