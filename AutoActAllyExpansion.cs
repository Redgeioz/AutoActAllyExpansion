using System.Reflection;
using AutoActMod;
using AutoActMod.Actions;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutoActAllyExpansion;

[BepInPlugin("redgeioz.plugin.AutoActAllyExpansion", "AutoActAllyExpansion", "1.0.0")]
public class AutoActAllyExpansion : BaseUnityPlugin
{
    void Awake()
    {
        Instance = this;
        Settings.keyCode = Config.Bind("Settings", "KeyCode", KeyCode.BackQuote);
        Settings.enable = Config.Bind("Settings", "Enable", true);
        Settings.pickForPC = Config.Bind("Settings", "PickToPc", true);
        Settings._PCWait = Config.Bind("Settings", "PCWait", false);
        Settings.autoWater = Config.Bind("Settings", "AutoWater", true);

        AutoAct.Register(Assembly.GetExecutingAssembly());

        new Harmony("AutoActAllyExpansion").PatchAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(Settings.KeyCode) && EClass.game.HasValue())
        {
            Settings.Enable = !Settings.Enable;
            AutoActMod.AutoActMod.Say(AAAELang.GetText(Settings.Enable ? "on" : "off"));
        }
    }

    internal static void Log(object payload)
    {
#if DEBUG
        Instance.Logger.LogMessage(payload);
#else
        Instance.Logger.LogInfo(payload);
#endif
    }

    internal static void LogWarning(object payload)
    {
        Instance.Logger.LogWarning(payload);
    }

    internal static AutoActAllyExpansion Instance { get; private set; }
}