/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Mobiles/BaseBizaarBroker.cs).
///
///     Базовый класс наёмного маклера на прилавке: банковский счёт, недельная пошлина и передача остатков на
///     склад при увольнении.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>EquipItem(new X(hue))</c> → <c>AddItem(new X { Hue = hue })</c>, как принято в этом
///             репозитории; <c>InitStats</c> в ModernUO нет — используются <c>SetStr/SetDex/SetInt</c>.
///         </item>
///         <item>
///             <c>Race.RandomSkinHue/RandomHair/RandomFacialHair</c> — члены экземпляра, поэтому вызываются
///             через <c>Race.Human</c>.
///         </item>
///         <item>
///             Имена маклеров берутся из <see cref="RussianNames" />: списки имён клиента английские, а перевод
///             нужен только для серверного текста.
///         </item>
///         <item>
///             <c>Serial(Serial)</c>-конструктора и ручной сериализации нет: <c>[SerializationGenerator]</c>.
///             Участок (<see cref="Plot" />) не сериализуется — его восстанавливает сам участок из ссылки на
///             маклера.
///         </item>
///         <item>
///             Передача остатков на склад перенесена из <c>Delete()</c> в <see cref="OnDelete" />, чтобы
///             списки товаров и питомцев были ещё живы.
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public abstract partial class BaseBazaarBroker : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _bankBalance;

    [SerializableField(1)]
    private DateTime _nextFee;

    protected BaseBazaarBroker(MaginciaBazaarPlot plot) : base(AIType.AI_Vendor, FightMode.None, 2, 1)
    {
        Plot = plot;
        _bankBalance = 0;
        _nextFee = Core.Now + TimeSpan.FromHours(23);

        InitBody();
        InitOutfit();

        Blessed = true;
        CantWalk = true;
        Direction = Direction.East;
    }

    /// <summary>Прилавок, на котором стоит маклер. Не сериализуется: его ставит сам участок при загрузке.</summary>
    public MaginciaBazaarPlot Plot { get; set; }

    public override bool IsInvulnerable => true;

    public virtual int CommissionFee => MaginciaBazaar.DefaultCommissionFee;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SendToWarehouse
    {
        get => false;
        set => Delete();
    }

    public virtual void InitOutfit()
    {
        switch (Utility.Random(3))
        {
            case 0:
                {
                    AddItem(new FancyShirt { Hue = GetRandomHue() });
                    break;
                }
            case 1:
                {
                    AddItem(new Doublet { Hue = GetRandomHue() });
                    break;
                }
            case 2:
                {
                    AddItem(new Shirt { Hue = GetRandomHue() });
                    break;
                }
        }

        switch (Utility.Random(4))
        {
            case 0:
                {
                    AddItem(new Shoes());
                    break;
                }
            case 1:
                {
                    AddItem(new Boots());
                    break;
                }
            case 2:
                {
                    AddItem(new Sandals());
                    break;
                }
            case 3:
                {
                    AddItem(new ThighBoots());
                    break;
                }
        }

        if (Female)
        {
            switch (Utility.Random(6))
            {
                case 0:
                    {
                        AddItem(new ShortPants { Hue = GetRandomHue() });
                        break;
                    }
                case 1:
                case 2:
                    {
                        AddItem(new Kilt { Hue = GetRandomHue() });
                        break;
                    }
                case 3:
                case 4:
                case 5:
                    {
                        AddItem(new Skirt { Hue = GetRandomHue() });
                        break;
                    }
            }
        }
        else
        {
            switch (Utility.Random(2))
            {
                case 0:
                    {
                        AddItem(new LongPants { Hue = GetRandomHue() });
                        break;
                    }
                case 1:
                    {
                        AddItem(new ShortPants { Hue = GetRandomHue() });
                        break;
                    }
            }
        }
    }

    public virtual void InitBody()
    {
        SetStr(100);
        SetDex(100);
        SetInt(25);

        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Female = Utility.RandomBool();
        Body = Female ? 0x191 : 0x190;
        Name = RussianNames.RandomPerson(Female);

        HairItemID = Race.Human.RandomHair(Female);
        HairHue = Race.Human.RandomHairHue();

        FacialHairItemID = Female ? 0 : Race.Human.RandomFacialHair(false);
        FacialHairHue = FacialHairItemID != 0 ? Race.Human.RandomHairHue() : 0;
    }

    public virtual int GetRandomHue() =>
        Utility.Random(5) switch
        {
            0 => Utility.RandomBlueHue(),
            1 => Utility.RandomGreenHue(),
            2 => Utility.RandomRedHue(),
            3 => Utility.RandomYellowHue(),
            _ => Utility.RandomNeutralHue()
        };

    public void Dismiss() => Delete();

    public abstract bool HasValidEntry(Mobile m);

    public override void OnDelete()
    {
        if (Plot != null)
        {
            MaginciaBazaar.AddInventoryToWarehouse(Plot.Owner, this);
            Plot.Merchant = null;
            Plot = null;
        }

        base.OnDelete();
    }

    public virtual void OnTick()
    {
        if (_nextFee < Core.Now)
        {
            _bankBalance -= GetWeeklyFee() / 7;
            _nextFee = Core.Now + TimeSpan.FromHours(23);
        }
    }

    public virtual int GetWeeklyFee() => 0;

    public void TryWithdrawFunds(Mobile from, int amount)
    {
        if (_bankBalance < amount || !Banker.Deposit(from, amount))
        {
            // Transfer of funds from the broker to your bank box failed...
            from.SendLocalizedMessage(1150214);
            return;
        }

        _bankBalance -= amount;
        from.SendMessage($"Вы снимаете {amount:#,0} золота со счёта маклера.");
        from.PlaySound(0x37);
    }

    public void TryDepositFunds(Mobile from, int amount)
    {
        if (Banker.Withdraw(from, amount))
        {
            _bankBalance += amount;
            from.SendMessage($"Вы вносите {amount:#,0} золота на счёт маклера.");
            from.PlaySound(0x37);
            return;
        }

        // You have entered an invalid value, or a non-numeric value. Please try again.
        from.SendLocalizedMessage(1150215);
    }
}
