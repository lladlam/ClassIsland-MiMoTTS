using CommunityToolkit.Mvvm.ComponentModel;

namespace MiMoTTS.CI1.Models;

public class MiMoSpeechSettings : ObservableRecipient
{
    public const string DefaultApiBaseUrl = "https://api.xiaomimimo.com/v1";
    public const string TokenPlanCnApiBaseUrl = "https://token-plan-cn.xiaomimimo.com/v1";
    public const string TokenPlanSgpApiBaseUrl = "https://token-plan-sgp.xiaomimimo.com/v1";
    public const string TokenPlanAmsApiBaseUrl = "https://token-plan-ams.xiaomimimo.com/v1";
    public const string ModelV2 = "mimo-v2-tts";
    public const string ModelV25 = "mimo-v2.5-tts";
    public const string ModelV25VoiceClone = "mimo-v2.5-tts-voiceclone";

    /// <summary>API 地址下拉框可选项：按量付费官方地址 + Token Plan 各地区地址。</summary>
    public static readonly IReadOnlyList<string> ApiBaseUrlOptions =
    [
        DefaultApiBaseUrl,
        TokenPlanCnApiBaseUrl,
        TokenPlanSgpApiBaseUrl,
        TokenPlanAmsApiBaseUrl
    ];

    /// <summary>
    /// 规范化 API 地址：去除首尾空白与结尾斜杠；为空或不是合法的 http(s) 地址时回退到官方默认地址。
    /// </summary>
    public static string NormalizeApiBaseUrl(string? apiBaseUrl)
    {
        var url = apiBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return DefaultApiBaseUrl;
        }

        return url;
    }

    private string _apiBaseUrl = DefaultApiBaseUrl;
    private string _apiKey = "";
    private string _model = ModelV2;
    private string _voice = "mimo_default";
    private string _audioFormat = "wav";
    private string _style = "";
    private string _speedStyle = "默认";
    private bool _enableUserPrompt = true;
    private string _userPrompt = "请自然、清晰地朗读内容。";
    private bool _enableSingingMode;
    private string _voiceCloneAudioPath = "";

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (value == _apiBaseUrl) return;
            _apiBaseUrl = value;
            OnPropertyChanged();
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (value == _apiKey) return;
            _apiKey = value;
            OnPropertyChanged();
        }
    }

    public string Model
    {
        get => _model;
        set
        {
            if (value == _model) return;
            _model = value;
            OnPropertyChanged();
        }
    }

    public string Voice
    {
        get => _voice;
        set
        {
            if (value == _voice) return;
            _voice = value;
            OnPropertyChanged();
        }
    }

    public string AudioFormat
    {
        get => _audioFormat;
        set
        {
            if (value == _audioFormat) return;
            _audioFormat = value;
            OnPropertyChanged();
        }
    }

    public string Style
    {
        get => _style;
        set
        {
            if (value == _style) return;
            _style = value;
            OnPropertyChanged();
        }
    }

    public string SpeedStyle
    {
        get => _speedStyle;
        set
        {
            if (value == _speedStyle) return;
            _speedStyle = value;
            OnPropertyChanged();
        }
    }

    public bool EnableUserPrompt
    {
        get => _enableUserPrompt;
        set
        {
            if (value == _enableUserPrompt) return;
            _enableUserPrompt = value;
            OnPropertyChanged();
        }
    }

    public string UserPrompt
    {
        get => _userPrompt;
        set
        {
            if (value == _userPrompt) return;
            _userPrompt = value;
            OnPropertyChanged();
        }
    }

    public bool EnableSingingMode
    {
        get => _enableSingingMode;
        set
        {
            if (value == _enableSingingMode) return;
            _enableSingingMode = value;
            OnPropertyChanged();
        }
    }

    public string VoiceCloneAudioPath
    {
        get => _voiceCloneAudioPath;
        set
        {
            if (value == _voiceCloneAudioPath) return;
            _voiceCloneAudioPath = value;
            OnPropertyChanged();
        }
    }
}
