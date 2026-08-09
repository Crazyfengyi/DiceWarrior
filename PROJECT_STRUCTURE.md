# DiceWarrior 项目结构说明

> 本文档是项目协作基线。后续处理本项目的每个任务前，先读取本文档，再根据任务需要检查具体文件。
>
> 生成日期：2026-08-09  
> 适用工程：`Project/DiceWarrior`  
> 文档状态：基于当前工作区静态勘察，代码和资源变化后需要同步更新

## 一、仓库布局

```text
仓库根目录/
├─ Project/
│  └─ DiceWarrior/          # Unity 工程根目录
├─ 策划/                    # 策划资料：数据表、流程说明、UI 设计等
├─ 资源/                    # 原始美术和设计资源
├─ PROJECT_STRUCTURE.md     # 本文档；任务开始前优先读取
└─ .gitignore
```

Unity 工程目录包含 `Assets`、`Packages`、`ProjectSettings`、`LubanTool` 和构建产物 `Bundles`。`Library`、`Temp`、`Logs`、`obj`、`UserSettings` 属于 Unity 生成或本机状态目录，不作为业务源码结构依据。

## 二、技术基线

- Unity：`2022.3.14f1c1`。
- UI：UGUI、TextMeshPro；项目自研 UI 基类集中在 `Assets/YangTools/Scripts/Core/YangUGUI`。
- 资源加载与热更新：YooAsset `3.0.2-beta`，默认包为 `DefaultPackage`；当前存在 Windows 模拟构建/运行产物。
- 异步：项目内置 UniTask 及其 YooAsset、Addressables、DOTween、TextMeshPro 扩展程序集。
- 动画/骨骼：Spine runtime 和 Spine Examples 位于 `Assets/Spine`、`Assets/Spine Examples`。
- 数据：Luban 数据源、生成代码和 JSON 位于 `LubanTool`；Unity 侧数据支持位于 `Assets/YangTools/LubanData`。
- 编辑器集成：Unity MCP、YangTools EditorStudio、Sirenix Odin Inspector。
- 主要包：UGUI、Timeline、2D Sprite、Visual Scripting、AI、AssetBundle 等，完整版本以 `Project/DiceWarrior/Packages/manifest.json` 和 `packages-lock.json` 为准。

## 三、Assets 结构

```text
Assets/
├─ Scripts/                 # 游戏业务代码
│  ├─ CommonScript/         # 通用按钮、特效销毁、文本和小组件
│  ├─ InitPatchLogic/       # 启动、补丁、YooAsset 状态机
│  ├─ Manager/              # 游戏管理器、背包、音频、事件
│  ├─ SDK/                  # 平台抽象及默认、抖音、微信实现
│  └─ UIWindow/             # 游戏窗口、窗口数据和窗口内 UI 组件
├─ YangTools/               # 自研通用框架、编辑器工具、战斗示例和数据支持
│  ├─ Scripts/Core/         # 资源、表格、语言、存档、任务、计时器、UI 等核心能力
│  ├─ Scripts/Function/     # 战斗、技能、Buff、武器、怪物、轮盘等可复用功能
│  ├─ Scripts/Window/       # 通用窗口组件
│  ├─ EditorStudio/         # 编辑器工具与自动化
│  ├─ LubanData/            # Luban 运行库及生成数据代码
│  └─ Plugins/              # UniTask、DOTween 等随项目维护的插件源码
├─ AssetBundle/             # 运行时资源、UI 窗口 Prefab、字体、图集和美术资源
├─ Scenes/                  # 项目场景
├─ Spine/                   # Spine runtime、editor 和模块
├─ Spine Examples/          # Spine 官方示例，不应与游戏业务混用
├─ Plugins/                 # 第三方插件，当前包含 Sirenix
├─ Editor/                  # 项目级编辑器资源/设置
├─ StreamingAssets/         # StreamingAssets 运行时资源
└─ TextMesh Pro/            # TMP 资源
```

## 四、运行流程与场景

Build Settings 当前启用的场景顺序为：

1. `Assets/Scenes/Enter.unity`：入口场景。
2. `Assets/Scenes/Init.unity`：初始化、资源包和补丁流程。
3. `Assets/Scenes/MainGame.unity`：主要游戏场景。

启动及热更新相关代码位于 `Assets/Scripts/InitPatchLogic`，其中 `FsmNode` 下的状态节点负责初始化包、请求版本、更新清单、创建下载器和进入游戏。YooAsset 资源管理实现位于 `Assets/YangTools/Scripts/Core/ResourceManager`。

## 五、业务模块概览

