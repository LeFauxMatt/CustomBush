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
    private readonly IModHelper helper;
    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly Dictionary<string, Texture2D> textures = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, CustomBushData>? data;

    private ModState(IModHelper helper)
    {
        // Init
        this.helper = helper;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        this.api = new ModApi(helper);

        // Events
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;

        var contentPatcherIntegration = new ContentPatcherIntegration(helper);
        if (contentPatcherIntegration.IsLoaded)
        {
            ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
    }

    public static ICustomBushApi Api => Instance!.api;

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Dictionary<string, CustomBushData> Data => Instance!.data ??=
        Instance.helper.GameContent.Load<Dictionary<string, CustomBushData>>(Constants.DataPath);

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

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
    }
}