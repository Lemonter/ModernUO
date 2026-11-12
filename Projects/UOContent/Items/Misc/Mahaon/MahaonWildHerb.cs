using System;
using ModernUO.Serialization;
using Server.Systems.MahaonHerbalism;
using Server.Systems.MahaonWorld;

namespace Server.Items;

/// <summary>
///     Куст дикой травы в мире. Ставит его MahaonHerbSpawner рядом с игроками, собирают
///     двойным кликом, а несобранный сам увядает через какое-то время.
///
///     Увядание нужно не для красоты: кусты появляются там, где ходят люди, и без срока
///     жизни мир за неделю зарос бы травой вдоль каждой дороги, по которой кто-то прошёл.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonWildHerb : Item
{
    /// <summary>Сколько куст живёт, если его никто не сорвал.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(40);

    [SerializableField(0)]
    private MahaonHerbKind _kind;

    [SerializableField(1)]
    private DateTime _witherAt;

    private Timer _witherTimer;

    [Constructible]
    public MahaonWildHerb(MahaonHerbKind kind = MahaonHerbKind.Plantain)
        : base(MahaonHerbs.Get(kind).NodeGraphic)
    {
        Movable = false;
        _kind = kind;
        _witherAt = Core.Now + Lifetime;
        Name = MahaonHerbs.Get(kind).NameRu;
        StartWither();
        MahaonHerbSpawner.Register(this);
    }

    public MahaonHerbInfo Info => MahaonHerbs.Get(_kind);

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        StartWither();
        MahaonHerbSpawner.Register(this);
    }

    private void StartWither()
    {
        _witherTimer?.Stop();
        var left = _witherAt - Core.Now;
        _witherTimer = Timer.DelayCall(left > TimeSpan.Zero ? left : TimeSpan.Zero, Delete);
    }

    public override void OnDelete()
    {
        _witherTimer?.Stop();
        MahaonHerbSpawner.Unregister(this);
        base.OnDelete();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        var info = Info;

        if (!MahaonHerbs.CanGather(from, info))
        {
            from.SendMessage(
                0x22,
                $"Ты не знаешь, как правильно собрать это ({info.NameRu}): нужно травничество {info.Required:0}."
            );
            return;
        }

        // Как у добычи: одно действие — раз в пару секунд, а не столько, сколько успеешь
        // нажать. Иначе неудачи, которые не губят куст, превратились бы в пулемёт прокачки.
        if (!from.BeginAction<MahaonWildHerb>())
        {
            from.SendMessage(0x22, "Ты ещё возишься с предыдущим растением.");
            return;
        }

        Timer.DelayCall(TimeSpan.FromSeconds(2), () => from.EndAction<MahaonWildHerb>());
        from.Animate(32, 5, 1, true, false, 0); // наклон, как при сборе с земли
        from.PlaySound(0x4F);

        if (!MahaonHerbs.CheckGather(from, info))
        {
            from.SendMessage(0x22, $"Ты неловко дёргаешь стебель — {info.NameRu} рвётся, ничего годного. Попробуй ещё.");
            return;
        }

        var amount = MahaonHerbs.Yield(from, info);
        var herb = info.Produce();
        herb.Amount = amount;
        Give(from, herb);

        from.SendMessage(0x59, $"Ты собираешь: {info.NameRu} ({amount}).");

        if (info.Seed is { } crop && Utility.RandomDouble() < MahaonHerbs.SeedChance)
        {
            Give(from, new MahaonCropSeed(crop));
            from.SendMessage(0x59, $"Среди листьев — семена ({MahaonCropTable.Data[(int)crop].NameRu}). Их можно посадить.");
        }

        Delete();
    }

    private static void Give(Mobile from, Item item)
    {
        if (from.Backpack?.TryDropItem(from, item, false) != true)
        {
            item.MoveToWorld(from.Location, from.Map);
        }
    }
}
