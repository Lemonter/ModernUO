using System.Collections.Generic;
using Server.Items;
using Server.Targeting;

namespace Server.Gumps;

public class MahaonDebugItemGump : StaticGump<MahaonDebugItemGump>
{
    private readonly object _target;

    public override bool Singleton => false;
    protected override bool Cached => false;

    public MahaonDebugItemGump(object target) : base(50, 50) => _target = target;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 320, 320, 5054);
        builder.AddAlphaRegion(10, 10, 300, 300);

        builder.AddHtml(15, 15, 290, 20, "Отладка предмета/статики");

        var lines = new List<(string Label, string Value)>();

        switch (_target)
        {
            case Item item:
                lines.Add(("Тип (C#):", item.GetType().Name));
                lines.Add(("ItemID:", $"0x{item.ItemID:X} ({item.ItemID})"));
                lines.Add(("Hue:", $"0x{item.Hue:X} ({item.Hue})"));
                lines.Add(("Локация:", $"{item.X}, {item.Y}, {item.Z}"));
                lines.Add(("Карта:", item.Map?.ToString() ?? "—"));
                lines.Add(("Movable:", item.Movable.ToString()));
                lines.Add(("Название:", item.Name ?? "(нет)"));

                if (item is MineRockWall wall)
                {
                    lines.Add(("— MineRockWall —", ""));
                    lines.Add(("Это жила:", wall.IsVein.ToString()));
                    lines.Add(("Ресурс жилы:", wall.VeinResourceName ?? "—"));
                    lines.Add(("Центр шахты:", wall.OriginCenter.ToString()));
                }

                if (item is MineFloorTile)
                {
                    lines.Add(("— MineFloorTile —", ""));
                }

                if (item is MahaonTree tree)
                {
                    lines.Add(("— MahaonTree —", ""));
                    lines.Add(("Вид:", tree.Species.ToString()));
                    lines.Add(("Возраст (лет):", tree.YearsOld().ToString()));
                    lines.Add(("Брёвен при рубке сейчас:", tree.CurrentLogYield().ToString()));
                }

                if (item is MahaonTreeFoliage foliage)
                {
                    lines.Add(("— MahaonTreeFoliage —", ""));
                    lines.Add(("Вид:", foliage.Species.ToString()));
                    lines.Add(("Осталось плодов:", foliage.FruitRemaining.ToString()));
                }

                break;

            case StaticTarget st:
                lines.Add(("Тип:", "StaticTarget (статика тайла)"));
                lines.Add(("ItemID:", $"0x{st.ItemID:X} ({st.ItemID})"));
                lines.Add(("Локация:", $"{st.X}, {st.Y}, {st.Z}"));
                break;

            case LandTarget lt:
                lines.Add(("Тип:", "LandTarget (тайл земли)"));
                lines.Add(("TileID:", $"0x{lt.TileID:X} ({lt.TileID})"));
                lines.Add(("Локация:", $"{lt.X}, {lt.Y}, {lt.Z}"));
                break;

            case Mobile mobile:
                lines.Add(("Тип (C#):", mobile.GetType().Name));
                lines.Add(("Body:", $"0x{mobile.Body.BodyID:X} ({mobile.Body.BodyID})"));
                lines.Add(("Локация:", $"{mobile.X}, {mobile.Y}, {mobile.Z}"));
                break;

            default:
                lines.Add(("Тип:", _target?.GetType().Name ?? "null"));
                break;
        }

        var y = 45;
        foreach (var (label, value) in lines)
        {
            builder.AddLabel(15, y, 0x480, label);
            builder.AddLabel(150, y, 0x59, value);
            y += 22;
        }
    }
}
