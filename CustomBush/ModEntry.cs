namespace LeFauxMods.CustomBush;

using Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
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

    private Dictionary<string, CustomBush>? data;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(this.Helper.Translation);
        Log.Init(this.Monitor);
        BushExtensions.Init(this.GetData);
        ModPatches.Init(this.GetData, this.GetTexture);

        var contentPatcherIntegration = new ContentPatcherIntegration(this.Helper);

        // Events
        this.Helper.Events.Content.AssetRequested += OnAssetRequested;
        this.Helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;

        if (contentPatcherIntegration.IsLoaded)
        {
            EventManager.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
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

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e)
    {
        this.data = null;
        this.textures.Clear();
    }

    /// <inheritdoc />
    public override object GetApi(IModInfo mod) => new ModApi(this.Helper, mod);

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
            (AssetEditPriority)int.MinValue);
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
}
