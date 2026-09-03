# PotatoVN 游戏统计插件

一个基于 [PotatoVN](https://potatovn.net/) 插件体系开发的**游戏时间统计与游戏库分析**插件，界面风格参考 `sample/_html_full.html` 原型。

## 功能

### 模块一：游戏时长统计

- **日 / 周 / 月** 三维度切换，配套自定义日期选择器（日历面板支持周维度整周高亮、未来日期禁用）
- 概览指标条：总游玩时长、游玩游戏数、最常玩游戏、平均时长
- 时长分布图：日维度为环形图（今日游戏构成），周/月维度为柱形图，点击柱形/扇区可与右侧面板联动筛选
- 游戏时长排行：支持按时长 / 按名称排序，显示时长与占比进度条
- 日维度近 7 日趋势面板：迷你柱形图 + 7 日总计 / 日均 / 最高单日 / 活跃天数摘要 + 每日明细（与选中日对比涨跌）

### 模块二：游戏统计

- 概览卡片：库中游戏数、累计游玩时长（含近 30 天增量）、时长最长游戏
- 游戏分布：游玩状态 / 游戏引擎 / 制作公司三标签，自适应网格展示分类统计
- 总时长排行 TOP 10（横向进度条）
- 年度游玩强度热力图（GitHub 贡献图风格，5 级颜色深度，悬浮查看每日时长）

### 其他

- 深色 / 浅色主题自适应（深色主题沿用原型 `#1b2838` 配色）
- 中 / 英 / 日三语（跟随宿主语言）
- 偏好持久化：默认维度、排行排序、分布标签（仅保存偏好，统计数据每次从宿主实时计算）

## 技术说明

- 界面全部使用 C# 描述（无 XAML），规避插件 XAML namespace stamping 的调试问题（见 `doc/ui.md`）
- 图表为 WinUI 原生自绘（`Path` 环形图 / `Rectangle` 柱形图），无第三方图表依赖
- 统计数据来自宿主 `GetAllGames()` 快照（`PlayedTime` 日期→分钟、`TotalPlayTime`、`PlayType`、`Engine`、`Developer`），无冗余持久化

## 构建方式

### 环境要求

- **.NET 8 SDK**
- **Windows 10 SDK 10.0.19041+**
- **Windows App SDK 2.1+**（NuGet 自动还原）

### 使用 `dotnet` CLI 构建

```powershell
# 还原依赖
dotnet restore PotatoVN.App.Plugin.sln

# 构建（Debug / Release）
dotnet build PotatoVN.App.Plugin.sln -c Release
```

### 构建产物

插件 DLL 位于 `PotatoVN.App.PluginBase\bin\Release\net8.0-windows10.0.22621.0\`，文件名为 `A9c3f7d214b8a4e6c8f2d7a5b1e0c9d43.dll`。
Release 构建还会在 `artifacts\` 生成 `plugin.pvnplugin.zip` 插件包。

将产物放入 PotatoVN 的插件目录即可加载，入口为侧边栏「游戏统计」按钮。

## 开发文档

插件开发文档位于 `doc/` 目录，建议先阅读 `doc/main.md`。
