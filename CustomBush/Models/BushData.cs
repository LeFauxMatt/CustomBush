using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class BushData(Dictionary<string, BushStage>? stages) : ICustomBushData
{
    private int ageToMature = -1;
    private List<Season>? seasons;

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

    /// <inheritdoc />
    public int GetAgeToMature()
    {
        if (this.ageToMature >= 0)
        {
            return this.ageToMature;
        }

        if (!this.ConditionsToProduce.Any())
        {
            this.ageToMature = 0;
            return 0;
        }

        foreach (var condition in this.ConditionsToProduce)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                continue;
            }

            foreach (var parsed in GameStateQuery.Parse(condition))
            {
                if (parsed.Error is not null ||
                    parsed.Query.Length != 2 ||
                    string.IsNullOrWhiteSpace(parsed.Query[0]) ||
                    !parsed.Query[0].Trim().Equals(ModConstants.QueryKeyAge, StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(parsed.Query[1], out var amount))
                {
                    continue;
                }

                this.ageToMature = amount;
                return amount;
            }
        }

        this.ageToMature = 0;
        return 0;
    }

    /// <inheritdoc />
    public List<Season> GetSeasons()
    {
        if (this.seasons is not null)
        {
            return this.seasons;
        }

        if (!this.ConditionsToProduce.Any())
        {
            this.seasons = [Season.Spring, Season.Summer, Season.Fall, Season.Winter];
            return this.seasons;
        }

        foreach (var condition in this.ConditionsToProduce)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                continue;
            }

            this.seasons = [];
            foreach (var parsed in GameStateQuery.Parse(condition))
            {
                if (parsed.Error is not null ||
                    parsed.Query.Length < 2 ||
                    string.IsNullOrWhiteSpace(parsed.Query[0]) ||
                    !parsed.Query[0].Trim().Equals(nameof(GameStateQuery.DefaultResolvers.SEASON),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                for (var i = 1; i < parsed.Query.Length; i++)
                {
                    if (!ArgUtility.TryGetEnum<Season>(parsed.Query, i, out var season, out var error, "Season season"))
                    {
                        continue;
                    }

                    this.seasons.Add(season);
                }

                return this.seasons;
            }
        }

        this.seasons = [Season.Spring, Season.Summer, Season.Fall, Season.Winter];
        return this.seasons;
    }
}