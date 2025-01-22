using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Utilities;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Services;

/// <inheritdoc />
public sealed class ModApi(IModHelper helper) : ICustomBushApi
{
    /// <inheritdoc />
    public IEnumerable<ICustomBushData> GetAllBushes() =>
        helper.GameContent.Load<Dictionary<string, CustomBushData>>(Constants.DataPath).Values;

    /// <inheritdoc />
    public IEnumerable<(string Id, ICustomBushDataOld Data)> GetData() =>
        this.GetAllBushes().Select(static data => (data.Id, (ICustomBushDataOld)data));

    /// <inheritdoc />
    public bool IsCustomBush(Bush bush) => this.TryGetCustomBush(bush, out _, out _);

    /// <inheritdoc />
    public bool IsInSeason(Bush bush)
    {
        if (!this.TryGetCustomBush(bush, out var data) || data is not CustomBushData customBush)
        {
            return false;
        }

        return customBush.ConditionsToProduce.Any(condition =>
            GameStateQuery.CheckConditions(condition,
                bush.Location, null, null, null, null,
                bush.Location.SeedsIgnoreSeasonsHere() || bush.IsSheltered()
                    ? GameStateQuery.SeasonQueryKeys
                    : null));
    }

    /// <inheritdoc />
    public bool TryGetBush(Bush bush, [NotNullWhen(true)] out ICustomBushData? customBush,
        [NotNullWhen(true)] out string? id)
    {
        customBush = null;
        if (!bush.modData.TryGetValue(Constants.ModDataId, out id))
        {
            return false;
        }

        var data = helper.GameContent.Load<Dictionary<string, CustomBushData>>(Constants.DataPath);
        if (!data.TryGetValue(id, out var bushData))
        {
            return false;
        }

        customBush = bushData;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetCustomBush(Bush bush, [NotNullWhen(true)] out ICustomBushDataOld? customBush) =>
        this.TryGetCustomBush(bush, out customBush, out _);

    /// <inheritdoc />
    public bool TryGetCustomBush(
        Bush bush,
        [NotNullWhen(true)] out ICustomBushDataOld? customBush,
        [NotNullWhen(true)] out string? id)
    {
        if (this.TryGetBush(bush, out var customBushNew, out id))
        {
            customBush = customBushNew;
            return true;
        }

        customBush = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetDrops(string id, [NotNullWhen(true)] out IList<ICustomBushDrop>? drops)
    {
        drops = null;
        var data = helper.GameContent.Load<Dictionary<string, CustomBushData>>(Constants.DataPath);
        if (!data.TryGetValue(id, out var bush))
        {
            return false;
        }

        drops = bush.ItemsProduced.ConvertAll(static ICustomBushDrop (drop) => drop);
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
        if (!this.TryGetBush(bush, out var customBush, out _))
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