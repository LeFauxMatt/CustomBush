using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Services;

/// <inheritdoc />
public sealed class ModApi : ICustomBushApi
{
    /// <inheritdoc />
    public bool IsCustomBush(Bush bush) =>
        bush.modData.TryGetValue(ModConstants.IdKey, out var id) && ModState.Data.ContainsKey(id);

    /// <inheritdoc />
    public bool TryGetBush(Bush bush, [NotNullWhen(true)] out ICustomBush? customBush)
    {
        customBush = null;
        if (!ModState.ManagedBushes.TryGetValue(bush, out var managedBush))
        {
            return false;
        }

        customBush = managedBush;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetData(Bush bush, [NotNullWhen(true)] out ICustomBushData? customBushData)
    {
        customBushData = null;
        if (!bush.modData.TryGetValue(ModConstants.IdKey, out var id))
        {
            return false;
        }

        if (!ModState.Data.TryGetValue(id, out var bushData))
        {
            return false;
        }

        customBushData = bushData;
        return true;
    }
}