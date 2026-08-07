using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiMoTTS.CI1.Controls.SpeechProviderSettingsControls;
using MiMoTTS.CI1.Services.SpeechService;
using MiMoTTS.CI1.Shared;

namespace MiMoTTS.CI1;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSpeechProvider<MiMoSpeechService, MiMoSpeechServiceSettingsControl>();
        GlobalConstants.PluginConfigFolder = PluginConfigFolder;
    }
}
