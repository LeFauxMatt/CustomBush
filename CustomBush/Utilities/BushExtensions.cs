using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Services;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Utilities;

internal static class BushExtensions
{
    public static void ClearCachedData(this Bush bush)
    {
        _ = bush.modData.Remove(Constants.ModDataCondition);
        _ = bush.modData.Remove(Constants.ModDataItem);
        _ = bush.modData.Remove(Constants.ModDataItemSeason);
        _ = bush.modData.Remove(Constants.ModDataQuality);
        _ = bush.modData.Remove(Constants.ModDataStack);
        _ = bush.modData.Remove(Constants.ModDataSpriteOffset);
        bush.tileSheetOffset.Value = 0;
        bush.setUpSourceRect();
    }

    public static bool TryProduceItem(
        this Bush bush,
        [NotNullWhen(true)] out Item? item,
        [NotNullWhen(true)] out ICustomBushDrop? drop)
    {
        const string logFormat = "{0} did not select {1}. Failed: {2}";
        item = null;
        drop = null;

        if (!bush.modData.TryGetValue(Constants.ModDataId, out var id) ||
            !ModState.Data.TryGetValue(id, out var customBush))
        {
            return false;
        }

        foreach (var itemProduced in customBush.ItemsProduced)
        {
            // Test overall chance
            if (!Game1.random.NextBool(itemProduced.Chance))
            {
                return false;
            }

            // Test drop condition
            if (itemProduced.Condition != null &&
                !GameStateQuery.CheckConditions(
                    itemProduced.Condition,
                    bush.Location,
                    null,
                    null,
                    null,
                    null,
                    bush.Location.SeedsIgnoreSeasonsHere() ? GameStateQuery.SeasonQueryKeys : null))
            {
                Log.Trace(logFormat, id, itemProduced.Id, itemProduced.Condition);
                return false;
            }

            // Test season condition
            if (itemProduced.Season.HasValue &&
                bush.Location.SeedsIgnoreSeasonsHere() &&
                itemProduced.Season != Game1.GetSeasonForLocation(bush.Location))
            {
                Log.Trace(logFormat, id, itemProduced.Id, itemProduced.Season.ToString());
                return false;
            }

            // Try to produce the item
            item = ItemQueryResolver.TryResolveRandomItem(
                itemProduced,
                new ItemQueryContext(bush.Location, null, null, $"custom bush '{id}' > drop '{itemProduced.Id}'"),
                false,
                null,
                null,
                null,
                (query, error) => Log.Error(
                    "{0} failed parsing item query {1} for item {2}: {3}",
                    id,
                    query,
                    itemProduced.Id,
                    error));

            if (item is null)
            {
                continue;
            }

            drop = itemProduced;
            return true;
        }

        return false;
    }
}