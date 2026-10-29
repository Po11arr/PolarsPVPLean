using System;
using System.Reflection;
using HarmonyLib;
using PolarsPVPLean.Settings;
using PolarsPVPLean.Settings.Layouts;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Plugins;

// Define assembly version when compiled by Pulsar
#if !LOCAL_BUILD
[assembly: AssemblyVersion("1.0.5.0")]
[assembly: AssemblyFileVersion("1.0.5.0")]
#endif

namespace PolarsPVPLean;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "PolarsPVPLean";
    private SettingsGenerator settingsGenerator;
    private Harmony harmony;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        try
        {
            var config = Config.Current; // loads the config, or writes it with defaults on first run
            harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MySession.OnUnloading += LeanController.Reset;
            Log.Info("Loaded " + Assembly.GetExecutingAssembly().GetName().Version + (config.Enabled ? "" : ", lean disabled in settings"));
        }
        catch (Exception e)
        {
            // A missing patch target (game update) must not leave half the patches in place.
            LeanController.Disable("Failed to load, the plugin is off", e);
            harmony?.UnpatchAll(Name);
        }
    }

    public void Dispose()
    {
        try
        {
            MySession.OnUnloading -= LeanController.Reset;
            harmony?.UnpatchAll(Name); // only this plugin's patches
            LeanController.Reset();
        }
        catch (Exception e)
        {
            Log.Error("Dispose failed", e);
        }
    }

    public void Update()
    {
    }

    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        try
        {
            settingsGenerator ??= new SettingsGenerator();
            settingsGenerator.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(settingsGenerator.Dialog);
        }
        catch (Exception e)
        {
            Log.Error("Could not open the settings", e);
        }
    }
}
