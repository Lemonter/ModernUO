# ТЗ: исправление багов и портирование недостающего контента

**Дата составления:** 2026-09-03
**Ветка:** `main`, коммит `d33510a81` + 283 незакоммиченных изменения
**Состояние сборки:** `dotnet build ModernUO.slnx` — **0 ошибок, 0 предупреждений** (2 мин 27 с)

Документ дополняет `dev-docs/code-audit-findings.md` (там — построчный аудит кастомного кода,
блоки 1–9). Здесь — то, что аудит не покрывал: **содержимое мира** (спауны, квесты, телепорты),
**пробелы ванильного контента** и **новые находки в портированных движках**.

---

## Решения владельца (2026-09-03)

| Вопрос | Решение |
|---|---|
| Целевая экспансия | **Полный TOL** — портируем Peerless, Eodon/Kotl, High Seas |
| High Seas | **Целиком**, не минимумом |
| Именные NPC Fel/Tram | Разбираться самостоятельно: проверить каждое имя, ваниль — портировать, мусор — вычистить из спаунеров |
| С чего начинать | **Этап 1** |

## Прогресс

### Этап 1 — в работе

| Задача | Статус | Что сделано |
|---|---|---|
| A1 · перегенерировать `badspawn.log` | ⏳ **за владельцем** | нужен запущенный сервер + `[XmlLoad` по 13 файлам `Distribution/XmlSpawner/` |
| A2 · `NavreyNightEyes` | ✅ | Оказалось **не** расхождением имён: JSON-спаунер `TerMur 1053,861,-32` стоит на той же клетке, что `NavreysController` (`1054,861,-31`), который уже создаёт Navrey вместе с головоломкой из 3 колонн. Алиас породил бы **вторую** Navrey, респавнящуюся каждые 5–10 мин мимо головоломки. Спаунер удалён из `Data/Spawns/post-uoml/termur/Underworld.json`. Проверено по сейву: `Server.Mobiles.Navrey` в мире есть, то есть `[GenNavrey` уже выполнялся — переименовывать класс было бы нельзя, сломало бы загрузку. |
| A2 · клановые крысы Ter Mur | ✅ | 7 классов переименованы из сокращённых ServUO-имён в полные, которых ждут данные: `ClanCA→ClanChitterAssistant`, `ClanCT→ClanChitterTinkerer`, `ClanRC→ClanRibbonCourtier`, `ClanRS→ClanRibbonSupplicant`, `ClanSH→ClanScratchHenchrat`, `ClanSS→ClanScratchScrounger`, `ClanSSW→ClanScratchSavageWolf`. Сопоставление подтверждено по `DefaultName`/`CorpseName` каждого класса. Атрибуция на исходные файлы ServUO сохранена. Мобов этих типов в сейве нет — переименование безопасно. |
| A2 · `DryadA`/`Dryada` | ✅ | → `Dryad` в `shared/ilshenar/TwistedWeald.json` (спаунятся в наборе CuSidhe / Changeling / Satyr / DireWolf — стандартный Twisted Weald) |
| A2 · `treasurelevel1h` | ✅ | → `TreasureLevel1` (4 записи, Fel+Tram `4534–4535, 2349, -2`). **Допущение:** «h» принята за опечатку; варианта без «h» в данных нет, класса `TreasureLevel1H` нигде не существует |
| E1 · `OnDelete` в `DespiseController` | ✅ | добавлен `OnAfterDelete()`: сворачивает все 3 таймера, снимает 4 региона, удаляет босса, чистит списки армий/транспорта/спаунеров, обнуляет `Instance` |
| C2.1 · межфасетный телепорт Abyss | ✅ | запись `TerMur 7149,756 → Felucca 511,585` удалена. **См. поправку ниже — это была латентная мина, а не активный баг** |
| C1 · `CitadelTele` | ✅ | реализован (`Items/Misc/The Citadel/CitadelTele.cs`), ящик на Isamu-Jima, двойной клик, переносит с питомцами в Malas `106,1884,0` (`GoLocation` региона «The Citadel»), гейт по ML через `MondainsLegacy.CheckML` |
| C1 · `CrystalFieldTele` | ✅ | `Items/Misc/CrystalFieldTele.cs`. Точка назначения — обоснованная догадка, см. ниже |
| D3 · **Spellweaving закрыт целиком, 16/16** | ✅ | `WildfireSpell` (609), `DryadAllureSpell` (611), `ArcaneEmpowermentSpell` (615) + проводка бонусов в `Spell.cs`, `Heal.cs`, `GreaterHeal.cs`, `Dispel.cs` |
| D5 · `Paralithode`, `DiabolicalSeaweed` | ✅ | портированы отдельно — по 2370 строк `badspawn.log` каждое, самые массовые неразрешимые имена; остальной High Seas по-прежнему открыт |
| B1 · Underworld | ✅ | закрыт целиком: 13 квестов, 8 NPC, цепочка Священного Поиска, 5 недостающих существ |

**Сборка после правок:** 0 ошибок, 0 предупреждений. JSON-файлы проверены на баланс скобок.

---

## Поправки к разделам выше

### C2 — переоценил серьёзность

Проверил `TeleportersCreator.CreateTeleporter`
(`Commands/Object Creation/GenTeleporter.cs:141`): перед созданием он вызывает
`DeleteTeleporters(telDef.Source)`, снося всё, что стоит на клетке-источнике в пределах
Z ± 12, а записи идут через `.ForEach` в порядке файла. Значит на каждой клетке
детерминированно побеждает **последняя** запись, и фелукканский адрес Abyss в игре
никогда не срабатывал.

Это **латентная мина в данных**, а не активный баг: при перестановке записей, частичной
генерации или изменении Z поведение молча переключится. Кросс-фасетную запись удалил
(единственная из 16 пар, где расхождение — целый фасет, а не 1–2 клетки). **Остальные
15 пар не трогал**: они инертны, а правки увели бы файл от побайтовой идентичности
апстриму и создали конфликты при будущих подтягиваниях ModernUO. Патч имеет смысл
отправить в апстрим, а не держать локально.

### D3 — Spellweaving не хватает трёх заклинаний, а не двух

В `Spells/Initializer.cs` регистрации уже заготовлены апстримом и **закомментированы**,
с точными именами классов. Отсюда точный список дыр:

- **Spellweaving (13/16):** 609 `WildfireSpell` ✅ сделан, 611 `DryadAllureSpell`,
  615 `ArcaneEmpowermentSpell`
- **Mysticism (8/16):** 677 `NetherBoltSpell`, 678 `HealingStoneSpell`,
  679 `PurgeMagicSpell`, 680 `EnchantSpell`, 681 `SleepSpell`, 685 `SpellTriggerSpell`,
  686 `MassSleepSpell`, 692 `RisingColossusSpell`

Свитки для всех существуют (`SpellweavingScrolls.cs`, включая `ArcaneEmpowermentScroll`),
слоты в книге тоже — `Spellbook.cs:381` относит 600–616 к `SpellbookType.Arcanist`.
То есть достаточно написать классы и снять комментарии; ничего вокруг менять не нужно.

---

## Сверка с исходниками ServUO

Владелец разрешил сверяться с ServUO/RunUO. Все три блокера сняты; заодно вскрылось, что
**первая версия Wildfire, написанная по догадке, была неверна** — исправлено по исходнику:

| Что | Было по догадке | По ServUO |
|---|---|---|
| Мантра | `Haelyk` | `Haelyn` |
| Задержка каста | 3.0 с | **2.5 с** |
| Геометрия | сплошной круг радиуса `1 + focus` | **8 тайлов кольцом с шагом `5 + focus`, центр пропускается** |
| Модель урона | каждый тайл бьёт сам | **тайлы чисто декоративные**, урон — отдельный таймер вокруг эпицентра |
| Длительность | `skill/24 + focus` | `max(1, skill/24) + focus` |
| Урон | `2 + focus` | `10 + max(1, skill/24) + focus`, делится на `min(3, число целей)` |

Плюс появился per-target кулдаун на секунду (два Wildfire на одном месте не бьют дважды)
и исключение домов.

## Сделанные допущения — где я не был уверен

### `CrystalFieldTele` — точка назначения выведена, а не найдена

Класса с таким именем нет ни в ServUO, ни где-либо ещё в поиске. Восстановил по данным:

- `6509,87` лежит внутри региона «The Prism of Light» (`6400,0 .. 6621,255`);
- сосед по спаунеру, `6511,80`, — `PrismaticCrystal`, квестовый предмет
  `UnfadingMemoriesPartOne`. Значит это и есть Crystal Field, и игроков туда шлёт квест;
- `teleporters.json` уже содержит **односторонний** вход в этот закуток
  (`6469,97,-50 → 6503,88,0`, `"back": false`) и никакого выхода. Описания данжа в
  сообществе тоже упоминают «hidden teleport» в Crystal Field.

Итог: это выход. Отправляет в `GoLocation` региона (`6474,188,0`) — единственную внутреннюю
координату, за которую ручается сам ModernUO, и достаточно далеко от входного телепорта на
`6469,97`, чтобы не было петли «вышел — сразу затянуло обратно». **Настоящая точка OSI
неизвестна**; если узнаете — менять одну константу в файле.

### `DryadAllureSpell` — вопрос о персистенсе снялся сам

Опасение про эксплойт с рестартом было основано на предположении, что чар временный. По
исходнику **срока действия нет вообще**: зачарованный гуманоид становится обычным
постоянным питомцем на 3 слота контроля. Persistence не нужен.

Флагов `Allured` / `AllureImmune` у здешнего `BaseCreature` не было — **добавлены**
(версия рукописной сериализации поднята до 21). Сейвы одноразовые, так что обходить это
было незачем. Правила отказа теперь совпадают с оригиналом: уже подконтрольное существо
отвергается, **если оно не зачаровано вами**, а существо может отказаться от заклинания
насовсем, переопределив `AllureImmune`.

### `ArcaneEmpowermentSpell` — сделан с полной проводкой

Не стал делать «только иконку». Все три места, где ServUO читает бафф, подключены:

| Файл | Что добавлено |
|---|---|
| `Spells/Base/Spell.cs:244` | `GetSpellBonus` в `GetNewAosDamage`, рядом с уже существующим бонусом `ReaperFormSpell` |
| `Spells/First/Heal.cs` | `AddHealBonus` |
| `Spells/Fourth/GreaterHeal.cs` | `AddHealBonus` |
| `Spells/Sixth/Dispel.cs` | `GetDispellBonus` к шансу диспела |

Формулы по исходнику: бонус `skill/12 + focus*5`, длительность `15 + skill/24 + focus*2` с,
лечение `×(1 + (10 + bonus)/100)`, диспел `+10 * focus` процентов, звуки 0x5C1/0x5C2.

## Mysticism — 16/16, школа закрыта

Сборка чистая: 0 ошибок, 0 предупреждений. Все 16 регистраций в `Spells/Initializer.cs`
раскомментированы, закомментированных не осталось ни одной.

| ID | Заклинание | Что потребовалось сверх самого заклинания |
|---|---|---|
| 677 | `NetherBoltSpell` | — |
| 678 | `HealingStoneSpell` | предмет `Items/Consumables/HealingStone.cs` |
| 679 | `PurgeMagicSpell` | публичные `ReactiveArmorSpell.HasArmor`, `MagicReflectSpell.HasReflect` |
| 680 | `EnchantSpell` | система `Enhancement`, `BaseWeapon.EnchantedWeilder`, гамп `EnchantGump` |
| 681 | `SleepSpell` | хук пробуждения в `AOS.Damage` |
| 685 | `SpellTriggerSpell` | `SpellTriggerDef`, предмет `SpellStone`, гамп `SpellTriggerGump` |
| 686 | `MassSleepSpell` | — |
| 692 | `RisingColossusSpell` | существо `RisingColossus`, `AIType.AI_Mystic`, класс `MysticAI` |

## Правило работы: никакой самодеятельности

Владелец задал направление явно: **нет системы — строим систему, а не обходной путь.**
Все прежние отступления от оригинала переделаны.

| Было (обход) | Стало (по оригиналу) |
|---|---|
| Sleep замедлял существ через `CurrentSpeed`, потому что «`SpeedControlType` нет» | Оригинал шлёт только клиентский пакет; у существа без `NetState` это no-op и там, и здесь. Обход убран |
| В Sleep убран `Delta(MobileDelta.WeaponDamage)` как «несуществующий» | **Он существует** — объявлен внутри `Mobile.cs`, первый греп искал не в том файле. Возвращён |
| MassSleep пропускал цели с иммунитетом («иначе эксплойт») | В оригинале проверки нет — убрана |
| Wildfire без SDI «для консистентности со школой» | Бонус SDI с капом 15 % на игроках возвращён; урон идёт через `AOS.Damage` с `DamageType.SpellAOE` |
| `DamageType.SpellAOE` отсутствовал | Добавлен в enum |
| RisingColossus на `AI_Melee`, потому что «`AI_Mystic` нет» | Добавлены `AIType.AI_Mystic` и `MysticAI : MageAI` — пулы заклинаний, пороги маны и наведение площадных из оригинала |
| DryadAllure без `Allured`, чтобы не ломать сейв | Добавлены `BaseCreature.Allured` и `AllureImmune` (версия сериализации 21); правила отказа теперь как в оригинале |
| Enchant писал бонус прямо в `WeaponAttributes`, побочный эффект 80+ не портировался | Портирована система `Enhancement`; бонус, Spell Channeling +1 и Cast Speed −1 идут через неё под титулом `EnchantAttribute` |
| SpellTrigger: одна графика на все камни, имена вместо клилоков | Реальные `ItemId` и клилоки подсказок из оригинальных определений |

### Новые системы, которых в кодовой базе не было

