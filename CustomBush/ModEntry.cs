using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Services;
using LeFauxMods.CustomBush.Utilities;
using StardewModdingAPI.Events;
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
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

        if (!Context.IsMainPlayer)
        {
            return;
        }

        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    /// <inheritdoc />
    public override object GetApi(IModInfo mod) => new ModApi();

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.DataPath))
        {
            e.LoadFrom(
                static () => new Dictionary<string, BushData>(StringComparer.OrdinalIgnoreCase),
                AssetLoadPriority.Exclusive);

            e.Edit(
                static asset =>
                {
                    foreach (var (key, value) in asset.AsDictionary<string, BushData>().Data)
                    {
                        value.Id = key;
                    }
                },
                (AssetEditPriority)int.MaxValue);
        }
    }

    [EventPriority(EventPriority.High)]
    private static void OnDayStarted(object? sender, DayStartedEventArgs e) =>
        ModState.ForEachBush(static customBush =>
        {
            var data = customBush.Data;
            if (!data.Stages.TryGetValue(customBush.StageId, out var stage))
            {
                return true;
            }

            // Increment the stage counter
            if (customBush.TestCondition(stage.ConditionToProgress))
            {
                customBush.StageCounter++;
            }

            // Check progress rules
            foreach (var progressRule in stage.ProgressRules)
            {
                if (!customBush.TestCondition(progressRule.Condition))
                {
                    continue;
                }

                // Check available space for bush size


                if (!data.Stages.TryGetValue(progressRule.StageId, out var nextStage))
                {
                    Log.Warn("Invalid stage id {0}", progressRule.StageId);
                    break;
                }

                // Drop items
                foreach (var drop in progressRule.ItemsDropped)
                {
                    if (!customBush.TestCondition(drop.Condition))
                    {
                    }
                }

                // Update bush stage
                customBush.StageId = progressRule.StageId;
                stage = nextStage;
                break;
            }

            // Check or update conditions to produce
            if (!customBush.TestCondition(customBush.Condition))
            {
                customBush.Condition = data.ConditionsToProduce.FirstOrDefault(customBush.TestCondition);
                customBush.Item = null;
            }

            if (!customBush.IsInSeason)
            {
                return true;
            }

            // Check item drops
            foreach (var drop in stage.ItemsProduced)
            {
                if (customBush.Item is not null && !drop.ReplaceItem)
                {
                    continue;
                }

                // Try to produce the item
                if (!customBush.TryProduceDrop(drop, out var item))
                {
                    continue;
                }

                customBush.Item = item;
                customBush.SpriteOffset = drop.SpriteOffset;
                break;
            }

            return true;
        });

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsPlayerFree || !ModState.Config.GrowBushKey.JustPressed())
        {
            return;
        }

        var location = Game1.player.currentLocation;
        var bushes = location.terrainFeatures.Values.OfType<Bush>()
            .Concat(location.largeTerrainFeatures.OfType<Bush>())
            .Concat(location.Objects.Values.OfType<IndoorPot>().Select(static pot => pot.bush.Value))
            .Where(static bush =>
                bush is not null &&
                bush.Tile.X >= Game1.player.Tile.X - 1 &&
                bush.Tile.X <= Game1.player.Tile.X + 1 &&
                bush.Tile.Y >= Game1.player.Tile.Y - 1 &&
                bush.Tile.Y <= Game1.player.Tile.Y + 1).ToList();

        foreach (var bush in bushes)
        {
            if (!bush.modData.TryGetValue(ModConstants.IdKey, out var id) ||
                !bush.modData.TryGetValue(ModConstants.StageKey, out var currentStage) ||
                !ModState.Data.TryGetValue(id, out var data))
            {
                continue;
            }

            var growNext = false;
            foreach (var (stage, stageId, _) in ((BushStages)data.Stages).GetSequentialStages(data.InitialStage))
            {
                if (growNext)
                {
                    if (bush.TryGrow(stage.BushType))
                    {
                        bush.modData[ModConstants.StageKey] = stageId;
                    }

                    break;
                }

                if (stageId == currentStage)
                {
                    growNext = true;
                }
            }

            // Try to produce an item
            if (bush.modData[ModConstants.StageKey] == currentStage)
            {
            }
        }
    }
}