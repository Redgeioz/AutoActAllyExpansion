using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class FilterOutAllyTarget
{
    public static List<AutoAct> AllyTasks = [];

    public static void UpdateAllyTasks(AutoAct current)
    {
        AllyTasks.Clear();
        EClass.pc.party.members.ForEach(chara =>
        {
            if (chara == current.owner)
            {
                return;
            }

            AutoAct ai;
            if (chara.ai.child.HasValue() && chara.ai.child.GetType() == current.GetType())
            {
                ai = chara.ai.child as AutoAct;
            }
            else if (chara.ai.GetType() == current.GetType())
            {
                ai = chara.ai as AutoAct;
            }
            else
            {
                return;
            }

            if (!ai.IsRunning)
            {
                return;
            }

            AllyTasks.Add(ai);
        });
    }

    [HarmonyPatch]
    static class FindPosFilter
    {
        static IEnumerable<MethodInfo> TargetMethods() => [
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPos)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPosRefToStartPos)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPosInField)),
        ];

        static void Prefix(AutoAct __instance, ref Predicate<Cell> filter)
        {
            if (!__instance.owner.IsPCParty)
            {
                return;
            }

            UpdateAllyTasks(__instance);

            var original = filter;
            filter = c => original(c) && AllyTasks.Find(t => t.Pos.x == c.x && t.Pos.z == c.z).IsNull();
        }
    }

    [HarmonyPatch]
    static class FindCardFilter
    {
        static IEnumerable<MethodInfo> TargetMethods() => [
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindThing)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindChara)),
        ];

        static void Prefix(AutoAct __instance, ref Predicate<Card> filter)
        {
            if (!__instance.owner.IsPCParty)
            {
                return;
            }

            UpdateAllyTasks(__instance);

            var original = filter;
            filter = c => original(c) && AllyTasks.Find(t => t.Pos.Equals(c.pos)).IsNull();
        }
    }

    [HarmonyPatch]
    static class FindNextBuildPosition_Patch
    {
        static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(AutoActBuild), "FindNextBuildPosition");
        }

        static void Prefix(AutoAct __instance, ref Point __result)
        {
            if (!__instance.owner.IsPCParty)
            {
                AllyTasks.Clear();
                return;
            }

            UpdateAllyTasks(__instance);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Brfalse))
                .Advance(1);

            var loadPoint = matcher.Instruction.Clone();
            var cont = matcher.Advance(2).Instruction.Clone();

            return matcher
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(loadPoint),
                    Transpilers.EmitDelegate((Point p) => AllyTasks.Find(t => t.Pos.Equals(p)).IsNull()),
                    new CodeInstruction(cont))
                .InstructionEnumeration();
        }
    }
}