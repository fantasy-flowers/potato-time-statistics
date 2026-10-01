# 浅色界面模式适配计划书

> 专题计划：为 PotatoVN 游戏统计插件补齐浅色界面模式适配。
> 基于对 
>
> `PotatoVN.App.PluginBase`
>
>  现有代码的核查编写，覆盖现状盘点、改造分级、具体方案、验证矩阵与排期。



***

## 一、背景与目标

### 1.1 背景

当前插件以**深色为主**（原型 `sample/_html_full.html` 为 Steam 风 `#1b2838`）。经代码核查，浅色适配的**架构基础已经就位**，但存在**零散的深色专属实现**会导致浅色下不可读 / 隐形，且缺少**系统性验证**与**浅色视觉参考**。

### 1.2 目标



1. 插件在宿主处于**浅色**或**跟随系统 (Default→浅色)** 时，呈现正确、可读、风格统一的浅色界面。

2. **运行时随宿主切换主题**无闪烁、无深色残留、无硬编码静态刷子遗漏。

3. 深色表现**不回归**，与浅色保持一致的功能与对比度标准。

4. 提供一份可复用的浅色调色板与检查清单，供后续界面迭代引用。



***

## 二、现状盘点（代码核查结论）

### 2.1 已就位的浅色基础设施 ✅



| 项          | 位置                                                                                                                          | 说明                                                                       |
| ---------- | --------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| 双色板        | `Controls/StatsTheme.cs` `DarkPalette`/`LightPalette` + `For(ElementTheme)` / `For(FrameworkElement)`                       | 深浅两套 `StatsPalette` 已定义，浅色各色值（Bg/Card/Border/Text/Accent/HeatLevels）均已给出 |
| 页面跟随主题     | `Controls/StatsPage.cs:32` 订阅 `ActualThemeChanged → BuildPage()`，`:44` 用 `StatsTheme.For(this)`                             | 主题变化时整页重建                                                                |
| 模块一视图      | `Controls/PlaytimeStatsView.cs:50` 订阅 `ActualThemeChanged → BuildUi()`，`:66` 用 `For(this)`                                  | 时长统计视图主题化                                                                |
| 模块二视图      | `Controls/GameStatsView.cs:33` 订阅 `ActualThemeChanged → BuildUi()`，`:49` 用 `For(this)`                                      | 游戏统计视图主题化                                                                |
| 图表         | `Charts/BarChart.cs`、`DonutChart.cs`、`TreemapChart.cs` 均通过 `SetData(..., StatsPalette palette, ...)` 接收色板                   | 重绘即换色，非硬编码                                                               |
| 热力图        | `StatsTheme.cs:118-123` `LightPalette.HeatLevels` 已提供浅色 5 级                                                                 | 浅色热力层级已定义                                                                |
| 宿主主题       | `PotatoVN/GalgameManager/Services/ThemeSelectorService.cs` 支持 `ElementTheme.Default/Dark/Light`，将 `RequestedTheme` 设到主窗口根元素 | 插件页继承 `ActualTheme`，机制上可跟随宿主                                             |
| 无 XAML 硬编码 | 插件全为 C# 描述 UI；`GalgamePrefab.xaml` 仅挂 `PlayTypeToSolidColorBrushConverter`（固定品牌色，主题无关）                                      | 无需处理 XAML 主题资源                                                           |

### 2.2 遗留问题（本次适配的真正工作量）



| 级别     | 问题                                              | 位置                                                                                                  | 影响                                                                 |
| ------ | ----------------------------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| **P0** | `FaintWhiteBorderBrush = #40FFFFFF` 为**静态只读**字段 | `Controls/PlaytimeStatsView.DatePicker.cs:24-25`                                                    | 日历格 (359)、月份按钮 (184) 用它勾出 "可选范围"；浅色卡片背景上**白色半透明边框完全隐形**，可选项失去边界提示  |
| **P1** | `Colors.White` 前景散落多处，需在浅色下逐处核对对比度              | 见 §4.2 清单                                                                                           | 大部分落在 accent / 系列色上仍成立，但需逐一确认，防止个别表面浅色下对比不足                        |
| **P2** | 无浅色视觉参考原型                                       | `sample/_html_full.html` 仅深色                                                                        | 无法直观对照浅色配色是否符合预期                                                   |
| **P3** | 运行时切换完整性未实测                                     | 图表 `_palette` 初值 `For(ElementTheme.Dark)`（`BarChart.cs:31`/`DonutChart.cs:36`/`TreemapChart.cs:26`） | 初值虽是深色，但视图重建时会用 `SetData` 覆盖为浅色；需**实测**确认所有图表 / 悬停 / 选择态都随重建更新，无残留。**实测已发现必现崩溃（2026-09）：浅色下点开统计页即闪退**——`ActualThemeChanged` 在 `BuildPage` 挂载共享视图中途重入、`DetachFromParent` 撞上"半挂载"中间态摘除失败、再次 Add 抛 0x800F1000，已修复（重入护栏 + 确定性摘除，见 §4.4.1） |
| **P4** | 系统控件表面（Flyout、ScrollBar、Focus 视觉、ToolTip）       | 依赖 WinUI 主题资源                                                                                       | 插件页继承宿主主题后理论上自适应，需回归确认                                             |



