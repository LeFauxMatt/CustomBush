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
}