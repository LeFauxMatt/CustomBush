using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.CustomBush.Services;

internal sealed class CustomBushOption : ComplexOption
{
    private readonly int[] age;
    private readonly List<ClickableComponent> components = [];
    private readonly IModHelper helper;

    public CustomBushOption(IModHelper helper)
    {
        this.helper = helper;
        this.Height = 2 * Game1.tileSize * (int)Math.Ceiling(ModState.Data.Count / 14f);
        var index = 0;
        foreach (var (id, customBush) in ModState.Data)
        {
            var assetName = helper.GameContent.ParseAssetName(customBush.Texture);
            if (assetName.IsEquivalentTo("TileSheets/bushes"))
            {
                continue;
            }

            var row = index / 14;
            var col = index % 14;
            var component =
                new ClickableComponent(
                    new Rectangle(col * Game1.tileSize, row * Game1.tileSize * 2, Game1.tileSize, Game1.tileSize * 2),
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

        this.age = new int[index];
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
            if (!ModState.Data.TryGetValue(component.name, out var customBush))
            {
                continue;
            }

            var bounds = component.bounds with
            {
                X = (int)pos.X + component.bounds.X, Y = (int)pos.Y + component.bounds.Y
            };

            this.age[index] = component.bounds.Contains(mouseX, mouseY)
                ? this.age[index] + 5
                : this.age[index] - 2;

            this.age[index] = Math.Max(0, Math.Min(customBush.AgeToProduce * 5, this.age[index]));

            var texture = !string.IsNullOrWhiteSpace(customBush.IndoorTexture)
                ? ModState.GetTexture(customBush.IndoorTexture)
                : ModState.GetTexture(customBush.Texture);

            var sourceRect = new Rectangle(
                (int)Math.Min(3, 3 * ((float)this.age[index] / customBush.AgeToProduce / 5)) * 16,
                customBush.TextureSpriteRow * 16,
                16,
                32);

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
                hoverTitle ??= TokenParser.ParseText(customBush.DisplayName);
                hoverText ??= TokenParser.ParseText(customBush.Description);
            }
        }

        if (!string.IsNullOrWhiteSpace(hoverTitle))
        {
            IClickableMenu.drawToolTip(spriteBatch, hoverTitle, null, null);
        }
    }
}