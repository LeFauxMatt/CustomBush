using LeFauxMods.Common.Integrations.CustomBush;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBushAutomate;

/// <inheritdoc />
internal sealed class Automatable : IMachine
{
    private readonly CustomBushIntegration customBush;
    private readonly Bush bush;

    public Automatable(CustomBushIntegration customBush, Bush bush)
    {
        this.customBush = customBush;
        this.bush = bush;
        this.MachineTypeID = "CustomBush";
        this.Location = bush.Location;
        var boundingBox = bush.getBoundingBox();
        this.TileArea = new Rectangle(
            boundingBox.X / Game1.tileSize,
            boundingBox.Y / Game1.tileSize,
            boundingBox.Width / Game1.tileSize,
            boundingBox.Height / Game1.tileSize);
    }

    /// <inheritdoc />
    public GameLocation Location { get; }

    /// <inheritdoc />
    public Rectangle TileArea { get; }

    /// <inheritdoc />
    public string MachineTypeID { get; }

    /// <inheritdoc />
    public MachineState GetState()
    {
        if (!this.customBush.IsLoaded || !this.customBush.Api.IsInSeason(this.bush))
        {
            return MachineState.Disabled;
        }

        return this.bush.readyForHarvest()
            ? MachineState.Done
            : MachineState.Processing;
    }

    /// <inheritdoc />
    public ITrackedStack? GetOutput()
    {
        if (!this.customBush.IsLoaded ||
            !this.customBush.Api.TryGetShakeOffItem(this.bush, out var item))
        {
            return null;
        }

        return new TrackedItem(item, _ =>
        {
            this.bush.tileSheetOffset.Value = 0;
            this.bush.setUpSourceRect();
        });
    }

    /// <inheritdoc />
    public bool SetInput(IStorage input) => false;
}