using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;
using StardewModdingAPI.Utilities;

namespace LeFauxMods.CustomBush;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets the keybind for growing nearby bushes.</summary>
    public KeybindList GrowBushKey { get; set; } = new(SButton.NumPad2);

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; } = LogAmount.Less;

    /// <inheritdoc />
    public void CopyTo(ModConfig other) => other.LogAmount = this.LogAmount;

    /// <inheritdoc />
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.GrowBushKey),25}: {this.GrowBushKey}")
            .ToString();
}