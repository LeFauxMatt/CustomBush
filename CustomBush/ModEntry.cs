using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Services;
using StardewModdingAPI.Events;

namespace LeFauxMods.CustomBush;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(helper.Translation);
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Init(helper);

        // Events
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
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

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        _ = new ConfigMenu(this.Helper, this.ModManifest);
}