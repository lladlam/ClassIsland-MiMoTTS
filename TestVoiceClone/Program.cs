using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("=== MiMo VoiceClone API 测试程序 ===\n");

// 配置
var apiKey = "tp-cedjg5nxq1zq42c7ztva8lo1i7iw8vzodh7kd1znpx02k2de";
var apiBaseUrl = "https://token-plan-cn.xiaomimimo.com/v1";

// 测试音频文件
var audioPath = args.Length > 0 ? args[0] : @"D:\lladl\Documents\ClassIsland-MiMoTTS\TestVoiceClone\test_audio.mp3";
var testText = "你好，这是一段音色克隆测试语音。";

Console.WriteLine($"API 地址: {apiBaseUrl}");
Console.WriteLine($"API Key: {apiKey[..8]}...");
Console.WriteLine($"音频文件: {audioPath}");
Console.WriteLine($"测试文本: {testText}");

if (!File.Exists(audioPath))
{
    Console.WriteLine("音频文件不存在！");
    return;
}

// 读取音频文件
Console.WriteLine("\n--- 读取音频文件 ---");
byte[] audioBytes;
try
{
    audioBytes = await File.ReadAllBytesAsync(audioPath);
    Console.WriteLine($"文件大小: {audioBytes.Length / 1024.0:F2} KB");
}
catch (Exception ex)
{
    Console.WriteLine($"读取文件失败: {ex.Message}");
    return;
}

// Base64 编码
var base64Audio = Convert.ToBase64String(audioBytes);
var base64SizeInBytes = Encoding.UTF8.GetByteCount(base64Audio);
Console.WriteLine($"Base64 大小: {base64SizeInBytes / 1024.0:F2} KB");

if (base64SizeInBytes > 10 * 1024 * 1024)
{
    Console.WriteLine("错误: Base64 超过 10MB 限制！");
    return;
}

// MIME 类型
var extension = Path.GetExtension(audioPath).ToLowerInvariant();
var mimeType = extension switch
{
    ".mp3" => "audio/mpeg",
    ".wav" => "audio/wav",
    _ => "audio/wav"
};
var voiceData = $"data:{mimeType};base64,{base64Audio}";

// 构建请求
Console.WriteLine("\n--- 发送 API 请求 ---");
var requestUri = $"{apiBaseUrl}/chat/completions";
Console.WriteLine($"请求地址: {requestUri}");

var messages = new List<RequestMessage>
{
    new() { Role = "assistant", Content = testText }
};

var requestBody = new RequestBody
{
    Model = "mimo-v2.5-tts-voiceclone",
    Messages = messages,
    Audio = new RequestAudio
    {
        Format = "wav",
        Voice = voiceData
    }
};

var json = JsonSerializer.Serialize(requestBody);
Console.WriteLine($"请求体大小: {json.Length / 1024.0:F2} KB");

try
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MiMoTTS-Test", "1.0"));
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
    request.Headers.Add("api-key", apiKey);
    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

    Console.WriteLine("正在调用 API...");
    var startTime = DateTime.Now;

    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    var responseContent = await response.Content.ReadAsStringAsync();

    var elapsed = (DateTime.Now - startTime).TotalSeconds;
    Console.WriteLine($"响应时间: {elapsed:F2} 秒");
    Console.WriteLine($"状态码: {response.StatusCode}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"请求失败！\n{responseContent}");
        return;
    }

    // 解析响应
    var result = JsonSerializer.Deserialize<ResponseBody>(responseContent);
    var audioBase64 = result?.Choices?.FirstOrDefault()?.Message?.Audio?.Data;

    if (string.IsNullOrWhiteSpace(audioBase64))
    {
        Console.WriteLine("响应中未找到音频数据！");
        Console.WriteLine($"响应内容: {responseContent[..Math.Min(500, responseContent.Length)]}...");
        return;
    }

    // 保存音频
    var outputDir = Path.Combine(Environment.CurrentDirectory, "Output");
    Directory.CreateDirectory(outputDir);
    var outputPath = Path.Combine(outputDir, $"voiceclone_test_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

    var outputBytes = Convert.FromBase64String(audioBase64);
    await File.WriteAllBytesAsync(outputPath, outputBytes);

    Console.WriteLine($"\n=== 测试成功！===");
    Console.WriteLine($"输出文件: {outputPath}");
    Console.WriteLine($"音频大小: {outputBytes.Length / 1024.0:F2} KB");
}
catch (Exception ex)
{
    Console.WriteLine($"测试失败: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("\n按任意键退出...");


// 请求模型类
public class RequestBody
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<RequestMessage> Messages { get; set; } = [];

    [JsonPropertyName("audio")]
    public RequestAudio Audio { get; set; } = new();
}

public class RequestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class RequestAudio
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "";
}

public class ResponseBody
{
    [JsonPropertyName("choices")]
    public List<ResponseChoice>? Choices { get; set; }
}

public class ResponseChoice
{
    [JsonPropertyName("message")]
    public ResponseMessage? Message { get; set; }
}

public class ResponseMessage
{
    [JsonPropertyName("audio")]
    public ResponseAudio? Audio { get; set; }
}

public class ResponseAudio
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = "";
}
