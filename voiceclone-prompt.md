# 任务：为 ClassIsland-MiMoTTS 插件添加音色克隆（VoiceClone）支持

## 背景

你正在开发一个 ClassIsland 课堂管理软件的插件 **MiMoTTS**，用于接入小米 MiMo 的 TTS API。当前插件已支持 `mimo-v2-tts` 和 `mimo-v2.5-tts` 两个模型，你需要为其添加 `mimo-v2.5-tts-voiceclone` 模型的支持。

**版本号变更为 `1.2.4.0`**（当前为 `1.2.3.13`）。

---

## 一、MiMo 官方文档 — 音色克隆 API 规范

### 模型信息

- **Model ID**: `mimo-v2.5-tts-voiceclone`
- **功能**: 通过传入音频样本，精准复刻目标音色并生成语音
- **限制**: 不支持唱歌模式、预置音色与音色设计
- **音频样本格式**: 仅支持 mp3 和 wav，Base64 编码后不超过 10MB
- **流式输出**: 暂未真正支持低延迟流式（降级为兼容模式，推理完成后一次性返回）

### API 调用规范

- **接口**: `POST https://api.xiaomimimo.com/v1/chat/completions`
- **认证**: Header 中携带 `api-key: {KEY}`
- **目标文本**: 放在 `role: assistant` 的消息中（不可放在 user）
- `role: user` 消息为可选参数，可传入自然语言指令控制风格（不会出现在合成语音中）
- **音频格式**: 流式时请指定 `pcm16`，非流式用 `wav`

### 请求体结构

```json
{
  "model": "mimo-v2.5-tts-voiceclone",
  "messages": [
    {
      "role": "user",
      "content": ""   // 可选：自然语言风格控制指令
    },
    {
      "role": "assistant",
      "content": "要合成的目标文本"
    }
  ],
  "audio": {
    "format": "wav",
    "voice": "data:audio/mpeg;base64,{BASE64_AUDIO}"
  }
}
```

### voice 字段格式

必须携带前缀：`data:{MIME_TYPE};base64,$BASE64_AUDIO`

- `{MIME_TYPE}`: `audio/mpeg`（或 `audio/mp3`）或 `audio/wav`
- `$BASE64_AUDIO`: 音频文件的纯 Base64 编码字符串（不含前缀）

### 风格控制

与其他模型一致，支持：
- 自然语言控制（放在 `role: user` 的 content 中）
- 音频标签控制（放在 `role: assistant` 的 content 中，如 `(东北话)` 开头，`[深呼吸]` 等内嵌）

---

## 二、ClassIsland 插件开发规范

### 插件入口

```csharp
[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSpeechProvider<MiMoSpeechService, MiMoSpeechServiceSettingsControl>();
        GlobalConstants.PluginConfigFolder = PluginConfigFolder;
    }
}
```

### 关键规则

- 插件继承 `PluginBase`，标记 `[PluginEntrance]` 属性
- 通过 `services.AddSpeechProvider<TService, TControl>()` 注册语音服务
- 使用 `PluginConfigFolder` 获取插件配置目录（不要存在插件安装目录下）
- 使用 `ConfigureFileHelper.LoadConfig<T>()` / `SaveConfig<T>()` 读写配置
- 资源引用用 `avares://程序集名称/资源路径`

---

## 三、现有代码结构与实现

### 项目结构

```
MiMoTTS/
├── MiMoTTS.csproj
├── Plugin.cs
├── manifest.yml
├── icon.ico
├── Models/
│   └── MiMoSpeechSettings.cs          # 设置模型（需修改）
├── Services/
│   └── SpeechService/
│       └── MiMoSpeechService.cs       # 核心服务（需修改）
├── Controls/
│   └── SpeechProviderSettingsControls/
│       ├── MiMoSpeechServiceSettingsControl.axaml      # 设置UI（需修改）
│       └── MiMoSpeechServiceSettingsControl.axaml.cs   # 设置UI代码（需修改）
└── Shared/
    └── GlobalConstants.cs
```

### 现有设置模型 (MiMoSpeechSettings.cs)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MiMoTTS.Models;

public class MiMoSpeechSettings : ObservableRecipient
{
    public const string DefaultApiBaseUrl = "https://api.xiaomimimo.com/v1";
    public const string TokenPlanApiBaseUrl = "https://token-plan-cn.xiaomimimo.com/v1";
    public const string ModelV2 = "mimo-v2-tts";
    public const string ModelV25 = "mimo-v2.5-tts";

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

