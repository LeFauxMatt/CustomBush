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
        item.modData.Remove(ModConstants.ModDataAge);
        item.modData.Remove(ModConstants.ModDataCondition);
        item.modData.Remove(ModConstants.ModDataItem);
        item.modData.Remove(ModConstants.ModDataItemSeason);
        item.modData.Remove(ModConstants.ModDataQuality);
        item.modData.Remove(ModConstants.ModDataSpriteOffset);
        item.modData.Remove(ModConstants.ModDataStack);
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

        if (!readyForHarvest || !bush.modData.TryGetValue(ModConstants.ModDataItem, out itemId) ||
            string.IsNullOrWhiteSpace(itemId))
        {
            itemId = null;
            condition = null;
            return false;
        }

        if (!bush.modData.TryGetValue(ModConstants.ModDataCondition, out condition) &&
            bush.modData.TryGetValue(ModConstants.ModDataItemSeason, out var itemSeason) &&
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

        itemQuality = bush.modData.GetInt(ModConstants.ModDataQuality);
        itemStack = bush.modData.GetInt(ModConstants.ModDataStack, 1);
        return true;
    }
}