***

## 三、改造范围与分级



* **P0（必修，浅色 bug 阻断）**：`FaintWhiteBorderBrush` 改为色板驱动。

* **P1（必做，可读性）**：白字前景清单逐项核对，必要时引入专用 "onAccent" 色。

* **P2（推荐）**：基于现有 HTML 原型产出浅色变体参考（或沉淀浅色样例表）。

* **P3（必做，验证）**：运行时深浅切换全链路实测。

* **P4（验证项）**：系统控件表面回归。

不涉及：新增设置项、数据结构、布局改动。**主题模式（深 / 浅 / 跟随系统）由宿主决定，插件不做独立切换入口**—— 与宿主风格统一是本插件的既定约束。



***

## 四、具体改造方案

### 4.1 P0 — 让 "可选范围边框" 随主题变色

**问题根因**：`StatsPalette` 没有 "淡边框" 语义，日历 / 月份可选项借用了深色专用的半透明白。

**改法**：



1. 在 `StatsTheme.cs` 的 `StatsPalette` 增加语义属性：



```
public Color FaintBorder { get; init; }

public SolidColorBrush FaintBorderBrush => Brush(FaintBorder);
```



1. 两个色板赋值：

* `DarkPalette`：`FaintBorder = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)`（沿用现值，深色不变）。

* `LightPalette`：`FaintBorder = Color.FromArgb(0x28, 0x1F, 0x2D, 0x3D)`（深板岩灰 @16% alpha，在白色卡片上清晰但不喧宾夺主；若需更弱可改 0x1E，更强改 0x38，**色值标为可调**）。

1. 删除 `DatePicker.cs` 静态字段 `FaintWhiteBorderBrush`（第 24-25 行）。

2. 替换两处引用为 `palette.FaintBorderBrush`：

* `DatePicker.cs:184`（月份按钮非当前月的默认边框）

* `DatePicker.cs:359`（日历格默认边框）

1. 调用链已携带 `palette`（`ApplyCalendarCellStyle(..., StatsPalette palette, ...)` / `BuildMonthPanel(..., StatsPalette palette, ...)` 均已入参），无需额外传参。

**验收**：浅色日历 / 月份面板可选项有清晰但柔和的轮廓；深色表现与现状一致（`#40FFFFFF`）。

### 4.2 P1 — 白字前景对比度核对清单

逐项在浅色下目测，必要时将 "深底白字" 与 "亮底深字" 分离处理。清单（含当前判定）：



| 位置                           | 场景          | 底色 (浅)                       | 判定                                 |
| ---------------------------- | ----------- | ---------------------------- | ---------------------------------- |
| `DatePicker.cs:186` / `:327` | 选中日期 / 选中月份 | `AccentBright` `#1a9fff`     | 白字，选择态可接受；若追求更高对比可换 `AccentDark` 底 |
| `StatsPage.cs:163`           | 激活模块按钮      | `AccentDark` `#0d6fb8`       | 白字 ✅ 对比充足                          |
| `TreemapChart.cs:96/106/116` | 树图色块标题      | 系列色                          | 白字 ✅（深系列色）                         |
| `UiKit.cs:213/259`           | 封面占位首字母     | `StatsTheme.SeriesColor(id)` | 白字 ✅（中亮度系列色）                       |
| `UiKit.cs:435/436/443`       | 激活按钮        | `AccentBright`               | 白字，同选择态判定                          |

> 结论：P1 大概率
>
> **无需改动**
>
> ，但须按上表逐项实测确认，避免遗漏。若选择态在浅色下对比不足，优先方案是选中底改用 
>
> `AccentDark`
>
> （与模块按钮一致），次选调整 
>
> `AccentBright`
>
>  明度。

### 4.3 P2 — 浅色参考原型（可选，建议）