    // ... 各属性的 getter/setter，均调用 OnPropertyChanged()
}
```

### 核心服务 (MiMoSpeechService.cs) 关键逻辑

```csharp
[SpeechProviderInfo("classisland.speech.mimo-tts", "MiMo TTS")]
public class MiMoSpeechService : ISpeechService
{
    // 配置目录下的缓存文件夹
    public static readonly string MiMoCacheFolderPath = 
        Path.Combine(GlobalConstants.PluginConfigFolder, "MiMoCache");

    private MiMoSpeechSettings _settings = new();
    private Queue<MiMoPlayInfo> PlayingQueue { get; } = new();

    // 入口：入队语音请求
    public void EnqueueSpeechQueue(string text) { ... }

    // 构建语音文本（处理风格标签）
    private string BuildSpeechText(string text, MiMoSpeechSettings settings) { ... }

    // V2 模型调用（使用 <style> 标签）
    private async Task<bool> GenerateSpeechV2Async(...) { ... }

    // V2.5 模型调用（使用括号风格标签）
    private async Task<bool> GenerateSpeechV25Async(...) { ... }

    // 播放队列处理
    private async Task ProcessPlayerList() { ... }

    // 缓存路径生成（MD5 哈希）
    private string GetCachePath(string text, string? userPrompt, MiMoSpeechSettings settings) { ... }
}
```

**V2 调用流程**:
1. 构建 messages: 可选 user(prompt) + assistant(带 `<style>` 标签的文本)
2. POST 到 `/chat/completions`，body 包含 model、messages、audio{format, voice}
3. 从响应 `choices[0].message.audio.data` 提取 Base64 音频
4. 解码保存为文件

**V2.5 调用流程**:
1. 构建 messages: 可选 user(prompt) + assistant(带括号风格标签的文本)
2. POST 到 `/chat/completions`
3. 同样提取 Base64 音频并保存

**两个版本共用**:
- 请求头: `api-key: {settings.ApiKey}`
- 请求体使用 `MiMoRequestBody` / `MiMoRequestMessage` / `MiMoRequestAudio` 等模型类
- 响应体使用 `MiMoResponseBody` / `MiMoChoice` / `MiMoChoiceMessage` / `MiMoAudio`
- 所有模型类定义在 MiMoSpeechService.cs 底部

### 缓存 Key 生成逻辑

```csharp
var key = string.Join("|",
    NormalizeModel(settings.Model),
    settings.Voice,
    settings.AudioFormat,
    settings.Style,
    settings.SpeedStyle,
    settings.EnableSingingMode,
    settings.EnableUserPrompt,
    userPrompt ?? "",
    text);
