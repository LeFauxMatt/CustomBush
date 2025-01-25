using LeFauxMods.CustomBush.Models;
using StardewModdingAPI.Events;

namespace LeFauxMods.CustomBush.Migrations;

/// <inheritdoc />
internal sealed class Migration_2_0
{
    public Migration_2_0(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Initialize 1.0 path
        if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.BaseDataPath))
        {
            e.LoadFrom(
                static () => new Dictionary<string, Data_1_0>(StringComparer.OrdinalIgnoreCase),
                AssetLoadPriority.Exclusive);
        }

        // Migrate 1.0 data into 2.0 data
        if (e.NameWithoutLocale.IsEquivalentTo(Path.Join(ModConstants.BaseDataPath, "2.0")))
        {
            e.Edit(asset =>
            {
            });
        }
    }

    private sealed class Data_1_0
    {
    }
}
