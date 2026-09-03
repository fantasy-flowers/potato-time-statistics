# 游戏统计插件实现计划书

> 基于 `sample/_html_full.html` 界面原型与 `README.md` 项目说明综合规划

---

## 一、项目背景与目标

### 1.1 项目背景

本项目是一个 **PotatoVN 客户端插件**，基于 PotatoVN 插件脚手架（`PotatoVN.App.PluginBase`）开发。PotatoVN 是一款 Galgame 管理软件，支持通过插件体系扩展功能。当前插件脚手架已提供：

- 基础的 `IPlugin` 生命周期管理（`Plugin.cs`）
- 侧边栏按钮注册（`doc/sidebar.md`）
- 多语言支持（`Helper/PluginLocalization.cs`）
- 数据持久化接口（`IPotatoVnApi.GetDataAsync/SaveDataAsync`，见 `doc/data.md`）
- 预设 UI 控件（`Controls/Prefabs/`，如 `StdSetting`、`StdAccountPanel`、`StdStackPanel`）
- 预设 Style 资源（`Controls/Styles/`，如 `FontSizes.xaml`、`TextBlock.xaml`、`Thickness.xaml`）

### 1.2 项目目标

基于 `sample/_html_full.html` 中定义的双模块界面原型，开发一个 **游戏时间统计与游戏库分析插件**，为 PotatoVN 用户提供：

- **模块一：游戏时长统计** — 按日/周/月维度展示游戏游玩时长，包含概览指标、时长分布图、游戏排行，支持日期筛选与图表联动。
- **模块二：游戏统计** — 展示游戏库整体规模、游玩状态/引擎/制作公司分布、总时长排行、年度游玩强度热力图（GitHub 贡献图风格）。

---

## 二、功能需求分析

> 以下需求均提取自 `sample/_html_full.html` 的实际界面元素与交互逻辑。

### 2.1 模块一：游戏时长统计（`#module-playtime`）

| 功能点 | 描述 | 对应 HTML 元素/逻辑 |
|--------|------|---------------------|
| 日期选择器 | 日历面板支持日/周/月选择，含月份导航、今天/本月按钮，未来日期禁用 | `.date-picker`、`#pickerPanel`、`renderCalendarPanel()`（L1450-L1580） |
| 维度切换 | 日/周/月三个 Tab，切换后联动日期选择器与图表 | `.period-tabs`、`currentPeriod` 变量 |
| 概览指标条 | 4 卡片：总游玩时长、游玩游戏数、最常玩游戏、平均时长 | `.stats-bar`、`#totalTime`/`#gameCount`/`#topGame`/`#avgTime`（L1063-L1080） |
| 时长分布图 | 左栏 ECharts 柱形图（日维=环形图，周/月=柱形图），点击柱形筛选右侧游戏排行 | `#mainChart`（L1088）、`mainChart.on('click')` |
| 游戏排行列表 | 右栏：排序切换（按时长/按名字）、游戏图标、名称、时长、百分比进度条 | `.rank-list`、`.rank-item`（L517-L580） |
| 日维度趋势面板 | 选中日期的近 7 日迷你折线图（ECharts）、趋势摘要、每日详情列表 | `.trend-panel`、`#miniChart`、`.trend-list`（L612-L720） |
| 图表联动 | 柱形图点击→右侧排行按该时段筛选；环形图点击扇区→按该游戏筛选 | `selectedIndex`/`selectedGameId` 机制 |

### 2.2 模块二：游戏统计（`#module-stats`）

| 功能点 | 描述 | 对应 HTML 元素/逻辑 |
|--------|------|---------------------|
| 年份导航 | 年选择器（上/下一年按钮），数据随年份切换 | `.gs-year-nav`、`#gsYearPrev`/`#gsYearNext`（L1099-L1104） |
| 概览卡片 | 3 卡片：库中游戏数、累计游玩时长、时长最长游戏 | `.gs-overview`、`#gsGameCount`/`#gsTotalTime`/`#gsTopGame`（L1108-L1124） |
| 游戏分布 | 三标签（游玩状态/游戏引擎/制作公司），6 列网格展示分类统计 | `.gs-dist-grid`、`#gsTabs`（L1134-L1139） |
| 总时长排行 TOP 10 | 横向进度条排行榜 | `.gs-rank-list`（L1146） |
| 年度游玩强度热力图 | GitHub 贡献图风格，7 行 × 约 53 列，5 级颜色深度 | `.gs-heat`、`.gs-cell`、`.gs-l0`~`.gs-l4`（L1150-L1170） |

