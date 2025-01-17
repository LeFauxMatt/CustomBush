using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;

namespace LeFauxMods.CustomBush.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IManifest manifest;

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.manifest = manifest;
        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.api = this.gmcm.Api;
        this.SetupMenu();
    }

    private static ModConfig Config => ModState.ConfigHelper.Temp;

    private static ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    private void SetupMenu()
    {
        this.gmcm.Register(ConfigHelper.Reset, ConfigHelper.Save);

        this.api.AddTextOption(
            this.manifest,
            static () => Config.LogAmount.ToStringFast(),
            static value => Config.LogAmount =
                LogAmountExtensions.TryParse(value, out var logAmount) ? logAmount : LogAmount.Less,
            I18n.Config_LogAmount_Name,
            I18n.Config_LogAmount_Tooltip,
            LogAmountExtensions.GetNames(),
            value => value switch
            {
                nameof(LogAmount.More) => I18n.Config_LogAmount_More(),
                _ => I18n.Config_LogAmount_Less()
            });
    }
}