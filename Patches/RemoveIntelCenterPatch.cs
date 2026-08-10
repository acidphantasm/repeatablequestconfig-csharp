namespace RepeatableQuestConfig.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;

[Injectable]
public class RemoveIntelCenterPatch : AbstractPatch
{
    private static RqcModConfig _modConfig = null!;
    
    public RemoveIntelCenterPatch(RqcModConfig modConfig)
    {
        _modConfig = modConfig;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RepeatableQuestController),"PlayerHasDailyScavQuestsUnlocked");
    }
    
    [PatchPrefix]
    public static bool Prefix(ref bool __result, PmcData pmcData)
    {
        if (!_modConfig.RemoveIntelCenterRequirement)
            return true;
        
        if (pmcData.TradersInfo != null && pmcData.TradersInfo.TryGetValue(Traders.FENCE, out var fence) && fence.Unlocked is not null && !fence.Unlocked.Value)
            return true;
        
        __result = true;

        return false;
    }
}
