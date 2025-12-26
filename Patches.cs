using System.Globalization;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Repeatable;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _repeatableQuestConfig;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class Patches(
    ModHelper modHelper)
    : IOnLoad
{
    public ModConfig _modConfig;
    
    public Task OnLoad()
    { 
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");
        
        if (_modConfig.InstantRepeatables) new CompleteQuestPatch().Enable();
        if (_modConfig.RemoveIntelCenterRequirement) new RemoveIntelCenterPatch().Enable();
        return Task.CompletedTask;
    }
}

public class CompleteQuestPatch : AbstractPatch
{
    private static RepeatableQuest? _completedQuest;
    private static string? _questType;
    
    protected override MethodBase GetTargetMethod()
    {
        return typeof(QuestController).GetMethod(nameof(QuestController.CompleteQuest));
    }

    [PatchPrefix]
    public static void PatchPrefix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionId)
    {
        foreach (var quest in pmcData.RepeatableQuests)
        {
            var currentCompletedQuest = quest.ActiveQuests.FirstOrDefault(q => q.Id == request.QuestId);
            if (currentCompletedQuest is not null) 
            {
                _completedQuest = quest.ActiveQuests.FirstOrDefault(q => q.Id == request.QuestId);
                _questType = quest.Name;
                return;
            }
        }
    }
    
    [PatchPostfix]
    public static ItemEventRouterResponse Postfix(ItemEventRouterResponse __result, PmcData pmcData, CompleteQuestRequestData request, MongoId sessionId)
    {
        if (_completedQuest is null) return __result;
        
        var replaced = false;
        var newlyGeneratedQuests = new List<PmcDataRepeatableQuest>();
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<QuestController>>();
        
        foreach (var quest in pmcData.RepeatableQuests)
        {
            if (_questType != quest.Name) continue;
            
            var typeToGenerate = quest.Name;
            var repeatableConfig = ServiceLocator.ServiceProvider.GetService<ConfigServer>().GetConfig<QuestConfig>().RepeatableQuests.FirstOrDefault(q => q.Name == typeToGenerate);
            Traverse traverseInstance = Traverse.Create(ServiceLocator.ServiceProvider.GetService<RepeatableQuestController>());
            var questPool = traverseInstance.Method("GenerateQuestPool", repeatableConfig, pmcData.Info.Level).GetValue<QuestTypePool>();
            var replacementRepeatable = traverseInstance.Method("AttemptToGenerateRepeatableQuest", sessionId, pmcData, questPool, repeatableConfig).GetValue<RepeatableQuest?>();

            if (replacementRepeatable is not null)
            {
                replacementRepeatable.Side = repeatableConfig.Side.ToString();
                quest.ActiveQuests.Add(replacementRepeatable);
                
                var newRequirements = new ChangeRequirement()
                {
                    ChangeCost = replacementRepeatable.ChangeCost,
                    ChangeStandingCost = replacementRepeatable.ChangeStandingCost ?? 0,
                };

                quest.ChangeRequirement[replacementRepeatable.Id] = newRequirements;

                replaced = true;
            }

            newlyGeneratedQuests.Add(cloner.Clone(quest));
            
            if (replaced)
            {
                __result.ProfileChanges[sessionId].RepeatableQuests = newlyGeneratedQuests;
                _completedQuest = null;
                return __result;
            }
        }

        return __result;
    }
}

public class RemoveIntelCenterPatch : AbstractPatch
{
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RepeatableQuestController),"PlayerHasDailyScavQuestsUnlocked");
    }
    
    [PatchPrefix]
    public static bool Prefix(ref bool __result, PmcData pmcData)
    {
        if (pmcData.TradersInfo.TryGetValue(Traders.FENCE, out var fence) && fence.Unlocked is not null && !fence.Unlocked.Value)
        {
            return true;
        }
        
        __result = true;

        return false;
    }
}

public class ModConfig
{
    public bool InstantRepeatables  { get; set; }
    
    public float XpMultiplier {  get; set; }
    public float CurrencyMultiplier {  get; set; }
    public float RepMultiplier {  get; set; }
    public float SkillRewardChanceMultiplier {  get; set; }
    public float SkillPointRewardMultiplier {  get; set; }
    
    public int DailyMinPlayerLevel {  get; set; }
    public int DailyNumberOfQuests {  get; set; }
    public long DailyResetTimer {  get; set; }
    
    public int WeeklyMinPlayerLevel {  get; set; }
    public int WeeklyNumberOfQuests {  get; set; }
    public long WeeklyResetTimer {  get; set; }
    
    public int FenceMinPlayerLevel {  get; set; }
    public int FenceNumberOfQuests {  get; set; }
    public long FenceResetTimer {  get; set; }
    public bool RemoveIntelCenterRequirement  { get; set; }
}
