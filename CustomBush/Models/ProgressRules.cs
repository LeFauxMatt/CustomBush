using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushProgressRules" />
internal sealed class ProgressRules : List<ICustomBushProgressRule>, ICustomBushProgressRules
{
    /// <inheritdoc />
    public ProgressRules()
    {
    }

    /// <inheritdoc />
    public ProgressRules(List<ProgressRule> rules)
        : base(rules.ConvertAll(static ICustomBushProgressRule (rule) => rule))
    {
    }

    /// <inheritdoc />
    public int GetCounterTo(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return this.Min(static rule => rule.GetStageCounter());
        }

        var rules = this.Where(rule => rule.StageId == id).ToList();
        if (!rules.Any())
        {
            return -1;
        }

        return rules.Min(static rule => rule.GetStageCounter());
    }
}