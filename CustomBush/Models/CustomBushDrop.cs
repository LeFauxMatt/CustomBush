using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushDrop" />
internal sealed class CustomBushDrop : GenericSpawnItemDataWithCondition, ICustomBushDrop
{
    /// <inheritdoc />
    public float Chance { get; set; } = 1f;

    /// <inheritdoc />
    public Season? Season { get; set; }

    /// <summary>Gets or sets an offset to the texture sprite which the item is produced.</summary>
    public int SpriteOffset { get; set; }
}
