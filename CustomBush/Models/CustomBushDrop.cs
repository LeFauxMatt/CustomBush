namespace LeFauxMods.CustomBush.Models;

using Common.Integrations.CustomBush;
using StardewValley.GameData;

/// <inheritdoc cref="ICustomBushDrop" />
internal sealed class CustomBushDrop : GenericSpawnItemDataWithCondition, ICustomBushDrop
{
    /// <inheritdoc />
    public float Chance { get; set; } = 1f;

    /// <inheritdoc />
    public Season? Season { get; set; }
}
