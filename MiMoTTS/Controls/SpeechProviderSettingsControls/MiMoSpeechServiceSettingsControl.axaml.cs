using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared.Helpers;
using MiMoTTS.Models;
using MiMoTTS.Shared;
using System.IO;

namespace MiMoTTS.Controls.SpeechProviderSettingsControls;

public partial class MiMoSpeechServiceSettingsControl : SpeechProviderControlBase
{
    public MiMoSpeechSettings Settings { get; set; } = new();

    public MiMoSpeechServiceSettingsControl()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        Settings = ConfigureFileHelper.LoadConfig<MiMoSpeechSettings>(
            Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"));

        Settings.PropertyChanged += (_, _) =>
        {
            ConfigureFileHelper.SaveConfig(
                Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"), Settings);
        };

        DataContext = this;
    }

    private void PasswordChangedHandler(object? sender, TextChangedEventArgs args)
    {
        if (sender is TextBox textBox)
        {
            Settings.ApiKey = textBox.Text ?? "";
        }
    }
}
