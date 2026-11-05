/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/BaseGalleon.cs — класс
///     <c>SecurityEntry</c> и перечисления <c>SecurityLevel</c>/<c>PartyAccess</c>).
///
///     Список доступа корабля: кто может командовать («капитан»/«офицер»/«матрос»), кому разрешено
///     подниматься на борт («пассажир»), а кому отказано. В ServUO это поле жило внутри самого
///     <c>BaseGalleon</c> и писалось его <c>Serialize</c>; здесь данные вынесены в
///     <see cref="HighSeasShips" /> (<c>GenericPersistence("HighSeasShips", 10)</c>) по серийнику
///     корабля, чтобы на самом корабле остались только <c>[SerializableField]</c>-поля.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             Ключ — серийник корабля, а не <c>Dictionary&lt;Mobile, SecurityLevel&gt;</c> внутри мульти;
///             владелец не дублируется в списке, им всегда считается <c>BaseBoat.Owner</c>.
///         </item>
///         <item>
///             Данные о группе/гильдии/аккаунте читаются «на лету» из <c>Party</c>/<c>Guild</c>/<c>Account</c>
///             (в ServUO так же); кэша нет, поэтому нет и рассинхрона после смены группы.
///         </item>
///         <item>
///             <c>IsPublic</c> в ServUO возвращал <c>true</c> при уровне <c>NA</c> — здесь «публичным»
///             считается только уровень выше <c>Denied</c>.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;

namespace Server.Engines.HighSeas;

public enum SecurityLevel
{
    NA,
    Denied,
    Passenger,
    Crewman,
    Officer,
    Captain
}

public enum PartyAccess
{
    Never,
    LeaderOnly,
    MemberOnly
}

public sealed class ShipAccessList
{
    private const SecurityLevel ImpliedAccessLevel = SecurityLevel.Passenger;

    private readonly Dictionary<Mobile, SecurityLevel> _manifest = new();

    public ShipAccessList(Serial boatSerial) => BoatSerial = boatSerial;

    /// <summary>Серийник корабля, к которому относится список (он же ключ в <see cref="HighSeasShips" />).</summary>
    public Serial BoatSerial { get; }

    /// <summary>
    ///     Сам корабль. Не сохраняется: ссылка восстанавливается при каждом
    ///     <see cref="HighSeasShips.GetAccessList" />, поэтому удалённый корабль не держится в памяти.
    /// </summary>
    public BaseGalleon Galleon { get; set; }

    public PartyAccess PartyAccess { get; set; }

    public SecurityLevel DefaultPublicAccess { get; set; }

    public SecurityLevel DefaultPartyAccess { get; set; }

    public SecurityLevel DefaultGuildAccess { get; set; }

    public Dictionary<Mobile, SecurityLevel> Manifest => _manifest;

    public bool IsPublic => DefaultPublicAccess > SecurityLevel.Denied;

    /// <summary>Уровни по умолчанию, как в <c>SecurityEntry.SetToDefault</c>: доступа нет ни у кого.</summary>
    public void SetToDefault()
    {
        PartyAccess = PartyAccess.Never;
        DefaultPublicAccess = SecurityLevel.NA;
        DefaultPartyAccess = SecurityLevel.NA;
        DefaultGuildAccess = SecurityLevel.NA;
    }

    public void AddToManifest(Mobile from, SecurityLevel access)
    {
        if (from == null)
        {
            return;
        }

        _manifest[from] = access;
    }

    public void RemoveFromAccessList(Mobile from)
    {
        if (from != null)
        {
            _manifest.Remove(from);
        }
    }

    public SecurityLevel GetEffectiveLevel(Mobile from) => GetEffectiveLevel(from, true);

    public SecurityLevel GetEffectiveLevel(Mobile from, bool checkImplied)
    {
        if (from == null)
        {
            return SecurityLevel.Denied;
        }

        var owner = Galleon?.Owner;

        // Владелец всегда капитан, даже если его нет в списке.
        if (owner == from)
        {
            return SecurityLevel.Captain;
        }

        var highest = SecurityLevel.Denied;

        if (_manifest.TryGetValue(from, out var listed))
        {
            if (listed == SecurityLevel.Denied)
            {
                return SecurityLevel.Denied;
            }

            highest = listed;
        }

        if (highest < DefaultPublicAccess)
        {
            highest = DefaultPublicAccess;
        }

        if (highest < DefaultPartyAccess && IsInParty(from))
        {
            highest = DefaultPartyAccess;
        }

        if (highest < DefaultGuildAccess && IsInGuild(from))
        {
            highest = DefaultGuildAccess;
        }

        if (checkImplied && highest == SecurityLevel.Denied)
        {
            highest = GetImpliedAccess(from);
        }

        return highest;
    }

    /// <summary>
    ///     Персонажи того же аккаунта, что и кто-то из списка, тоже считаются «своими» (в ServUO —
    ///     <c>GetImpliedAccess</c>/<c>HasImpliedAccess</c>).
    /// </summary>
    public SecurityLevel GetImpliedAccess(Mobile from)
    {
        var account = from?.Account;

        if (account == null)
        {
            return SecurityLevel.Denied;
        }

        for (var i = 0; i < account.Length; i++)
        {
            var character = account[i];

            if (character == null || character == from || character.Deleted)
            {
                continue;
            }

            if (GetEffectiveLevel(character, false) > SecurityLevel.Denied)
            {
                return ImpliedAccessLevel;
            }
        }

        return SecurityLevel.Denied;
    }

    public bool IsInParty(Mobile from)
    {
        var owner = Galleon?.Owner;

        if (from == null || owner == null)
        {
            return false;
        }

        var fromParty = Party.Get(from);
        var ownerParty = Party.Get(owner);

        if (fromParty == null || fromParty != ownerParty)
        {
            return false;
        }

        return PartyAccess switch
        {
            PartyAccess.Never      => false,
            PartyAccess.LeaderOnly => ownerParty.Leader == owner,
            PartyAccess.MemberOnly => true,
            _                      => false
        };
    }

    public bool IsInGuild(Mobile from)
    {
        var owner = Galleon?.Owner;

        return from?.Guild != null && owner?.Guild != null && from.Guild == owner.Guild;
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.Write((int)PartyAccess);
        writer.Write((int)DefaultPublicAccess);
        writer.Write((int)DefaultPartyAccess);
        writer.Write((int)DefaultGuildAccess);

        writer.WriteEncodedInt(_manifest.Count);

        foreach (var (mobile, level) in _manifest)
        {
            writer.Write(mobile);
            writer.Write((byte)level);
        }
    }

    public void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        PartyAccess = (PartyAccess)reader.ReadInt();
        DefaultPublicAccess = (SecurityLevel)reader.ReadInt();
        DefaultPartyAccess = (SecurityLevel)reader.ReadInt();
        DefaultGuildAccess = (SecurityLevel)reader.ReadInt();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var level = (SecurityLevel)reader.ReadByte();

            if (mobile != null && !mobile.Deleted)
            {
                _manifest[mobile] = level;
            }
        }
    }
}
