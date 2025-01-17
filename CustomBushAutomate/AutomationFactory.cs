using LeFauxMods.Common.Integrations.CustomBush;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBushAutomate;

/// <inheritdoc />
internal sealed class AutomationFactory(CustomBushIntegration customBush) : IAutomationFactory
{
    /// <inheritdoc />
    public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile) => null;

    /// <inheritdoc />
    public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile)
    {
        if (feature is not Bush bush || !customBush.IsLoaded || !customBush.Api.IsCustomBush(bush))
        {
            return null;
        }

        return new Automatable(customBush, bush);
    }

    /// <inheritdoc />
    public IAutomatable? GetFor(Building building, GameLocation location, in Vector2 tile) => null;

    /// <inheritdoc />
    public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) => null;
}