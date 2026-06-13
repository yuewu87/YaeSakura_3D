# 3D 八重樱角色扮演 — 设计文档

## 概述

将 2D 八重樱桌面助手（PyQt5 + Live2D）升级为 3D 版本，基于 Unity 2022.3.62f1c1。核心体验：3D 角色展示 + LLM 流式对话 + GPT-SoVITS TTS 语音合成。桌面悬浮窗 + 全屏 3D 场景双模式。

## 架构

```
Unity Application
├── 3D Scene Layer
│   ├── Character (FBX + Animator + BlendShape)
│   ├── Environment (地面 → 神社场景迭代)
│   └── Camera (悬浮窗: RenderTexture / 全屏: 场景相机)
├── UI Layer (uGUI)
│   ├── ChatPanel (ScrollView 气泡列表 + 输入框)
│   └── SettingsPanel (侧边栏: API/TTS/自动回复)
├── Audio Layer
│   └── TTS Playback (AudioSource 排队播放 + 口型同步)
├── Core Services
│   ├── LLMClient (HttpClient + SSE 流式)
│   ├── TTSClient (WebSocket → GPT-SoVITS)
│   ├── ChatManager (消息状态 + 断句 + 流式分发)
│   └── MemoryManager (JSON 本地存储，后期桥接 Python)
└── Future Bridge (预留)
    └── PythonBackendClient (HTTP localhost，后期连接记忆/视觉服务)
```

## 数据流

```
用户输入 → ChatManager.SendMessage()
  → 拼装 Message History (角色设定 + 历史)
  → LLMClient.SendStreaming()
    → onChunk(text) → ChatPanel 逐字更新当前气泡
    → 遇 。！？ 断句 → 创建新气泡 + TTSClient.Synthesize(sentence)
    → TTS WebSocket → GPT-SoVITS → 接收 WAV → AudioSource.Play()
  → onComplete(fullText) → 最后一句 TTS + 更新对话历史
```

## LLM 客户端

- C# HttpClient + StreamReader 逐行读取 SSE
- OpenAI 兼容格式，支持 DeepSeek / 千问 切换
- 支持 reasoning_content（思考模式）
- Message 结构：role (system/user/assistant) + content
- System prompt 注入角色设定（sakura_prompt，参考 2D 版本）
- 流式回调：onChunk(string text), onComplete(string fullText)
- 非流式方法：SendSync() 用于记忆提取等场景

## TTS 客户端

- 连接 GPT-SoVITS WebSocket `ws://localhost:8770`
- 输入文本 → 服务端合成 → 返回 WAV 音频数据
- AudioSource 排队播放（`_playQueue`）
- 新消息到达 → 5 步渐弱停止 (150ms) + 清空队列，避免爆音
- 口型同步：AudioSource.GetOutputData() 取振幅 → 驱动 BlendShape `Mouth_Open`

## 角色管线

1. Blender 导入 MMD 模型 (pmx)，修正贴图和材质
2. 导出 FBX（含骨架 + BlendShape）
3. Unity 导入，Rig 设为 Humanoid 或 Generic
4. 配置 Animator Controller（Idle 动画循环）
5. 配置 BlendShape 映射：Mouth_Open（口型）、Smile/Surprised/Sad（表情）

## 场景方案

**第1步 — 验证核心流程：**
- 简单地面 + 天空盒
- 角色站中心，镜头正对
- Chat UI 覆盖
- LLM + TTS 直连可用

**第2步 — 场景迭代：**
- 寻找/搭建八重村神社场景（鸟居、樱花树、神社建筑）
- 樱花瓣粒子效果
- 光照烘焙 + 后处理 (Bloom, Color Grading)

## 交互模式

| 模式 | 实现 | 摄像机 |
|------|------|--------|
| 桌面悬浮窗 | Windows DWM 透明窗口 + RenderTexture | 固定机位，角色半身 |
| 全屏 3D 场景 | 标准 Unity 全屏 | 自由视角，鼠标拖动旋转 |

一键切换，场景相机和 UI 布局联动。

## UI 设计

- **对话区域**：ScrollView + ContentSizeFitter，自动滚动到底部
- **用户气泡**：蓝色/深色，右对齐，圆角（右上角为直角）
- **角色气泡**：紫色/暖色，左对齐，圆角（左上角为直角）
- **流式逐字**：每收到 chunk 更新 Text 内容，遇 `。！？` 断句创建新气泡
- **动作行**：括号内容提取为独立居中灰色斜体 Text
- **输入框**：InputField 多行，Enter 发送，Shift+Enter 换行
- **设置侧边栏**：左侧滑出，API 运营商/模型选择/TTS 连接/自动回复超时/API Key
- **标题栏**：角色名 + 模式切换按钮 + 设置按钮
- **API Key**：PlayerPrefs 存储（替代 .env）

## 对话流程细节

1. 注入角色设定到 system message
2. 发送用户消息 → LLM 流式返回
3. 逐字更新当前气泡
4. 遇断句标点 → 当前气泡完成 → 发送 TTS 合成 → 创建新气泡
5. 括号内容（如 `（狐耳微动）`）作为动作行独立显示，不送 TTS
6. 流结束 → 最后一句合成 → 更新历史

## 运营商支持

- DeepSeek（deepseek-chat / deepseek-reasoner）
- 千问（qwen-turbo / qwen-plus）
- 统一 OpenAI 兼容格式，配置切换

## 后续迭代

- 记忆系统（Unity JSON 本地 → HTTP 桥接 Python Vault）
- 视觉感知（摄像头 → 表情识别 → 自动问候）
- 自动回复（空闲超时触发）
- 音乐感知（SMTC 检测切歌）
- 成长系统、故事系统（参考 `00_待实现功能.md`）

## 非功能需求

- GPT-SoVITS 服务需提前启动（独立进程）
- MMD 模型需通过 Blender 转换为 FBX
- Windows 平台优先
- API Key 不提交到版本控制（PlayerPrefs 存储）
