using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.CustomBush.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.CustomBush.Services;

internal sealed class CustomBushOption : ComplexOption
{
    private readonly List<ClickableComponent> components = [];
    private readonly IModHelper helper;
    private readonly List<string>[] stages;
    private readonly int[] ticks;

    public CustomBushOption(IModHelper helper)
    {
        this.helper = helper;
        this.Height = 2 * Game1.tileSize * (int)Math.Ceiling(ModState.Data.Count / 14f);
        var index = 0;
        foreach (var (id, data) in ModState.Data)
        {
            if (!data.Stages.TryGetValue(data.InitialStage, out var initialStage))
            {
                continue;
            }

            var assetName = helper.GameContent.ParseAssetName(initialStage.Texture);
            if (assetName.IsEquivalentTo("TileSheets/bushes"))
            {
                continue;
            }

            var maxHeight = data.Stages.Values.Max(static stage => stage.BushType.GetHeight());
            var maxWidth = data.Stages.Values.Max(static stage => stage.BushType.GetWidth());

            var row = index / maxWidth;
            var col = index % maxHeight;
            var component =
                new ClickableComponent(
                    new Rectangle(
                        col * maxWidth,
                        row * maxHeight,
                        maxWidth,
                        maxHeight),
                    id) { myID = index };

            if (col > 0)
            {
                component.leftNeighborID = index - 1;
                this.components[component.leftNeighborID].rightNeighborID = index;
            }

            if (row > 0)
            {
                component.upNeighborID = index - 12;
                this.components[component.upNeighborID].downNeighborID = index;
            }

            this.components.Add(component);
            index++;
        }

        this.stages = new List<string>[this.components.Count];
        this.ticks = new int[this.components.Count];
        for (index = 0; index < this.components.Count; index++)
        {
            var component = this.components[index];
            if (!ModState.Data.TryGetValue(component.name, out var data))
            {
                continue;
            }

            this.stages[index] = [];
            var nextStage = data.InitialStage;
            while (nextStage is not null && data.Stages.TryGetValue(nextStage, out var stage))
            {
                this.stages[index].Add(nextStage);
                nextStage = stage.ProgressRules.FirstOrDefault(rule => !this.stages[index].Contains(rule.StageId))
                    ?.StageId;
            }
        }
    }

    /// <inheritdoc />
    public override int Height { get; }

    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        var availableWidth = Math.Min(1200, Game1.uiViewport.Width - 200);
        pos.X -= availableWidth / 2f;
        var (originX, originY) = pos.ToPoint();
        var (mouseX, mouseY) = this.helper.Input.GetCursorPosition().GetScaledScreenPixels().ToPoint();

        mouseX -= originX;
        mouseY -= originY;

        var hoverText = default(string);
        var hoverTitle = default(string);

        for (var index = 0; index < this.components.Count; index++)
        {
            var component = this.components[index];
            if (!ModState.Data.TryGetValue(component.name, out var data))
            {
                continue;
            }

            var bounds = component.bounds with
            {
                X = (int)pos.X + component.bounds.X, Y = (int)pos.Y + component.bounds.Y
            };

            var hover = component.bounds.Contains(mouseX, mouseY);

            this.ticks[index] = hover
                ? this.ticks[index] + 1
                : this.ticks[index] - 1;

            this.ticks[index] = Math.Max(0, Math.Min(this.stages[index].Count * 5, this.ticks[index]));
            var stageIndex = Math.Max(0, Math.Min(this.stages[index].Count - 1, this.ticks[index] / 5));
            if (!data.Stages.TryGetValue(this.stages[index][stageIndex], out var stage))
            {
                continue;
            }

            var texture = !string.IsNullOrWhiteSpace(stage.IndoorTexture)
                ? ModState.GetTexture(stage.IndoorTexture)
                : ModState.GetTexture(stage.Texture);

            var xOffset = this.ticks[index] == this.stages[index].Count * 5
                ? stage.ItemsProduced.Min(static drop => drop.SpriteOffset) * stage.BushType.GetWidth()
                : 0;

            var sourceRect = new Rectangle(
                stage.SpritePosition.X + xOffset,
                stage.SpritePosition.Y,
                stage.BushType.GetWidth(),
                stage.BushType.GetHeight());

            spriteBatch.Draw(
                texture,
                bounds,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                1f);

            if (component.bounds.Contains(mouseX, mouseY))
            {
                hoverTitle ??= TokenParser.ParseText(data.DisplayName);
                hoverText ??= TokenParser.ParseText(data.Description);
            }
        }

        if (!string.IsNullOrWhiteSpace(hoverTitle))
        {
            IClickableMenu.drawToolTip(spriteBatch, hoverTitle, null, null);
        }
    }
}