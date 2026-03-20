using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;
using MiMoTTS.Models;
using MiMoTTS.Shared;
using SoundFlow;
using SoundFlow.Abstracts;
using SoundFlow.Components;
using SoundFlow.Providers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiMoTTS.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.mimo-tts", "MiMo TTS")]
public class MiMoSpeechService : ISpeechService
{
    public static readonly string MiMoCacheFolderPath = Path.Combine(GlobalConstants.PluginConfigFolder, "MiMoCache");

    private readonly ILogger<MiMoSpeechService> _logger;
    private readonly IAudioService _audioService;
    private readonly AudioEngine _audioEngine;

    private MiMoSpeechSettings _settings = new();
    private Queue<MiMoPlayInfo> PlayingQueue { get; } = new();
    private bool IsPlaying { get; set; }
    private CancellationTokenSource? RequestingCancellationTokenSource { get; set; }
    private MiMoPlayInfo? CurrentPlayInfo { get; set; }
    private SoundPlayer? CurrentSoundPlayer { get; set; }

    public MiMoSpeechService(ILogger<MiMoSpeechService> logger, IAudioService audioService)
    {
        _logger = logger;
        _audioService = audioService;
        _audioEngine = Task.Run(() => audioService.AudioEngine).Result;
        ReloadConfig();
        _logger.LogInformation("初始化了 MiMo TTS 服务。");
    }

    public void ReloadConfig()
    {
        _settings = ConfigureFileHelper.LoadConfig<MiMoSpeechSettings>(
            Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json"));
    }

    public void EnqueueSpeechQueue(string text)
    {
        ReloadConfig();
        var settings = _settings;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var speechText = BuildSpeechText(text, settings.Style, settings.SpeedStyle);

        _logger.LogInformation("使用模型 {Model}、音色 {Voice} 朗读文本：{Text}",
            settings.Model, settings.Voice, speechText);

        var previousCts = RequestingCancellationTokenSource;
        RequestingCancellationTokenSource = new CancellationTokenSource();
        if (previousCts is { IsCancellationRequested: false })
        {
            RequestingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                previousCts.Token,
                RequestingCancellationTokenSource.Token);
        }

        var cachePath = GetCachePath(speechText, settings);
        _logger.LogDebug("语音缓存路径：{CachePath}", cachePath);

        Task<bool>? downloadTask = null;
        if (!File.Exists(cachePath))
        {
            downloadTask = GenerateSpeechAsync(speechText, cachePath, settings, RequestingCancellationTokenSource.Token);
        }

        if (RequestingCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        PlayingQueue.Enqueue(new MiMoPlayInfo(cachePath, new CancellationTokenSource(), downloadTask));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        RequestingCancellationTokenSource?.Cancel();

        try
        {
            CurrentSoundPlayer?.Stop();
            CurrentSoundPlayer?.Dispose();
            CurrentSoundPlayer = null;
        }
        catch
        {
            // 忽略停止播放时的异常
        }

        CurrentPlayInfo?.CancellationTokenSource.Cancel();

        while (PlayingQueue.Count > 0)
        {
            var playInfo = PlayingQueue.Dequeue();
            playInfo.CancellationTokenSource.Cancel();
        }
    }

    private string BuildSpeechText(string text, string style, string speedStyle)
    {
        var styleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(style))
        {
            styleParts.Add(style.Trim());
        }

        if (!string.IsNullOrWhiteSpace(speedStyle) && speedStyle.Trim() != "默认")
        {
            styleParts.Add(speedStyle.Trim());
        }

        if (styleParts.Count == 0)
        {
            return text;
        }

        if (text.TrimStart().StartsWith("<style>", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        return $"<style>{string.Join(" ", styleParts)}</style>{text}";
    }

    private static string EnsureFormatExtension(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "wav";
        }

        return format.Trim().TrimStart('.').ToLowerInvariant();
    }

    private string GetCachePath(string text, MiMoSpeechSettings settings)
    {
        var key = $"{settings.Model}|{settings.Voice}|{settings.AudioFormat}|{settings.Style}|{text}";
        var md5 = MD5.HashData(Encoding.UTF8.GetBytes(key));
        var md5String = md5.Aggregate("", (current, t) => current + t.ToString("x2"));
        var extension = EnsureFormatExtension(settings.AudioFormat);
        var path = Path.Combine(MiMoCacheFolderPath, settings.Voice, $"{md5String}.{extension}");
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ClassIsland", AppBase.AppVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<bool> GenerateSpeechAsync(
        string text,
        string filePath,
        MiMoSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = CreateHttpClient();
            var requestUri = $"{settings.ApiBaseUrl.TrimEnd('/')}/chat/completions";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                request.Headers.Add("api-key", settings.ApiKey);
            }

            var messages = new List<MiMoRequestMessage>();
            if (settings.EnableUserPrompt && !string.IsNullOrWhiteSpace(settings.UserPrompt))
            {
                messages.Add(new MiMoRequestMessage
                {
                    Role = "user",
                    Content = settings.UserPrompt
                });
            }

            messages.Add(new MiMoRequestMessage
            {
                Role = "assistant",
                Content = text
            });

            var requestBody = new MiMoRequestBody
            {
                Model = settings.Model,
                Messages = messages,
                Audio = new MiMoRequestAudio
                {
                    Format = EnsureFormatExtension(settings.AudioFormat),
                    Voice = settings.Voice
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("发送 MiMo TTS 请求到：{RequestUri}", requestUri);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MiMo TTS 请求失败，状态码：{StatusCode}, 内容：{ErrorContent}",
                    response.StatusCode,
                    responseContent);
                return false;
            }

            var result = JsonSerializer.Deserialize<MiMoResponseBody>(responseContent);
            var audioBase64 = result?.Choices?.FirstOrDefault()?.Message?.Audio?.Data;

            if (string.IsNullOrWhiteSpace(audioBase64))
            {
                _logger.LogError("MiMo TTS 响应中未找到音频数据：{ResponseContent}", responseContent);
                return false;
            }

            byte[] audioBytes;
            try
            {
                audioBytes = Convert.FromBase64String(audioBase64);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "MiMo TTS 响应中的音频数据不是合法 Base64。");
                return false;
            }