- `Scripts/UIWindow`：按窗口拆分业务 UI，例如骰子战斗、骰子强化、主界面、游戏界面、结果、设置、奖励和道具窗口。窗口通常继承 `UGUIPanelBase<T>`，并配套窗口数据类。
- `Scripts/Manager`：游戏级管理逻辑，包括 `GameManager`、背包、音频和事件管理。
- `Scripts/SDK`：通过 `IPlatform` 抽象平台差异，当前有 Default、DouYin、WeiXin 实现。
- `YangTools/Scripts/Core`：基础设施层，提供 `YangUIManager`、`ResourceManager`、`GameTableManager`、语言、存档、任务、对象池、红点和工具管理等能力。
- `YangTools/Scripts/Function/Battle`：通用战斗能力，包含角色属性、生命、Buff、技能效果和武器定义/控制。
- `LubanTool` 与 `Assets/YangTools/LubanData`：策划表到 C# / JSON 的数据生成链路。修改表结构或字段时，需要关注源表、生成代码和输出 JSON 的同步关系。

## 六、程序集与依赖边界

项目业务侧明确存在的程序集包括：

- `Assets/YangTools/YangTool.asmdef`
- `Assets/Scripts/InitPatchLogic/UniMachine/Runtime/UniFramework.Machine.asmdef`
- `Assets/YangTools/Plugins/UniTask/Runtime/UniTask.asmdef` 及其扩展程序集
- `Assets/Spine/Runtime/spine-unity.asmdef`
- `Assets/Spine/Editor/spine-unity-editor.asmdef`
- Spine Examples 的 runtime/editor 程序集

未发现覆盖整个 `Assets/Scripts` 的独立业务 asmdef，因此新增业务脚本前应先确认所在目录的程序集归属和现有引用关系，不要随意新增程序集或改变第三方程序集。

## 七、现有架构信号与约定

- UI 采用“窗口脚本 + 数据对象 + Prefab”的组织方式，优先复用现有窗口基类和 UI 管理器。
- 事件和回调较常见，既有 C# `Action` / `event`，也有项目事件管理器；新增事件时应保持调用方所在模块的现有风格。
- 项目同时存在管理器、单例/MonoSingleton、状态机和资源管理器模式；修改前先阅读相邻模块，避免引入新的全局入口。
- 数据配置主要走 Luban 生成数据和少量 ScriptableObject；不要将已有表格数据重复迁移到新的配置体系。
- 运行时资源优先通过 YooAsset/现有资源管理器加载，避免在业务代码中直接引入另一套加载路径。
- UI GameObject 和 Prefab 优先在 Unity 编辑器/现有资源中配置；不要为了方便在运行时代码中大规模创建 UI 层级。
- C# 注释使用中文；方法前保留必要的中文说明，保持现有代码风格。

## 八、风险与注意事项

- `Assets/YangTools` 同时包含框架、示例、编辑器工具和插件源码，修改前必须确认目标文件是否为通用模块，避免把游戏专用逻辑写入框架层。
- `Assets/Spine Examples` 包含大量官方示例场景和脚本，统计或检索业务代码时应排除该目录。
- `Library/PackageCache` 中的 asmdef 和代码是缓存/依赖，不应直接修改；以 `Packages` 和 `Assets` 中的项目配置为准。
- `Bundles` 是构建输出，不应手工修改；资源变更后应通过现有 YooAsset 构建流程重新生成。
- 当前未发现明确的项目级自动化测试目录；涉及核心逻辑时，需要至少做静态检查，并在条件允许时通过 Unity Editor 验证。
- 生成代码目录和原始数据目录存在耦合，不能只改生成结果来长期解决数据问题。

## 九、后续任务检查顺序

1. 先读取本文档，确认当前目录、技术栈和既有边界。
2. 根据任务定位到 `Assets/Scripts`、`Assets/YangTools`、场景、Prefab 或数据目录。
3. 修改前阅读目标文件及其相邻基类、调用方和资源引用。
4. 只做任务所需的最小修改，保留 Unity 序列化字段、Prefab 绑定和现有命名。
5. 修改后进行静态检查；如条件允许，运行 Unity 编译、场景或测试验证。
6. 若目录结构、依赖、场景顺序或主要架构发生变化，同步更新本文档。

## 十、尚未确认的信息

- 当前主入口场景中各 GameObject 的精确层级和依赖关系未在本文档展开，需要涉及场景时再读取场景内容。
- YooAsset 的完整构建参数、远端地址和发布流程未在本文档展开，需要涉及打包/热更新时再检查相关配置。
- 各业务窗口的完整 Prefab 绑定关系未在本文档展开，需要修改 UI 时同时检查对应 `Assets/AssetBundle/UIWindow` Prefab。