### 2.3 非功能需求

- **数据来源**：游戏统计模块数据全部来自宿主 `GetAllGames()` 快照（`PlayType`/`TotalPlayTime`/`PlayedTime`），无需额外持久化（见 AGENTS.md 记忆区）。
- **主题一致性**：深色 `#1b2838` 主题，与宿主应用风格统一，使用预设 Style 资源。
- **响应式**：支持窗口缩放（参考 HTML 中 `@media` 断点：1024px、900px、600px、480px）。
- **多语言**：利用 `PluginLocalization` 支持中/英/日三语。

---

## 三、技术架构设计

### 3.1 整体架构

```
┌─────────────────────────────────────────────────────┐
│  PotatoVN 宿主应用                                    │
│  ┌───────────────────────────────────────────────┐  │
│  │  IPotatoVnApi (HostApi)                       │  │
│  │  - GetAllGames() → List<Galgame>               │  │
│  │  - GetDataAsync() / SaveDataAsync()            │  │
│  │  - RegisterSidebarButton()                     │  │
│  │  - NavigateTo()                                │  │
│  │  - Info() (调试提示)                            │  │
│  └───────────────────────────────────────────────┘  │
│                      ▲                               │
│                      │ 依赖注入                        │
│  ┌───────────────────┴───────────────────────────┐  │
│  │  插件本体 (PotatoVN.App.PluginBase)             │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │  Plugin.cs (IPlugin, IPluginSetting)     │  │  │
│  │  │  - InitializeAsync()                     │  │  │
│  │  │  - 侧边栏注册 + 导航                      │  │  │
│  │  ├─────────────────────────────────────────┤  │  │
│  │  │  Models/                                 │  │  │
│  │  │  - PluginData.cs (插件持久化数据)         │  │  │
│  │  │  - GameStatsData.cs (统计计算模型)        │  │  │
│  │  ├─────────────────────────────────────────┤  │  │
│  │  │  Services/                               │  │  │
│  │  │  - StatsCalculator.cs (统计计算服务)     │  │  │
│  │  ├─────────────────────────────────────────┤  │  │
│  │  │  Controls/                               │  │  │
│  │  │  - PlaytimeStatsPage.xaml(.cs) 模块一    │  │  │
│  │  │  - GameStatsPage.xaml(.cs)    模块二     │  │  │
│  │  │  - DatePickerControl.xaml(.cs) 日期选择器 │  │  │
│  │  │  - HeatmapControl.cs         热力图控件   │  │  │
│  │  │  - RankListControl.cs        排行列表     │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### 3.2 关键设计决策

| 决策 | 方案 | 原因 |
|------|------|------|
| 图表渲染 | 使用 ECharts 的 WebView2 嵌入方案，或改用 WinUI 原生绘图（OxyPlot / LiveCharts2） | ECharts 效果最佳但需引入 WebView2；若宿主不支持 WebView2，则使用 LiveCharts2（WinUI 友好） |
| UI 描述方式 | 优先使用 C# 描述 UI（`StdStackPanel` 等预设控件） | 避免 XAML namespace stamping 的调试问题（见 `doc/ui.md` L17-L24），且预设控件已提供布局能力 |
| 导航方式 | 侧边栏按钮 + `NavigateTo()` 打开插件专属页面 | 符合 `doc/sidebar.md` 推荐方式 |
| 数据持久化 | 仅保存用户偏好（默认维度、排序方式等），统计数据每次从宿主实时计算 | 数据源来自宿主 API，无需冗余存储 |
| 多语言 | 使用 `PluginLocalization` + `Strings/*.json` | 脚手架已内置，零成本接入 |

### 3.3 数据流

```
用户操作 → 日期选择/维度切换
              │
              ▼
    StatsCalculator 从 HostApi.GetAllGames()
    获取 Galgame 列表，提取 PlayedTime 字典
              │
              ▼
    按当前维度(日/周/月)聚合计算
              │
              ▼
    更新 ViewModel 绑定的 Observable 属性
              │
              ▼
    WinUI 数据绑定自动刷新图表/列表/指标
```

---

## 四、开发环境配置

### 4.1 环境要求

| 组件 | 版本要求 | 说明 |
|------|----------|------|
| .NET SDK | 8.0 | 编译目标 `net8.0-windows10.0.22621.0` |
| Windows 10 SDK | 10.0.19041+ | WinUI 3 依赖 |
| Windows App SDK | 2.1+ | NuGet 自动还原 |
| Visual Studio 2022 | 17.5+ | 含 .NET 桌面开发、Windows App SDK 工作负载 |
| Git | 任意 | 管理 submodule |

### 4.2 初始化步骤

```powershell
# 1. 克隆仓库并初始化 submodule
git clone <repo-url> && cd potato-time-statistics
git submodule update --init --recursive

# 2. 还原依赖
dotnet restore PotatoVN.App.Plugin.sln

# 3. 构建验证
dotnet build PotatoVN.App.Plugin.sln -c Debug
```

### 4.3 开发前修改（Plugin.cs TODO 事项）

1. 修改 `PotatoVN.App.PluginBase.csproj` 中的 `AssemblyName` 为 `ATimeStatistics`（或自定义 ID）
2. 生成新的 GUID 替换 `PluginInfo.Id` 中的默认值
3. 修改 `PluginInfo.Name` 为 `"游戏统计"`，`Description` 为插件功能描述

---

## 五、详细开发步骤

### 阶段一：基础框架搭建（预计工作量：2-3 天）

| 步骤 | 任务 | 产出 |
|------|------|------|
| 1.1 | 修改 `Plugin.cs` TODO 事项（AssemblyName、GUID、名称） | 插件身份标识就绪 |
| 1.2 | 在 `Plugin_Ui.cs` 中注册侧边栏按钮，`NavigateTo` 到主页面 | 可从宿主侧边栏进入插件 |
| 1.3 | 创建 `Controls/PlaytimeStatsPage.xaml(.cs)` — 模块一主页面（先用 C# 描述 UI 骨架） | 页面框架 |
| 1.4 | 创建 `Controls/GameStatsPage.xaml(.cs)` — 模块二主页面（同上） | 页面框架 |
| 1.5 | 创建 `Models/GameStatsData.cs` — 定义统计计算的输入/输出模型 | 数据模型 |
| 1.6 | 创建 `Services/StatsCalculator.cs` — 从 `GetAllGames()` 聚合计算 | 核心计算逻辑 |

### 阶段二：模块一 — 游戏时长统计（预计工作量：4-5 天）

| 步骤 | 任务 | 对应 HTML 参考 |
|------|------|---------------|
| 2.1 | 实现 `DatePickerControl`（自定义日历面板，支持日/周/月选择） | `.date-picker`、`renderCalendarPanel()`（L1450-L1580） |
| 2.2 | 实现维度切换 Tab（日/周/月） | `.period-tabs`（L310-L330） |
| 2.3 | 实现概览指标条（4 卡片数据绑定） | `.stats-bar`（L1063-L1080） |
| 2.4 | 集成图表库（LiveCharts2 或 OxyPlot），实现主图表（日维=环形图，周/月=柱形图） | `#mainChart`（L1088） |
| 2.5 | 实现游戏排行列表（排序切换、进度条、游戏图标） | `.rank-list`（L517-L580） |
| 2.6 | 实现日维度趋势面板（迷你折线图 + 近 7 日列表） | `.trend-panel`（L612-L720） |
| 2.7 | 实现图表→排行联动（点击柱形/扇区筛选） | `selectedIndex`/`selectedGameId` 机制 |

### 阶段三：模块二 — 游戏统计（预计工作量：3-4 天）

| 步骤 | 任务 | 对应 HTML 参考 |
|------|------|---------------|
| 3.1 | 实现年份导航器 | `.gs-year-nav`（L1099-L1104） |
| 3.2 | 实现概览卡片（3 卡片） | `.gs-overview`（L1108-L1124） |
| 3.3 | 实现游戏分布面板（三标签切换 + 6 列网格） | `.gs-dist-grid`（L1134-L1139） |
| 3.4 | 实现总时长排行 TOP 10（横向进度条） | `.gs-rank-list`（L1146） |
| 3.5 | 实现年度游玩强度热力图（GitHub 贡献图风格，7 行 × 53 列） | `.gs-heat`（L1150-L1170） |

### 阶段四：模块切换与导航（预计工作量：1 天）

| 步骤 | 任务 | 对应 HTML 参考 |
|------|------|---------------|
| 4.1 | 实现模块切换按钮（"游戏时长统计"/"游戏统计"） | `.module-switch`（L1020-L1023） |
| 4.2 | 实现 `IPluginSetting` 接口，提供设置页（`UserControl1` 替换为实际设置） | 现有 `CreateSettingUi()` 方法 |

### 阶段五：多语言与样式（预计工作量：1-2 天）

| 步骤 | 任务 | 产出 |
|------|------|------|
| 5.1 | 在 `Strings/zh-CN.json` 中添加中文字符串 | 中文 UI 文案 |
| 5.2 | 在 `Strings/en-US.json` 中添加英文翻译 | 英文 UI 文案 |
| 5.3 | 在 `Strings/ja-JP.json` 中添加日文翻译（可选） | 日文 UI 文案 |
| 5.4 | 统一使用预设 Style（`FontSizes`、`TextBlock`、`Thickness`） | 与宿主风格一致 |

### 阶段六：README 与收尾（预计工作量：0.5 天）

| 步骤 | 任务 | 产出 |
|------|------|------|
| 6.1 | 修改仓库根目录 `README.md`，删除脚手架内容，写入插件说明 | 最终 README |
| 6.2 | 删除 `Plugin.cs` 中已完成 TODO 注释 | 代码整洁 |

---

## 六、测试策略

### 6.1 单元测试

| 测试对象 | 测试内容 | 方法 |
|----------|----------|------|
| `StatsCalculator` | 验证日/周/月维度聚合计算正确性 | 构造固定 `Galgame` 列表，验证输出 |
| `StatsCalculator` | 边界情况：空游戏库、无游玩记录、未来日期 | 输入空数据/零值 |
| `PluginData` | 序列化/反序列化、版本兼容 | JSON 序列化测试 |

### 6.2 集成测试

| 测试对象 | 测试内容 | 方法 |
|----------|----------|------|
| 日期选择器联动 | 维度切换→日历面板类型切换→数据刷新 | 手动操作验证 |
| 图表联动 | 点击柱形→右侧排行筛选→点击排行→图表联动 | 手动操作验证 |
| 模块切换 | 时长统计↔游戏统计切换，数据正确渲染 | 手动操作验证 |
| 多语言切换 | 宿主切换语言→插件 UI 文案跟随 | 手动操作验证 |

### 6.3 兼容性测试

| 测试项 | 说明 |
|--------|------|
| 窗口缩放 | 测试不同窗口尺寸下 UI 响应式表现 |
| 数据版本兼容 | 修改 `PluginData` 结构后旧数据能正常迁移 |
| 插件卸载/重装 | 数据保留/清除功能正常 |

---

## 七、部署流程

### 7.1 构建产物

```powershell
# Release 构建
dotnet build PotatoVN.App.Plugin.sln -c Release
```

产物位于 `PotatoVN.App.PluginBase\bin\Release\net8.0-windows10.0.22621.0\`，文件名为 `A{AssemblyName}.dll`（例如 `ATimeStatistics.dll`）。

### 7.2 部署方式

1. 将构建产物 DLL 及 `.pri` 资源文件复制到 PotatoVN 的插件目录。
2. 启动 PotatoVN，插件将被自动发现并加载。
3. 用户可在设置中启用/禁用插件，或在侧边栏找到插件入口。

### 7.3 发布检查清单

- [ ] `AssemblyName` 已修改为唯一标识
- [ ] `PluginInfo.Id` GUID 已替换为随机新值
- [ ] `README.md` 已更新为插件实际说明
- [ ] `Plugin.cs` 中 TODO 注释已删除
- [ ] 多语言文件已填充完整
- [ ] Release 构建通过
- [ ] 手动测试两个模块功能正常

---

## 八、项目进度安排

| 阶段 | 内容 | 预计工期 | 里程碑 |
|------|------|----------|--------|
| 阶段一 | 基础框架搭建 | 2-3 天 | 插件可加载、侧边栏入口可用 |
| 阶段二 | 模块一：游戏时长统计 | 4-5 天 | 日/周/月维度统计功能完整 |
| 阶段三 | 模块二：游戏统计 | 3-4 天 | 分布图、热力图、排行功能完整 |
| 阶段四 | 模块切换与导航 | 1 天 | 双模块切换流畅 |
| 阶段五 | 多语言与样式 | 1-2 天 | 中/英/日三语支持 |
| 阶段六 | README 与收尾 | 0.5 天 | 插件可发布 |
| **总计** | | **11.5-15.5 天** | |

---

## 九、资源需求

### 9.1 技术依赖

| 依赖 | 用途 | 备选方案 |
|------|------|----------|
| LiveCharts2 | WinUI 图表渲染 | OxyPlot.WinUI |
| `System.Text.Json` | 插件数据序列化 | 已内置 |

### 9.2 参考资源

| 资源 | 路径 | 用途 |
|------|------|------|
| 界面原型（双模块） | `sample/_html_full.html` | UI 设计参考、CSS 变量/颜色/布局 |
| 插件开发文档 | `doc/main.md` | 整体架构与 API 说明 |
| UI 开发文档 | `doc/ui.md` | XAML/C# 描述 UI、预设控件、Style |
| 侧边栏文档 | `doc/sidebar.md` | 按钮注册方式 |
| 数据读写文档 | `doc/data.md` | 持久化 API |
| Dialog 文档 | `doc/dialog.md` | 弹窗开发 |
| 宿主 API 定义 | `GalgameManager.WinApp.Base/Contracts/` | 接口定义 |

### 9.3 HTML 原型中提取的关键设计参数

| 参数 | 值 | 来源 |
|------|-----|------|
| 主背景色 | `#1b2838` | `--bg-primary` |
| 卡片背景色 | `#1f2d3d` | `--bg-card` |
| 强调色 | `#66c0f4` | `--accent` |
| 亮强调色 | `#1a9fff` | `--accent-bright` |
| 字体 | `Noto Sans SC` | body `font-family` |
| 热力图层级颜色 | `#243449`→`#123c27`→`#0e5a2e`→`#199048`→`#2fbf5f` | `.gs-l0`~`.gs-l4` |
| 统计卡片网格 | 4 列（`repeat(4, 1fr)`） | `.stats-bar` |
| 游戏分布网格 | 6 列（`repeat(6, 1fr)`） | `.gs-dist-grid` |
| 主内容区布局 | 1.6fr : 1fr | `.main-content` |
| 排行+热力图布局 | 1fr : 1.5fr | `.gs-main` |

---

## 十、风险评估与应对措施

| 风险 | 概率 | 影响 | 应对措施 |
|------|------|------|----------|
| **图表库兼容性** | 中 | 高 | 优先验证 LiveCharts2 在 WinUI 3 中的表现；若不兼容，改用 OxyPlot 或直接用 WinUI `Canvas` 自绘简单图表 |
| **namespace stamping 调试困难** | 中 | 中 | 优先使用 C# 描述 UI，减少 XAML 依赖；必要时使用 `HostApi.Info()` 输出调试信息 |
| **GetAllGames() 返回数据结构变化** | 低 | 高 | 在 `StatsCalculator` 中做防御性编程，对缺失字段使用默认值，记录异常日志 |
| **热力图性能** | 低 | 中 | 热力图格子数约 7×53=371 个，WinUI 原生控件可承受；若性能不足可改用虚拟化或 `Canvas` 自绘 |
| **插件与其他插件冲突** | 低 | 低 | 使用随机生成的 GUID，AssemblyName 使用唯一前缀 |
| **多语言维护成本** | 低 | 低 | 优先完成中/英文，日文后续按需补充 |
| **宿主 API 不支持 WebView2** | 中 | 中 | 不使用 ECharts，改用 LiveCharts2 等原生 WinUI 图表库 |