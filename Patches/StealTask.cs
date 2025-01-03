using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class StealTask
{
    public static Point TaskPos = null;

    [HarmonyPatch]
    static class FindPos_Patch
    {
        static Predicate<Cell> OriginalFilter = null;

        static IEnumerable<MethodInfo> TargetMethods() => [
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPos)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPosRefToStartPos)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindPosInField)),
        ];

        [HarmonyPriority(Priority.High)]
        static void Prefix(AutoAct __instance, ref Predicate<Cell> filter)
        {
            OriginalFilter = filter;
        }

        static void Postfix(AutoAct __instance, ref Point __result)
        {
            if (!__instance.owner.IsPC || __result.HasValue())
            {
                return;
            }

            var t = FilterOutAllyTarget.AllyTasks
                .Where(t => OriginalFilter(t.Pos.cell))
                .ToList()
                .FindMax(t => -__instance.CalcDist2(t.Pos));
            if (t.HasValue())
            {
                var p = t.Pos.Copy();
                __result = p;
                SetTaskPos(p);
            }
        }
    }

    [HarmonyPatch]
    static class FindCard_Patch
    {
        static String MethodName = null;
        static Predicate<Card> OriginalFilter = null;

        static IEnumerable<MethodInfo> TargetMethods() => [
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindThing)),
            AccessTools.Method(typeof(AutoAct), nameof(AutoAct.FindChara)),
        ];

        [HarmonyPriority(Priority.High)]
        static void Prefix(AutoAct __instance, ref Predicate<Card> filter, MethodBase __originalMethod)
        {
            OriginalFilter = filter;
            MethodName = __originalMethod.Name;
        }

        static void Postfix(AutoAct __instance, ref Card __result)
        {
            if (!__instance.owner.IsPC || __result.HasValue())
            {
                return;
            }

            static Card FindCard(Point p)
            {
                if (MethodName == nameof(AutoAct.FindThing))
                {
                    return p.FindThing(t => OriginalFilter(t));
                }
                else
                {
                    return p.FindChara(c => OriginalFilter(c));
                }
            }

            var t = FilterOutAllyTarget.AllyTasks
                .Where(t => FindCard(t.Pos).HasValue())
                .ToList()
                .FindMax(t => -__instance.CalcDist2(t.Pos));
            if (t.HasValue())
            {
                var p = t.Pos.Copy();
                __result = FindCard(p);
                SetTaskPos(p);
            }
        }
    }

    [HarmonyPatch]
    static class FindNextBuildPosition_Patch
    {
        static Predicate<Point> Filter = null;

        static MethodInfo TargetMethod() => AccessTools.Method(typeof(AutoActBuild), "FindNextBuildPosition");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Newobj),
                    new CodeMatch(OpCodes.Stloc_S))
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    Transpilers.EmitDelegate((Predicate<Point> f) => Filter = f)
                )
                .InstructionEnumeration();
        }

        static void Postfix(AutoActBuild __instance, ref Point __result)
        {
            if (!__instance.owner.IsPC || __result.HasValue())
            {
                return;
            }

            var t = FilterOutAllyTarget.AllyTasks
                .Where(t => Filter(t.Pos))
                .ToList()
                .FindMax(t => -__instance.CalcDist2(t.Pos)) as AutoActBuild;
            if (t.HasValue())
            {
                var p = t.Pos.Copy();
                __result = p;
                __instance.Child.recipe._dir = t.Child.recipe._dir;
                SetTaskPos(p);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnSuccess))]
    static void OnSuccess_Patch(AutoAct __instance)
    {
        if (!__instance.owner.IsPCParty || TaskPos.IsNull())
        {
            return;
        }

        if (__instance.owner.IsPC)
        {
            if (!__instance.Pos.Equals(TaskPos))
            {
                TaskPos = null;
                return;
            }

            SetTaskPos(null);
            return;
        }

        var ai = EClass.pc.ai as AutoAct;
        if (ai.GetType() == __instance.GetType() && __instance.Pos.Equals(TaskPos) && ai.Pos.Equals(TaskPos))
        {
            TaskPos = null;
            ai.child?.Success();
        }
    }

    static void SetTaskPos(Point p)
    {
        if (TaskPos.IsNull())
        {
            TaskPos = p;
            return;
        }

        var current = EClass.pc.ai.GetType();
        foreach (var chara in EClass.pc.party.members)
        {
            if (chara.IsPC || !chara.ai.IsRunning)
            {
                continue;
            }

            if (chara.ai is AutoAct ai && ai.GetType() == current && ai.Pos.Equals(TaskPos))
            {
                ai.child?.Success();
                break;
            }
        }
        TaskPos = p;
    }
}