**`Misc/Enhancement.cs`** — слой временных бонусов на мобиле, а не на предмете. Именованные
записи, `GetValue` суммирует по всем, удаление по титулу. Подключён в трёх статических
`GetValue` (`AosAttributes`, `AosWeaponAttributes`, `AosArmorAttributes`).

**`Mobiles/AI/MysticAI.cs`** — AI мистика. У здешнего `MageAI` другая форма, чем у ServUO
(нет `GetHealSpell` / `GetCureSpell` / `RandomCombatSpell` под такими именами), поэтому
решения оригинала разложены по существующим хукам: `GetRandomDamageSpell`,
`GetRandomCurseSpell`, `CheckCastHealingSpell`, `ProcessTarget`. Два последних были `private`
и расширены до `protected virtual`; поведение самого `MageAI` не изменилось.

Плюс точечные добавления: `BaseCreature.Allured` / `AllureImmune`,
`BaseWeapon.EnchantedWeilder`, `DamageType.SpellAOE`, `AIType.AI_Mystic`.

### Что осталось непортированным и почему

- **Barrab Hemolymph и Urali Trance** — два из девяти баффов, снимаемых Purge Magic. Это
  эффекты племенных зелий Эодона; ни зелий, ни эффектов в кодовой базе нет — снимать нечего.
  Дописать в `GetRandomBuff` / `RemoveBuff`, когда Эодон будет портирован.
- **`SAAbsorptionAttributes` и `ExtendedWeaponAttributes`** — два из пяти контейнеров
  `EnhancementAttributes` в оригинале. Это отдельные подсистемы Stygian Abyss, здесь их нет
  ни в каком виде и ни один код их не читает. Добавить поля в `EnhancementAttributes`, когда
  системы появятся; перестраивать `Enhancement` под это не придётся.

### Правки в чужих файлах

| Файл | Что |
|---|---|
| `Misc/AOS.cs` | хуки `SleepSpell.OnDamage` и `PurgeMagicSpell.OnDamage`; `Enhancement.GetValue` в трёх статических `GetValue` |
| `Misc/Enhancement.cs` | новый файл — система |
| `Mobiles/BaseCreature.cs` | `Allured`, `AllureImmune`, `AI_Mystic` в фабрике, версия сериализации 21 |
| `Mobiles/AI/BaseAI/AIType.cs` | `AI_Mystic` |
| `Mobiles/AI/MageAI.cs` | `CheckCastHealingSpell` и `ProcessTarget` → `protected virtual` |
| `Items/Weapons/BaseWeapon.cs` | `EnchantedWeilder` |
| `Spells/SkillMasteries/SkillMasterySpell.cs` | `DamageType.SpellAOE` |
| `Spells/First/ReactiveArmor.cs`, `Spells/Fifth/MagicReflect.cs` | публичные `HasArmor` / `HasReflect` |
| `Spells/Base/Spell.cs` | `ArcaneEmpowermentSpell.GetSpellBonus` в `GetNewAosDamage` |
| `Spells/First/Heal.cs`, `Spells/Fourth/GreaterHeal.cs` | `ArcaneEmpowermentSpell.AddHealBonus` |
| `Spells/Sixth/Dispel.cs` | `ArcaneEmpowermentSpell.GetDispellBonus` |

## Peerless — система и все шесть подземелий готовы

Портировано из ServUO (`Scripts/Services/Peerless/`, `Scripts/Items/Functional/*Altar.cs`,
`Scripts/Mobiles/Bosses/`, `Scripts/Items/Quest/`). Сборка чистая.

### Ядро — `Projects/UOContent/Engines/Peerless/`

| Файл | Что это |
|---|---|
| `PeerlessKey.cs` | база ключей: `BaseDecayingItem`, благословлён, живёт неделю, показывает фасет |
| `MasterKey.cs` | мастер-ключ, который алтарь выдаёт участникам; двойной клик ведёт партию внутрь |
| `BasePeerless.cs` | база боссов: кольцо огня, волны помощников, колбэк смерти в алтарь |
| `PeerlessAltar.cs` | алтарь: приём подношения, выдача мастер-ключей, призыв босса, телепорт внутрь/наружу, таймер на 90 мин, зачистка комнаты |
| `ConfirmGumps.cs` | подтверждение переноса партии и входа для каждого участника |

Абстрактных точек расширения ровно пять, как в оригинале: `KeyCount`, `Keys`, `Boss`,
`BossBounds`, `MasterKey`.

### Шесть комплектов

| Подземелье | Босс | Подношение | Ключей | Арена |
|---|---|---|---|---|
| Blighted Grove | Lady Melisande | `DryadsBlessing` | 3 | `6456,922` 84×47 |
| Twisted Weald | Dread Horn | 6 трофеев Уэлда | 3 | `2126,1237` 33×38 |
| Palace of Paroxysmus | Chief Paroxysmus | 4 останка | **16** | `6501,351` 35×48 |
| Prism of Light | Shimmering Effusion | 6 кристаллов, через пьедесталы | 3 | `6500,111` 45×35 |
| Bedlam | Monstrous Interred Grizzle | `LibrariansKey` | 3 | `99,1609` 14×18 |
| Citadel | Travesty | 3 ключа кланов | 3 | `66,1936` 51×39 |

Prism of Light — единственный, куда подношение кладут не на алтарь: сам алтарь невидим, а
шесть пьедесталов вокруг принимают каждый свой кристалл и подсвечиваются. Citadel —
единственный межфасетный: арена на Malas, вход и выход на Tokuno (`ExitMap` переопределён),
вход — ящик `CitadelTele` на Isamu-Jima, сделанный раньше.

### Поправка: связка «подземелье ↔ босс» в ТЗ была неверной

Я расписал её по памяти, а надо было по исходнику:

| Подземелье | Босс | Было у меня |
|---|---|---|
| Blighted Grove | **Lady Melisande** | ~~Shimmering Effusion~~ |
| Prism of Light | **Shimmering Effusion** | ~~Monstrous Interred Grizzle~~ |
| Bedlam | **Monstrous Interred Grizzle** | ~~не считался peerless~~ |

Местный `InterredGrizzle` — это не босс, а рядовой обитатель Bedlam; боссом зовут
`MonstrousInterredGrizzle`, конфликта нет.

### Системы, добавленные ради Peerless

- **`BaseCreature.CanBeParagon`** (virtual, по умолчанию `true`). В ModernUO отбор парагонов
  гейтился жёстким списком типов внутри `Paragon.CheckConvert`; в оригинале это виртуальное
  свойство. Список остался, свойство добавлено как дополнительное вето.
- **`BaseCreature.CanDiscord` / `CanPeace` / `CanProvoke`** — разрешение существу применять
  бардовские навыки против игроков. `CanDiscord` подключён в `Skills/Discordance.cs` вместе с
  фиксированной музыкальностью 120 для существ, как в оригинале. **Важно:** ни один AI в этой
  кодовой базе не начинает песню сам, так что пока это разрешения, а не поведение. Travesty
  выставляет их ровно тогда, когда это делает оригинал.
- **Три лут-пака** в `Misc/LootPack.cs`: `PeerlessResource` (шесть ингредиентов имбуинга),
  `ArcanistScrolls` (14 свитков Spellweaving), `Talisman`.
- **`InfernalOoze`** — кислота Гриззла, идёт через существующие `SpillAcid` / `NewHarmfulItem`.
- **`BulbousPutrification`** — помощник Пароксизма. Заодно закрывает имя
  `bulbousputrification` из `badspawn.log` (спаунер «fel bulbous putrification» в `felucca.xml`).

### Четыре «упрощённых» ключа переведены на настоящую базу

`DragonFlameKey`, `SerpentFangKey`, `ShatteredCrystals`, `RareSerpentEgg` были написаны как
обычные `Item` с комментарием «PeerlessKey base doesn't exist here». База появилась — все
четыре теперь на ней. `ShatteredCrystals` при этом оказался шестым кристаллом Prism of Light,
а два ключа Цитадели — двумя из трёх её клановых ключей.

### Адаптации, где у ServUO есть типы AI, которых здесь нет

`Travesty.ChangeBody` переключает AI под скопированного игрока. Из девяти типов оригинала
здесь есть четыре, остальные отображены на ближайшие:

| Навык игрока | ServUO | Здесь |
|---|---|---|
| Swords / Fencing / Macing ≥50 | AI_Melee | AI_Melee |
| Archery ≥50 | AI_Archer | AI_Archer |
| Mysticism ≥50 | AI_Mystic | AI_Mystic |
| Magery ≥50 | AI_Mage | AI_Mage |
| Spellweaving ≥50 | AI_Spellweaving | AI_Mage |
| Necromancy ≥50 (и +Magery) | AI_Necro / AI_NecroMage | AI_Mage — `MageAI` сам переходит на некромантию выше 50 навыка |
| Ninjitsu ≥50 | AI_Ninja | AI_Melee |
| Bushido ≥50 | AI_Samurai | AI_Melee |

Lady Melisande по той же причине собрана на `AI_Mage`, а не `AI_NecroMage`.

### Чего нет и почему

**Размещения алтарей.** Проверил: в ServUO его нет ни в `Data/Decoration/*` (ни в Britannia,
Felucca, Malas, Trammel), ни отдельной командой генерации. Алтари ставят руками через `[add`.
Выдумывать координаты не стал. Нужны — снимите в игре и добавим команду по образцу
`[GenNavrey`.

**Мелкие дропы.** Каждый босс теряет часть «сувенирной» добычи, для которой в этой кодовой
базе нет классов. Ингредиенты имбуинга и ключевые артефакты на месте — не хватает статуэток и
пары украшений:

| Босс | Нет классов |
|---|---|
| все шесть | `LootPack.Parrot` закомментирован апстримом («TODO: Uncomment once added Legacy»), класса `ParrotItem` нет |
| Lady Melisande | `AlbinoSquirrelImprisonedInCrystal` |
| Dread Horn | `DreadFlute`, `DreadsRevenge` |
| Chief Paroxysmus | `ParoxysmusSwampDragonStatue` |
| Shimmering Effusion | 4 статуэтки, `FerretImprisonedInCrystal`, `CrystallineRing` |
| Travesty | `TragicRemainsOfTravesty`, `MarkOfTravesty`, `MalekisHonor`, `ImprisonedDog` |

Это независимый хвост: добавляются в `OnDeath` по одной строке каждый, механику боёв не
трогают.

**`LootPack.LootItem<T>()` и `RandomLootItem()`** — хелперов ServUO здесь нет, гарантированные
дропы идут через `OnDeath`, как везде в этой кодовой базе.

## Поправка: два моих прежних вывода «в ServUO этого нет» были ложными

