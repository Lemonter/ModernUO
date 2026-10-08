using System;
using System.Collections.Generic;
using System.Linq;

namespace Server;

/// <summary>
///     Ported from ServUO's Scripts/Misc/WeakEntityCollection.cs — not Despise-specific
///     (ServUO uses it for many event systems), but this is its only consumer in this
///     codebase so far, hence living alongside Despise rather than in its own top-level
///     folder. Lets DespiseRevampedSetup tag every entity it spawns under one "despise" key
///     and later bulk-delete them all via `[DeleteDespise` without hunting them down
///     individually.
///
///     ServUO's original persisted via a standalone `Persistence.Serialize(path, writer =>
///     {...})` static helper that has no equivalent here — ModernUO's own `Persistence` is
///     an abstract base meant to be subclassed, so this uses the established local
///     convention instead: a nested `GenericPersistence` class, same shape every other
///     Mahaon system in this codebase already uses (see e.g. AnimalTrainingSystem's own
///     Persistence).
/// </summary>
public static class WeakEntityCollection
{
    public sealed class EntityCollection : List<IEntity>
    {
        public IEnumerable<Item> Items => this.OfType<Item>();
        public IEnumerable<Mobile> Mobiles => this.OfType<Mobile>();

        public EntityCollection() : this(0x400)
        {
        }

        public EntityCollection(int capacity) : base(capacity)
        {
        }
    }

    private static readonly Dictionary<string, EntityCollection> _collections =
        new(StringComparer.OrdinalIgnoreCase);

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static EntityCollection GetCollection(string name)
    {
        if (!_collections.TryGetValue(name, out var col) || col == null)
        {
            _collections[name] = col = new EntityCollection();
        }

        return col;
    }

    public static bool HasCollection(string name) => name != null && _collections.ContainsKey(name);

    public static void Add(string key, IEntity entity)
    {
        if (entity == null || entity.Deleted)
        {
            return;
        }

        var col = GetCollection(key);

        if (!col.Contains(entity))
        {
            col.Add(entity);
        }
    }

    public static bool Remove(string key, IEntity entity) => entity != null && GetCollection(key).Remove(entity);

    public static int Clean(string key)
    {
        var col = GetCollection(key);
        var removed = 0;

        for (var i = col.Count - 1; i >= 0; i--)
        {
            if (i < col.Count && col[i].Deleted)
            {
                col.RemoveAt(i);
                ++removed;
            }
        }

        return removed;
    }

    public static int Delete(string key)
    {
        var col = GetCollection(key);
        var deleted = 0;

        for (var i = col.Count - 1; i >= 0; i--)
        {
            if (i < col.Count)
            {
                col[i].Delete();
                ++deleted;
            }
        }

        col.Clear();
        _collections.Remove(key);

        return deleted;
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("WeakEntityCollection", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(_collections.Count);

            foreach (var (key, col) in _collections)
            {
                writer.Write(key);

                col.RemoveAll(ent => ent == null || ent.Deleted);

                writer.WriteEncodedInt(col.Count);

                foreach (var ent in col)
                {
                    writer.Write(ent.Serial);
                }
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            var entries = reader.ReadEncodedInt();

            for (var i = 0; i < entries; i++)
            {
                var key = reader.ReadString();
                var count = reader.ReadEncodedInt();

                var col = new EntityCollection(count);

                for (var j = 0; j < count; j++)
                {
                    var ent = World.FindEntity(reader.ReadSerial());

                    if (ent is { Deleted: false })
                    {
                        col.Add(ent);
                    }
                }

                _collections[key] = col;
            }
        }
    }
}
