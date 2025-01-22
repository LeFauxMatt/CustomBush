using System.Reflection;
using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Utilities;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI.Events;

namespace LeFauxMods.CustomBushAutomate;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        Log.Init(this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var automateInfo = this.Helper.ModRegistry.Get("Pathoschild.Automate");
        var automate = (IMod?)automateInfo?.GetType().GetProperty("Mod")?.GetValue(automateInfo);
        var automateType = automate?.GetType();
        if (automateType is null)
        {
            Log.Warn("Failed to get Automate");
            return;
        }

        var machineManager = automateType.GetField("MachineManager", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(automate);

        var machineGroupFactory = machineManager?.GetType()
            ?.GetProperty("Factory", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(machineManager);

        var automationFactories = machineGroupFactory?.GetType()
            ?.GetField("AutomationFactories", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(machineGroupFactory);

        if (automationFactories is not IList<IAutomationFactory> listOfFactories)
        {
            Log.Warn("Failed to get AutomationFactories");
            return;
        }

        var customBush = new CustomBushIntegration(this.Helper.ModRegistry, true);
        listOfFactories.Insert(0, new AutomationFactory(customBush));
    }
}