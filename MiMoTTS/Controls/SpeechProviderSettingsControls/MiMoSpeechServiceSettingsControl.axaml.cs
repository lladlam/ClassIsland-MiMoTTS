using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared.Helpers;
using MiMoTTS.Models;
using MiMoTTS.Shared;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MiMoTTS.Controls.SpeechProviderSettingsControls;

public partial class MiMoSpeechServiceSettingsControl : SpeechProviderControlBase, INotifyPropertyChanged
{
    public MiMoSpeechSettings Settings { get; set; } = new();
    public IReadOnlyList<string> ModelOptions { get; } =
    [
        MiMoSpeechSettings.ModelV2,
        MiMoSpeechSettings.ModelV25
    ];
    public IReadOnlyList<string> SpeedStyleOptions { get; } =
    [
        "默认",
        "变快",
        "变慢"
    ];

    public string FixedApiBaseUrl => MiMoSpeechSettings.DefaultApiBaseUrl;
    public bool IsV25Model =>
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25, StringComparison.OrdinalIgnoreCase);

    public MiMoSpeechServiceSettingsControl()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        Settings = ConfigureFileHelper.LoadConfig<MiMoSpeechSettings>(
            Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"));

        Settings.PropertyChanged += SettingsOnPropertyChanged;

        if (Settings.ApiBaseUrl != MiMoSpeechSettings.DefaultApiBaseUrl)
        {
            Settings.ApiBaseUrl = MiMoSpeechSettings.DefaultApiBaseUrl;
        }

        if (string.IsNullOrWhiteSpace(Settings.Model) ||
            (Settings.Model != MiMoSpeechSettings.ModelV2 && Settings.Model != MiMoSpeechSettings.ModelV25))
        {
            Settings.Model = MiMoSpeechSettings.ModelV2;
        }

        if (string.IsNullOrWhiteSpace(Settings.SpeedStyle))
        {
            Settings.SpeedStyle = "默认";
        }

        DataContext = this;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MiMoSpeechSettings.Model))
        {
            OnPropertyChanged(nameof(IsV25Model));
        }

        ConfigureFileHelper.SaveConfig(
            Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"), Settings);
    }

    private void PasswordChangedHandler(object? sender, TextChangedEventArgs args)
    {
        if (sender is TextBox textBox)
        {
            Settings.ApiKey = textBox.Text ?? "";
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
