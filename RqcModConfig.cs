namespace RepeatableQuestConfig;

public class RqcModConfig {
    public bool InstantRepeatables { get; set; } = true;
    public float XpMultiplier { get; set; } = 1;
    public float CurrencyMultiplier {  get; set; } = 1;
    public float RepMultiplier {  get; set; } = 1;
    public float SkillRewardChanceMultiplier {  get; set; } = 1;
    public float SkillPointRewardMultiplier {  get; set; } = 1;
    public int DailyMinPlayerLevel {  get; set; } = 5;
    public int DailyNumberOfQuests {  get; set; } = 3;
    public long DailyResetTimer {  get; set; } = 86400;
    public int WeeklyMinPlayerLevel {  get; set; } = 15;
    public int WeeklyNumberOfQuests {  get; set; } = 1;
    public long WeeklyResetTimer {  get; set; } = 604800;
    public int FenceMinPlayerLevel {  get; set; } = 1;
    public int FenceNumberOfQuests {  get; set; } = 1;
    public long FenceResetTimer {  get; set; } = 86400;
    public bool RemoveIntelCenterRequirement { get; set; } = true;
    public bool UseSpecificQuestType { get; set; } = false;
    public bool CompletionOnly { get; set; } = false;
    public bool ExplorationOnly { get; set; } = false;
    public bool EliminationOnly { get; set; } = false;
    public bool UseRandomQuestType { get; set; } = true;
    public List<string> RandomDailyTypes { get; set; } =
    [
        "Exploration",
        "Elimination",
        "Completion"
    ];
    public List<string> RandomWeeklyTypes { get; set; } =
    [
        "Exploration",
        "Elimination",
        "Completion"
    ];
    public List<string> RandomFenceTypes { get; set; } =
    [
        "Exploration",
        "Elimination",
        "Completion",
        "Pickup"
    ];
    public bool DebugLogging { get; set; } = false;
}
