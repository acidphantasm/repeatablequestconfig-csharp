namespace RepeatableQuestConfig.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Generators.RepeatableQuests;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Repeatable;
using SPTarkov.Server.Core.Utils;

[Injectable]
public class GetClientRepeatableQuestsPatch : AbstractPatch
{
    private static ISptLogger<RepeatableQuestController> _logger = null!;
    private static QuestConfig _questConfig = null!;
    private static TimeUtil _timeUtil = null!;
    private static ProfileHelper _profileHelper = null!;
    private static RandomUtil _randomUtil = null!;
    private static EliminationQuestGenerator _eliminationQuestGenerator = null!;
    private static CompletionQuestGenerator _completionQuestGenerator = null!;
    private static ExplorationQuestGenerator _explorationQuestGenerator = null!;

    private static readonly MethodInfo GetRepeatableQuestSubTypeFromProfileMethod = AccessTools.Method(typeof(RepeatableQuestController), "GetRepeatableQuestSubTypeFromProfile");
    private static readonly MethodInfo CanProfileAccessRepeatableQuestsMethod = AccessTools.Method(typeof(RepeatableQuestController), "CanProfileAccessRepeatableQuests");
    private static readonly MethodInfo ProcessExpiredQuestsMethod = AccessTools.Method(typeof(RepeatableQuestController), "ProcessExpiredQuests");
    private static readonly MethodInfo GenerateQuestPoolMethod = AccessTools.Method(typeof(RepeatableQuestController), "GenerateQuestPool");
    private static readonly MethodInfo DrawRandomTraderIdMethod = AccessTools.Method(typeof(RepeatableQuestController), "DrawRandomTraderId");
    private static readonly MethodInfo TryGenerateRandomRepeatableMethod = AccessTools.Method(typeof(RepeatableQuestController), "TryGenerateRandomRepeatable");
    private static readonly MethodInfo GetQuestCountMethod = AccessTools.Method(typeof(RepeatableQuestController), "GetQuestCount");

    public GetClientRepeatableQuestsPatch(
        ISptLogger<RepeatableQuestController> logger,
        QuestConfig questConfig,
        TimeUtil timeUtil,
        ProfileHelper profileHelper,
        RandomUtil randomUtil,
        EliminationQuestGenerator eliminationQuestGenerator,
        CompletionQuestGenerator completionQuestGenerator,
        ExplorationQuestGenerator explorationQuestGenerator)
    {
        _logger = logger;
        _questConfig = questConfig;
        _timeUtil = timeUtil;
        _profileHelper = profileHelper;
        _randomUtil = randomUtil;
        _eliminationQuestGenerator = eliminationQuestGenerator;
        _completionQuestGenerator = completionQuestGenerator;
        _explorationQuestGenerator = explorationQuestGenerator;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));
    }

    [PatchPrefix]
    public static bool Prefix(RepeatableQuestController __instance, ref List<PmcDataRepeatableQuest> __result, MongoId sessionID)
    {
        var returnData = new List<PmcDataRepeatableQuest>();
        var fullProfile = _profileHelper.GetFullProfile(sessionID);
        var pmcData = fullProfile.CharacterData.PmcData;
        var currentTime = _timeUtil.GetTimeStamp();

        foreach (var repeatableConfig in _questConfig.RepeatableQuests)
        {
            var generatedRepeatables = (PmcDataRepeatableQuest)GetRepeatableQuestSubTypeFromProfileMethod.Invoke(__instance, [repeatableConfig, pmcData])!;
            var repeatableTypeLower = repeatableConfig.Name.ToLowerInvariant();

            var canAccessRepeatables = (bool)CanProfileAccessRepeatableQuestsMethod.Invoke(__instance, [repeatableConfig, pmcData])!;
            if (!canAccessRepeatables)
                continue;

            if (currentTime < generatedRepeatables.EndTime - 1)
            {
                returnData.Add(generatedRepeatables);
                continue;
            }

            generatedRepeatables.EndTime = currentTime + repeatableConfig.ResetTime;
            generatedRepeatables.InactiveQuests = [];

            ProcessExpiredQuestsMethod.Invoke(__instance, [generatedRepeatables, pmcData]);

            var questTypePool = (QuestTypePool)GenerateQuestPoolMethod
                .Invoke(__instance, [repeatableConfig, pmcData.Info.Level.GetValueOrDefault(1)])!;

            var questCount = (int)GetQuestCountMethod.Invoke(__instance, [repeatableConfig, fullProfile])!;

            for (var i = 0; i < questCount; i++)
            {
                RepeatableQuest? quest = null;

                if (repeatableConfig.Name == "Daily" && i < 3)
                {
                    var forcedType = i switch
                    {
                        0 => "Elimination",
                        1 => "Completion",
                        2 => "Exploration",
                        _ => null,
                    };

                    if (forcedType is not null && repeatableConfig.Types.Contains(forcedType))
                    {
                        var traderId = (MongoId)DrawRandomTraderIdMethod
                            .Invoke(__instance, [pmcData.TradersInfo, forcedType, repeatableConfig])!;

                        quest = forcedType switch
                        {
                            "Elimination" => _eliminationQuestGenerator.Generate(sessionID, pmcData.Info.Level ?? 0, traderId, questTypePool, repeatableConfig),
                            "Completion" => _completionQuestGenerator.Generate(sessionID, pmcData.Info.Level ?? 0, traderId, questTypePool, repeatableConfig),
                            "Exploration" => _explorationQuestGenerator.Generate(sessionID, pmcData.Info.Level ?? 0, traderId, questTypePool, repeatableConfig),
                            _ => null,
                        };
                    }
                    else
                    {
                        quest = (RepeatableQuest?)TryGenerateRandomRepeatableMethod.Invoke(__instance, [sessionID, pmcData, questTypePool, repeatableConfig]);
                    }
                }
                else
                {
                    quest = (RepeatableQuest?)TryGenerateRandomRepeatableMethod.Invoke(__instance, [sessionID, pmcData, questTypePool, repeatableConfig]);
                }

                if (questTypePool.Types.Count == 0)
                {
                    break;
                }

                if (quest is null)
                {
                    continue;
                }

                quest.Side = Enum.GetName(repeatableConfig.Side);
                generatedRepeatables.ActiveQuests.Add(quest);
            }

            fullProfile.SptData.FreeRepeatableRefreshUsedCount ??= new Dictionary<string, int>();
            fullProfile.SptData.FreeRepeatableRefreshUsedCount[repeatableTypeLower] = 0;

            generatedRepeatables.ChangeRequirement = [];
            foreach (var quest in generatedRepeatables.ActiveQuests)
            {
                generatedRepeatables.ChangeRequirement.TryAdd(
                    quest.Id,
                    new ChangeRequirement
                    {
                        ChangeCost = quest.ChangeCost,
                        ChangeStandingCost = _randomUtil.GetArrayValue(repeatableConfig.StandingChangeCost),
                    });
            }

            generatedRepeatables.FreeChangesAvailable = generatedRepeatables.FreeChanges;

            returnData.Add(new PmcDataRepeatableQuest
            {
                Id = repeatableConfig.Id,
                Name = generatedRepeatables.Name,
                EndTime = generatedRepeatables.EndTime,
                ActiveQuests = generatedRepeatables.ActiveQuests,
                InactiveQuests = generatedRepeatables.InactiveQuests,
                ChangeRequirement = generatedRepeatables.ChangeRequirement,
                FreeChanges = generatedRepeatables.FreeChanges,
                FreeChangesAvailable = generatedRepeatables.FreeChangesAvailable,
            });
        }

        __result = returnData;
        return false;
    }
}