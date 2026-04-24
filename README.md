# MiMoTTS

`MiMoTTS` 是一个 `ClassIsland` 语音服务插件，用于接入 Xiaomi MiMo 的 `mimo-v2-tts` 与 `mimo-v2.5-tts` 模型。

## 功能

- 在 ClassIsland 中新增 `MiMo TTS` 语音提供方。
- API 地址固定为官方 `https://api.xiaomimimo.com/v1`。
- 支持从下拉框选择 `mimo-v2-tts` 或 `mimo-v2.5-tts`。
- 支持设置 API Key、音色、音频格式。
- 支持风格标签与语速设置。
- `mimo-v2-tts` 会自动在文本前拼接 `<style>...</style>`。
- `mimo-v2.5-tts` 会自动转换为官方文档推荐的括号风格标签写法。
- `mimo-v2.5-tts` 额外支持自然语言控制提示词与唱歌模式。
- 支持本地缓存，减少重复请求。

## 快速配置

1. 在插件设置中填写 `API Key`。
2. 选择模型（`mimo-v2-tts` 或 `mimo-v2.5-tts`）。
3. 选择音色（默认 `mimo_default`）。
4. 可选填写风格（如 `开心`、`东北话` 等）。
5. 使用 `mimo-v2.5-tts` 时，可额外启用自然语言控制或唱歌模式。

## 参考

- GSVIsland: https://github.com/Gamma13-Software/GSVIsland
