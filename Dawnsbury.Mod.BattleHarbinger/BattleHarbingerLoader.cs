using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.BattleHarbinger;


public static class BattleHarbingerLoader
{

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // enable the debugger in debug mode, and assert that the right version of the game's DLL is being built against
#if DEBUG || DEBUG_V2
        //Debugger.Launch();
#endif
#if DAWNSBURY_V2
        ModManager.AssertV2();
#else
        ModManager.AssertV3();
#endif


    }

    static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat f in feats)
        {
            ModManager.AddFeat(f);
        }
    }

}
