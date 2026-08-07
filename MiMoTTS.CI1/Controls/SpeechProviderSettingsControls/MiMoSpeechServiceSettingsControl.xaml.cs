using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared.Helpers;
using Microsoft.Win32;
using MiMoTTS.CI1.Models;
using MiMoTTS.CI1.Shared;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace MiMoTTS.CI1.Controls.SpeechProviderSettingsControls;

public partial class MiMoSpeechServiceSettingsControl : SpeechProviderControlBase, INotifyPropertyChanged
{
    public MiMoSpeechSettings Settings { get; set; } = new();
    public IReadOnlyList<string> ModelOptions { get; } =
    [
        MiMoSpeechSettings.ModelV2,
        MiMoSpeechSettings.ModelV25,
        MiMoSpeechSettings.ModelV25VoiceClone
    ];
    public IReadOnlyList<string> ApiBaseUrlOptions { get; } =
    [
        MiMoSpeechSettings.DefaultApiBaseUrl,
        MiMoSpeechSettings.TokenPlanApiBaseUrl
    ];
    public IReadOnlyList<string> SpeedStyleOptions { get; } =
    [
        "默认",
        "变快",
        "变慢"
    ];

    public bool IsV25Model =>
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

    public bool IsVoiceCloneModel =>
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

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

        if (string.IsNullOrWhiteSpace(Settings.ApiBaseUrl) ||
            (Settings.ApiBaseUrl != MiMoSpeechSettings.DefaultApiBaseUrl &&
             Settings.ApiBaseUrl != MiMoSpeechSettings.TokenPlanApiBaseUrl))
        {
            Settings.ApiBaseUrl = MiMoSpeechSettings.DefaultApiBaseUrl;
        }

        if (string.IsNullOrWhiteSpace(Settings.Model) ||
            (Settings.Model != MiMoSpeechSettings.ModelV2 &&
             Settings.Model != MiMoSpeechSettings.ModelV25 &&
             Settings.Model != MiMoSpeechSettings.ModelV25VoiceClone))
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
            OnPropertyChanged(nameof(IsVoiceCloneModel));
        }

        ConfigureFileHelper.SaveConfig(
            Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"), Settings);
    }

    private void PasswordChangedHandler(object sender, System.Windows.RoutedEventArgs args)
    {
        if (sender is PasswordBox passwordBox)
        {
            Settings.ApiKey = passwordBox.Password ?? "";
        }
    }

    private void BrowseAudioFile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择音色克隆音频样本",
            Filter = "音频文件|*.mp3;*.wav|所有文件|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            Settings.VoiceCloneAudioPath = openFileDialog.FileName;
        }
    }

    private void ModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsVoiceCloneModel));
        OnPropertyChanged(nameof(IsV25Model));
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
