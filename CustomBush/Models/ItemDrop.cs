using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushDrop" />
internal sealed class ItemDrop : GenericSpawnItemDataWithCondition, ICustomBushDrop
{
    /// <inheritdoc />
    public bool ReplaceItem { get; set; }

    /// <inheritdoc />
    public int SpriteOffset { get; set; } = 1;
}