using System.Collections.Generic;
using HarmonyLib;
using AutoActMod;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class AutoWater
{
    [HarmonyPostfix, HarmonyPatch(typeof(Scene), nameof(Scene.Init))]
    static void Scene_Init_Patch()
    {
        if (EClass.game.IsNull() || !EClass._zone.IsPCFaction || !Settings.AutoWater)
        {
            return;
        }

        var hasWaterSource = false;
        var range = new List<Point>();
        EClass._map.ForeachPoint(p =>
        {
            hasWaterSource = hasWaterSource || ActDrawWater.HasWaterSource(p);
            if (TaskWater.ShouldWater(p))
            {
                range.Add(p.Copy());
            }
        });

        if (range.Count == 0 || !hasWaterSource)
        {
            return;
        }

        EClass.pc.party.members.ForEach(chara =>
        {
            if (chara.IsPC || chara.ride.HasValue() || chara.host.HasValue())
            {
                return;
            }

            var pos = range.FindMax(p => -Utils.Dist2(p, chara.pos));
            AutoAct_Patch.TrySetAutoActWater(chara, pos);
        });
    }
}