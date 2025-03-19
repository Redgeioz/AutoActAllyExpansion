using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        static string MethodName = null;
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
        static MethodInfo TargetMethod() => AccessTools.Method(typeof(AutoActBuild), nameof(AutoActBuild.FindNextBuildPosition));

        static void Postfix(AutoActBuild __instance, ref Point __result)
        {
            if (!__instance.owner.IsPC || __result.HasValue())
            {
                return;
            }

            var t = FilterOutAllyTarget.AllyTasks
                .Where(t => __instance.PointChecker(t.Pos))
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

    [HarmonyPostfix, HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnSuccess))]
    static void OnSuccess_Patch(AutoAct __instance)
    {
        if (!__instance.owner.IsPCParty || __instance.owner.IsPC || TaskPos.IsNull())
        {
            return;
        }

        var ai = EClass.pc.ai as AutoAct;
        if (ai.GetType() == __instance.GetType() && __instance.Pos.Equals(TaskPos) && ai.Pos.Equals(TaskPos))
        {
            TaskPos = null;
            ai.Retry();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnCancelOrSuccess))]
    static void OnCancelOrSuccess_Patch(AutoAct __instance)
    {
        if (!__instance.owner.IsPC || TaskPos.IsNull())
        {
            return;
        }

        SetTaskPos(null);
    }

    [HarmonyPatch]
    static class OnChildSuccess_Patch
    {
        static IEnumerable<MethodInfo> TargetMethods()
        {
            var methods = new List<MethodInfo>();
            AutoAct.SubClasses.ForEach(t =>
            {
                if (AccessTools.Method(t, nameof(AutoAct.OnChildSuccess)) is MethodInfo method)
                {
                    methods.Add(method);
                }
            });
            return methods;
        }

        static void Postfix(AutoAct __instance)
        {
            if (!__instance.owner.IsPCParty || TaskPos.IsNull())
            {
                return;
            }

            if (__instance.owner.IsPC)
            {
                SetTaskPos(null);
            }
            else if (__instance.Pos.Equals(TaskPos))
            {
                TaskPos = null;
                (EClass.pc.ai as AutoAct).Retry();
            }
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
                ai.Retry();
                break;
            }
        }
        TaskPos = p;
    }
}