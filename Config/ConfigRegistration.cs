namespace RepeatableQuestConfig.Config;

using System.Reflection;
using System.Text.Json;
using SPTarkov.Server.Core.DI;

public class ConfigRegistration : IOnDIConstruct
{
    public static async Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken ct)
    {
        RqcModConfig config = await LoadConfigFromDiskAsync(ct);
        serviceCollection.AddSingleton(config);
    }

    private static async Task<RqcModConfig> LoadConfigFromDiskAsync(CancellationToken ct)
    {
        var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "config.json");

        if (!File.Exists(configPath))
        {
            var defaultConfig = new RqcModConfig();
            await SaveConfigToDiskAsync(defaultConfig, configPath, ct);
            return defaultConfig;
        }

        await using FileStream stream = File.OpenRead(configPath);
        var config = await JsonSerializer.DeserializeAsync<RqcModConfig>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        return config ?? new RqcModConfig();
    }

    private static async Task SaveConfigToDiskAsync(RqcModConfig config, string path, CancellationToken ct)
    {
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, new JsonSerializerOptions { WriteIndented = true }, ct);
    }
}
