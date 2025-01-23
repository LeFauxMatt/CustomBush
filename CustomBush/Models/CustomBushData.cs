using System.Text;
using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class CustomBushData : ICustomBushData
{
    private List<string>? conditionsToProduce;

    public CustomBushData(List<CustomBushDrop> itemsProduced) =>
        this.ItemsProduced = new CustomBushDrops(itemsProduced.ConvertAll(static ICustomBushDrop (drop) => drop));

    /// <inheritdoc />
    public int AgeToProduce { get; set; } = 20;

    /// <inheritdoc />
    public BushType BushType { get; set; } = BushType.Tea;

    /// <inheritdoc />
    public List<string> ConditionsToProduce
    {
        get
        {
            return this.conditionsToProduce ??= this.Seasons.ConvertAll(GetGameStateQuery);

            string GetGameStateQuery(Season season)
            {
                var sb = new StringBuilder();

                sb.Append("SEASON ")
                    .Append(season.ToString());

                if (this.DayToBeginProducing <= 0)
                {
                    return sb.ToString();
                }

                sb.Append(",DAY_OF_MONTH ");
                for (var day = this.DayToBeginProducing; day <= 28; day++)
                {
                    sb.Append(' ').Append(day);
                }

                return sb.ToString();
            }
        }
        set => this.conditionsToProduce = value;
    }

    /// <inheritdoc />
    public int DayToBeginProducing { get; set; } = 22;

    /// <inheritdoc />
    public string Description { get; set; } = I18n.Placeholder_Description();

    /// <inheritdoc />
    public string DisplayName { get; set; } = I18n.Placeholder_Name();

    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public string IndoorTexture { get; set; } = "TileSheets/bushes";

    /// <summary>Gets or sets the items produced by this custom bush.</summary>
    public ICustomBushDrops ItemsProduced { get; set; }

    /// <inheritdoc />
    public List<PlantableRule> PlantableLocationRules { get; set; } = [];

    /// <inheritdoc />
    public List<Season> Seasons { get; set; } = [];

    /// <inheritdoc />
    public string Texture { get; set; } = "TileSheets/bushes";

    /// <inheritdoc />
    public int TextureSpriteRow { get; set; }
}