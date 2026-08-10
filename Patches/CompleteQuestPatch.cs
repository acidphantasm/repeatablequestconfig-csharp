namespace RepeatableQuestConfig.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Repeatable;
using SPTarkov.Server.Core.Utils.Cloners;

[Injectable]
public class CompleteQuestPatch : AbstractPatch
{
    private static ICloner _cloner = null!;
    private static QuestConfig _questConfig = null!;
    private static RepeatableQuestController _repeatableQuestController = null!;
    private static RqcModConfig _modConfig = null!;
    
    public CompleteQuestPatch(ICloner cloner, QuestConfig questConfig, RepeatableQuestController repeatableQuestController, RqcModConfig modConfig)
    {
        _cloner = cloner;
        _questConfig = questConfig;
        _repeatableQuestController = repeatableQuestController;
        _modConfig = modConfig;
    }
    
    private static RepeatableQuest? _completedQuest;
    private static string? _questType;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(QuestController), nameof(QuestController.CompleteQuest));
    }

    [PatchPrefix]
    public static void PatchPrefix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionId)
    {
        if (!_modConfig.InstantRepeatables)
            return;
        
        foreach (var quest in pmcData.RepeatableQuests ?? [])
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
    public static ItemEventRouterResponse Postfix(ItemEventRouterResponse __result, PmcData pmcData, MongoId sessionId)
    {
        if (_completedQuest is null || !_modConfig.InstantRepeatables) 
            return __result;
        
        var replaced = false;
        var newlyGeneratedQuests = new List<PmcDataRepeatableQuest>();
        
        foreach (var quest in pmcData.RepeatableQuests ?? [])
        {
            if (_questType != quest.Name) 
                continue;
            
            var typeToGenerate = quest.Name;
            var repeatableConfig = _questConfig.RepeatableQuests.FirstOrDefault(q => q.Name == typeToGenerate);
            var traverseInstance = Traverse.Create(_repeatableQuestController);
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

            newlyGeneratedQuests.Add(_cloner.Clone(quest));
            
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