* 基于 `sample/_html_full.html` 复制一份浅色变体 `sample/_html_full_light.html`，仅替换 CSS 变量（`--bg-primary/--bg-card/--bg-secondary/--text-*/--accent/--border` 等）为 `LightPalette` 对应值，作为视觉对照基线。

* 若不希望维护两份原型，可在计划文档内沉淀 "浅色色值表"（§ 附 A）作为替代参考。

### 4.4 P3 — 运行时切换完整性与闪烁

**核查结论**：三处主入口均订阅 `ActualThemeChanged` 并整树重建；图表 / 悬停 / 选择态均随之重建。需实测确认的细项：



1. **图表重绘**：主题切换后，`BarChart/DonutChart/TreemapChart` 的 `_palette` 是否随视图 `BuildUi()` 的 `SetData(palette)` 刷新（当前逻辑应满足，实测确认）。

2. **共享元素复用**：`StatsPage.BuildPage()` 已 `DetachFromParent` 后再挂载（规避 COMException `0x800F1000`）。**实测暴露出该路径的必现崩溃（浅色点开即闪退），已修复，见 §4.4.1**。

3. **悬停残留**：`UiKit.AttachHover` 用的 `palette.HoverBrush` 随重建更新，实测确认切换主题后无旧色悬停残留。

4. **Flyout 内面板**：日期 / 月份选择面板在 `Flyout.Opening` 时按当前 `palette` 构建，切换主题后下次打开即为新色，无需处理；确认 Flyout 自身背景随宿主主题自适应。

**验收**：浅→深、深→浅、Default→系统切换时，整页（含图表、选择器、排行、热力图）一次性换色，无旧主题色块残留、无可察觉闪烁。

#### 4.4.1 必现崩溃：主题事件重入导致 0x800F1000（2026-09 实测发现并修复）

* **现象**：宿主浅色模式下点开统计页即闪退（深色不崩）。崩溃链：`StatsPage` 构造里的 `ActualThemeChanged` 回调 → `BuildPage()` → `body.Children.Add(_playtimeView)`（`StatsPage.cs:63`）抛 COMException `0x800F1000`。

* **根因**：宿主应用主题的时机不定，浅色下 `Default→Light` 的 `ActualThemeChanged` 事件会落在 `BuildPage` 挂载共享视图的**中途**（`Children.Add` 窗口内）。嵌套重建执行 `DetachFromParent` 时，`element.Parent` 处于 "半挂载" 中间态（为 null 或指向新容器），摘除失效；嵌套再 `Add` 同一视图 → "Element is already the child of another element"。深色下事件时机恰好避开该窗口，故不崩。

* **修复**（`Controls/StatsPage.cs`）：

  1. **重入护栏**：`BuildPage` 加 `_buildingPage/_rebuildPageQueued`——构建中再次触发主题事件只排队，当前构建完成后统一重建；
  2. **确定性摘除**：记录上一任容器 `_body`，`DetachSharedView` 直接从 `_body.Children` 移除共享视图，不再依赖 `element.Parent`；`DetachFromParent` 保留为兜底。

* **验证**：Release 构建通过（0 警告 0 错误）；宿主侧需实测复核——浅色打开统计页、深↔浅 / Default↔固定各切换一轮，确认无闪退、无残留。

### 4.5 P4 — 系统控件表面回归



* `ScrollViewer` 滚动条、`Flyout` 背景 / 边框、`Button` focus 视觉、默认 `ToolTip`：均由 WinUI 主题资源提供，宿主主题决定。回归清单中逐一过一遍即可，预期无需改动。



***

## 五、验证方案

### 5.1 构建与部署



```
\# 依赖未变可加 --no-restore；会话 APPDATA 为空时报 NuGet path1 时需补 APPDATA

dotnet build PotatoVN.App.Plugin.sln -c Release
```

> 部署到宿主由用户侧完成，本计划不自动拷贝构建产物到 
>
> `_PluginXamlHotReload`
>
> （遵守既有约定）。

### 5.2 主题 × 功能矩阵



| 宿主主题      | 模块一 (日 / 周 / 月) | 模块二 | 日期 / 月份选择器 | 排行 / 下钻 | 热力图 | 环形 / 柱形 / 树图 |
| --------- | --------------- | --- | ---------- | ------- | --- | ------------ |
| 浅色        | ✓               | ✓   | ✓          | ✓       | ✓   | ✓            |
| 深色 (回归)   | ✓               | ✓   | ✓          | ✓       | ✓   | ✓            |
| 跟随系统 (默认) | ✓               | ✓   | ✓          | ✓       | ✓   | ✓            |

**核对点**：



