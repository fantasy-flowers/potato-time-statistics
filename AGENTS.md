# AGENTS.md

> 本文件由 AI 自维护，是这个插件仓库**跨对话的长期记忆**。
> OpenHands 会把整份文件作为 repo skill 注入每次对话的 `<REPO_CONTEXT>` 系统提示，
> 因此你写进去的内容下一次对话依然看得到。

---

## 第一部分：协议（DO NOT EDIT — 协议区，禁止修改）

### 启动协议（每个新任务的第一步，先于一切其他操作）

1. 你正在阅读的这段就在 `<REPO_CONTEXT>` 里 —— 启动时已经被注入，**不需要再用 file_editor 读 AGENTS.md**。
2. 直接跳到下方「## 第二部分：记忆区」并扫读，然后用一两行简述「与本次任务相关的记忆条目」，再开始干活。
3. 记忆区为空 / 与任务无关时，明确说一句 `memory: nothing relevant`，再继续。

### 收尾协议（在你认为任务已完成、准备给用户最终回复之前）

按以下规则更新**记忆区**（仅记忆区！协议区一字不动），用 file_editor 工具写回 `./AGENTS.md`：

- **必须保存**：
  - 用户偏好/约定（"以后都用 X 风格"、"不要再做 Y"）
  - 项目事实（架构决策、依赖版本约束、外部系统位置、为什么这么做）
  - 校正/反馈（用户纠正过你的做法 —— 记下规则 + 原因）
  - 非显然的踩坑结论（从代码 / git log 看不出来的）
- **不要保存**：
  - 代码本身能表达的事（文件路径、函数签名、目录结构）
  - git log / git blame 已记录的变更历史
  - 本次会话内的临时状态、TODO 进度（这些走 TaskTracker）
  - 重复条目 —— 先查再写，能更新就别新增
- **写法**：
  - 用 file_editor 的精确替换，**只改记忆区内的对应小节**，不要重写整份文件。
  - 每条 ≤ 3 行；记忆区总长控制在 200 行内，超出时合并/精简旧条目。
  - 不在记忆里粘贴大段代码或日志 —— 留指针即可。
- **空更新也要说**：本次任务没有产生需要长期记住的新信息，
  在最终回复里加一行 `AGENTS.md memory: no update.`（不写文件）。

### 硬性规则

- **协议区禁止修改**：本节及上方，到 `<!-- MEMORY START -->` 之间的任何字节都不能动。
  Agent 一旦修改协议区，视为协议违反 —— 后端会告警并回滚。
- **启动时没扫读记忆区就调其他工具 = 协议违反**。
- **绝对不写入** 密钥 / token / 个人隐私（这是仓库内文件，会进 git）。
- 任何"我觉得不需要这条协议"的修改建议 —— 反馈给用户，由人改，不要自改。

---

## 第二部分：记忆区（你可以在这里更新）

<!-- MEMORY START -->

### User Preferences
<!-- 用户偏好与约定。例：- 提交信息一律使用中文 -->
- 插件页面内容超宽（>1400）时要整体居中（对齐 HTML 原型 `.container { max-width:1400px; margin:0 auto }`），不要靠左留右侧（2026-09 用户明确要求）

