using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace AutoActAllyExpansion;

public static class Settings
{
    public static ConfigEntry<bool> enable;
    public static ConfigEntry<bool> pickForPC;
    public static ConfigEntry<bool> _PCWait;
    public static ConfigEntry<KeyCode> keyCode;

    public static bool Enable
    {
        get { return enable.Value; }
        set { enable.Value = value; }
    }

    public static bool PickForPC
    {
        get { return pickForPC.Value; }
        set { pickForPC.Value = value; }
    }

    public static bool PCWait
    {
        get { return _PCWait.Value; }
        set { _PCWait.Value = value; }
    }

    public static KeyCode KeyCode
    {
        get { return keyCode.Value; }
        set { keyCode.Value = value; }
    }

    public static void SetupSettings(UIContextMenu menu)
    {
        menu.AddToggle(AAAELang.GetText("enable"), Enable, v => Enable = v);
        menu.AddToggle(AAAELang.GetText("PCWait"), PCWait, v => PCWait = v);
        menu.AddToggle(AAAELang.GetText("pickForPC"), PickForPC, v => PickForPC = v);
    }
}

public static class AAAELang
{
    static public string GetText(string text)
    {
        string lang = EClass.core.config.lang;
        if (!langData.ContainsKey(lang))
        {
            lang = "EN";
        }
        return langData[lang][text];
    }

    public static Dictionary<string, Dictionary<string, string>> langData = new()
    {
        {
            "CN", new Dictionary<string, string> {
                { "on", "队友自动行动: 开启。"},
                { "off", "队友自动行动: 关闭。"},
                { "enable", "启用队友自动行动　　　　　　　" },
                { "PCWait", "在队友工作时原地等待　　　　　"  },
                { "pickForPC", "将队友拾取的采集物交给PC 　 　 " },
            }
        },
        {
            "ZHTW", new Dictionary<string, string> {
                { "on", "隊友自動行動: 開啟。"},
                { "off", "隊友自動行動: 關閉。"},
                { "enable", "啟用隊友自動行動　　　　　　　" },
                { "PCWait", "在隊友工作時原地等待　　　　　"  },
                { "pickForPC", "將隊友拾取的採集物交給PC 　 　 " },
            }
        },
        {
            "JP", new Dictionary<string, string> {
                { "on", "仲間の自動行動: オン。"},
                { "off", "仲間の自動行動: オフ。"},
                { "enable", "仲間の自動行動　　　　　　　　" },
                { "PCWait", "仲間作業中はその場待機　　　　" },
                { "pickForPC", "仲間収集物をPCに渡す　　 　 　" },
            }
        },
        {
            "EN", new Dictionary<string, string> {
                { "on", "Auto Act For Allies: On."},
                { "off", "Auto Act For Allies: Off."},
                { "enable", "Enable Auto Act For Allies　　　　　　　　　　　　　 　 　" },
                { "PCWait", "Wait In Place When Allies Are Working　　　　　　　"  },
                { "pickForPC", "Transfer Items Collected By Allies to PC　　　　　　" },
            }
        }
    };
}
