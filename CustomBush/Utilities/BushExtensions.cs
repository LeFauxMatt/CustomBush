namespace LeFauxMods.CustomBush.Utilities;

using Common.Integrations.CustomBush;
using Common.Utilities;
using Models;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;

internal static class BushExtensions
{
    private static Func<Dictionary<string, CustomBush>>? getData;

    private static Dictionary<string, CustomBush> Data => getData!();

    public static void ClearCachedData(this Bush bush)
    {
        _ = bush.modData.Remove(Constants.ModDataItem);
        _ = bush.modData.Remove(Constants.ModDataItemSeason);
        _ = bush.modData.Remove(Constants.ModDataQuality);
        _ = bush.modData.Remove(Constants.ModDataStack);
    }

    public static void Init(Func<Dictionary<string, CustomBush>> gD) => getData ??= gD;

    public static bool TryGetCachedData(this Bush bush, out string? itemId, out int itemQuality, out int itemStack)
    {
        itemQuality = 1;
        itemStack = 1;

        if (!bush.modData.TryGetValue(Constants.ModDataItem, out itemId) || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
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

    public static bool TryProduceAny(this Bush bush, [NotNullWhen(true)] out Item? item, CustomBush? customBush = null)
    {
        if (customBush is null)
        {
            if (!bush.modData.TryGetValue(Constants.ModDataId, out var id) || !Data.TryGetValue(id, out customBush))
            {
                item = null;
                return false;
            }
        }

        foreach (var drop in customBush.ItemsProduced)
        {
            item = bush.TryProduceOne(drop);
            if (item is not null)
            {
                return true;
            }
        }

        item = null;
        return false;
    }

    public static Item? TryProduceOne(this Bush bush, ICustomBushDrop drop)
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
