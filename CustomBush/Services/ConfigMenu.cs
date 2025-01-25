using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;

namespace LeFauxMods.CustomBush.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IModHelper helper;
    private readonly IManifest manifest;

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.helper = helper;
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

    public void SetupMenu()
    {
#if DEBUG
        this.gmcm.Register(this.Reset, ConfigHelper.Save);
#else
        this.gmcm.Register(ConfigHelper.Reset, ConfigHelper.Save);
#endif

        this.gmcm.AddComplexOption(new CustomBushOption(this.helper));
    }

    private void Reset()
    {
        ConfigHelper.Reset();
        this.SetupMenu();
    }
}