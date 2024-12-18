namespace LeFauxMods.CustomBush;

using Common.Integrations.ContentPatcher;
using Common.Integrations.GenericModConfigMenu;
using Common.Models;
using Common.Services;
using Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Models;
using Services;
using StardewModdingAPI.Events;
using StardewValley.Extensions;
using Utilities;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private readonly Dictionary<string, Texture2D> textures = new(StringComparer.OrdinalIgnoreCase);

    private ModConfig config = null!;
    private ConfigHelper<ModConfig> configHelper = null!;
    private Dictionary<string, CustomBush>? data;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(this.Helper.Translation);

        this.configHelper = new ConfigHelper<ModConfig>(helper);
        this.config = this.configHelper.Load();

        Log.Init(this.Monitor, this.config);
        BushExtensions.Init(this.GetData);
        ModPatches.Init(this.GetData, this.GetTexture);

        // Events
        this.Helper.Events.Content.AssetRequested += OnAssetRequested;
        this.Helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);

        var contentPatcherIntegration = new ContentPatcherIntegration(this.Helper);
        if (contentPatcherIntegration.IsLoaded)
        {
            ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
    }

    /// <inheritdoc />
    public override object GetApi(IModInfo mod) => new ModApi(this.Helper);

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(Constants.DataPath))
        {
            return;
        }

        e.LoadFrom(
            static () => new Dictionary<string, CustomBush>(StringComparer.OrdinalIgnoreCase),
            AssetLoadPriority.Exclusive);

        e.Edit(
            static asset =>
            {
                var data = asset.AsDictionary<string, CustomBush>().Data;
                foreach (var (key, value) in data)
                {
                    value.Id = key;

                    if (string.IsNullOrWhiteSpace(value.DisplayName))
                    {
                        value.DisplayName = I18n.Placeholder_Name();
                    }

                    if (string.IsNullOrWhiteSpace(value.Description))
                    {
                        value.Description = I18n.Placeholder_Description();
                    }
                }
            },
            (AssetEditPriority)int.MaxValue);
    }

    private Dictionary<string, CustomBush> GetData() =>
        this.data ??= this.Helper.GameContent.Load<Dictionary<string, CustomBush>>(Constants.DataPath);

    private Texture2D GetTexture(string path)
    {
        if (this.textures.TryGetValue(path, out var texture))
        {
            return texture;
        }

        texture = this.Helper.GameContent.Load<Texture2D>(path);
        this.textures[path] = texture;
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

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e) => e.Config.CopyTo(this.config);

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry);
        if (!gmcm.IsLoaded)
        {
            return;
        }

        var tempConfig = this.configHelper.Load();

        gmcm.Register(() => tempConfig = new ModConfig(), () => this.configHelper.Save(tempConfig));

        gmcm.Api.AddTextOption(
            this.ModManifest,
            () => tempConfig.LogAmount.ToStringFast(),
            value => tempConfig.LogAmount =
                LogAmountExtensions.TryParse(value, out var logAmount) ? logAmount : LogAmount.Less,
            I18n.Config_LogAmount_Name,
            I18n.Config_LogAmount_Tooltip,
            LogAmountExtensions.GetNames(),
            value => value switch
            {
                nameof(LogAmount.More) => I18n.Config_LogAmount_More(),
                _ => I18n.Config_LogAmount_Less()
            });
    }
}
