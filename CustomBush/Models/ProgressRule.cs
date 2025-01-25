using LeFauxMods.Common.Integrations.CustomBush;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc />
internal sealed class ProgressRule(List<ItemDrop>? itemsDropped) : ICustomBushProgressRule
{
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
}