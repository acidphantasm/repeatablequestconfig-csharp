namespace RepeatableQuestConfig.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;

[Injectable]
public class UseFreeRefreshPatch : AbstractPatch
{
    private static RqcModConfig _modConfig = null!;
    
    public UseFreeRefreshPatch(RqcModConfig modConfig)
    {
        _modConfig = modConfig;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RepeatableQuestController),"UseFreeRefreshIfAvailable");
    }
    
    [PatchPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!_modConfig.RemoveChangeStandingCost)
        {
            return true;
        }
        
        __result = true;
        return false;
    }
}
