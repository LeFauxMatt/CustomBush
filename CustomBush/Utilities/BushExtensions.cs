using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Utilities;

internal static class BushExtensions
{
    private static Func<Dictionary<string, CustomBushData>>? getData;

    private static Dictionary<string, CustomBushData> Data => getData!();

    public static void ClearCachedData(this Bush bush)
    {
        _ = bush.modData.Remove(Constants.ModDataCondition);
        _ = bush.modData.Remove(Constants.ModDataItem);
        _ = bush.modData.Remove(Constants.ModDataItemSeason);
        _ = bush.modData.Remove(Constants.ModDataQuality);
        _ = bush.modData.Remove(Constants.ModDataStack);
        _ = bush.modData.Remove(Constants.ModDataSpriteOffset);
    }

    public static void Init(Func<Dictionary<string, CustomBushData>> getter) => getData ??= getter;

    public static bool TryGetCachedData(this Bush bush, [NotNullWhen(true)] out string? itemId, out int itemQuality,
        out int itemStack, out string? condition)
    {
        itemQuality = 1;
        itemStack = 1;

        if (!bush.modData.TryGetValue(Constants.ModDataItem, out itemId) || string.IsNullOrWhiteSpace(itemId))
        {
            condition = null;
            return false;
        }

        if (!bush.modData.TryGetValue(Constants.ModDataCondition, out condition) &&
            bush.modData.TryGetValue(Constants.ModDataItemSeason, out var itemSeason) &&
            Enum.TryParse(itemSeason, out Season season))
        {
            condition = $"SEASON {season.ToString()}";
        }

        if (bush.modData.TryGetValue(Constants.ModDataQuality, out var qualityString) &&
            int.TryParse(qualityString, out var qualityInt))
        {
            itemQuality = qualityInt;
        }

        if (bush.modData.TryGetValue(Constants.ModDataStack, out var stackString) &&
            int.TryParse(stackString, out var stackInt))
        {
            itemStack = stackInt;
        }

        return true;
    }

    public static bool TryProduceAny(this Bush bush, [NotNullWhen(true)] out Item? item,
        [NotNullWhen(true)] out CustomBushDrop? drop, CustomBushData? customBush = null)
    {
        if (customBush is null)
        {
            if (!bush.modData.TryGetValue(Constants.ModDataId, out var id) || !Data.TryGetValue(id, out customBush))
            {
                item = null;
                drop = null;
                return false;
            }
        }

        foreach (var itemProduced in customBush.ItemsProduced)
        {
            item = bush.TryProduceOne(itemProduced);
            drop = itemProduced;
            if (item is not null)
            {
                return true;
            }
        }

        item = null;
        drop = null;
        return false;
    }

    private static Item? TryProduceOne(this Bush bush, ICustomBushDrop drop)
    {
        const string logFormat = "{0} did not select {1}. Failed: {2}";

        if (!bush.modData.TryGetValue(Constants.ModDataId, out var id))
        {
            return null;
        }

        // Test overall chance
        if (!Game1.random.NextBool(drop.Chance))
        {
            return null;
        }

        // Test drop condition
        if (drop.Condition != null &&
            !GameStateQuery.CheckConditions(
                drop.Condition,
                bush.Location,
                null,
                null,
                null,
                null,
                bush.Location.SeedsIgnoreSeasonsHere() ? GameStateQuery.SeasonQueryKeys : null))
        {
            Log.Trace(logFormat, id, drop.Id, drop.Condition);
            return null;
        }

        // Test season condition
        if (drop.Season.HasValue &&
            bush.Location.SeedsIgnoreSeasonsHere() &&
            drop.Season != Game1.GetSeasonForLocation(bush.Location))
        {
            Log.Trace(logFormat, id, drop.Id, drop.Season.ToString());
            return null;
        }

        // Try to produce the item
        return ItemQueryResolver.TryResolveRandomItem(
            drop,
            new ItemQueryContext(bush.Location, null, null, $"custom bush '{id}' > fruit '{drop.Id}'"),
            false,
            null,
            null,
            null,
            (query, error) => Log.Error(
                "{0} failed parsing item query {1} for item {2}: {3}",
                id,
                query,
                drop.Id,
                error));
    }
}
