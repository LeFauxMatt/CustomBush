using LeFauxMods.Common.Utilities;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Utilities;

internal static class BushExtensions
{
    public static bool TestCondition(this Bush bush, string condition) =>
        GameStateQuery.CheckConditions(condition, bush.Location, null, null, null, null,
            bush.Location.SeedsIgnoreSeasonsHere() || bush.IsSheltered()
                ? GameStateQuery.SeasonQueryKeys
                : null);

    public static void ClearCachedData(this IHaveModData item)
    {
        item.modData.Remove(Constants.ModDataAge);
        item.modData.Remove(Constants.ModDataCondition);
        item.modData.Remove(Constants.ModDataItem);
        item.modData.Remove(Constants.ModDataItemSeason);
        item.modData.Remove(Constants.ModDataQuality);
        item.modData.Remove(Constants.ModDataSpriteOffset);
        item.modData.Remove(Constants.ModDataStack);
    }

    public static bool TryGetCachedData(
        this Bush bush,
        bool readyForHarvest,
        [NotNullWhen(true)] out string? itemId,
        out int itemQuality,
        out int itemStack,
        out string? condition)
    {
        itemQuality = 0;
        itemStack = 0;

        if (!readyForHarvest || !bush.modData.TryGetValue(Constants.ModDataItem, out itemId) ||
            string.IsNullOrWhiteSpace(itemId))
        {
            itemId = null;
            condition = null;
            return false;
        }

        if (!bush.modData.TryGetValue(Constants.ModDataCondition, out condition) &&
            bush.modData.TryGetValue(Constants.ModDataItemSeason, out var itemSeason) &&
            Enum.TryParse(itemSeason, out Season season))
        {
            condition = $"SEASON {season.ToString()}";
        }

        if (!string.IsNullOrWhiteSpace(condition) && !bush.TestCondition(condition))
        {
            Log.Trace("Cached item's condition does not pass: {0}", condition);
            itemId = null;
            condition = null;
            return false;
        }

        itemQuality = bush.modData.GetInt(Constants.ModDataQuality);
        itemStack = bush.modData.GetInt(Constants.ModDataStack, 1);
        return true;
    }
}