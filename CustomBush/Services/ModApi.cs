namespace LeFauxMods.CustomBush.Services;

using Common.Integrations.CustomBush;
using Models;
using StardewValley.TerrainFeatures;

/// <inheritdoc />
public sealed class ModApi(IModHelper helper) : ICustomBushApi
{
    /// <inheritdoc />
    public IEnumerable<(string Id, ICustomBush Data)> GetData() =>
        helper
            .GameContent.Load<Dictionary<string, CustomBush>>(Constants.DataPath)
            .Select(pair => (pair.Key, (ICustomBush)pair.Value));

    /// <inheritdoc />
    public bool IsCustomBush(Bush bush) => this.TryGetCustomBush(bush, out _, out _);

    /// <inheritdoc />
    public bool TryGetCustomBush(Bush bush, out ICustomBush? customBush) =>
        this.TryGetCustomBush(bush, out customBush, out _);

    /// <inheritdoc />
    public bool TryGetCustomBush(Bush bush, out ICustomBush? customBush, out string? id)
    {
        customBush = null;
        if (!bush.modData.TryGetValue(Constants.ModDataId, out id))
        {
            return false;
        }

        var data = helper.GameContent.Load<Dictionary<string, CustomBush>>(Constants.DataPath);
        if (!data.TryGetValue(id, out var bushData))
        {
            return false;
        }

        customBush = bushData;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetDrops(string id, out IList<ICustomBushDrop>? drops)
    {
        drops = null;
        var data = helper.GameContent.Load<Dictionary<string, CustomBush>>(Constants.DataPath);
        if (!data.TryGetValue(id, out var bush))
        {
            return false;
        }

        drops = bush.ItemsProduced.ConvertAll(ICustomBushDrop (drop) => drop);
        return true;
    }
}
