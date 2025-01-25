using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.TerrainFeatures;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace LeFauxMods.CustomBush.Utilities;

internal static class ModExtensions
{
    public static int GetHeight(this BushType bushType) =>
        bushType switch
        {
            BushType.Medium or BushType.Large => 48,
            _ => 32
        };

    public static Vector2[] GetSurroundingTiles(this Bush bush, BushType bushType) =>
        bush.size.Value switch
        {
            Bush.largeBush => [],
            Bush.mediumBush or Bush.walnutBush => bushType switch
            {
                BushType.Large => [bush.Tile + new Vector2(-1, 0)],
                _ => []
            },
            _ => bushType switch
            {
                BushType.Medium or BushType.Walnut => [bush.Tile + new Vector2(1, 0)],
                BushType.Large => [bush.Tile + new Vector2(-1, 0), bush.Tile + new Vector2(1, 0)],
                _ => []
            }
        };

    public static int GetWidth(this BushType bushType) =>
        bushType switch
        {
            BushType.Medium or BushType.Walnut => 32,
            BushType.Large => 48,
            _ => 16
        };

    /// <summary>Loosely based on <see cref="FruitTree.IsGrowthBlocked" />.</summary>
    /// <param name="bush">The bush to check.</param>
    /// <param name="bushType">The bush type to grow into.</param>
    /// <returns>True if the bush growth is blocked.</returns>
    public static bool IsGrowthBlocked(this Bush bush, BushType bushType)
    {
        foreach (var pos in bush.GetSurroundingTiles(bushType))
        {
            switch (bush.Location.Objects.GetValueOrDefault(pos))
            {
                case { } obj when obj.QualifiedItemId != "(O)590" && obj.QualifiedItemId != "(O)SeedSpot":
                    return true;
            }

            switch (bush.Location.terrainFeatures.GetValueOrDefault(pos))
            {
                case not (HoeDirt or Grass or null):
                case HoeDirt { crop: not null }:
                    return true;
            }

            if (bush.Location.IsTileOccupiedBy(pos,
                    CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.Furniture |
                    CollisionMask.LocationSpecific))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGrow(this Bush bush, BushType bushType)
    {
        var location = bush.Location;
        var tile = bush.Tile;

        // Verify additional space requirement
        switch (bushType)
        {
            // Always okay
            case BushType.Tea or BushType.Small:
                bush.size.Value = (int)bushType;
                break;

            // Never okay
            case { } when bush.inPot.Value || bush.IsGrowthBlocked(bushType):
                return false;
        }

        // Remove from terrainFeature/largeTerrainFeature if needed
        if (bush.size.Value is Bush.greenTeaBush && bushType is not BushType.Tea)
        {
            location.terrainFeatures.Remove(tile);
        }
        else if (bush.size.Value is not Bush.greenTeaBush && bushType is BushType.Tea)
        {
            location.largeTerrainFeatures.Remove(bush);
        }

        // Reposition if needed
        if (bushType is BushType.Large && bush.size.Value is not Bush.largeBush)
        {
            tile = new Vector2(tile.X - 1, tile.Y);
        }
        else if (bushType is not BushType.Large && bush.size.Value is Bush.largeBush)
        {
            tile = new Vector2(tile.X + 1, tile.Y);
        }

        // Add to terrainFeature/largeTerrainFeatures
        if (bushType is BushType.Tea && bush.size.Value is not Bush.greenTeaBush)
        {
            location.terrainFeatures.Add(tile, bush);
        }
        else if (bushType is not BushType.Tea && bush.size.Value is Bush.greenTeaBush)
        {
            location.largeTerrainFeatures.Add(bush);
        }

        // Update bush size
        bush.Tile = tile;
        bush.size.Value = (int)bushType;
        return true;
    }
}