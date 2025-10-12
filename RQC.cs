using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace _repeatableQuestConfig;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.acidphantasm.repeatablequestconfig";
    public override string Name { get; init; } = "Repeatable Quest Config";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)]
public class RQC(
    Patches patches,
    ConfigServer configServer,
    ISptLogger<RQC> logger,
    ModHelper modHelper)
    : IOnLoad
{
    private ModConfig _modConfig;
    private QuestConfig _questConfig = configServer.GetConfig<QuestConfig>();
    
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");
        
        // i caps at 2 because 0 = daily, 1 = weekly, 2 = daily savage - edit them all at the same time and reduce code
        for (int i = 0; i <= 2; i++)
        {
            EditRepeatableXP(i);
            EditRepeatableCurrency(i);
            EditRepeatableRep(i);
            EditRepeatableSkillReward(i);
            EditRepeatableSkillPointReward(i);
            EditRepeatableMinLevels(i);
            EditRepeatableCounts(i);
            EditRepeatableTimer(i);
        }
        return Task.CompletedTask;
    }
    
    private void EditRepeatableXP(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];

        for (int i = 0; i < typeOfQuest.RewardScaling.Experience.Count; i++)
        {
            typeOfQuest.RewardScaling.Experience[i] *= _modConfig.XpMultiplier;
        }
    }
    
    private void EditRepeatableCurrency(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];

        for (int i = 0; i < typeOfQuest.RewardScaling.Roubles.Count; i++)
        {
            typeOfQuest.RewardScaling.Roubles[i] *= _modConfig.CurrencyMultiplier;
        }
        for (int i = 0; i < typeOfQuest.RewardScaling.GpCoins.Count; i++)
        {
            typeOfQuest.RewardScaling.GpCoins[i] *= _modConfig.CurrencyMultiplier;
        }
    }
    
    private void EditRepeatableRep(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];

        for (int i = 0; i < typeOfQuest.RewardScaling.Reputation.Count; i++)
        {
            typeOfQuest.RewardScaling.Reputation[i] *= _modConfig.RepMultiplier;
        }
    }
    
    private void EditRepeatableSkillReward(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];

        for (int i = 0; i < typeOfQuest.RewardScaling.SkillRewardChance.Count; i++)
        {
            typeOfQuest.RewardScaling.SkillRewardChance[i] *= _modConfig.SkillRewardChanceMultiplier;
        }
    }
    
    private void EditRepeatableSkillPointReward(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];

        for (int i = 0; i < typeOfQuest.RewardScaling.SkillPointReward.Count; i++)
        {
            typeOfQuest.RewardScaling.SkillPointReward[i] *= _modConfig.SkillPointRewardMultiplier;
        }
    }
    
    private void EditRepeatableMinLevels(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];
        if (index == 0) typeOfQuest.MinPlayerLevel = _modConfig.DailyMinPlayerLevel;
        if (index == 1) typeOfQuest.MinPlayerLevel = _modConfig.WeeklyMinPlayerLevel;
        if (index == 2) typeOfQuest.MinPlayerLevel = _modConfig.FenceMinPlayerLevel;

    }
    
    private void EditRepeatableCounts(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];
        if (index == 0) typeOfQuest.NumQuests = _modConfig.DailyNumberOfQuests;
        if (index == 1) typeOfQuest.NumQuests = _modConfig.WeeklyNumberOfQuests;
        if (index == 2) typeOfQuest.NumQuests = _modConfig.FenceNumberOfQuests;
    }
    
    private void EditRepeatableTimer(int index)
    {
        var typeOfQuest = _questConfig.RepeatableQuests[index];
        if (index == 0) typeOfQuest.ResetTime = _modConfig.DailyResetTimer;
        if (index == 1) typeOfQuest.ResetTime = _modConfig.WeeklyResetTimer;
        if (index == 2) typeOfQuest.ResetTime = _modConfig.FenceResetTimer;
    }
}