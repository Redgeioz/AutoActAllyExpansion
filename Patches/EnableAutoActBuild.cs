using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActAllyExpansion.Actions;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;
using UnityEngine;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableAutoActBuild
{
    static HashSet<Point> Field = new();
    static bool IsFieldValid = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnStart))]
    static void OnStart_Patch(AutoAct __instance)
    {
        if (__instance is AutoActBuild && __instance.owner.IsPC)
        {
            IsFieldValid = false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatch(typeof(AutoActBuild), nameof(AutoActBuild.Init))]
    static class Init_Patch
    {
        static bool Prefix(AutoActBuild __instance)
        {
            if (__instance.Child.IsNull())
            {
                return false;
            }
            return true;
        }

        static void Postfix(AutoActBuild __instance)
        {
            if (__instance.owner.IsNull() || !__instance.owner.IsPCParty)
            {
                return;
            }

            if (IsFieldValid)
            {
                __instance.field = Field;
            }
            else
            {
                IsFieldValid = true;
                Field = __instance.field;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoActBuild), nameof(AutoActBuild.MaxRestart), MethodType.Getter)]
    static bool MaxRestart_Patch(AutoActBuild __instance, ref int __result)
    {
        if (__instance.owner.HasValue() && !__instance.owner.IsPC)
        {
            __result = 1;
            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaskBuild), nameof(TaskBuild.OnProgressComplete))]
    static IEnumerable<CodeInstruction> OnProgressComplete_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AIAct), "owner")))
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Stfld),
                new CodeMatch(OpCodes.Call))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AIAct), "owner")))
            .MatchEndForward(
                new CodeMatch(OpCodes.Call))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AIAct), "owner")))
            .InstructionEnumeration();
    }
}