Владелец дал локальную копию `D:\MyUltima\ServUO-57.4.1\`. Выяснилось, что WebFetch по github
**режет по размеру** и листинги каталогов, и большие файлы — поэтому «файл не найден» там
часто значило «ответ обрезался». Два вывода, сделанных на этом основании, неверны:

| Что я утверждал | Как на самом деле |
|---|---|
| «ServUO не поставляет размещение Peerless-алтарей, их ставят руками через `[add`» | Координаты всех шести — в `Scripts/Services/Expansions/MondainsLegacy.cs`, вместе с телепортами выхода, шестью пьедесталами Prism и воротами арены Пароксизма |
| «Ни один AI не инициирует бардовские навыки у существ, флаги `CanDiscord/CanPeace/CanProvoke` — мёртвые разрешения» | Драйвер есть, в `BaseCreature.OnThink` (~7467): 33% за тик при кулдауне 5–12,5 с, затем `DoDiscord`/`DoPeace`/`DoProvoke`, каждый из которых сам подкладывает существу арфу через `CheckInstrument` |
| «Семи предметов-наград Underworld в ServUO нет» | Все семь есть, просто разбросаны по `Items/Functional`, `Items/Artifacts/Tools`, `Items/Artifacts/Equipment/Jewelry`, `Items/Containers`, `Items/Consumables`, `Items/Equipment/Talismans` |

**Вывод на будущее:** исходники для порта искать грепом по локальной копии, не через github.

## Peerless — добавлены телепорты и размещение

| Файл | Что |
|---|---|
| `Engines/Peerless/PeerlessTeleporter.cs` | шестой файл ядра, которого не хватало: выход из арены — живым предлагает подтвердить, мёртвых выталкивает |
| `Engines/Peerless/ConfirmGumps.cs` | добавлен `ConfirmExitGump` |
| `Engines/Peerless/PeerlessGeneration.cs` | команды `[GenPeerless` и `[DeletePeerless` |

`[GenPeerless` ставит все шесть алтарей по координатам оригинала, телепорты выхода, шесть
пьедесталов Prism of Light (три одного оттенка, три другого) и ворота арены Пароксизма.
Идемпотентна — алтарь, уже стоящий на своей клетке, не трогает. Всё созданное помечено в
`WeakEntityCollection` тегом `peerless`, так что `[DeletePeerless` снимает разом, по образцу
`[SetupDespise`.

## Underworld — 13 квестов, все восемь NPC

| Квест | NPC | Цель | Награда |
|---|---|---|---|
| Untangling the Web | Vernix | 12 кислотных тварей | `AcidPopper` |
| Green with Envy | Vernix | `EyeOfNavrey` | сумка сокровищ* |
| Missing | Dugan | 4 × `ArielHavenWritofMembership` | `CandlewoodTorch` |
| Ending the Threat | Dugan | 10 `GrayGoblin` в регионе Abyss | большая сумка сокровищ |
| The Lost Brightwhistle | Neville Brightwhistle | довести до региона NPC Encampment | `TalismanofGoblinSlaying` |
| Thieves Be Afoot! | Quartermaster Flint | 4 × `BarrelOfBarley` | `BottleOfFlintsPungnentBrew` |
| Bibliophile | Quartermaster Flint | `FlintsLogbook` | `KegOfFlintsPungnentBrew` |
| Bad Company | Jaacar | 10 `GreenGoblin` | `JaacarBox` |
| A Tangled Web | Jaacar | 12 `IBloodCreature` | большая сумка сокровищ |
| Done in the Name of Tinkering | Tobin | 5 × `FloorTrapComponent` | `GoblinFloorTrapKit` |
| Scraping the Bottom | Xenrr | `MudPuppy` | `XenrrFishingPole` |
| Something Fishy | Barreraak | `RedHerring` | `BarreraaksRing` |
| Curiosities | Gretchen | 3 × `FertileDirt`, 3 × `Bone` | `ExplodingTarPotion` |

\* В оригинале — `RewardBox`, общий контейнер ServUO без аналога здесь.

Все 13 записей в `Data/MLQuests.cfg` проверены: и типы квестов, и типы квестодателей
резолвятся. Последние две — «A Tangled Web» и «Curiosities» — добавлены позже, см. ниже.

**Решение по форме.** ServUO пишет эти квесты на `BaseQuest`/`MondainQuester`. Здесь уже есть
полноценная квестовая система — 265 квестов, та же форма «клилоки + цели + награды». Портировать
второй, дублирующий каркас незачем; квесты выражены в `MLQuest`, клилоки и счётчики — из
оригинала.

### Предметы и существа, добавленные по ходу

`BarrelOfBarley`, `FlintsLogbook`, `FloorTrapComponent`, `MudPuppy`, `RedHerring`,
`BottleOfFlintsPungnentBrew`, `KegOfFlintsPungnentBrew`, `ArielHavenWritofMembership`,
`CandlewoodTorch`, `TalismanofGoblinSlaying`, `JaacarBox`, `XenrrFishingPole`,
`BarreraaksRing`, `GoblinFloorTrapKit` + `GoblinFloorTrap`, а также существо
`GreenGoblinScout`.

Пять из них закрывают имена прямо из `badspawn.log`: `barrelofbarley`, `flintslogbook`,
`greengoblinscout`, плюс все восемь NPC.

### Две системы, дописанные ради этого

- **`TalismanSlayerName.Goblin`** и его таблица существ. Локальный enum кончался на `Bovine`;
  в ServUO после него ещё девять значений, из них добавлено одно, нужное талисману.
- **`FishingPole.Attributes`.** ServUO-шная удочка несёт собственные `AosAttributes` (магические
  свойства, бонусы статов, Spell Channeling); здешняя не несла ничего, и артефактной удочке
  Ксенрра некуда было положить свои бонусы. Добавлены атрибуты, их сериализация, показ в
  свойствах, применение бонусов статов при надевании и учёт Spell Channeling в
  `AllowEquippedCast`.

### Что осталось в Underworld

| Что | Чего не хватает |
|---|---|
| квест **Curiosities** (Gretchen) | `ExplodingTarPotion` → нужен базовый класс `BaseExplodingTarPotion` (~226 строк, круговой эффект через `Geometry.Circle2D`) |
| квест **A Tangled Web** (продолжение Bad Company) | маркер-интерфейс `IBloodCreature` и существа, которые его несут |
| существа | `IronBeetle`, `SentinelSpider`, `GreaterPoisonElemental` |
| сюжет | `Garamon` (NPC) и `TyballsShadow` (финальный босс подземелья) |

### Отдельно: драйвер бардовских способностей существ

Теперь известно, где он: `BaseCreature.OnThink` + `DoDiscord`/`DoPeace`/`DoProvoke` +
`CheckInstrument` (ServUO `Scripts/Mobiles/Normal/BaseCreature.cs`, ~7040–7180 и ~7467).
Здесь пока есть только разрешающая половина. Кроме Travesty его ждут `GargishRouser` и `Satyr`
— оба уже в кодовой базе с `CanDiscord => true` в оригинале.

## Бардовские способности существ — система портирована

`BaseCreature.OnThink` теперь несёт драйвер оригинала: не больше одной песни за тик, 33 % за
попытку, кулдаун 5–12,5 с на каждый из трёх навыков. Плюс `DoDiscord` / `DoPeace` / `DoProvoke`,
`CheckInstrument` (сам подкладывает существу арфу и регистрирует её, иначе `PickInstrument`
попытался бы спросить клиента, которого нет), `GetBardTarget` и `GetSecondTarget` для второй
цели провокации.

Правки в чужих файлах, все минимальные:

| Файл | Что |
|---|---|
| `Skills/Discordance.cs` | публичный `UnderEffects` |
| `Skills/Peacemaking.cs` | публичный `UnderEffects` (состояние живёт на существе как `BardPacified`) |
| `Skills/Peacemaking.cs`, `Skills/Provocation.cs` | целевые классы `private` → `internal`, чтобы `BaseCreature` мог их узнать |
| `Mobiles/BaseCreature.cs` | флаги приведены к форме оригинала: read-only virtual вместо auto-property |

Подключены все четыре существа, которые это ждут: `Travesty` (через поля-подложки, как в
оригинале), `GargishRouser`, `Satyr`, `Juonar` (последний — без звука инструмента, как в
оригинале).

### Попутно: `GargishRouser` восстановлен

Его порт был урезан из-за трёх отсутствовавших вещей. Две с тех пор появились и возвращены:
`AIType.AI_Mystic` (оригинал выбирает Mystic или Mage случайно) и призыв `RisingColossusSpell`
в бою. Бардовские навыки и флаги — тоже. Осталось непортированным только
`VoidManifestation` — существо из набора Void Creatures, которого в этом порте TerMur нет.

### Сверка устаревших пометок в портах

Прошёлся грепом по комментариям вида «этого здесь нет» и нашёл две, устаревшие из-за
собственной работы:

- `Mobiles/Monsters/Underworld/Navrey.cs` — «квестовой цепочки от NPC по имени Vernix здесь
  нет». Теперь есть, и «Green with Envy» собирает ровно этот трофей.
- `Mobiles/TerMur/CrystalHydra.cs` — «`LootPack.ArcanistScrolls` не существует, выкинут».
  Пометка неверна вдвойне: сверил с исходником — у оригинальной гидры в луте
  `UltraRich`/`HighScrolls`/`Parrot`, никакого `ArcanistScrolls` там нет вовсе. Ничего не
  терялось; единственный реальный пробел — `Parrot`.

## Хвост Underworld закрыт

Всё, что оставалось по блоку B1, портировано. Сборка чистая: 0 ошибок, 0 предупреждений.

### Квесты

| Квест | Квестодатель | Чем был заблокирован |
|---|---|---|
| **A Tangled Web** | Джаакар (цепочка от «Bad Company») | `IBloodCreature` |
| **Curiosities** | Гретхен | `ExplodingTarPotion` |

Обе строки добавлены в `Data/MLQuests.cfg`. Итого по Underworld — 13 квестов, 8 NPC.

`IBloodCreature` сделан ровно так, как в оригинале: пустой маркерный интерфейс на `BloodWorm`
и `BloodElemental`. Цель квеста ссылается на сам интерфейс — `KillObjective` здесь сверяет
`AcceptedTypes` через `IsAssignableFrom`, так что любое будущее существо с этим маркером
зачтётся без правки квеста.

Награда «A Tangled Web» — не замена: у оригинального `LargeTreasureBag` клилок 1072706, и у
здешнего `ItemReward.LargeBagOfTreasure` тот же клилок и та же роль.

### Зелье дёгтя

`BaseExplodingTarPotion` + `ExplodingTarPotion`. В оригинале это копия его же
`BaseConfusionBlastPotion` с подменённым эффектом — здесь ровно та же правка, но поверх
здешнего `BaseConfusionBlastPotion`: тот же бросок, тот же 60-секундный кулдаун, то же
двухпроходное кольцо анимации. Эффект — не умиротворение, а дёготь: всё в радиусе взрыва,
кроме бросавшего, на минуту переводится на шаг (`SendSpeedControl`).

`PotionEffect.ExplodingTarPotion` добавлен **в конец** перечисления, а не после `Darkglow`,
где он стоит у ServUO: таблица названий бочонка ниже `FlintsPungentBrew` считается
арифметикой по значениям, а дегтярное зелье в бочонок всё равно не наливается.

### Существа и предметы

| Класс | Откуда | Заметка |
|---|---|---|
| `IronBeetle` | `Mobiles/Normal/IronBeetle.cs` | приручаемый, сам копает руду каждые 5 с и ест чужую, перекрашиваясь в её цвет |
| `SentinelSpider` | `Mobiles/Normal/SentinelSpider.cs` | |
| `GreaterPoisonElemental` | `Mobiles/Normal/GreaterPoisonElemental.cs` | |
| `TyballsShadow` | `Items/Quest/TyballsShadow.cs` | в оригинале лежит среди предметов — видимо, по ошибке |
| `Garamon` | `Mobiles/NPCs/Garamon.cs` | обычный `Mobile`, не `BaseCreature`, как в оригинале |
| `LuckyCoin`, `UndamagedIronBeetleScale` | `Items/Quest/SAQuestItems.cs` | |
| `ShroudOfTheCondemned` | `Items/Artifacts/Equipment/Clothing/` | падает с тени Тайболла в 10 % случаев |

Правка в чужом файле: `BaseHarvestTool.ToggleMiningStoneEntry` из `private` стал `internal` —
жук выкладывает те же две записи в своё контекстное меню, ровно как в оригинале.

Починены два места в оригинале, оба задокументированы в шапках файлов:
`Garamon.OnSpeech` кастует говорящего в `PlayerMobile` и сразу разыменовывает без проверки
на `null` — падал бы на любой речи не от игрока; таймер добычи руды у жука никогда не
останавливается при удалении — здесь это `TimerExecutionToken`, снимаемый в `OnAfterDelete`.

### Священный Поиск — система построена

`Garamon` и `TyballsShadow` без неё бессмысленны: первый объясняет головоломку, второй
стережёт последнюю часть ключа. Портировано целиком (`Items/Underworld/AbyssKeys.cs`,
`AbyssGates.cs`):

`AbyssKey` (у ServUO это построчный дубль его же `BaseDecayingItem` — здесь тонкий наследник
уже существующего), `RedKey1`, `BlueKey1`, `YellowKey1`, `TyballsKey`, `TripartiteKey`,
`Redkeyfragment`, `Bluekeyfragment`, `AbyssBarrier`, `SacredQuestBlocker`, `UnderworldTele`.
Имена классов — оригинальные буква в букву, чтобы декорации и `objects.xml` ServUO по ним
резолвились. Плюс флаг `PlayerFlag.AbyssEntry` с тем же значением `0x00400000`, что у ServUO.

Осколки ключей в данных этого шарда сейчас нигде не расставлены — классы есть, декорацию
надо будет добавить. Второй путь к тому же флагу — вопросник «La Insep Om» у Shrine of
Singularity — относится к Tomb of Kings, то есть к блоку B2, и здесь не делался.

### Попутно: `LuckyCoin` вернулся в семь мест

Монета была выброшена из семи портов с пометкой «такого класса здесь нет». Теперь есть —
дроп возвращён с оригинальными шансами: `BloodWorm` 2 %, `GreenGoblin` 1 %,
`GreenGoblinAlchemist` 2 %, `TanglingRoots` 2 %, `WolfSpider` 1 %, `Navrey` 10 %, а также в
таблице наград головоломки Лабиринта Смерти (стек 2–6, как в оригинале). Устаревшие
комментарии в шапках всех семи файлов переписаны.

## Дешёвые и громкие: два существа Gravewater Lake

`DiabolicalSeaweed` и `Paralithode` — по 2370 строк в `badspawn.log` каждое, самые массовые
из неразрешимых имён. Оба из `Services/ExploringTheDeep/Mobiles/`, оба самодостаточные.

В водоросли починен баг оригинала: она пропускает всех, у кого `AccessLevel == Player`, то
есть всех игроков — и ветка ниже, проверяющая `m.Player`, недостижима. Главная её механика
(затаскивать к себе всё в радиусе 9) на игроках просто не работала. Условие перевёрнуто на
«пропускать стафф», что там очевидно и имелось в виду.

## Перепись неразрешимых типов — 112 вместо 218

Пересчитал по `badspawn.log`, снимая суффиксы XmlSpawner (`,{rnd,4,8}`, `/name/…`, `,3`),
которые в лог попали как часть строки, но при спауне разбираются:

| | было | стало |
|---|---|---|
| различных имён в логе | 218 | 194 (после снятия суффиксов) |
| из них не резолвятся | 218 | **112** |

Оставшееся группируется так:

| Группа | Имена | Строк в логе |
|---|---|---|
| **High Seas / Exploring the Deep** | `lavaelemental` | 553 |
| **Exodus Encounter** (B3) | `exoduschest` 948, `cubenclosure` 237, `exodusdrone`/`juggernaut`/`minionlord`/`sentinel`/`zealot`/`archzealot` | ~2.1 тыс. |
| **Лагеря** | `elfbrigandcamp` 474, `prisonercamp` 395 | 869 |
| **Ter Mur / Tomb of Kings** (B2) | `agralem`, `aurvidlem`, `ansikart`, `axem`, `beninort`, `broolol`, `cohenn`, `drelgor`, `egwexem`, `gnosos`, `laifem`, `naxatilor`, `niporailem`, `ortanord`, `percolem`, `prassel`, `queenzhah`, `sliem`, `thepem`, `zosilem`, `xeninlor`, `bookofcircles`, `shrinemantra`, `gargishwanderinghealer`, `undeadgargoyle`, `putridundeadguardian`, `lowlandboura`, `descicatedmyrmidexlarvae`, `slasherofveils` | ~2.3 тыс. |
| **Именные NPC Fel/Tram** (B4) | `aminia`, `abbein`, `acob`, `alethanian`, `aneen`, `athialon`, `brae`, `calendor`, `fabrizio`, `frazer`, `gregorio`, `ioseph`, `jothan`, `lefty`, `lenley`, `lucius`, `mallew`, `natalie`, `neil`, `nillaen`, `onallan`, `oolua`, `petrus`, `rollarn`, `ryal`, `sarakki`, `siarra`, `sirberran`, `sirfelean`, `sirhareus`, `szandor`, `taellia`, `tyleelor`, `verity`, `vicaie`, `jacob`, `amelia`, `aliabeth`, `farmernash`, `flurry`, `grim`, `mistral`, `tempest` | ~6.8 тыс. |
| **Книги** (содержимое библиотечных полок) | `artssection`, `britanniawaters`, `denthesjournal`, `foldedsteel`, `lightandmight`, `maceandblade`, `oilandoubliette`, `pasttreasures`, `songsofnote`, `sosariasap`, `trades`, `understandinganimals`, `wizardscompendium`, `ariellesbauble` | ~2.3 тыс. |
| **Цитадель и прочее** | `tigersclawmaster`, `minionofscelestus`, `skeletonkey`, `dupreschampion`, `dupresknight`, `dupressquire`, `paladin`, `escortablehealer`, `maulbear`, `firerabbit`, `ignisfatalis`, `hawkwind2`, `skeletaldragonrenowned` | ~1.6 тыс. |

Пересчёт повторяем: разобрать 5-е поле лога, отрезать по `[,/]`, сверить с именами классов
в `Projects/UOContent`.

## Ter Mur, заход первый: три недостающие системы

Прежде чем браться за квестовую линейку, закрыл три системы, на которых у уже сделанных
портов висели пометки «здесь такого нет». Сборка чистая: 0 ошибок, 0 предупреждений.

### 1. Мех и драконья кровь при разделке

`ICarvable` в этой кодовой базе есть, а `Fur`/`FurType`/`DragonBlood` не было — из-за этого
из пяти существ Ter Mur выбрасывали стрижку меха, а из пяти же — карв драконьей крови.

Добавлено: `FurType` (5 значений) и виртуальные `Fur`, `FurType`, `DragonBlood` на
`BaseCreature`; две ветки в `BaseCreature.OnCarve`; предметы `Fur`
(`Items/Resources/Fur.cs`, один стековый на все четыре цвета) и `DragonBlood`
(`Items/Resources/Reagents/`).

Стрижка живого зверя в оригинале повторена дословно в пяти файлах — здесь это один хелпер
`FurShearing.TryShear`, каждое существо держит только свой флаг «уже стрижен» и свои клилоки.

Восстановлено на `HighPlainsBoura`, `RuddyBoura`, `Kepetch`, `KepetchAmbusher` и добавлено
сразу с мехом на новом `LowlandBoura`. Заодно у всех пятерых вернулся карв драконьей крови.

Оригинал прогоняет драконью кровь в рюкзак, если разделывают `HarvestersBlade` — этого
предмета здесь нет, а без него оригинал тоже всегда кладёт кровь на труп; так и сделано.

### 2. `AIType.AI_NecroMage`

Семь уже портированных существ строились на нём в оригинале и были опущены до `AI_Mage`:
`SkeletalLich`, `AncientLichRenowned`, `DevourerRenowned`, `Lifestealer`, `MaddeningHorror`,
`LadyMelisande`, `Travesty`. Теперь AI есть (`Mobiles/AI/NecroMageAI.cs`), и все семь
переведены обратно.

Здешний `MageAI` уже сам выбирает некро-заклинания через `IsNecromancer`/`UseNecromancy` —
чего у ServUO нет, — поэтому половина оригинального `NecroMageAI`, существующая только ради
этих пулов, не дублируется. Своё у него:

- Spirit Speak как самолечение: 10 % попыток лечения в бою и отдельная ветка в `DoActionGuard`,
  где шанс масштабируется по Некромантии так же, как магический — по Магии;
- Curse Weapon и призыв Animate Dead в слоте самобаффа.

Правка в `MageAI`: слот самобаффа был захардкоженный `BlessSpell`, стал
`GetRandomBuffSpell()`; `HealChance` из `private` в `protected`. Поведение самого `MageAI` не
изменилось. У Травести в «копировании стиля боя» появилась ветка Некромантии.

### 3. `BaseSABoss`

Боссы Stygian Abyss платят иначе, чем Peerless: вместо ML-спецнаграды один бросок при смерти —
5 % из личного списка артефактов босса, ещё 10 % из общего, и вещь достаётся одному участнику,
выбранному взвешенно по урону. База построена на уже портированном `BasePeerless`
(`Engines/Peerless/BaseSABoss.cs`).

Две недоделки оригинала не копировал, а описал в шапке: его `AwardArtifact` собирает
отфильтрованный словарь подходящих игроков и потом никогда его не читает — тянет из
нефильтрованного, так что артефакт может «выиграть» тот, кому его некуда положить, и вещь
молча удаляется; и его `OnDeath` собирает список игроков с правом на лут и ничего с ним не
делает.

### Существа, которые на этом поехали

| Класс | Заметка |
|---|---|
| `Niporailem` | `BaseSABoss`. Отрывает от себя куски призрачной брони в ближнем бою и швыряет в противника стостоуновый мешок золота каждые 5–15 с |
| `SlasherOfVeils` | `BaseSABoss`. Мигает на кастера: половина заклинаний в него — и он оказывается сверху |
| `NiporailemsTreasure` | положил куда угодно — превратилось в бесполезный песок вчетверо легче |
| `UndeadGargoyle`, `PutridUndeadGuardian`, `Ortanord`, `DescicatedMyrmidexLarvae` | прямые порты |
| `Drelgor` | Old Haven. Оригинал ради рассылки обходит весь список `NetState` и сравнивает имена регионов строками с обеих сторон — здесь он спрашивает игроков у собственного региона |
| `GargishWanderingHealer` | бродячий лекарь с телом горгульи, учит ещё и Мистицизму |
| `BookOfCircles`, `ShrineMantra` | книги у Святилища Сингулярности — то, по чему экзаменует «La Insep Om» |

Оба списка артефактов у `Niporailem` и `SlasherOfVeils` пока пустые: **ни одного** из
названных ими 13 + 28 артефактов в кодовой базе нет. `BaseSABoss` бросает по пустому списку
без нареканий — заполнить, когда будут порты самих артефактов.

### Новый блок работ: артефакты Stygian Abyss

Оформился отдельный крупный кусок, которого раньше в ТЗ не было:

- **Комплекты Villainous / Virtuous Epiphany** — 24 предмета, 1700 строк исходника, с
  сетовыми бонусами. Их ждёт `Niporailem`.
- **Общий список SA** — `BladeOfBattle`, `DemonBridleRing`, `GiantSteps`,
  `SwordOfShatteredHopes`, `AxesOfFury`, `PetrifiedSnake`, `PillarOfStrength`, `SummonersKilt`.
- **Личный список Разрывателя Завес** — `ClawsOfTheBerserker`, `Lavaliere`, `Mangler`,
  `HumanSignOfChaos`, `GargishSignOfChaos`, `StandardOfChaos`, `StandardOfChaosG`.

Их же ждут пометки в `Medusa.cs`, `AcidElementalRenowned.cs`, `AncientLichRenowned.cs`,
`DevourerRenowned.cs` и ещё нескольких портах — то есть это закрывает хвосты сразу у десятка
файлов.

### Что осталось по Ter Mur

Сама квестовая линейка — следующий шаг. Это линейка Соулфорджа (Imbuing) плюс лорные квесты:
16 NPC (`Agralem`, `Aurvidlem`, `Ansikart`, `Axem`, `Beninort`, `Broolol`, `Cohenn`,
`Egwexem`, `Gnosos`, `Laifem`, `Naxatilor`, `Percolem`, `QueenZhah`, `Sliem`, `Thepem`,
`Zosilem`) и их квесты (`KnowledgeoftheSoulforge`, `MasteringtheSoulforge`, `ALittleSomething`,
`SecretsoftheSoulforge`, `TheAncientWorldQuest`, `UnusualGoods`,
`JourneyToTheAthenaeumIsleQuest`), плюс «La Insep Om» у Святилища Сингулярности и система
ярусных квестов (`ITierQuester`/`TierQuestInfo`), на которой сидят `Percolem`, `Thepem` и
`Zosilem`.

Попутно понадобится мелочь по экипировке: здешняя броня горгулий разделена на `Type1`/`Type2`
(как в настоящем UO), а у ServUO имена без суффикса — сопоставляется один к одному. Реально
недостают только `SerpentStoneStaff` и `GargishClothWingArmor`.

### Перепись после этого захода

| | было в начале | после хвоста Underworld | сейчас |
|---|---|---|---|
| неразрешимых имён | 218 | 112 | **99** |

## Перепись переведена на живые данные

`badspawn.log` устарел (генерация 26.08). Пересчёт теперь идёт не по нему, а по самим файлам
`Distribution/XmlSpawner/*.xml`: разобрать `<Objects2>`, разбить по `:OBJ=`, взять первый
токен до `:`, отрезать по `[,/]`. Это ровно то, что спаунер попросит у
`AssemblyHandler.FindTypeByName`.

По живым данным: **800 различных типов, из них не резолвились 85** (а не 99, как показывал
лог). После этого захода — **81**.

### 15 имён оказались не пропущенным контентом, а расхождением данных

`Broolol` в спаунере, `LorekeeperBroolol` в классе — и так пятнадцать раз. ModernUO
складывает отображаемый префикс в имя класса, ServUO держит его отдельным полем; данные
XmlSpawner приехали из ServUO-источника. Классы, `MLQuests.cfg` и `categorization.json` здесь
согласованы между собой — расходились только XML-спаунеры, их и поправил:

| В спаунере было | Класс |
|---|---|
| `Abbein`, `Alethanian`, `Jothan`, `Mallew`, `Onallan`, `Taellia`, `Vicaie` | `Elder…` |
| `Aneen`, `Broolol`, `Calendor`, `Nillaen`, `Oolua`, `Rollarn`, `Siarra` | `Lorekeeper…` |
| `Gnosos` | `MasterGnosos` |

Каждая пара сверена по отображаемому имени и титулу с обеих сторон. Правка в
`felucca.xml` (13), `trammel.xml` (14), `malas.xml` (1).

Проверка на такие же расхождения по всему остатку — ещё семь кандидатов отсеялись как ложные
(`amelia`, `jacob`, `petrus`, `paladin`, `szandor`, `foldedsteel`, `trades` — совпадения
случайные, это разные сущности).

## Что портировано этим заходом

| Класс | Спаунеров | Заметка |
|---|---|---|
| `ToxicElemental` | **48** | самый массовый пропуск во всех данных. Имя — странность оригинала, а не описка: класс `ToxicElemental`, а зовётся кислотным элементалем, вместе с трупом |
| `LavaElemental` | 7 | |
| `ElfBrigandCamp` | 6 | обычный лагерь бандитов, в котором одна строка другая |
| `PrisonerCamp` | 5 | пленник за запертыми железными воротами и шесть тюремщиков — орки, крысолюды, ящеры или бандиты, выбор один раз при создании |
| `OrcChopper` | — | нужен лагерю пленников |
| `EvilOrcHelm` | — | падает с дровосека в 10 % случаев |

`PrisonerCamp` в оригинале зовёт `AddCampChests()` у `BaseCamp` — здесь этот метод лежал
только на `BrigandCamp`. Поднял его на `BaseCamp` как `protected virtual`, ровно туда, где он
у оригинала; `LizardmenCamp`, `OrcCamp` и `RatCamp` со своими вариантами стали `override`.

У дровосека выброшен дроп `Yeast` — это ингредиент дистилляции Новой Магинции, системы здесь
нет; и две регистрации `SetWeaponAbility`, которые вернулись через `GetWeaponAbility`.

## Линейка Соулфорджа: награды готовы

Под квесты Ter Mur портированы `ScrollBox`, `ScrollBox2`, `ScrollBox3` (по свитку силы
Imbuing — 115, 120, 105/110 — и один шанс из двадцати на рунический молоток с зубилом),
`RunicMalletAndChisel` (рунический инструмент каменщика, недостававший к уже имеющимся
руническим молоту, набору для шитья и инструменту лучника) и `MeagerImbuingBag`.

Бросок на руническое в оригинале повторён дословно во всех трёх коробках — здесь один хелпер.
У `ScrollBox` в оригинале есть приватный `PlaceItemIn`, который никто не зовёт — выброшен.
`MeagerImbuingBag` наследуется у ServUO от `BaseRewardBag`, но оставляет `ItemAmount` нулём,
так что цикл наполнения оружием и украшениями никогда не крутится и в сумке лежит только
ингредиент — так и сделано, без наследства, которое не срабатывает.

### Открытый вопрос: ингредиенты имбуинга SA

Сумка выдаёт один из четырёх ингредиентов SA — `SlithTongue`, `GoblinBlood`,
`ReflectiveWolfEye`, `RaptorTeeth`. Эти четыре портированы, потому что их выдаёт награда.

Остальные ~26 и таблица `IngredientDropEntry`, которая вешает их на смерть существ,
**намеренно не портированы**: этот шард заменил OSI-имбуинг собственной трёхъярусной системой
материалов (`Systems/MahaonImbuing`), и у этих предметов здесь не было бы потребителя.
Тянуть ли всю OSI-экономику ингредиентов — решение владельца.

## Ещё один новый блок: Community Collections

Четырнадцать имён, которые я в прошлой переписи отнёс к «книгам с библиотечных полок», —
на самом деле NPC системы общественных коллекций: Библиотека Британии, Королевский зоопарк и
музей. `ArtsSection` — это Зорда-художница, представитель отдела искусств, а не книга.

| Что | Объём |
|---|---|
| Каркас `Services/CommunityCollections` | ~2.9 тыс. строк: `BaseCollectionMobile`, `BaseCollectionItem`, `CollectionItem`, `CollectionsSystem`, гамп коллекции, два ящика пожертвований |
| 14 NPC-представителей | `ArtsSection`, `BritanniaWaters`, `DenthesJournal`, `FoldedSteel`, `LightandMight`, `MaceandBlade`, `OilandOubliette`, `PastTreasures`, `SongsofNote`, `SosariasAp`, `Trades`, `UnderstandingAnimals`, `WizardsCompendium`, `AriellesBauble` |
| Награды | одежда «Library Friend», талисманы, очки, книги цитат, статуэтки — десятки предметов |

Блок крупный и самодостаточный; в ТЗ он теперь отдельной строкой, а не в куче «книг».

## Ter Mur: Королевский Город, семь NPC и восемь квестов

| Квест | Квестодатель | Цель | Награда |
|---|---|---|---|
| Knowledge of the Soulforge | Аурвидлем | 50 × `EnchantedEssence` | `ScrollBox` (свиток силы Imbuing 115) |
| Mastering the Soulforge | Ансикарт | 50 × `RelicFragment` | `ScrollBox2` (120) |
| A Little Something | Ансикарт | 1 × `BrilliantAmber` | `MeagerImbuingBag` |
| Secrets of the Soulforge | Бенинорт | 50 × `MagicalResidue` | `ScrollBox3` (105 или 110) |
| Rumors Abound | Эгвексем | доставить `EgwexemWrit` Наксатиллору | клилок-награда |
| The Arisen | Наксатиллор | 10 нежитей-горгулий (какая из трёх — бросок при создании квеста) | `NecklaceofDiligence` |
| Misplaced | мастер Кохенн | 5 × `DisintegratingThesisNotes` | `LibrariansKey` |
| Unusual Goods | Слием | 2 × `PerfectEmerald` + 1 × `CrystallineBlackrock` | `EssenceBox` |

Все восемь записей добавлены в `Data/MLQuests.cfg`. Форма — `MLQuest`, как и в линейке
Underworld: второй каркас квестов (`BaseQuest`/`MondainQuester`) не тянется, клилоки, счётчики
и награды взяты из оригинала.

Две мелочи, где оригинал не даёт клилока: у «Rumors Abound» строка «ты ещё не говорил с
Наксатиллором» написана у ServUO простым английским текстом — переведена, как и остальной
свободный текст этого шарда. У «Unusual Goods» вторая награда — очко Loyalty Rating; системы
лояльности здесь нет, выдаётся только коробка.

`Naxatilor` у оригинала — класс с одной «л», файл с двумя, и сам оригинал заклеивает
расхождение через `TypeAlias` и переписывание спаунеров. Взял имя, на котором он остановился,
и повесил тот же алиас.

### Предметы и снаряжение под линейку

`EssenceBox`, `DisintegratingThesisNotes` (наследник `PeerlessKey`, как в оригинале),
`EgwexemWrit`, `NecklaceofDiligence`, `CrystallineBlackrock`, `GargishClothWingArmor`.

**Одиннадцать эссенций добродетелей** (`Items/Resources/Essences.cs`) — замкнутый набор из
лора Ter Mur, по одной на каждую добродетель Книги Кругов, плюс `Loot.RandomEssence()`,
которым `EssenceBox` их и выдаёт. В отличие от остальных ингредиентов имбуинга SA этот набор
портирован целиком: он самодостаточен и его раздаёт квестовая награда.

### Чуть не создал дубль

`SerpentStoneStaff` из оригинала здесь уже есть — под именем `SerpentstoneStaff`, со строчной
«s» в «stone», и с `[TypeAlias("Server.Items.SerpentStoneStaff")]`, чтобы данные ServUO по
нему резолвились. Мои проверки наличия классов шли грепом с учётом регистра и этого не
увидели; файл был перезаписан и восстановлен из git.

Прогнал по всему `Projects/UOContent` детектор столкновений имён без учёта регистра —
других дублей нет.

## Три NPC Королевского Города отложены — и почему

| NPC | Блокирует |
|---|---|
| `Axem` | цепочка музейных квестов: `AncientPotteryFragments`, `TatteredAncientScroll`, `UntranslatedAncientTome`, три «музейные сумки» и Loyalty Rating |
| `Thepem` | торговец с BOD-ветками алхимии (`SmallAlchemyBOD`/`LargeAlchemyBOD`, `BulkOrderSystem.NewSystemEnabled`) плюс мини-игра с проверкой слитков |
| `Zosilem`, `Percolem` | система ярусных квестов `ITierQuester`/`TierQuestInfo` |

Каждый из них тянет за собой систему, которой в этом шарде нет. Половинчатый порт тут хуже
отсутствия, так что они отдельными строками в списке работ, а не молча упрощены.

`Agralem` тоже отложен, но по другой причине: его квест «Into the Void» требует
`BaseVoidCreature` — набор Void Creatures, 289 строк базы плюс десять существ. Этот же набор
ждёт `GargishRouser` (его призыв `VoidManifestation` был единственным, что осталось у него
неперенесённым). Набор небольшой и самодостаточный — хороший следующий кусок.

`Laifem` и `QueenZhah` — цепочка ткачества (нужны `BritannianWool`, `LetterOfIntroduction`,
`MasteringWeaving` и NPC `Dermott` в Веспере) и путешествие на Атенеум (нужны
`MinionOfScelestus` и `ChronicleOfTheGargoyleQueen1`). Обе доступны, просто не влезли в этот
заход.

### Перепись

| | по логу | по живым данным |
|---|---|---|
| в начале | 218 | — |
| после хвоста Underworld | 112 | — |
| после первого захода Ter Mur | 99 | 85 |
| **сейчас** | — | **74** (143 записи спаунеров) |

## Void Creatures — система портирована целиком

Одиннадцать существ и их механика эволюции (`Mobiles/TerMur/Void/`). Смысл набора в том, что
тварь Пустоты не остаётся собой: постояв достаточно долго или собравшись со своими, она
заменяет себя следующей по одной из трёх линий.

| Линия | Стадия 1 | Стадия 2 | Стадия 3 |
|---|---|---|---|
| Killing | Betballem | Ballem | Usagralem Ballem |
| Grouping | Anlorzen | Anlorlem | Anlorvaglem |
| Survival | Anzuanord | Relanord | Vasanord |

`Korpre` — нулевая стадия, с которой всё начинается. Какую линию выберет первая мутация,
решается один раз: «групповая», если рядом в двенадцати клетках набралось достаточно своих,
иначе «на выживание»; дальше линию несёт уже само существо. Каждая мутация с шансом 5 %
вырывает из Пустоты `Ortanord`.

Дропы: `VoidEssence` со второй стадии и выше, `VoidCore` — только с третьей.

### Что при этом пришлось достроить

**`FightMode.Good`.** Все твари Пустоты принудительно охотятся на игроков с положительной
кармой — этого значения в здешнем перечислении не было. Добавил и провёл через
`BaseAI.IsInvalidFightModeTarget` и `BaseCreature.IsEnemy`, зеркально к уже имевшемуся `Evil`
(питомец при этом судится по карме хозяина, как и там).

Заодно это чинит второе место, где значение уже было нужно: восемь злых существ Despise
портировались с `FightMode.Aggressor` вместо `Good`, и `DespiseCreature.OnKarmaChange`
сваливал обе ветки в `Aggressor`. И то и другое возвращено к оригиналу.

**Передача места спаунера при мутации.** Оригинал при эволюции удаляет существо и подменяет
его в списке `XmlSpawner`, чтобы слот не освободился и не заполнился тут же новой нулевой
стадией. Сделал то же, но против здешнего интерфейса спаунера, а не внутренностей
XmlSpawner — работает и для нативных JSON-спаунеров.

**Выброшено:** `RemoveVoidSpawners` — разовая миграция сейвов, обходившая на старте все
предметы мира и печатавшая в консоль (что здесь запрещено правилом). Сейвы одноразовые,
мигрировать нечего.

### Два сломанных блока оригинала

Портированы по фактическому результату, а не построчно, с пометкой на месте:

- `Betballem` четыре раза подряд пишет сопротивление огню (побеждает последнее, 100) и ни
  разу — холод, яд и энергию.
- `Anzuanord` трижды пишет физическое и дважды яд; в итоге остаются физическое 100 и яд 0–20,
  а огонь, холод и энергия не выставлены вовсе.
- `Vasanord` пишет `new TaintedSeeds(2)`, но этот предмет не стековый и не принимает
  количество — двойка у ServUO уходит в конструктор десериализации как `Serial`, и падает одна
  битая вещь. Здесь падает два семени, что там явно и имелось в виду.

## `GargishRouser` наконец целый

Его порт был урезан из-за трёх недостающих вещей. Последняя из них — `VoidManifestation` —
теперь есть, вместе с тремя кристаллами Пустоты, которые она за собой оставляет.

Само воплощение поднимает Rising Colossus каждые полминуты и перекидывается между Мистицизмом
и Магией каждые 10–30 секунд, сохраняя цель через переключение. Раусер зовёт его с шансом 1 к
20 вместо очередного колосса — один раз за жизнь — и ещё раз при собственной смерти.

## Квест Агралема

`IntoTheVoid` — десять тварей Пустоты за `AbyssReaver`. Цель, как и в оригинале, ссылается на
`BaseVoidCreature`, поэтому засчитывается любая стадия любой линии. Плюс сам `Agralem`
Клинкоплёт. Итого по Королевскому Городу — **восемь NPC и девять квестов**.

### Перепись

| | по живым данным |
|---|---|
| после первого захода Ter Mur | 85 |
| после квестов Ter Mur | 74 |
| **сейчас** | **73** (141 запись спаунеров) |

Число почти не сдвинулось, и это ожидаемо: твари Пустоты в спаунерах этого шарда пока не
расставлены — портированы ради квеста Агралема и раусера. Зато `FightMode.Good` закрыл хвост
у восьми уже сделанных существ Despise.

## Exodus: сундук и восемь существ

| Класс | Спаунеров | Заметка |
|---|---|---|
| `ExodusChest` | **12** | невидим, пока рядом не окажется кто-то с 98 Detect Hidden; найденный живёт пять минут и исчезает вместе с содержимым |
| `ExodusDrone`, `ExodusSentinel`, `ExodusJuggernaut`, `ExodusMinionLord` | по 2 | одинаковое энергополе, вынесено в общую базу |
| `ExodusZealot` | 2 | человеческая половина культа |
| `DupresSquire`, `DupresKnight`, `DupresChampion` | по 2 | три ранга с одинаковыми числами, различие только в снаряжении |

### Энергополе

Пока поле поднято, механических прислужников не берёт физический урон и берёт любой
магический; ниже девяти десятых здоровья поле падает, и всё наоборот. Лечение его
восстанавливает. Удар заклинанием или из лука — и они отвечают молнией.

Оригинал повторяет этот код дословно в четырёх файлах; здесь он один раз, в базе
`BaseExodusMinion`. Обе пометки оригинала «баг OSI не даёт это проверить» (порог девяти
десятых и восстанавливается ли поле) оставлены там, где стояли.

### Регион сундука

У сундука собственный регион 5×5, который шепчет проходящему мимо с нужным навыком, что рядом
что-то спрятано, — единственная подсказка, которую даёт подземелье. Регион снимается в
`OnAfterDelete` вместе с таймером.

### Ритуальные предметы — крюк на месте, список пуст

`ExodusChest.RitualItems` пока пустой массив, и `GiveRitualItem` при пустом списке молчит.
Там должны лежать четыре предмета призыва — свиток обряда, жертвенный кинжал, ритуальная роба
и алтарь призыва, — но это вход в сам Encounter: алтарь его строит, свиток и кинжал им правят,
а `ClockworkExodus` его заканчивает. Всего этого пока нет.

Крюк стоит в правильном месте, и все восемь существ его уже зовут. Когда ритуальная цепочка
будет портирована, четыре типа попадут в один массив и вся цепочка дропа оживёт. Раздавать
предметы, которыми не на что воздействовать, было бы хуже, чем не раздавать их вовсе.

### Ещё три сломанных вызова оригинала

Та же ошибка, что у `Vasanord` с семенами, ещё трижды: `new SmokeBomb(Utility.Random(3,6))`,
`new ParasiticPotion(Utility.Random(1,3))`, `new InvisibilityPotion(...)`. Ни один из трёх
предметов не принимает количество — число уходит в конструктор десериализации как `Serial`, и
в сундук падает одна битая вещь вместо стопки. Исправлено на то, что явно имелось в виду.

### Отложено из блока Exodus

`ExodusArchZealot` (1 спаунер) — он объясняет ритуал многостраничным гампом и превращает
кинжал в гаргульский вариант; ему место рядом с ритуальной цепочкой, а не отдельно.

### Перепись

| | неразрешимых | записей спаунеров |
|---|---|---|
| после Void Creatures | 73 | 141 |
| **после Exodus** | **64** | **113** |

## Следующий очевидный шаг

1. **Именные NPC Fel/Tram** (B4) — самая крупная оставшаяся группа: ~30 имён по два
   спаунера, по файлу на каждое, чистая ваниль.
2. **Ритуальная цепочка Exodus** — четыре предмета призыва, алтарь, `ClockworkExodus`,
   `ExodusArchZealot`; оживляет уже стоящий крюк `ExodusChest.RitualItems`.
3. **Артефакты Stygian Abyss** — закрывает хвосты сразу у десятка уже сделанных портов и
   заполняет оба списка у `Niporailem` и `SlasherOfVeils`.
4. **Community Collections** — 14 имён, крупный самостоятельный блок.
5. **Хвост Ter Mur** — `Laifem` с цепочкой ткачества, `QueenZhah` с Атенеумом; затем
   `Axem`/`Thepem`/`Zosilem`/`Percolem`, каждый со своей системой (музей, BOD, ярусные квесты).
6. **«La Insep Om»** у Святилища Сингулярности — второй путь к `AbyssEntry`, нужен вопросник
   `QuestionAndAnswerObjective`/`QAndAGump`.
7. **Мелочь Eodon** — `CubEnclosure` из квеста «Valley of One» (3 спаунера).
8. **Тройной набор спаунеров Цитадели** (данные, не код).
9. Хвост мелких дропов Peerless.

## Новая находка по ходу работы

### 🟠 СРЕДНИЙ — The Citadel спаунится тройным набором

Один и тот же данж заполняется из трёх источников сразу:

| Источник | Спаунеров | Координаты |
|---|---|---|
| `Data/Spawns/shared/malas/Citadel.json` (нативный ModernUO) | 15 | 77,1883 … 185,1920 |
| `XmlSpawner/malas.xml`, набор `Citadel#0..14` | 15 | **те же 15 координат, 1:1 дубль** |
| `XmlSpawner/malas.xml`, набор `TheCitadel#0..12` | ~16 | сильно перекрывается с первыми двумя |

То есть после `[XmlLoad malas.xml` плотность мобов в Цитадели втрое выше задуманной.
Проверить стоит и другие данжи, где есть и `Data/Spawns/**`, и запись в XmlSpawner —
Prism of Light как минимум тоже присутствует в обоих (`PrismOfLight.json` +
`PrismOfLight#N` в `felucca.xml`/`trammel.xml`).

### Открытый вопрос по ходу работы

`ClanChitterTinkerer` (бывш. `ClanCT`) показывает игроку **«Clan Scratch Tinkerer»** —
и в `DefaultName`, и в `CorpseName`. Сокращение «CT» рядом с `ClanCA`/`ClanRC`/`ClanRS`/
`ClanSH`/`ClanSS`/`ClanSSW` читается как Chitter Tinkerer, и данные ModernUO просят
именно `ClanChitterTinkerer` — похоже на копипасту в исходнике ServUO (файла `ClanST.cs`
там нет). **Видимые игроку строки я не трогал** — комментарий с разбором оставлен в файле.
Менять на «chitter» или оставить «scratch»?

---

## 0. Что и как проверялось

| Область | Метод | Источник данных |
|---|---|---|
| Спауны XmlSpawner | сверка 264 уникальных имён из лога с 6285 классами кода | `Distribution/badspawn.log` (20 347 строк), `Distribution/XmlSpawner/*.xml` |
| Спауны нативные (ModernUO) | сверка 546 имён из JSON с классами | `Distribution/Data/Spawns/**` (109 файлов) |
| Квесты ML | сверка 265 квестов и 104 NPC-квестодателей с классами | `Distribution/Data/MLQuests.cfg` |
| Телепорты | парсинг 1368 записей, поиск коллизий src | `Distribution/Data/teleporters.json` |
| Регионы | сверка имён регионов с реализованным контентом | `Distribution/Data/regions.json` (76 `DungeonRegion`) |
| Код | правила CLAUDE.md #2/#3/#4/#5/#6/#10/#17, точечный разбор портов | `Projects/UOContent/**` |

**Ключевой вывод:** код в целом здоров (сборка чистая, порты сделаны по конвенциям —
`[SerializationGenerator]`, `[Constructible]`, `GenericPersistence`, `TimerExecutionToken` —
проверено по всем портированным папкам мобов, нарушений нет). Основная проблема шарда —
**не баги в коде, а дыры в контенте**: данные мира (спаунеры, квестовые цепочки, порталы)
ссылаются на сущности, которых в коде физически нет.

---

## 1. Резюме находок

| # | Блок | Влияние | Объём |
|---|---|---|---|
| **A** | Спауны: 140 неразрешимых типов | ~4400 строк лога, мёртвые зоны | L |
| **B** | Мёртвые линейки квестов (4 крупных) | контент недоступен игроку | XL |
| **C** | Телепорты: 2 отсутствующих класса + 16 конфликтов координат | подземелья недостижимы / ведут не туда | S |
| **D** | Пробелы ванильного контента (Peerless, Mysticism, High Seas, TOL) | 6 подземелий без боссов, 8/16 заклинаний школы | XL |
| **E** | Новые баги в коде | утечка таймера и регионов, нарушения правил | S |
| **F** | Незакрытые пункты старого аудита (17 шт.) | см. `code-audit-findings.md` | M |

---

## Блок A. Спауны: неразрешимые типы

### A1. Устаревшая часть лога — требует перегенерации (приоритет: сделать первым)

`badspawn.log` датирован **26.08.2026**, то есть **до** портирования Despise/Eodon/TerMur.
Из 217 уникальных имён **68 уже существуют** в коде и резолвятся сейчас нормально
(`AcidSlug`, `BirlingBlades`, `Hellion`, `Phantom`, `Naba`, `Fairy`, `Skeletrex`, `Ursadane`,
`Silenii`, `Sagittarri`, `Prometheoid`, `DivineGuardian`, `DespiseUnicorn`, `Dendrite`,
`Darkmane`, `Echidnite`, `ForestNymph`, `SerpentsFangAssassin`, `DragonsFlameMage`,
`TigersClawThief`, `TrapdoorSpider`, `KepetchAmbusher`, `CoralSnake` и др.).

**Задача A1.** Удалить `Distribution/badspawn.log`, выполнить `[XmlLoad` по всем 13 файлам
`Distribution/XmlSpawner/`, получить актуальный лог. Ожидаемый результат: лог сократится
с 20 347 до ~5–6 тыс. строк. Все дальнейшие оценки в блоке A опираются на анализ по
текущему коду, но перегенерация нужна как контрольная точка.

> Резолв типов в ModernUO **регистронезависимый** (`AssemblyHandler.FindTypeByName`,
> `Projects/Server/AssemblyHandler.cs:189`, `ignoreCase = true` по умолчанию), поэтому
> расхождения регистра в данных (`maulbear` / `MaulBear`) багом **не являются** — не тратить
> на них время.

### A2. Расхождение имён «класс ↔ данные» — быстрый фикс

Классы существуют, но под другим именем. Нативные спауны ModernUO
(`Distribution/Data/Spawns/**`) ссылаются на ванильные имена:

| Имя в данных | Класс в коде | Файл |
|---|---|---|
| `NavreyNightEyes` | `Navrey` | `Projects/UOContent/Mobiles/Monsters/Underworld/Navrey.cs` |
| `DryadA`, `Dryada` | `Dryad` | опечатка в данных |
| `GreenGoblinScout` | `EnslavedGoblinScout` | разные сущности в ванилле — нужен отдельный класс |
| `SkeletalDragonRenowned` | `SkeletalDragon` | нужен Renowned-вариант (база `BaseRenowned` уже есть) |
| `treasurelevel1h` | — | опечатка в данных, ср. алиасы `TreasureLevel1..4` в `Items/TreasureChests/Chests.cs` |

**Задача A2.1.** Переименовать `Navrey` → `NavreyNightEyes` (ванильное имя) либо добавить
класс-алиас. Проверить все ссылки: `EyeOfNavrey`, `NavreyParalyzingWeb`, `NavreysController`,
`NavreysPillar`.
**Задача A2.2.** Починить опечатки `DryadA`/`Dryada`/`treasurelevel1h` в JSON-спаунах.
**Задача A2.3.** Добавить `SkeletalDragonRenowned`, `FireElementalRenowned` (паттерн уже есть:
`AcidElementalRenowned`, `AncientLichRenowned`, `DevourerRenowned` в `Mobiles/TerMur/`).

**Оценка:** 0,5 дня.

### A3. Реально отсутствующие типы — 140 имён

Распределение по зонам (строк в логе):

| Зона / файл спаунов | Строк | Отсутствующих имён | Что это |
|---|---|---|---|
| `GravewaterLake.xml` | 1680 | 2 (`Paralithode`, `DiabolicalSeaweed`) | High Seas: рыбалка |
| `termur.xml` | 1105 | 30 | Ter Mur: NPC Royal City + линейка SA-квестов |
| `TheExodusEncounterQuest.xml` | 812 | 10 | Exodus Encounter целиком |
| `underworld.xml` | 532 | 16 | Underworld: квестовая линейка целиком |
| felucca/trammel | 420 | ~40 | квесты Aminia/Sarakki/Verity, Bard Mastery, книги библиотеки |
| `Eodon.xml` | 112 | 2 | `cubenclosure`, `hawkwind2` |
| `tokuno.xml` / телепорты | 84 | 2 | `CitadelTele`, `CrystalFieldTele` |
| `TreasuresOfKotl.xml` | 56 | 2 | `IgnisFatalis`, `DescicatedMyrmidexLarvae` |

Полный список — см. Приложение 1.

---

## Блок B. Мёртвые линейки квестов

Это прямой ответ на вопрос «проверить линейки квестов». Движки квестов **исправны**:
все 265 квестов и 104 NPC-квестодателя из `Distribution/Data/MLQuests.cfg` имеют классы
в коде. Ломается не движок, а **спаун квестодателей** — NPC просто не появляются в мире.

### B1. Underworld — линейка мертва полностью 🔴 ВЫСОКИЙ

Спаунеры на месте, ни один квестодатель не резолвится:

| Спаунер (`underworld.xml`) | Тип | Роль |
|---|---|---|
| `BarreraakQuestGiver` | `Barreraak` | квестодатель |
| `JacarQuestGiver` | `Jaacar` | квестодатель |
| `VernixQuestGiver` | `Vernix` | квестодатель |
| `XenrrQuestGiver` | `Xenrr` | квестодатель |
| `Elder Dugan` | `Dugan` | квестодатель |
| `Gretchen` | `Gretchen` | квестодатель |
| `Fiddling Tobin` | `Tobin` | квестодатель |
| `QuartermasterFlint` | `QuartermasterFlint` | квестодатель (MX=8) |
| `UnderworldQuest_Flint`, `_Flint1..4` | `flintslogbook`, `barrelofbarley` | квестовые предметы |
| `Garamon` | `Garamon` | сюжетный NPC |
| `Tyball'sShadow` | `TyballsShadow` | **финальный босс** |

Плюс мобы окружения: `IronBeetle`, `SentinelSpider`, `GreaterPoisonElemental`,
`greengoblinscout`.

**Итого:** подземелье Underworld построено, регион `UnderworldRegion` зарегистрирован,
Navrey работает — но **вся квестовая линейка и финальный босс отсутствуют**.

### B2. Ter Mur / Stygian Abyss — линейка мертва 🔴 ВЫСОКИЙ

Именованные спаунеры прямо говорят, какие квесты сломаны:

| Спаунер (`termur.xml`) | Тип | Квест |
|---|---|---|
| `Rumors Abound Quest` | `Egwexem` | «Rumors Abound» |
| `Singularity Quest 1` | `Naxatilor` | «Singularity» |
| `Quest_GatheringEvidence` | `Xeninlor` | «Gathering Evidence» |
| `Quest_GatheringProof` | `Prassel` | «Gathering Proof» |
| `Quest_Zhah` | `QueenZhah` | линейка королевы Жа |
| `Shrine quest` / `Shrine2` | `ShrineMantra`, `bookofcircles` | квест святилищ |
| `Axem the curator` | `Axem` | куратор музея |
| `Tomb/Niporailem` | `Niporailem` | Tomb of Kings |
| `Percolem The Hunter` | `Percolem` | охотник |
| `Slasher1` | `SlasherOfVeils` | **SA-босс Peerless** |
| `MinionOfScelestus#1/#2` | `MinionOfScelestus` | мобы |

Плюс городские NPC Royal City: `Agralem`, `Aliabeth`, `Ansikart`, `Aurvidlem`, `Beninort`,
`Laifem`, `Zosilem`, `sliem`, `thepem`, `ortanord`, `farmernash`, `gargishwanderinghealer`.
И мобы: `lavaelemental`, `lowlandboura`, `putridundeadguardian`, `UndeadGargoyle`,
`skeletaldragonrenowned`, `GargishRouser` (варианты 1/2/3).

### B3. Exodus Encounter — контент есть в данных, кода нет 🟠 СРЕДНИЙ

`Distribution/XmlSpawner/TheExodusEncounterQuest.xml` (25 КБ) полностью нерабочий.
Отсутствуют: `ExodusZealot`, `ExodusArchZealot`, `ExodusSentinel`, `ExodusDrone`,
`ExodusJuggernaut`, `ExodusMinionLord`, `ExodusChest`, `DupresKnight`, `DupresSquire`,
`DupresChampion`.

В коде **частично** есть: `ExodusDungeonRegion`, `ExodusMinion`, `ExodusOverseer`
(`Projects/UOContent/Mobiles/Monsters/LBR/Exodus/`) — старое LBR-подземелье Ilshenar.
Самого `Exodus` (босса) и всей механики энкаунтера нет.

### B4. Квесты Felucca/Trammel 🟠 СРЕДНИЙ

| Спаунер | Тип | Комментарий |
|---|---|---|
| `Aminia Quest`, `Aminia Compassion Quest` | `Aminia` | квест добродетели Compassion |
| `Sarakki Quest` | `Sarakki` | |
| `Verity Quest` | `Verity` | |
| `BardMasteryQuest` | `sirhareus` (+ `sirberran`, `sirfelean`) | **квест получения Bard Mastery** |
| `Sanctuary` | `Lenley` | |
| `TrinsicPaladinSpawner` | `paladin` | |
| `Vendors#53` | `escortablehealer` | эскорт-NPC |
| `Spawner` × много | `Acob`, `Aneen`, `Athialon`, `Brae`, `Fabrizio`, `Frazer`, `Ioseph`, `Lefty`, `Lucius`, `Natalie`, `Neil`, `Nillaen`, `Onallan`, `Oolua`, `Rollarn`, `Ryal`, `Siarra`, `Szandor`, `Vicaie`, `Abbein`, `Alethanian`, `Calendor`, `Gregorio`, `Jothan`, `Mallew`, `Taellia`, `Tyleelor` | именные NPC |
| `Spawner` (книги) | `ArtsSection`, `PastTreasures`, `WizardsCompendium`, `SongsOfNote`, `Trades`, `UnderstandingAnimals`, `LightAndMight`, `MaceAndBlade`, `OilAndOubliette`, `BritanniaWaters`, `DenthesJournal`, `AriellesBauble` | книги библиотек / квестовые предметы |
| прочее | `SkeletonKey`, `FoldedSteel`, `SosariaSap`, `MaulBear`, `Prisonercamp`, `elfbrigandcamp`, `bulbousputrification`, `Grim`, `Petrus`, `Cohenn`, `Gnosos`, `Flurry`, `Mistral`, `Tempest`, `tigersclawMaster` | |

> **Важно для приоритизации:** `BardMasteryQuest` — это точка входа в систему Skill Masteries.
> Шард уже портировал 43 файла мастерств (`Projects/UOContent/Spells/SkillMasteries/`),
> но квест, который выдаёт доступ к ним, не спаунится. Стоит проверить, есть ли
> альтернативный путь получения (`SkillMasteryPrimer.cs`, `BookOfMasteries.cs` есть в коде).

---

## Блок C. Телепорты подземелий

### C1. Отсутствующие классы телепортов 🔴 ВЫСОКИЙ, дёшево чинится

| Класс | Координата спаунера | Следствие |
|---|---|---|
| `CitadelTele` | Tokuno `1344, 769, 21` (спаунер `Citadel Crate`) | **Подземелье Citadel недостижимо** |
| `CrystalFieldTele` | Felucca **и** Trammel `6509, 87, -4` | недоступен переход |

`CitadelTele` — критично: шард **уже портировал** мобов Citadel
(`Projects/UOContent/Mobiles/The Citadel/` — 6 файлов: `DragonsFlameMage`,
`DragonsFlameGrandMage`, `SerpentsFangAssassin`, `SerpentsFangHighExecutioner`,
`TigersClawThief`, `FireDaemonRenowned`) и ключи
(`Items/Misc/The Citadel/DragonFlameKey.cs`, `SerpentFangKey.cs`, `MantleOfTheFallen.cs`,
`ResonantStaffofEnlightenment.cs`) — но войти в подземелье нельзя, и босса (`Travesty`) нет.
Порт сделан наполовину.

**Задача C1.** Реализовать оба класса телепортов. Базовых классов в коде достаточно:
`Teleporter`, `ConditionTeleporter`, `DynamicTeleporter`, `SkillTeleporter`, `TicketTeleporter`,
`KeywordTeleporter`, `GateTeleporter`. **Оценка: 0,5 дня.**

### C2. Конфликты координат в `teleporters.json` 🟠 СРЕДНИЙ

Файл побайтово идентичен апстриму `modernuo/ModernUO@main` — это не устаревшая копия,
а официальная версия (сверялось ранее, см. `code-audit-findings.md`). ServUO эту схему не
использует, там телепорты зашиты в C#, так что «взять у другого форка» тут не выйдет —
чинить надо руками.

Из 1368 записей **32 записи (16 пар) используют одинаковую исходную клетку с разными
пунктами назначения**. `[TelGen` при генерации сносит предыдущий телепорт на клетке, поэтому
срабатывает только **последняя** запись пары — вторая точка назначения недостижима.

**Самая опасная пара — межфасетный баг:**

```
TerMur 7149,756,25  →  Felucca 511,585,11     ← почти наверняка ошибка
TerMur 7149,756,25  →  TerMur  511,585,9      ← правильная
```

`TerMur 7149,756` находится в регионе **`Abyss` (DungeonRegion)**. Первая запись выкидывает
игрока на **Felucca** в чистое поле. Если у вас в игре кто-то жаловался, что «из Бездны
выкинуло не туда» — это оно.

Остальные 15 пар (обе точки на одном фасете, расхождение в 1–2 клетки — похоже на опечатки
координат в исходных данных ModernUO):

```
Felucca/Trammel 1714,2996 → 6308,892 | 6309,891      (The Painted Caves, вход)
Felucca/Trammel 1714,2997 → 6308,892 | 6309,892      (The Painted Caves, вход)
Felucca/Trammel 6310,890  → 1715,2996 | 1716,2997    (The Painted Caves, выход)
Felucca/Trammel 6310,891  → 1715,2996 | 1716,2997    (The Painted Caves, выход)
Felucca/Trammel 6310,892  → 1715,2997 | 1716,2997    (The Painted Caves, выход)
Felucca/Trammel 6310,893  → 1715,2997 | 1716,2997    (The Painted Caves, выход)
Felucca/Trammel 5698,662  → 5793,527  | 5793,527     (полный дубль — безвредно, удалить)
Felucca/Trammel 5825,631  → 2042,215  | 2043,215
Felucca/Trammel 6083,144  → 5918,168  | 5920,168
Felucca/Trammel 6083,145  → 5918,169  | 5920,169
Felucca/Trammel 6083,146  → 5918,170  | 5920,170
TerMur 519,919            → 1125,1075 | 1125,1076
TerMur 520,919            → 1125,1075 | 1126,1076
TerMur 1125..1128,1215    → Trammel 4194,3261 (полные дубли — безвредно)
TerMur 1129..1131,1215    → Trammel 4194,3261 | 4195,3261
```

**Задача C2.1.** Исправить межфасетную запись `TerMur 7149,756 → Felucca` (убрать или
переправить на TerMur). **Приоритет высокий, 15 минут.**
**Задача C2.2.** Разобрать 15 оставшихся пар: удалить полные дубли, для расходящихся —
решить, какая координата верна (соседние «парные» телепорты в файле используют **разные**
соседние клетки, то есть один из дублей, вероятно, должен указывать на клетку рядом).
**Оценка: 0,5 дня.** Стоит отправить патч в апстрим ModernUO.

---

## Блок D. Отсутствующий ванильный контент (порт с других форков)

### D1. Система Peerless — отсутствует целиком 🔴 ВЫСОКИЙ, самый крупный пробел

**Это главная дыра шарда.** В коде **нет** ни `PeerlessAltar`, ни `BasePeerless`, ни
`PeerlessKey`. При этом:

- **все миньоны-ключники каждого подземелья на месте**,
- **вся артефактная награда на месте**,
- **боссов нет**.

| Подземелье | Миньоны в коде | Босс | Артефакты в коде |
|---|---|---|---|
| Blighted Grove | `Abscess`, `Coil`, `Saliva`, `Thrasher`, `Tangle`, `Hydra`, `InsaneDryad`, `EnslavedSatyr` | ❌ **Shimmering Effusion** | ✅ |
| Labyrinth | `Miasma`, `Pyre`, `Rend` | ❌ **Lady Melisande** | ✅ `MelisandesCorrodedHatchet`, `MelisandesFermentedWine`, `MelisandesHairDye` |
| Palace of Paroxysmus | `Putrefier` | ❌ **Chief Paroxysmus** + `ParoxysmusDaemons`/`Succubi`/`ArcaneDaemons` | ✅ `ParoxysmusCorrodedStein`, `LardOfParoxysmus`, `SweatOfParoxysmus` |
| Prism of Light | `CorporealBrume`, `CrystalDaemon`, `CrystalLatticeSeeker`, `CrystalVortex`, `CrystalWisp`, `MantraEffervescence`, `Protector`, `UnfrozenMummy` | ⚠️ есть `InterredGrizzle` как обычный `BaseCreature` без механики Peerless | ✅ `GlobOfMonstreousInterredGrizzle` |
| Twisted Weald | `Irk`, `Guile`, `Spite`, `Malefic`, `Silk`, `Virulent`, `Changeling`, `Gnaw`, `Swoop`, `LadyLissith`, `LadySabrix` | ❌ **Dread Horn** | ✅ `DreadHornMane`, `HornOfTheDreadhorn`, `MangledHeadOfDreadhorn`, `PristineDreadHorn`, `MountedDreadHorn` |
| Citadel (Tokuno) | 3 клана (`DragonsFlame*`, `SerpentsFang*`, `TigersClaw*`) | ❌ **Travesty** | ✅ `TravestysCollectionOfShells`, `TravestysFineTeakwoodTray`, `TravestysSushiPreparations`, `EyeOfTheTravesty` |
| Stygian Abyss | — | ❌ **Slasher of Veils**, `AbyssalInfernal`, `PrimevalLich`, `ScaleneAbyss` | частично |

Также: ML-квест `DreadhornQuest` **существует в коде** и ссылается на босса, которого нельзя
убить.

**Задача D1.**
1. Портировать ядро: `PeerlessAltar`, `BasePeerless`, `PeerlessKey`, механика ключей/входа/
   таймаута/выброса (ServUO: `Scripts/Services/Peerless*` — точный путь зависит от ревизии,
   проверить при порте).
2. Портировать 6 боссов ML + 4 боссов SA.
3. Разместить алтари, привязать к существующим `DungeonRegion`.
4. Проверить `InterredGrizzle` (`Mobiles/Monsters/ML/Humanoid/Magic/InterredGrizzle .cs` —
   **обратите внимание на пробел в имени файла**, тоже поправить) — переделать под `BasePeerless`
   или заменить.
5. Для Citadel — вместе с задачей C1 (`CitadelTele`), иначе босс останется недостижим.

**Оценка: 5–8 дней.** Крупнейший пункт ТЗ. Даёт максимальный прирост играбельного контента:
6 подземелий переходят из «зачистка миньонов без цели» в «полноценный энкаунтер с лутом,
который уже лежит в коде».

### D2. Mysticism — 8 из 16 заклинаний отсутствуют 🔴 ВЫСОКИЙ

Есть (`Projects/UOContent/Spells/Mysticism/`): `AnimatedWeapon`, `Bombard`, `CleansingWinds`,
`EagleStrike`, `HailStorm`, `NetherCyclone`, `SpellPlague`, `StoneForm`.

**Отсутствуют:** `NetherBolt` (1-й круг!), `HealingStone`, `PurgeMagic`, `Enchant`, `Sleep`,
`SpellTrigger`, `MassSleep`, `RisingColossus` (8-й круг, ключевой саммон школы).

Школа непроходима: нет заклинания 1-го круга и нет вершины школы.
**Оценка: 2–3 дня.** Источник: ServUO `Scripts/Spells/Mysticism/`.

### D3. Spellweaving — 2 заклинания 🟡 НИЗКИЙ

Отсутствуют `WildfireSpell` и `DryadAllureSpell`. При этом `DryadAllureScroll` **в коде есть** —
свиток существует, заклинания нет.
**Оценка: 0,5 дня.**

### D4. Eodon / Time of Legends — регионы есть, контента нет 🟠 СРЕДНИЙ

`Distribution/Data/regions.json` содержит регионы Eodon: `Great Ape Lair`, `Kotl City`,
`Myrmidex Queen Lair`, `Zipactriotl Lair`, `Volcano`, `Spider Island`, `Raptor Island`,
`Toxic Desert`, `Kepetch Waste`, `Slith Valley`, `Northern Steppes`, `High Plain`,
`Lost Settlement`, `Talon Point`, `Walled Circus`, `Chicken Chase` и др.

В коде: `MyrmidexDrone`, `MyrmidexLarvae`, `MyrmidexWarrior`, `CubeEnclosure`, `Hawkwind`,
папки `Mobiles/Animals/Eodon/`, `Monsters/*/Eodon/` — но **по 1 файлу в каждой**.

**Отсутствуют:** `Zipactriotl`, `MyrmidexQueen`, `KotlAutomaton`, `DragonTurtle`, вся система
Myrmidex Battleground, племена Эодона, Treasures of Kotl (`IgnisFatalis`,
`DescicatedMyrmidexLarvae`).
**Оценка: 4–6 дней.** Источник: ServUO `Scripts/Services/Expansions/Time Of Legends/`.

### D5. High Seas 🟡 НИЗКИЙ (зависит от того, нужен ли шарду морской контент)

Отсутствуют: `BaseGalleon`, `RowBoat`, `FishingNet`, `DockMaster`, `BoatPainter`,
`Paralithode`, `DiabolicalSeaweed` (последние два — **1680 строк лога**, самая массовая
позиция в `badspawn.log`, файл `GravewaterLake.xml`).

Есть только классический `BaseBoat` + `TillerMan` + `Hold`.
**Оценка: 5–7 дней** на полный порт, либо **0,5 дня** на минимум — добавить два типа рыбалки
(`Paralithode`, `DiabolicalSeaweed`), чтобы убрать 1680 строк ошибок и оживить Gravewater Lake.

**Рекомендация:** сделать минимум сейчас, полный High Seas — отдельным решением.

### D6. Прочие ванильные системы 🟡 НИЗКИЙ

| Система | Статус | Комментарий |
|---|---|---|
| City Loyalty / Governor | ❌ нет | у шарда своя система городов `Systems/MahaonCities` — возможно, не нужна |
| Clean Up Britannia | ❌ нет | `CleanUpBritanniaData`, `TurnInGump` |
| `SoulForge` | ❌ нет | при этом `ImbuingSystem`/`ImbuingGump` **есть** (кастомные) — проверить, не дублируется ли |
| `ArtifactChest` | ❌ нет | |
| `ArenaMoongate` | ❌ нет | `ArenasMoongate` есть — возможно опечатка, проверить |

---

## Блок E. Новые находки в коде

### E1. 🔴 ВЫСОКИЙ — `DespiseController`: утечка таймера и 4 регионов при `[DeleteDespise`

**Файлы:** `Projects/UOContent/Engines/Despise/DespiseController.cs:158-180`,
`Projects/UOContent/Engines/Despise/DespiseSetup.cs:20-24`

`BeginTimer()` (строка 158) делает две вещи: запускает **повторяющийся таймер на 1 минуту**
(`Timer.StartTimer(..., OnTick, out _timer)`) и регистрирует **4 региона** (`Despise Lower`,
`Despise Evil`, `Despise Good`, `Despise Start`). Парный `EndTimer()` (строка 170) всё это
корректно сворачивает.

Но в классе **нет ни одного переопределения `OnDelete()` / `OnAfterDelete()`** — проверено
по всему файлу. Команда `[DeleteDespise` (`DespiseSetup.cs:22`) вызывает
`WeakEntityCollection.Delete("despise")`, что удаляет контроллер-предмет, и вручную обнуляет
`DespiseController.Instance` — но `EndTimer()` не вызывается **никогда**.

**Последствия:**
- таймер продолжает тикать раз в минуту по удалённому контроллеру до перезапуска сервера;
- 4 региона остаются зарегистрированными;
- повторный `[SetupDespise` регистрирует **ещё 4 региона** на тех же координатах — регионы
  накапливаются друг на друге.

Нарушает правила CLAUDE.md **#5** (очистка ссылок в `OnDelete`) и **#6** (отмена таймеров).

**Исправление:** добавить `OnDelete()`/`OnAfterDelete()` с вызовом `EndTimer()`, отменой
`_sequenceTimer`/`_cleanupTimer` и обнулением `Instance`, если `Instance == this`.

### E2. 🟡 НИЗКИЙ — LINQ Tier 3 и полные обходы мира в портах

Нарушают правила #1 и #4, но оба — холодные пути (админ-команды / одноразовая миграция),
так что критичности нет. Зафиксировано для полноты:

- `Engines/Despise/DespiseController.cs:690` — `World.Items.Values.OfType<XmlSpawner>().Where(...)`
  в `CheckSpawnersVersion3()`. Плюс внутри цикла `Region.Find(...)` вызывается **дважды подряд**
  с одинаковыми аргументами (строка 705).
- `Engines/VvV/ViceVsVirtueSystem.cs:857` — `World.Mobiles.Values.Where(m => m is SilverTrader).ToList()`
  в `DeleteSilverTraders()`.
- `Engines/VvV/Gumps/BattleWarningGump.cs:51` — `World.Items.Values.OfType<PublicMoongate>().Where(...)`
  — **этот на тёплом пути** (показ гампа игроку при старте битвы), стоит переписать на
  `map.GetItemsInRange<PublicMoongate>()`.
- `Engines/Despise/DespiseController.cs:157` — `WispOrb.Orbs.FirstOrDefault(...)` в `GetWispOrb()`,
  вызывается из проверок региона.

### E3. 🟡 НИЗКИЙ — `System.Text.StringBuilder` в XmlSpawner (правило #17)

`Engines/XmlSpawner/XmlSpawner.cs:11587,11604`, `BaseXmlSpawner.cs:3255`,
`XmlUtils/XmlAdd.cs:188,204`. XmlSpawner — сторонний порт, править по желанию; на
производительность в проде не влияет (вызовы редкие).

### E4. 🟡 НИЗКИЙ — `Console.WriteLine` в ванильных файлах (правило #2)

~19 мест, все в **апстримном** коде ModernUO (`Commands/Object Creation/*`, `Engines/ML Quests/*`,
`Engines/Pathing/*`, `Engines/Chat/ChatPackets.cs`). Не наш код — трогать только если решите
причёсывать под свои стандарты.

### E5. Косметика

- `Projects/UOContent/Mobiles/Monsters/ML/Humanoid/Magic/InterredGrizzle .cs` — **пробел
  перед `.cs`** в имени файла.

---

## Блок F. Незакрытые пункты предыдущего аудита

Из `dev-docs/code-audit-findings.md` остаются со статусом `[ ]` (17 пунктов). Кратко,
по убыванию важности:

| # | Уровень | Суть |
|---|---|---|
| 39 | СИСТЕМНО | ~половина ростера Skill Masteries механически мертва — работает только иконка баффа |
| 11 | СИСТЕМНО | почти вся `Systems/MahaonCombat/` без сохранения прогресса между рестартами |
| 10 | ВЫСОКИЙ | 4 системы описаны в комментариях как подключённые, но нигде не вызываются |
| 17 | СРЕДНИЙ | ростер занятий ботов в `DoIdle` перенаправляет «охоту» в «формирование пати» |
| 16 | СРЕДНИЙ | множитель щедрости личности бота посчитан, но не читается |
| 28 | СРЕДНИЙ | `MineVoidAllocator.cs` — мёртвый код, противоречит устройству шахт |
| 35 | СРЕДНИЙ | автобинтовка реализована, но никогда не вызывается |
| 3 | СРЕДНИЙ | коллизия ID кнопок в `MahaonSaplingAddGump` при ≥9 страницах |
| 4, 5, 18, 19, 20, 31 | НИЗКИЙ/ИНФО | GM-золото при неудачной перестройке дома; кулдауны и состояния без персистенса; мёртвый код ботов |

Пункт **39** пересекается с блоком B4 этого ТЗ: если мастерства получить нельзя (квест не
спаунится), их неработоспособность до сих пор не всплывала в игре.

---

## Приоритеты и план работ

### Этап 1 — «дешёвое и громкое» (2–3 дня)

| Задача | Блок | Эффект |
|---|---|---|
| Перегенерировать `badspawn.log` | A1 | получить актуальную картину, минус ~14 тыс. строк шума |
| `CitadelTele` + `CrystalFieldTele` | C1 | открывает Citadel (мобы и ключи уже портированы) |
| Межфасетный телепорт Abyss | C2.1 | 15 минут, чинит реальный баг с выбросом на другой фасет |
| `NavreyNightEyes`, `DryadA`, `treasurelevel1h` | A2 | чинит нативные спауны |
| `OnDelete` в `DespiseController` | E1 | утечка таймера и регионов |
| `Paralithode` + `DiabolicalSeaweed` | D5-min | минус 1680 строк лога, оживляет Gravewater Lake |
| Spellweaving: `Wildfire`, `DryadAllure` | D3 | закрывает школу |

### Этап 2 — линейки квестов (6–9 дней)

1. **Underworld** (B1) — 16 сущностей, самая цельная линейка, подземелье уже построено.
2. **Bard Mastery quest** (B4) — `sirhareus`/`sirberran`/`sirfelean`, открывает уже
   портированную систему мастерств. Делать **вместе** с пунктом 39 старого аудита.
3. **Ter Mur / SA** (B2) — 30 сущностей, 6 квестов + городские NPC.
4. Остальные NPC Fel/Tram и книги (B4).

### Этап 3 — Peerless (5–8 дней) 🎯 максимальный прирост контента

Ядро + 6 боссов ML + алтари. Лут и миньоны уже в коде.

### Этап 4 — крупные системы (по решению владельца)

- Mysticism 8 заклинаний (D2) — 2–3 дня, **рекомендую поднять в этап 2**, школа сейчас нерабочая
- Exodus Encounter (B3) — 3–4 дня
- Eodon / TOL (D4) — 4–6 дней
- High Seas полностью (D5) — 5–7 дней
- SA Peerless (Slasher of Veils, Abyssal Infernal, Primeval Lich) — 3–4 дня

> Оценки грубые, «на глаз по объёму сущностей». Реальный срок сильно зависит от того,
> насколько чисто исходники ложатся на ModernUO-конвенции (`[SerializationGenerator]`,
> `TimerExecutionToken`, отсутствие `EventSink.OnEnterRegion` и т.п. — судя по комментариям
> в уже сделанных портах Despise/VvV/Shadowguard, адаптация заметная).

---

## Источники для портирования

| Контент | Форк | Ориентир по пути (проверить при порте) |
|---|---|---|
| Peerless (ядро + боссы) | ServUO | `Scripts/Services/Peerless*`, `Scripts/Mobiles/Normal/` |
| Mysticism | ServUO | `Scripts/Spells/Mysticism/` |
| Spellweaving (Wildfire, DryadAllure) | ServUO | `Scripts/Spells/Spellweaving/` |
| Underworld квесты | ServUO | квестовые NPC Underworld / Tomb of Kings |
| Ter Mur / SA квесты | ServUO | `Scripts/Services/Expansions/Stygian Abyss/` |
| Exodus Encounter | ServUO | `Scripts/Services/ExodusEncounter/` |
| Eodon / TOL | ServUO | `Scripts/Services/Expansions/Time Of Legends/` |
| High Seas | ServUO | `Scripts/Services/High Seas/` |
| Телепорты `teleporters.json` | — | **только ручная правка**, ServUO эту схему не использует |

**Замечание по лицензии:** ModernUO и ServUO — GPL-производные RunUO. Порт кода допустим,
но требует сохранения атрибуции. В уже сделанных портах шарда это соблюдается — в шапках
файлов есть комментарии вида «Ported from ServUO's ... (Scripts/Services/Dungeons/
DespiseRevamped/Setup.cs)». Держать тот же стиль.

**Методика порта** — в репозитории уже есть готовые скиллы:
`dev-docs/claude-skills/migrate-from-runuo/` (migrate-foundation, migrate-serialization,
migrate-items-mobiles, migrate-timers, migrate-gumps, migrate-packets и др.) +
`dev-docs/runuo-migration-docs/`. Активируются копированием в `.claude/skills/`.

---

## Приложение 1. Полный список неразрешимых типов (140)

<details>
<summary>Развернуть</summary>

**Ter Mur (30):** `Agralem` `Aliabeth` `Ansikart` `Aurvidlem` `Axem` `Beninort` `Egwexem`
`GargishRouser,1..3` `Laifem` `Naxatilor` `Niporailem` `Percolem` `Prassel` `QueenZhah`
`ShrineMantra` `SlasherOfVeils` `MinionOfScelestus` `UndeadGargoyle` `Xeninlor` `Zosilem`
`bookofcircles` `farmernash` `gargishwanderinghealer` `lavaelemental` `lowlandboura`
`ortanord` `putridundeadguardian` `skeletaldragonrenowned` `sliem` `thepem`

**Underworld (16):** `Barreraak` `Dugan` `Garamon` `GreaterPoisonElemental` `Gretchen`
`IronBeetle` `Jaacar` `QuartermasterFlint` `SentinelSpider` `Tobin` `TyballsShadow` `Vernix`
`Xenrr` `barrelofbarley` `flintslogbook` `greengoblinscout`

**Exodus Encounter (10):** `DupresChampion` `DupresKnight` `DupresSquire` `ExodusArchZealot`
`ExodusChest` `ExodusDrone` `ExodusJuggernaut` `ExodusMinionLord` `ExodusSentinel` `ExodusZealot`

**Felucca / Trammel — NPC (27):** `Abbein` `Acob` `Alethanian` `Aminia` `Aneen` `Athialon`
`Brae` `Calendor` `Fabrizio` `Frazer` `Gregorio` `Ioseph` `Jothan` `Lefty` `Lenley` `Lucius`
`Mallew` `Natalie` `Neil` `Nillaen` `Onallan` `Oolua` `Rollarn` `Ryal` `Sarakki` `Siarra`
`Szandor` `Taellia` `Tyleelor` `Verity` `Vicaie` `escortablehealer` `paladin` `sirberran`
`sirfelean` `sirhareus`

**Felucca / Trammel — предметы и книги (14):** `AriellesBauble` `ArtsSection` `BritanniaWaters`
`DenthesJournal` `FoldedSteel` `LightAndMight` `MaceAndBlade` `OilAndOubliette` `PastTreasures`
`ShrineMantra` `SkeletonKey` `SongsOfNote` `SosariaSap` `Trades` `UnderstandingAnimals`
`WizardsCompendium`

**Прочие мобы / лагеря:** `MaulBear` `Prisonercamp` `elfbrigandcamp` `bulbousputrification`
`Grim` `Petrus` `Cohenn` `Gnosos` `Flurry` `Mistral` `Tempest` `tigersclawMaster` `FireRabbit`
`SentinelSpider`

**High Seas:** `Paralithode` `DiabolicalSeaweed`

**Eodon / Kotl:** `cubenclosure` `hawkwind2` `IgnisFatalis` `DescicatedMyrmidexLarvae`
`BlightedCotton` `ThornyBriar`

**Телепорты:** `CitadelTele` `CrystalFieldTele`

**Артефакты парсинга (не баги):** `08` `09` `Z F Name` `SET,0x400029C7`

</details>

---

## Открытые вопросы к владельцу шарда

1. **Целевая эра/экспансия.** CLAUDE.md правило #11 запрещает её угадывать. Шард уже
   содержит контент ML + SA + TOL (Despise Revamped, Shadowguard, Underworld, Ter Mur,
   Eodon, Skill Masteries, VvV). Подтвердите: цель — **полный TOL**? От этого зависит,
   портировать ли Peerless/High Seas/Eodon вообще или отсечь часть как ненужное.
2. **Кастом vs ваниль.** Есть пересечения: кастомный `ImbuingSystem` против ванильного
   `SoulForge`, `Systems/MahaonCities` против City Loyalty/Governor. Ванильные аналоги
   портировать не нужно?
3. **Именные NPC Fel/Tram (~27 штук)** — это ванильные квестовые NPC или остатки чужого
   шарда в XML? Если второе — проще вычистить спаунеры, чем портировать сущности.
4. **High Seas** — нужен морской контент целиком или хватит минимума (2 типа рыбалки,
   чтобы убрать 1680 строк ошибок)?
5. **С чего начинать.** Мой приоритет: Этап 1 (2–3 дня, дешёвое и заметное) → Mysticism →
   Peerless. Согласны или есть своё «болит прямо сейчас»?
