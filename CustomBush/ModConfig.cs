using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.CustomBush;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <inheritdoc />
    public LogAmount LogAmount { get; set; } = LogAmount.Less;

    /// <inheritdoc />
    public void CopyTo(ModConfig other) => other.LogAmount = this.LogAmount;

    /// <inheritdoc />
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.LogAmount),25}: {this.LogAmount}")
            .ToString();
}