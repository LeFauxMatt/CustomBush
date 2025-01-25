using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class ProgressRule(List<ItemDrop>? itemsDropped) : ICustomBushProgressRule
{
    private int stageCounter = -1;

    /// <inheritdoc />
    public string? Condition { get; set; }

    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public ICustomBushDrops ItemsDropped { get; set; } = itemsDropped is not null
        ? new ItemDrops(itemsDropped)
        : new ItemDrops();

    /// <inheritdoc />
    public string StageId { get; set; } = string.Empty;

    /// <inheritdoc />
    public int GetStageCounter()
    {
        if (this.stageCounter >= 0)
        {
            return this.stageCounter;
        }

        if (string.IsNullOrWhiteSpace(this.Condition))
        {
            this.stageCounter = 0;
            return 0;
        }

        foreach (var parsed in GameStateQuery.Parse(this.Condition))
        {
            if (parsed.Error is not null ||
                parsed.Query.Length != 2 ||
                string.IsNullOrWhiteSpace(parsed.Query[0]) ||
                !parsed.Query[0].Trim().Equals(ModConstants.QueryKeyStageCounter, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(parsed.Query[1], out var amount))
            {
                continue;
            }

            this.stageCounter = amount;
            return amount;
        }

        this.stageCounter = 0;
        return 0;
    }
}