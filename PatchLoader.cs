namespace RepeatableQuestConfig;

using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class PatchLoader(
    IEnumerable<IRuntimePatch> patches)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }
        
        return Task.CompletedTask;
    }
}