### Project Facts
<!-- 架构、依赖、外部系统、为什么这么做。例：- 编译目标 net8.0-windows，原因：宿主 PotatoVN 限定 -->
- 构建说明位于仓库根目录 BUILD.md，涵盖桌面客户端(MSIX)和后端服务(Docker/dotnet publish)两种构建方式
- `sample/` 界面原型：`_html_full.html` = 双模块 HTML 原型（游戏时长统计 + 游戏统计，顶部 module-switch 切换，深色 #1b2838 主题）；原始单模块为 `游戏时长统计插件界面（含日期选择器） (1).html`
- 踩坑：勿把 web.fetch/Read 长内容持久化输出直接存成 .html（每行带"行号<TAB>"前缀，浏览器打不开）；`_html_full.html` 曾因此损坏，已用原始 HTML 重建
- 游戏统计模块数据全部来自宿主 `GetAllGames()` 快照（PlayType/TotalPlayTime/PlayedTime 日期→分钟），无需额外持久化；热力图直接聚合 PlayedTime
- `Galgame.PlayedTime` 键格式为 `yyyy/M/d`（ToStringDefault），值为分钟；插件只引用 WinApp.Base（无 GalgameManager.Core），日期解析需自带（StatsService 支持 yyyy/M/d、yyyy-MM-dd 等）
- 宿主以 `Activator.CreateInstance(pageType)` 无参构造插件 Page，且 `NavigateTo(Type,title,parameter)` 的 parameter 不会传给页面 → 插件页面必须保留无参 ctor，共享数据走 `Plugin.CurrentData`
- 统计图表采用 WinUI 原生自绘（Path 环形图/Rectangle 柱形图），零图表库依赖；踩坑：WinUI 3 无 UniformGrid 控件、Thickness 无 2 参构造、Color 在 Windows.UI、FontWeight 在 Windows.UI.Text、FlyoutPlacementMode 在 Microsoft.UI.Xaml.Controls.Primitives
- 脚手架依赖 AngleSharp 未被插件代码使用且带已知漏洞（NU1902），已从 csproj 移除
- 踩坑：COMException 0x800F1000「没有检测到已安装的组件」是 XAML 错误码与 SPAPI 撞号，真实含义 = "Element is already the child of another element"。共享 UIElement（readonly 字段的 Canvas/Grid/View）重复挂载到新父级前必须先从旧父级移除；`Children.Clear()`/`Content=null` 只解除直接子级，孙级仍保留父级引用（BarChart 用 _plotGrid 字段复用、StatsPage 加 DetachFromParent 修复，2026-09）
- 构建后置步骤用裸 `powershell` 命令，Git Bash 下 9009 失败；可用 System32 bsdtar（`tar -a -cf xxx.zip`）替代 Compress-Archive 打包 .pvnplugin.zip
- 踩坑：ScrollViewer 的直接子元素设 MaxWidth+HorizontalAlignment.Stretch，窗口宽度超过 MaxWidth 后内容被截断时按"居中"排列 → 窗口越大整体内容越往右漂。正确做法：外层 Grid 撑满视口（不设 MaxWidth）+ 内容列 `ColumnDefinition { Star, MaxWidth }` 封顶；超宽居中用 container.SizeChanged 加左右对称 Padding（等价 CSS margin:0 auto，2026-09 StatsPage）。不能用三列 Star 对称留白（窗口不足封顶宽时内容列被挤窄），也不能 MaxWidth+Center（图表等拉伸元素 desired 宽不可靠会缩成自然宽）
- 踩坑：Grid 同一 cell 内多个子元素不会自动排布——BarChart X 轴标签曾全部堆在绘图区左边缘，需 `Margin.Left = slotWidth * i` 偏移到对应柱形槽位；进度条类填充宽度不要把 0-100 百分数当像素值，用 `GridLength(percent, Star)` 星列按比例（GameStatsView trackGrid 模式）
- 踩坑：`Enumerable.Range(start, count)` 生成的是 start 起的**递增**序列——GetRecentDays 曾把 `Range(count-1, count)` 当倒序偏移 {6..0} 用，实际得到 {6..12}，近7日窗口整体前移 6 天且漏掉选中日；倒序偏移要自己算 `i - (count-1)`（2026-09 修复）
- 踩坑：Path 画环形扇区 = 外弧(ArcSegment 顺时针) + 径向 LineSegment + 内弧(逆时针) + IsClosed；若外圈误写成直线弦、径向连接误写成弧线，扇区会变成上下两片「月牙」（DonutChart 2026-09 修复）。样式已对齐原型 ECharts 饼图：radius 48%/72%、padAngle 2° + 卡片底色描边、占比 ≥5% 外部标签带引导线
- 踩坑：单条 ArcSegment 不允许起点=终点——360° 满圆时两点重合属退化弧，整段不渲染（100% 单扇区整环消失，2026-09 二修）；弧必须按 ≤180° 分段绘制，IsLargeArc 恒 false

### Feedback / Lessons
<!-- 用户纠正过的做法 + 原因。例：- 不要 mock 数据库测试，原因：上次 mock 通过但生产迁移失败 -->

### References
<!-- 外部资源指针。例：- 报错日志查 Grafana: grafana.internal/d/plugin-runtime -->

<!-- MEMORY END -->
