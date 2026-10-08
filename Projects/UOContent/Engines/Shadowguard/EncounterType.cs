using System;

namespace Server.Engines.Shadowguard;

[Flags]
public enum EncounterType
{
    Bar = 0x00000001,
    Orchard = 0x00000002,
    Armory = 0x00000004,
    Fountain = 0x00000008,
    Belfry = 0x00000010,
    Roof = 0x00000020,

    Required = Bar | Orchard | Armory | Fountain | Belfry
}
