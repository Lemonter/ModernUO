using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lantern : BaseEquipableLight
{
    public static TimeSpan FullDuration = TimeSpan.FromMinutes(20);

    [Constructible]
    public Lantern() : base(0xA25)
    {
        Duration = Burnout ? FullDuration : TimeSpan.Zero;

        Burning = false;

        // ---- Это число парное, и вторая половина лежит не в коде ----
        //
        // На сервере у света нет ни радиуса, ни яркости — только НОМЕР картинки в
        // клиентском light.mul. И клиент берёт этот номер из трёх разных мест, смотря по
        // тому, где предмет (ClassicUO, GameScene.AddLight):
        //
        //     лежит на земле   — из пакета 0xF3, то есть отсюда;
        //     у кого-то в руке — из tiledata.mul, поле lightIndex;
        //     статика карты    — из tiledata.mul, поле layer.
        //
        // Поэтому поправить одно это место мало. Пока здесь стоял Circle300, а в тайлдате
        // единица, фонарь на земле светил дальше, чем в руке, и по коду это было не
        // видно. Сейчас в обоих местах Circle225 — тот же свет, что у свечи.
        Light = LightType.Circle225;
    }

    public override double DefaultWeight => 2.0;

    public override int LitItemID => ItemID is 0xA15 or 0xA17 ? ItemID : 0xA22;

    public override int UnlitItemID => ItemID == 0xA18 ? ItemID : 0xA25;
}

[SerializationGenerator(0)]
public partial class LanternOfSouls : Lantern
{
    [Constructible]
    public LanternOfSouls() => Hue = 0x482;

    public override int LabelNumber => 1061618; // Lantern of Souls
}
