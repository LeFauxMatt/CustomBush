using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class BushData(Dictionary<string, BushStage>? stages) : ICustomBushData
{
    /// <inheritdoc />
    public List<string> ConditionsToProduce { get; set; } = [];

    /// <inheritdoc />
    public string Description { get; set; } = I18n.Placeholder_Description();

    /// <inheritdoc />
    public string DisplayName { get; set; } = I18n.Placeholder_Name();

    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public string InitialStage { get; set; } = string.Empty;

    /// <inheritdoc />
    public List<PlantableRule> PlantableLocationRules { get; set; } = [];

    /// <inheritdoc />
    public ICustomBushStages Stages { get; set; } = stages is not null
        ? new BushStages(stages)
        : new BushStages();
}