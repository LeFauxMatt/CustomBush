using System.Runtime.CompilerServices;
using LeFauxMods.Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Services;

internal sealed class ModState
{
    private static ModState? Instance;

    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly IModHelper helper;
    private readonly ConditionalWeakTable<Bush, ManagedBush> managedBushes = new();
    private readonly IManifest manifest;
    private readonly Dictionary<string, Texture2D> textures = new(StringComparer.OrdinalIgnoreCase);
    private ConfigMenu? configMenu;
    private Dictionary<string, BushData>? data;

    private ModState(IModHelper helper, IManifest manifest)
    {
        // Init
        this.helper = helper;
        this.manifest = manifest;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        _ = new ContentPatcherIntegration(helper);

        // Events
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

        ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
    }

    public static ICustomBushApi Api { get; } = new ModApi();

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Dictionary<string, BushData> Data => Instance!.data ??= Instance.GetData();

    public static ConditionalWeakTable<Bush, ManagedBush> ManagedBushes => Instance!.managedBushes;

    public static void ForEachBush(Func<ManagedBush, bool> action) =>
        Utility.ForEachLocation(location =>
        {
            var bushes =
                location.terrainFeatures.Values.OfType<Bush>()
                    .Concat(location.largeTerrainFeatures.OfType<Bush>())
                    .Concat(location.Objects.Values.OfType<IndoorPot>().Select(static pot => pot.bush.Value)
                        .Where(static bush => bush is not null));

            foreach (var bush in bushes)
            {
                if (!bush.modData.TryGetValue(ModConstants.IdKey, out var id) ||
                    !Data.TryGetValue(id, out var customBush))
                {
                    continue;
                }

                if (!ManagedBushes.TryGetValue(bush, out var managedBush))
                {
                    managedBush = new ManagedBush(bush);
                    ManagedBushes.Add(bush, managedBush);
                }

                if (!action(managedBush))
                {
                    return false;
                }
            }

            return true;
        });

    public static Texture2D GetTexture(string path) =>
        Instance!.textures.GetOrAdd(path, () => Instance.helper.GameContent.Load<Texture2D>(path));

    public static void Init(IModHelper helper, IManifest manifest) => Instance ??= new ModState(helper, manifest);

    private Dictionary<string, BushData> GetData()
    {
        this.data ??= this.helper.GameContent.Load<Dictionary<string, BushData>>(ModConstants.DataPath);
        this.configMenu?.SetupMenu();
        return this.data;
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var assetName in e.NamesWithoutLocale)
        {
            if (assetName.IsEquivalentTo(ModConstants.DataPath))
            {
                this.data = null;
                continue;
            }

            _ = this.textures.RemoveWhere(kvp => assetName.IsEquivalentTo(kvp.Key));
        }
    }

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e)
    {
        this.data = null;
        this.textures.Clear();
        this.helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        this.configMenu = new ConfigMenu(this.helper, this.manifest);

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => this.managedBushes.Clear();

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        this.helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;
        _ = Data;
    }
}