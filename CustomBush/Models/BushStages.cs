using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushStages" />
internal sealed class BushStages : Dictionary<string, ICustomBushStage>, ICustomBushStages
{
    private List<(ICustomBushStage Stage, string Id, int Counter)>? sequentialStages;

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

    /// <inheritdoc />
    public IEnumerable<(ICustomBushStage Stage, string Id, int Counter)> GetSequentialStages() =>
        this.sequentialStages ?? [];

    public IEnumerable<(ICustomBushStage Stage, string Id, int Counter)> GetSequentialStages(string initialStage)
    {
        return this.sequentialStages ??= SequentialStages().ToList();

        IEnumerable<(ICustomBushStage Stage, string Id, int Counter)> SequentialStages()
        {
            var stages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stageId = initialStage;
            while (stageId is not null && this.TryGetValue(stageId, out var stage) && stages.Add(stageId))
            {
                var nextStageId = stage.ProgressRules.FirstOrDefault(rule => !stages.Contains(rule.StageId))?.StageId;
                yield return (stage, stageId, stage.ProgressRules.GetCounterTo(nextStageId));

                stageId = nextStageId;
            }
        }
    }
}