namespace RepeatableQuestConfig.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Locales;
using Color=Spectre.Console.Color;

[Injectable]
public class GenerateRepeatableTemplatePatch : AbstractPatch
{
    private static ISptLogger<RepeatableQuestHelper> _logger = null!;
    private static ServerLocalisationService _serverLocalisationService = null!;
    private static readonly MethodInfo GetRepeatableQuestTemplatesByGroupMethod = AccessTools.Method(typeof(RepeatableQuestHelper), "GetRepeatableQuestTemplatesByGroup");
    
    public GenerateRepeatableTemplatePatch(ISptLogger<RepeatableQuestHelper> logger, ServerLocalisationService serverLocalisationService)
    {
        _logger = logger;
        _serverLocalisationService = serverLocalisationService;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RepeatableQuestHelper), nameof(RepeatableQuestHelper.GenerateRepeatableTemplate));
    }
    
    [PatchPrefix]
    public static bool Prefix(RepeatableQuestHelper __instance, ref RepeatableQuest? __result, RepeatableQuestType type, MongoId traderId, PlayerGroup playerGroup, MongoId sessionId)
    {
        var needsTemplateRedirect = (traderId == Traders.MECHANIC || traderId == Traders.REF) && type == RepeatableQuestType.Elimination;

        if (!needsTemplateRedirect)
            return true;

        var desiredTraderId = Traders.PRAPOR;

        var questData = __instance.GetClonedQuestTemplateForType(type, desiredTraderId);
        if (questData is null)
        {
            _logger.Error(_serverLocalisationService.GetText("repeatable-quest_helper_template_not_found", type));
            __result = null;
            return false;
        }
        
        var templateName = Enum.GetName(type);
        if (templateName is null)
        {
            _logger.Error(_serverLocalisationService.GetText("repeatable-quest_helper_template_name_not_found", type));
            __result = null;
            return false;
        }

        var typeIds = (Dictionary<string, MongoId>)GetRepeatableQuestTemplatesByGroupMethod.Invoke(__instance, [playerGroup])!;
        questData.TemplateId = typeIds.GetValueOrDefault(templateName);
        questData.TraderId = traderId;
        questData.Name = questData.Name.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);
        questData.Note = questData.Note?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.Description = questData.Description.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.SuccessMessageText = questData.SuccessMessageText?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.FailMessageText = questData.FailMessageText?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.StartedMessageText = questData.StartedMessageText?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.ChangeQuestMessageText = questData.ChangeQuestMessageText?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.AcceptPlayerMessage = questData.AcceptPlayerMessage?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.DeclinePlayerMessage = questData.DeclinePlayerMessage?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);
        questData.CompletePlayerMessage = questData.CompletePlayerMessage?.Replace("{traderId}", desiredTraderId).Replace("{templateId}", questData.TemplateId);

        if (questData.QuestStatus is null)
        {
            _logger.Error(_serverLocalisationService.GetText("repeatable-quest_helper_no_status", type));
            __result = null;
            return false;
        }

        questData.QuestStatus.Id = new MongoId();
        questData.QuestStatus.Uid = sessionId;
        questData.QuestStatus.QId = questData.Id;

        __result = questData;
        return false;
    }
}
