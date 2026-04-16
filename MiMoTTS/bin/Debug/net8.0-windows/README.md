# MiMoTTS

`MiMoTTS` 是一个 `ClassIsland` 语音服务插件，用于接入 Xiaomi MiMo 的 `mimo-v2-tts` 模型。

## 功能

- 在 ClassIsland 中新增 `MiMo TTS` 语音提供方。
- 支持设置 API 地址、API Key、模型、音色、音频格式。
- 支持可选风格标签（自动在文本前拼接 `<style>...</style>`）。
- 支持语速设置（默认 / 变快 / 变慢）。
- 支持本地缓存，减少重复请求。

## 快速配置

1. 在插件设置中填写 `API Key`。
2. 保持 API 地址为 `https://api.xiaomimimo.com/v1`（如有变更可自行修改）。
3. 选择音色（默认 `mimo_default`）。
4. 可选填写风格（如 `开心`、`东北话` 等）。

## 参考

- GSVIsland: https://github.com/Gamma13-Software/GSVIsland
