# 八重樱 3D 桌面助手

基于 Unity 的 3D 角色扮演桌面应用，由 2D Live2D 版本演进而来。

## 功能

- **3D 角色** — MMD 模型导入（礼服八重樱），55 个 BlendShape 表情/口型
- **LLM 流式对话** — 支持 DeepSeek / 千问，角色扮演风格
- **TTS 语音合成** — GPT-SoVITS 驱动，断句合成 + 排队播放
- **对话记忆** — JSON 本地存储，最近 20 轮历史
- **UI Toolkit 界面** — 右侧面板，圆角气泡，匹配 2D 版 CSS 布局

## 技术栈

- Unity 2022.3.62f1c1
- C# (.NET Standard 2.1)
- UI Toolkit (USS flexbox)
- DeepSeek API + SSE streaming
- Blender MCP 资产管线

## 快速开始

1. `git clone` 仓库，用 Unity Hub 打开（编辑器版本 2022.3.62f1c1）
2. `DeepSeek API Key` → 项目内设置面板填入，或 PlayerPrefs 写入
3. 运行 `SampleScene`
4. （可选）启动 GPT-SoVITS TTS 服务

## 项目结构

```
Assets/
├── Scripts/
│   ├── Core/          LLM客户端、TTS客户端、对话编排、记忆管理
│   ├── UI/            聊天面板（UI Toolkit）、设置面板
│   ├── Character/     口型同步
│   ├── Models/        数据模型
│   └── Config/        PlayerPrefs 配置
├── Models/Characters/ 角色模型和贴图
├── Resources/         角色设定提示词
└── Scenes/            SampleScene
```

## 参考

- 2D 版本：`E:\Study_Projects\yuewu_bachong`
- 模型来源：MMD 礼服八重樱（纸月寒绯）
- TTS：GPT-SoVITS v2

## License

个人学习项目
