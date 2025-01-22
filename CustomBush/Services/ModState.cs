using LeFauxMods.Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley.Extensions;

namespace LeFauxMods.CustomBush.Services;

internal sealed class ModState
{
    private static ModState? Instance;

    private readonly ICustomBushApi api;
    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly Dictionary<string, Texture2D> textures = new(StringComparer.OrdinalIgnoreCase);
    private ConfigMenu? configMenu;
    private Dictionary<string, CustomBushData>? data;

    private ModState(IModHelper helper, IManifest manifest)
    {
        // Init
        this.helper = helper;
        this.manifest = manifest;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        this.api = new ModApi(helper);
        _ = new ContentPatcherIntegration(helper);

        // Events
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
    }

    public static ICustomBushApi Api => Instance!.api;

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Dictionary<string, CustomBushData> Data => Instance!.data ??= Instance.GetData();

    public static void Init(IModHelper helper, IManifest manifest) => Instance ??= new ModState(helper, manifest);

    public static Texture2D GetTexture(string path)
    {
        if (Instance!.textures.TryGetValue(path, out var texture))
        {
            return texture;
        }

        texture = Instance.helper.GameContent.Load<Texture2D>(path);
        Instance.textures[path] = texture;
        return texture;
    }

    private Dictionary<string, CustomBushData> GetData()
    {
        this.data ??= this.helper.GameContent.Load<Dictionary<string, CustomBushData>>(Constants.DataPath);
        this.configMenu?.SetupMenu();
        return this.data;
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var assetName in e.NamesWithoutLocale)
        {
            if (assetName.IsEquivalentTo(Constants.DataPath))
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

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        this.helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;
        _ = Data;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        this.configMenu = new ConfigMenu(this.helper, this.manifest);
}