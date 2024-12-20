using LeFauxMods.Common.Models;
using LeFauxMods.CustomBush.Models;

namespace LeFauxMods.CustomBush;

/// <inheritdoc />
internal sealed class ModConfig : IModConfig
{
    /// <inheritdoc />
    public LogAmount LogAmount { get; set; } = LogAmount.Less;

    /// <summary>Copy config option values to another instance.</summary>
    /// <param name="other">The instance to copy values to.</param>
    public void CopyTo(ModConfig other) => other.LogAmount = this.LogAmount;
}
