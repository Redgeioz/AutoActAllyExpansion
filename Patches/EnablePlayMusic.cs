using AutoActAllyExpansion.Actions;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;
using UnityEngine;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnablePlayMusic
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AI_PlayMusic), nameof(AI_PlayMusic.CancelKeepPlaying))]
    static void CancelKeepPlaying_Patch()
    {
        if (!AutoActPlayMusic.isPCPlaying)
        {
            return;
        }

        AutoActPlayMusic.isPCPlaying = false;
        EClass.pc.party.members.ForEach(chara =>
        {
            if (chara.IsPC)
            {
                return;
            }

            if (chara.ai is AutoAct a)
            {
                a.Fail();
            }
        });
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AI_PlayMusic), nameof(AI_PlayMusic.Evaluate))]
    static void Evaluate_Patch(AI_PlayMusic __instance, bool success)
    {
        if (__instance.parent is not AutoAct || __instance.owner.IsPC)
        {
            return;
        }

        var thiz = __instance;
        if (success)
        {
            thiz.score = thiz.score * 110 / 100;
        }
        else
        {
            thiz.score = thiz.score / 2 - 20;
        }

        thiz.score = Mathf.Max(thiz.score, 0);
        if (thiz.score > 4)
        {
            thiz.score = 4 + (thiz.score - 4) / 2;
        }

        int num = Mathf.Clamp(thiz.score / 20 + 1, 0, 9);
        thiz.owner.Say(Lang.GetList("music_result")[num], null, null);
        if (thiz.gold > 0)
        {
            thiz.owner.Say("music_reward", thiz.owner, thiz.gold.ToString() ?? "", null);
        }
        QuestMusic questMusic = EClass.game.quests.Get<QuestMusic>();
        if (questMusic.HasValue())
        {
            AutoActAllyExpansion.Log($"[QuestMusic] {__instance.owner.Name} | score: {thiz.score} money: {thiz.gold}");
            questMusic.score += thiz.score;
            questMusic.sumMoney += thiz.gold;
            int num2 = num / 2 - 1;
            if (num > 0)
            {
                SE.Play("clap" + num2.ToString());
            }
        }
    }
}