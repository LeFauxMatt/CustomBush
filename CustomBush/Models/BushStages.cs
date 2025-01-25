using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushStages" />
internal sealed class BushStages : Dictionary<string, ICustomBushStage>, ICustomBushStages
{
    /// <inheritdoc />
    public BushStages()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <inheritdoc />
    public BushStages(Dictionary<string, BushStage> stages)
        : base(StringComparer.OrdinalIgnoreCase)
    {
        foreach (var (key, stage) in stages)
        {
            this.Add(key, stage);
        }
    }
}