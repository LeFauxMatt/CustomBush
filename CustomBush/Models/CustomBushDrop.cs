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

    /// <inheritdoc />
    public int SpriteOffset { get; set; }
}