            await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
            _logger.LogDebug("语音生成并保存到：{FilePath}", filePath);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MiMo TTS 请求已取消。");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 MiMo TTS 请求时发生异常。");
            return false;
        }
    }

    private async Task ProcessPlayerList()
    {
        if (IsPlaying)
        {
            return;
        }

        IsPlaying = true;
        while (PlayingQueue.Count > 0)
        {
            var playInfo = CurrentPlayInfo = PlayingQueue.Dequeue();
            if (playInfo.CancellationTokenSource.IsCancellationRequested)
            {
                continue;
            }

            if (playInfo.DownloadTask != null)
            {
                _logger.LogDebug("等待语音生成完成...");
                var result = await playInfo.DownloadTask;
                if (!result)
                {
                    _logger.LogError("语音 {FilePath} 生成失败。", playInfo.FilePath);
                    continue;
                }

                _logger.LogDebug("语音生成完成。");
            }

            if (!File.Exists(playInfo.FilePath))
            {
                _logger.LogError("语音文件不存在：{FilePath}", playInfo.FilePath);
                continue;
            }

            try
            {
                _logger.LogDebug("开始播放 {FilePath}", playInfo.FilePath);

                CurrentSoundPlayer?.Stop();
                CurrentSoundPlayer?.Dispose();

                using var device = _audioService.TryInitializeDefaultPlaybackDevice();
                device?.Start();

                await using var stream = File.OpenRead(playInfo.FilePath);
                var provider = new StreamDataProvider(_audioEngine, IAudioService.DefaultAudioFormat, stream);
                var player = new SoundPlayer(_audioEngine, IAudioService.DefaultAudioFormat, provider)
                {
                    Volume = (float)ISpeechService.GlobalSettings.SpeechVolume
                };

                CurrentSoundPlayer = player;
                device?.MasterMixer.AddComponent(player);

                var playbackTcs = new TaskCompletionSource<bool>();

                void PlaybackStoppedHandler(object? sender, EventArgs args)
                {
                    playbackTcs.TrySetResult(true);
                }

                player.PlaybackEnded += PlaybackStoppedHandler;
                player.Play();

                await playbackTcs.Task;

                player.PlaybackEnded -= PlaybackStoppedHandler;
                player.Dispose();
                _logger.LogDebug("结束播放 {FilePath}", playInfo.FilePath);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("语音播放已取消：{FilePath}", playInfo.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "无法播放语音。");
            }
        }

        CurrentPlayInfo = null;
        CurrentSoundPlayer = null;
        IsPlaying = false;
    }
}

public class MiMoPlayInfo
{
    public MiMoPlayInfo(string filePath, CancellationTokenSource cancellationTokenSource, Task<bool>? downloadTask = null)
    {
        FilePath = filePath;
        CancellationTokenSource = cancellationTokenSource;
        DownloadTask = downloadTask;
    }

    public string FilePath { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    public Task<bool>? DownloadTask { get; }
}

public class MiMoRequestBody
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "mimo-v2-tts";

    [JsonPropertyName("messages")]
    public List<MiMoRequestMessage> Messages { get; set; } = [];

    [JsonPropertyName("audio")]
    public MiMoRequestAudio Audio { get; set; } = new();
}

public class MiMoRequestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class MiMoRequestAudio
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "mimo_default";
}

public class MiMoResponseBody
{
    [JsonPropertyName("choices")]
    public List<MiMoChoice>? Choices { get; set; }
}

public class MiMoChoice
{
    [JsonPropertyName("message")]
    public MiMoChoiceMessage? Message { get; set; }
}

public class MiMoChoiceMessage
{
    [JsonPropertyName("audio")]
    public MiMoAudio? Audio { get; set; }
}

public class MiMoAudio
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = "";
}
