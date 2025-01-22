using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Services;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Utilities;

internal static class BushExtensions
{
    public static bool TestCondition(this Bush bush, string condition) =>
        GameStateQuery.CheckConditions(condition, bush.Location, null, null, null, null,
            bush.Location.SeedsIgnoreSeasonsHere() || bush.IsSheltered()
                ? GameStateQuery.SeasonQueryKeys
                : null);

    public static bool TryProduceItem(
        this Bush bush,
        [NotNullWhen(true)] out Item? item,
        [NotNullWhen(true)] out ICustomBushDrop? drop)
    {
        const string logFormat = "{0} did not select {1}. Failed: {2}";
        item = null;
        drop = null;

        if (!ModState.Api.TryGetBush(bush, out var customBush, out var id) || customBush is not CustomBushData data)
        {
            return false;
        }

        foreach (var itemProduced in data.ItemsProduced)
        {
            // Test overall chance
            if (!Game1.random.NextBool(itemProduced.Chance))
            {
                continue;
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
                continue;
            }

            // Test season condition
            if (itemProduced.Season.HasValue &&
                bush.Location.SeedsIgnoreSeasonsHere() &&
                itemProduced.Season != Game1.GetSeasonForLocation(bush.Location))
            {
                Log.Trace(logFormat, id, itemProduced.Id, itemProduced.Season.ToString());
                continue;
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