```

---

## 四、你需要完成的工作

### 1. 修改 MiMoSpeechSettings.cs

- 添加新常量: `public const string ModelV25VoiceClone = "mimo-v2.5-tts-voiceclone";`
- 添加新字段和属性:
  - `VoiceCloneAudioPath` (string): 音色克隆音频样本文件路径（用户在设置界面选择的本地音频文件）
- 更新 `EnableSingingMode` 的注释/说明（voiceclone 不支持唱歌）

### 2. 修改 MiMoSpeechService.cs

- 在 `IsV25Model()` 方法中识别新模型（或新增 `IsVoiceCloneModel()` 方法）
- 新增 `GenerateSpeechVoiceCloneAsync()` 方法，逻辑如下:
  - 读取 `settings.VoiceCloneAudioPath` 指向的音频文件
  - 转为 Base64，拼接前缀 `data:{MIME_TYPE};base64,{BASE64}`
  - MIME 类型根据文件扩展名判断: `.mp3` → `audio/mpeg`, `.wav` → `audio/wav`
  - Base64 字符串大小检查（不超过 10MB）
  - 构建请求体: model=`mimo-v2.5-tts-voiceclone`, voice=带前缀的 Base64 字符串
  - messages 中 assistant 放目标文本，可选 user 放风格控制提示
  - **不支持唱歌模式**，如果用户启用了唱歌模式应忽略或提示
  - POST 到 `/chat/completions`，提取 Base64 音频响应并保存
- 修改 `GenerateSpeechAsync()` 的路由逻辑，增加 voiceclone 分支
- 修改 `BuildSpeechText()` 使其兼容 voiceclone（voiceclone 也支持括号风格标签）
- 更新 `GetCachePath()` 的缓存 key 生成逻辑，voiceclone 模型的 key 应包含音频样本的哈希（而非文件路径），确保同一音频样本+同一文本 = 命中缓存

### 3. 修改设置界面

**MiMoSpeechServiceSettingsControl.axaml**:
- 添加"音色克隆音频文件"选择区域：
  - 一个 TextBox 显示当前已选文件路径（只读或 IsReadOnly="True"）
  - 一个"浏览..."按钮，点击后打开文件选择对话框
- 当模型选择为 `mimo-v2.5-tts-voiceclone` 时显示此区域
- 当模型为 voiceclone 时，隐藏/禁用"音色"下拉框、"唱歌模式"开关（因为不支持）
- 在模型下拉框中添加 `mimo-v2.5-tts-voiceclone` 选项

**MiMoSpeechServiceSettingsControl.axaml.cs**:
- 处理文件选择逻辑：使用 Avalonia 的 `IStorageProvider.OpenFilePickerAsync()` 打开文件对话框
  ```csharp
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
          // 获取文件的本地路径
          var filePath = files[0].Path.LocalPath;
          // 将路径写入 settings.VoiceCloneAudioPath
          // 同时更新 TextBox 显示
      }
  }
  ```
- 处理模型切换时的 UI 显隐逻辑：监听 Model 下拉框的 SelectionChanged 事件，当选择 voiceclone 时显示音频文件选择区域、隐藏音色下拉和唱歌模式；切换回其他模型时恢复
- **重要**: `IStorageProvider` 是 Avalonia 11+ 的 API，ClassIsland 基于 Avalonia，确保使用此方式而非旧的 `OpenFileDialog`

### 4. 更新 manifest.yml

```yaml
version: 1.2.4.0
```

### 5. 更新 MiMoTTS.yml（插件索引清单）

```yaml
version: 1.2.4.0
```

### 6. 更新 README.md

在功能列表中添加音色克隆支持的说明，更新快速配置步骤。

---

## 五、技术约束

- **不要使用 NuGet 安装任何新依赖**（项目已有的依赖够用：HttpClient、SoundFlow、System.Text.Json 等）
- 使用现有的 `HttpClient` 发送请求，不要引入额外的 HTTP 库
- Base64 编码使用 `System.Convert.ToBase64String()`
- 音频文件读取使用 `File.ReadAllBytesAsync()`
- 文件扩展名判断使用 `Path.GetExtension()`
- 保持与现有 V2/V2.5 调用代码风格一致
- 所有新增代码需要有适当的日志记录（使用 `_logger`）
- 异常处理要完善，特别是文件不存在、文件过大、API 调用失败等情况

---

## 六、自检清单（完成后逐项检查）

- [ ] `MiMoSpeechSettings.cs` 新增了 `ModelV25VoiceClone` 常量和 `VoiceCloneAudioPath` 属性
- [ ] `MiMoSpeechService.cs` 新增了 `GenerateSpeechVoiceCloneAsync()` 方法
- [ ] 路由逻辑能正确识别 voiceclone 模型并调用对应方法
- [ ] voiceclone 的 voice 字段格式正确：`data:{MIME_TYPE};base64,{BASE64}`
- [ ] Base64 大小不超过 10MB 的检查
- [ ] 唱歌模式在 voiceclone 下被正确忽略
- [ ] 缓存 key 包含音频样本内容的哈希
- [ ] 设置界面能选择音频文件
- [ ] 模型切换时 UI 正确显隐（voiceclone 时隐藏音色下拉和唱歌模式）
- [ ] manifest.yml 版本号为 `1.2.4.0`
- [ ] MiMoTTS.yml 版本号为 `1.2.4.0`
- [ ] README.md 已更新
- [ ] 编译通过，无错误无警告
- [ ] 代码风格与现有代码一致

---

## 七、不要做的事

- **不要** `git push` 或上传到 GitHub（由我本人操作）
- **不要** 安装新的 NuGet 包
- **不要** 修改现有 V2/V2.5 的调用逻辑（只新增，不改动已有行为）
- **不要** 引入新的命名空间或项目引用
