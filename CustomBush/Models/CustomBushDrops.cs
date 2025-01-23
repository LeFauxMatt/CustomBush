using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

internal sealed class CustomBushDrops(IEnumerable<ICustomBushDrop> collection)
    : List<ICustomBushDrop>(collection), ICustomBushDrops;