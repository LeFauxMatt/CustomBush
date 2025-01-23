using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.CustomBush.Utilities;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Services;

/// <inheritdoc />
public sealed class ModApi : ICustomBushApi
{
    /// <inheritdoc />
    public bool IsCustomBush(Bush bush) => this.TryGetBush(bush, out _);

    /// <inheritdoc />
    public bool IsInSeason(Bush bush) =>
        this.TryGetBush(bush, out var customBush) && customBush.ConditionsToProduce.Any(bush.TestCondition);

    /// <inheritdoc />
    public bool TryGetBush(Bush bush, [NotNullWhen(true)] out ICustomBushData? customBush)
    {
        customBush = null;
        if (!bush.modData.TryGetValue(ModConstants.ModDataId, out var id))
        {
            return false;
        }

        if (!ModState.Data.TryGetValue(id, out var bushData))
        {
            return false;
        }

        customBush = bushData;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetShakeOffItem(Bush bush, [NotNullWhen(true)] out Item? item)
    {
        // Create cached item
        if (bush.TryGetCachedData(true, out var itemId, out var itemQuality, out var itemStack, out _))
        {
            item = ItemRegistry.Create(itemId, itemStack, itemQuality);
            return true;
        }

        item = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetModData(
        Bush bush,
        [NotNullWhen(true)] out string? itemId,
        out int itemQuality,
        out int itemStack,
        out string? condition) =>
        bush.TryGetCachedData(bush.readyForHarvest(), out itemId, out itemQuality, out itemStack, out condition);

    public bool TryGetTexture(Bush bush, [NotNullWhen(true)] out Texture2D? texture)
    {
        if (!this.TryGetBush(bush, out var customBush))
        {
            texture = null;
            return false;
        }

        texture = bush.IsSheltered() && !string.IsNullOrWhiteSpace(customBush.IndoorTexture)
            ? ModState.GetTexture(customBush.IndoorTexture)
            : ModState.GetTexture(customBush.Texture);

        return true;
    }
}