* 背景 / 卡片 / 边框 / 文字三级对比度正常，无 "白字贴白底"。

* 可选项（日历格、月份格）有清晰淡边框（P0 验收）。

* 选中态（日期、月份、模块按钮、图表柱）对比充足。

* 图表网格线、坐标轴标签、图例、引导线可读。

* 热力图浅色 5 级层次分明（0 级与卡片背景能区分）。

### 5.3 运行时切换



* 宿主设置页在**深↔浅**、**Default↔固定**间切换，观察插件页即时换色、无残留。

* 切换前后各抓一次截图（深 / 浅各一），留档对比。

### 5.4 回归与反证



* 深色下逐项过 5.2 矩阵，确认与本次改动前一致（重点 `#40FFFFFF` 边框不变）。

* 主动寻找反证：全仓 `Grep` `FromArgb`/`Colors.White`/`#1b|#1f2d`，确认除 `StatsTheme` 与白字清单外**无新增深色专属硬编码**。



***

## 六、风险与应对



| 风险                    | 概率 | 影响 | 应对                                                      |
| --------------------- | -- | -- | ------------------------------------------------------- |
| 浅色下某表面对比不足（如选中态白字贴亮蓝） | 低  | 中  | 4.2 逐项实测；不达标则选中底改 `AccentDark`，色值集中在 `StatsPalette` 一处改 |
| 运行时切换存在旧色残留 / 闪烁      | 低  | 中  | 4.4 专项实测；若残留源于静态刷子，纳入 P0 同类 "去静态化" 处理                   |
| 热力图浅色 0 级与卡片同色、层次不清   | 中  | 低  | 已提供 `LightPalette.HeatLevels`；验证后微调 0 级明度即可             |
| 浅色参考原型维护成本            | 中  | 低  | 可选；以 § 附 A 色值表替代亦可                                      |



***

## 七、任务拆分与排期



| #      | 任务                                                       | 产出                                     | 依赖    | 预计                           |
| ------ | -------------------------------------------------------- | -------------------------------------- | ----- | ---------------------------- |
| 1      | `StatsPalette` 增加 `FaintBorder`/`FaintBorderBrush`，两色板赋值 | `StatsTheme.cs`                        | 无     | 0.5h                         |
| 2      | 移除 `DatePicker.cs` 静态 `FaintWhiteBorderBrush`，替换 2 处引用   | `DatePicker.cs`                        | #1    | 0.5h                         |
| 3      | 白字清单逐项实测，按需微调（优先 `AccentDark` 底）                         | 可能仅改 `StatsPalette`                    | #1    | 1h                           |
| 4      | 浅色参考原型（可选）或色值表沉淀                                         | `sample/_html_full_light.html` 或 § 附 A | 无     | 1-2h                         |
| 5      | Release 构建通过                                             | 产物                                     | #2/#3 | 0.5h                         |
| 6      | 主题 × 功能矩阵 + 运行时切换实测 + 深色回归                               | 验证记录 + 截图                              | #5    | 2h                           |
| 7      | 全仓硬编码复查（反证）                                              | 复查结论                                   | #6    | 0.5h                         |
| **合计** |                                                          |                                        |       | **约 6-8h**（原型可选，含 / 不含差别在上限） |

**里程碑**：M1=P0 修复完成（#1-#2，1h）；M2 = 浅色全矩阵验证通过（#6）；M3 = 收尾复查（#7）。



***

## 附 A：浅色调色板值（来自 `StatsTheme.LightPalette`，供对照）



| 语义                    | 值                                                         |
| --------------------- | --------------------------------------------------------- |
| BgPrimary             | `#f3f7fa`                                                 |
| BgSecondary           | `#e9eff5`                                                 |
| Card                  | `#ffffff`                                                 |
| Hover                 | `#dbe7f1`                                                 |
| Border                | `#cdd9e4`                                                 |
| TextPrimary           | `#1f2d3d`                                                 |
| TextSecondary         | `#54687a`                                                 |
| TextMuted             | `#8291a0`                                                 |
| Accent                | `#1a7fc0`                                                 |
| AccentBright          | `#1a9fff`                                                 |
| AccentDark            | `#0d6fb8`                                                 |
| Success               | `#3d9e4f`                                                 |
| Danger                | `#d6453d`                                                 |
| HeatLevels(0→4)       | `#e4e9ee` → `#bee2c2` → `#82cd91` → `#46b05e` → `#1f9e40` |
| **FaintBorder（本次新增）** | `#281f2d3d`（深板岩灰 @16% alpha，可调）                           |