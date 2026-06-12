using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private void PasswordChangedHandler(object? sender, TextChangedEventArgs args)
    {
        if (sender is TextBox textBox)
        {
            Settings.ApiKey = textBox.Text ?? "";
        }
    }

    private async void BrowseAudioFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择音色克隆音频样本",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("音频文件")
                {
                    Patterns = new[] { "*.mp3", "*.wav" }
                }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            Settings.VoiceCloneAudioPath = filePath;
        }
    }

    private void ModelSelectionChanged(object? sender, SelectionChangedEventArgs e)
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
