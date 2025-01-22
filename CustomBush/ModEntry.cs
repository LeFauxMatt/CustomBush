using System.Globalization;
using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Services;
using LeFauxMods.CustomBush.Utilities;
using StardewModdingAPI.Events;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(helper.Translation);
        ModState.Init(helper, this.ModManifest);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Init(helper);

        // Events
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    /// <inheritdoc />
    public override object GetApi(IModInfo mod) => new ModApi(this.Helper);

    [EventPriority(EventPriority.High)]
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        const string logFormat = "{0} did not select {1}. Failed: {2}";

        if (!Context.IsMainPlayer)
        {
            return;
        }

        Utility.ForEachLocation(static location =>
        {
            var bushes =
                location.terrainFeatures.Values.OfType<Bush>().Concat(
                    location.Objects.Values.OfType<IndoorPot>().Select(static pot => pot.bush.Value)
                        .Where(static bush => bush is not null));

            foreach (var bush in bushes)
            {
                // Check if bush has custom bush data
                if (!ModState.Api.TryGetBush(bush, out var customBush, out var id) ||
                    customBush is not CustomBushData data)
                {
                    continue;
                }

                // Skip additional checks if cached data is still relevant
                if (ModState.Api.TryGetModData(bush, out _, out _, out _, out _))
                {
                    continue;
                }

                bush.ClearCachedData();
                bush.tileSheetOffset.Value = 0;
                bush.setUpSourceRect();

                var age = bush.getAge();

                // Check if bush meets the age requirement
                if (age < customBush.AgeToProduce)
                {
                    Log.Trace(
                        "{0} will not produce. Age: {1} < {2}",
                        id,
                        age.ToString(CultureInfo.InvariantCulture),
                        customBush.AgeToProduce.ToString(CultureInfo.InvariantCulture));

                    continue;
                }

                // Check if bush meets any condition requirement
                var condition = customBush.ConditionsToProduce.FirstOrDefault(bush.TestCondition);
                if (string.IsNullOrWhiteSpace(condition))
                {
                    Log.Trace("{0} will not produce. None of the required conditions was met.", id);
                    continue;
                }

                // Try to produce item
                Log.Trace("{0} attempting to produce random item.", id);
                ICustomBushDrop? drop = null;
                Item? item = null;
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
                        new ItemQueryContext(bush.Location, null, null,
                            $"custom bush '{id}' > drop '{itemProduced.Id}'"),
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
                    break;
                }

                if (drop is null || item is null)
                {
                    Log.Trace("{0} will not produce. No item was produced.", id);
                    continue;
                }

                Log.Trace(
                    "{0} selected {1} to grow with quality {2} and quantity {3}.",
                    id,
                    item.QualifiedItemId,
                    item.Quality,
                    item.Stack);

                bush.modData[Constants.ModDataCondition] = condition;
                bush.modData[Constants.ModDataItem] = item.QualifiedItemId;
                bush.modData[Constants.ModDataQuality] = item.Quality.ToString(CultureInfo.InvariantCulture);
                bush.modData[Constants.ModDataStack] = item.Stack.ToString(CultureInfo.InvariantCulture);
                bush.modData[Constants.ModDataSpriteOffset] = drop.SpriteOffset.ToString(CultureInfo.InvariantCulture);
                bush.tileSheetOffset.Value = 1;
                bush.setUpSourceRect();
            }

            return true;
        });
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(Constants.DataPath))
        {
            return;
        }

        e.LoadFrom(
            static () => new Dictionary<string, CustomBushData>(StringComparer.OrdinalIgnoreCase),
            AssetLoadPriority.Exclusive);

        e.Edit(
            static asset =>
            {
                var data = asset.AsDictionary<string, CustomBushData>().Data;
                foreach (var (key, value) in data)
                {
                    value.Id = key;
                }
            },
            (AssetEditPriority)int.MaxValue);
    }
}