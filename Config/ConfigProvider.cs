namespace RepeatableQuestConfig.Config;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;

[Injectable(InjectionType.Singleton)]
public class ConfigProvider(RqcModConfig config, RqcOnLoad onLoad) : IConfigEditorConfigProvider
{
    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        var metadata = new ModMetadata();
        yield return ConfigEditorConfigRegistration.Create(
        metadata.ModGuid,
        metadata.Name,
        config,
        Path.Combine("user", "mods", "acidphantasm-repeatablequestconfig", "config.json")
        ) with
        {
            OnAppliedToRuntimeAsync = CallbackMethod
        };
    }

    private ValueTask CallbackMethod(object modifiedConfig, CancellationToken ct)
    {
        onLoad.OnLoadAsync(ct);
        return ValueTask.CompletedTask;
    }
}
