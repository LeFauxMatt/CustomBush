using LeFauxMods.Common.Integrations.CustomBush;
using Microsoft.Xna.Framework;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class BushStage(List<ItemDrop>? itemsProduced, List<ProgressRule>? progressRules)
    : ICustomBushStage
{
    /// <inheritdoc />
    public BushType BushType { get; set; } = BushType.Tea;

    /// <inheritdoc />
    public string? ConditionToProgress { get; set; }

    /// <inheritdoc />
    public string IndoorTexture { get; set; } = string.Empty;

    /// <inheritdoc />
    public ICustomBushDrops ItemsProduced { get; set; } = itemsProduced is not null
        ? new ItemDrops(itemsProduced)
        : new ItemDrops();

    /// <inheritdoc />
    public ICustomBushProgressRules ProgressRules { get; set; } = progressRules is not null
        ? new ProgressRules(progressRules)
        : new ProgressRules();

    /// <inheritdoc />
    public Point SpritePosition { get; set; }

    /// <inheritdoc />
    public string Texture { get; set; } = "TileSheets/bushes";
}