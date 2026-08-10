namespace RepeatableQuestConfig;

using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using Color=Spectre.Console.Color;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1337)]
public class RqcOnLoad(
    QuestConfig questConfig,
    RqcModConfig modConfig,
    ISptLogger<RqcOnLoad> logger)
    : IOnLoad
{
    
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // i caps at 2 because 0 = daily, 1 = weekly, 2 = daily savage - edit them all at the same time and reduce code
        for (int i = 0; i <= 2; i++)
        {
            EditRepeatableXp(i);
            EditRepeatableCurrency(i);
            EditRepeatableRep(i);
            EditRepeatableSkillReward(i);
            EditRepeatableSkillPointReward(i);
            EditRepeatableMinLevels(i);
            EditRepeatableCounts(i);
            EditRepeatableTimer(i);
        }

        RunTypeQuestConfig();
        return Task.CompletedTask;
    }

    private void EditRepeatableXp(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];

        for (var i = 0; i < typeOfQuest.RewardScaling.Experience.Count; i++)
        {
            typeOfQuest.RewardScaling.Experience[i] *= modConfig.XpMultiplier;
        }
    }
    
    private void EditRepeatableCurrency(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];

        for (var i = 0; i < typeOfQuest.RewardScaling.Roubles.Count; i++)
        {
            typeOfQuest.RewardScaling.Roubles[i] *= modConfig.CurrencyMultiplier;
        }
        for (var i = 0; i < typeOfQuest.RewardScaling.GpCoins.Count; i++)
        {
            typeOfQuest.RewardScaling.GpCoins[i] *= modConfig.CurrencyMultiplier;
        }
    }
    
    private void EditRepeatableRep(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];

        for (var i = 0; i < typeOfQuest.RewardScaling.Reputation.Count; i++)
        {
            typeOfQuest.RewardScaling.Reputation[i] *= modConfig.RepMultiplier;
        }
    }
    
    private void EditRepeatableSkillReward(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];

        for (var i = 0; i < typeOfQuest.RewardScaling.SkillRewardChance.Count; i++)
        {
            typeOfQuest.RewardScaling.SkillRewardChance[i] *= modConfig.SkillRewardChanceMultiplier;
        }
    }
    
    private void EditRepeatableSkillPointReward(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];

        for (var i = 0; i < typeOfQuest.RewardScaling.SkillPointReward.Count; i++)
        {
            typeOfQuest.RewardScaling.SkillPointReward[i] *= modConfig.SkillPointRewardMultiplier;
        }
    }
    
    private void EditRepeatableMinLevels(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];
        typeOfQuest.MinPlayerLevel = index switch
        {
            0 => modConfig.DailyMinPlayerLevel,
            1 => modConfig.WeeklyMinPlayerLevel,
            2 => modConfig.FenceMinPlayerLevel,
            _ => typeOfQuest.MinPlayerLevel
        };

    }
    
    private void EditRepeatableCounts(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];
        typeOfQuest.NumQuests = index switch
        {
            0 => modConfig.DailyNumberOfQuests,
            1 => modConfig.WeeklyNumberOfQuests,
            2 => modConfig.FenceNumberOfQuests,
            _ => typeOfQuest.NumQuests
        };
    }
    
    private void EditRepeatableTimer(int index)
    {
        var typeOfQuest = questConfig.RepeatableQuests[index];
        typeOfQuest.ResetTime = index switch
        {
            0 => modConfig.DailyResetTimer,
            1 => modConfig.WeeklyResetTimer,
            2 => modConfig.FenceResetTimer,
            _ => typeOfQuest.ResetTime
        };
    }
    
    private void RunTypeQuestConfig()
    {
        var repeatableQuestList = questConfig.RepeatableQuests;
        var dailyQuest = repeatableQuestList[0];
        var weeklyQuest = repeatableQuestList[1];
        var fenceQuest = repeatableQuestList[2];
        
        switch (modConfig)
        {
            case { UseSpecificQuestType: true, UseRandomQuestType: false }:
            {
                // Set Static Types
                var typeOfQuest = GetStaticConfigType();
                if (typeOfQuest == null)
                {
                    logger.Error($"[RQC] [ERROR] Unable to set quest types. Broken config. Loading default quest types instead.");
                    return;
                }
            
                if (modConfig.DebugLogging)
                {
                    logger.LogWithColor($"[RQC] Setting Repeatable Quests Type: {typeOfQuest}.", Color.Magenta);
                }
                SetStaticQuestType(dailyQuest, typeOfQuest);
                SetStaticQuestType(weeklyQuest, typeOfQuest);
                SetStaticFenceType(fenceQuest, typeOfQuest);
                break;
            }
            case { UseRandomQuestType: true, UseSpecificQuestType: false }:
            {
                // Set Dynamic Types
                var dailyType = GetDynamicConfigType(0, "dailyTypes");
                var weeklyType = GetDynamicConfigType(1, "weeklyTypes");
                var fenceType = GetDynamicConfigType(2, "fenceTypes");

                if (dailyType == null || weeklyType == null || fenceType == null)
                {
                    logger.Error($"[RQC] [ERROR] Unable to set quest types. See above error for troubleshooting. No changes have been made to any quest types.");
                }
                else
                {
                    if (modConfig.DebugLogging)
                    {
                        logger.LogWithColor($"[RQC] Setting Daily Repeatable Quests Type: {string.Join(", ", dailyType)}.", Color.Magenta);
                        logger.LogWithColor($"[RQC] Setting Weekly Repeatable Quests Type: {string.Join(", ", weeklyType)}.", Color.Magenta);
                        logger.LogWithColor($"[RQC] Setting fence Repeatable Quests Type: {string.Join(", ", fenceType)}.", Color.Magenta);
                    }
                    SetDynamicQuestType(dailyQuest, dailyType);
                    SetDynamicQuestType(weeklyQuest, weeklyType);
                    SetDynamicFenceType(fenceQuest, fenceType);
                }
                break;
            }
            default:
                logger.Error($"[RQC] [ERROR] No changes made to Repeatable Quest Types. If this is not intentional, validate your config settings.");
                break;
        }
    }
    
    private string? GetStaticConfigType()
    {
        if (modConfig.CompletionOnly)
        {
            return "Completion";
        }
        if (modConfig.ExplorationOnly)
        {
            return "Exploration";
        }
        return modConfig.EliminationOnly ? "Elimination" : null;
    }

    private List<string>? GetDynamicConfigType(int i, string typeString)
    {
        bool validationCheck;
        switch (i)
        {
            case 0:
            {
                validationCheck = ValidateDynamicArray(modConfig.RandomDailyTypes, typeString);
                return validationCheck ? modConfig.RandomDailyTypes : null;
            }
            case 1:
            {
                validationCheck = ValidateDynamicArray(modConfig.RandomWeeklyTypes, typeString);
                return validationCheck ? modConfig.RandomWeeklyTypes : null;
            }
            case 2:
            {
                validationCheck = ValidateDynamicArray(modConfig.RandomFenceTypes, typeString);
                return validationCheck ? modConfig.RandomFenceTypes : null;
            }
        }
        return null;
    }
    
    private void SetStaticQuestType(RepeatableQuestConfig typeQuest, string typeOfQuest)
    {
        typeQuest.Types = [typeOfQuest];

        foreach (var trader in typeQuest.TraderWhitelist)
        {
            trader.QuestTypes = [typeOfQuest];
            if (modConfig.DebugLogging)
            {
                logger.LogWithColor($"[RQC] Set [{typeQuest.Name}] Trader [{trader.Name}] Quest Types to: [{string.Join(", ", trader.QuestTypes)}]. ", Color.Yellow);
            }
        }
    }

    private void SetStaticFenceType(RepeatableQuestConfig typeQuest, string typeOfQuest)
    {
        typeQuest.Types = [typeOfQuest];

        typeQuest.TraderWhitelist[0].QuestTypes = [typeOfQuest];
        if (modConfig.DebugLogging)
        {
            logger.LogWithColor($"[RQC] Set [{typeQuest.Name}] Trader [fence] Quest Types to: [{string.Join(", ", typeQuest.TraderWhitelist[0].QuestTypes)}]. ", Color.Yellow);
        }
    }

    private void SetDynamicQuestType(RepeatableQuestConfig typeQuest, List<string> typeOfQuest)
    {
        typeQuest.Types = typeOfQuest;

        foreach (var trader in typeQuest.TraderWhitelist)
        {
            trader.QuestTypes = typeOfQuest.ToHashSet();
            if (modConfig.DebugLogging)
            {
                logger.LogWithColor($"[RQC] Set [{typeQuest.Name}] Trader [{trader.Name}] Quest Types to: [{string.Join(", ", trader.QuestTypes)}]. ", Color.Yellow);
            }
        }
    }

    private void SetDynamicFenceType(RepeatableQuestConfig typeQuest, List<string> typeOfQuest)
    {
        typeQuest.Types = typeOfQuest;

        typeQuest.TraderWhitelist[0].QuestTypes = typeOfQuest.ToHashSet();
        if (modConfig.DebugLogging)
        {
            logger.LogWithColor($"[RQC] Set [{typeQuest.Name}] Trader [fence] Quest Types to: [{string.Join(", ", typeQuest.TraderWhitelist[0].QuestTypes)}]. ", Color.Yellow);
        }
    }

    private bool ValidateDynamicArray(List<string> typeOfQuest, string typeString)
    {
        var typeCheck = typeOfQuest;
        var typeCheckSize = typeCheck.Count;
        var typeCheckCounter = 0;

        if (typeCheck.Contains("Exploration"))
        {
            ++typeCheckCounter;
        }
        if (typeCheck.Contains("Elimination"))
        {
            ++typeCheckCounter;
        }
        if (typeCheck.Contains("Completion"))
        {
            ++typeCheckCounter;
        }
        if (typeString == "fenceTypes")
        {
            if (typeCheck.Contains("Pickup"))
            {
                ++typeCheckCounter;
            }
        }
        if (typeCheck.All(string.IsNullOrEmpty) || typeCheckSize != typeCheckCounter)
        {
            logger.Error($"[RQC] [ERROR] Validation Failed for: [{typeString}]. Invalid config setting!");
            return false;
        }
        if (modConfig.DebugLogging)
        {
            logger.LogWithColor($"[RQC] Validation Passed for: [{typeString}]. Valid config setting!", Color.Green);
        }
        return true;
    }
}