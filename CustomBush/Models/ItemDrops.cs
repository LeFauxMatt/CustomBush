using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushDrops" />
internal sealed class ItemDrops : List<ICustomBushDrop>, ICustomBushDrops
{
    /// <inheritdoc />
    public ItemDrops()
    {
    }

    /// <inheritdoc />
    public ItemDrops(List<ItemDrop> drops)
        : base(drops.ConvertAll(static ICustomBushDrop (drop) => drop))
    {
    }
}