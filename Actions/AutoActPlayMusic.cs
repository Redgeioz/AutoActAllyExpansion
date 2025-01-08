using System.Collections.Generic;
using AutoActAllyExpansion.Patches;
using AutoActMod;
using AutoActMod.Actions;

namespace AutoActAllyExpansion.Actions;

public class AutoActPlayMusic(AI_PlayMusic source) : AutoAct(source)
{
    public AI_PlayMusic Child => child as AI_PlayMusic;
    public override Point Pos => owner.pos;
    public override int MaxRestart => 32;
    public static bool isPCPlaying = false;

    public static AutoActPlayMusic TryCreate(AIAct source)
    {
        if (source is not AI_PlayMusic a) { return null; }
        if (source.owner == pc.Chara)
        {
            isPCPlaying = true;
            AI_PlayMusic.keepPlaying = true;
            AI_PlayMusic.playingTool = pc.Tool;
            AutoActMod.AutoActMod.Say(AALang.GetText("start"));
            if (Settings.Enable)
            {
                pc.party.members.ForEach(chara =>
                {
                    if (!chara.IsPC)
                    {
                        AutoAct_Patch.TrySetAutoActPlayMusic(chara);
                    }
                });
            }
            return null;
        }
        return new AutoActPlayMusic(a);
    }

    public override IEnumerable<Status> Run()
    {
        do
        {
            yield return StartNextTask();
            yield return KeepRunning();
        } while (CanProgress());
        yield break;
    }
}