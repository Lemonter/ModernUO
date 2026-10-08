using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server.Engines.Spawners;

/// <summary>
///     Lazily built, cached list of every Mobile-derived type that has a public
///     [Constructible] constructor — the same requirement Spawner itself already needs
///     (AssemblyHandler.FindTypeByName + Activator.CreateInstance), just enumerated once
///     up front instead of the player having to already know the exact name to type in.
///     Built once on first use and kept for the life of the server; new types only show up
///     after a restart, same as everything else resolved through AssemblyHandler.
/// </summary>
public static class MahaonCreaturePicker
{
    private static List<string> _allNames;

    public static IReadOnlyList<string> AllNames => _allNames ??= BuildList();

    private static List<string> BuildList()
    {
        var names = new List<string>();
        var mobileType = typeof(Mobile);

        foreach (var asm in AssemblyHandler.Assemblies)
        {
            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (var t in types)
            {
                if (!mobileType.IsAssignableFrom(t) || t.IsAbstract)
                {
                    continue;
                }

                var hasConstructible = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Any(c => c.GetParameters().Length == 0 && c.IsDefined(typeof(ConstructibleAttribute), false));

                if (hasConstructible)
                {
                    names.Add(t.Name);
                }
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    /// <summary>Case-insensitive substring match against the cached name list — good
    /// enough for a quick filter box, no need for anything fancier.</summary>
    public static List<string> Search(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return new List<string>(AllNames);
        }

        return AllNames
            .Where(n => n.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
