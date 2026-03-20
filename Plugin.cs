using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiMoTTS.Controls.SpeechProviderSettingsControls;
using MiMoTTS.Services.SpeechService;
using MiMoTTS.Shared;

namespace MiMoTTS;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSpeechProvider<MiMoSpeechService, MiMoSpeechServiceSettingsControl>();
        GlobalConstants.PluginConfigFolder = PluginConfigFolder;
    }
}
