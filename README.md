# PotatoVN插件脚手架

这个项目是一个基于PotatoVN的插件脚手架，旨在帮助开发者快速创建和开发PotatoVN插件。请阅读[插件开发文档](https://potatovn.net/development/client-plugin/quick-start.html)以获取更多关于插件开发的信息。

## 脚手架内容
本插件脚手架内包含了以下快捷功能，请自行选择是否保留：
* `Controls/Prefabs`: 包含了一系列预制的UI控件，方便开发者快速构建插件界面，建议使用它们来保持界面的一致性。
* `Controls/UserControl1.xaml`: 一个示例控件，显示在插件的设置界面中。
* `Helper/PluginLocalization.cs/LocalizationExtensions.cs`: 一个帮助类，用于处理插件的本地化字符串，方便开发者进行多语言支持。如果你的插件不需要支持多国语言，可以选择删除这个类。

## 构建方式

### 环境要求

- **.NET 8 SDK**
- **Windows 10 SDK 10.0.19041+**
- **Windows App SDK 2.1+**（NuGet 自动还原）

### 使用 `dotnet` CLI 构建

```powershell
# 还原依赖
dotnet restore PotatoVN.App.Plugin.sln

# 构建（Debug）
dotnet build PotatoVN.App.Plugin.sln -c Debug

# 构建（Release）
dotnet build PotatoVN.App.Plugin.sln -c Release
```

### 使用 Visual Studio

1. 打开 `PotatoVN.App.Plugin.sln`
2. 选择配置：`Release | Any CPU`
3. 生成 → 生成解决方案

### 构建产物

编译后插件 DLL 位于 `PotatoVN.App.PluginBase\bin\Release\net8.0-windows10.0.22621.0\`，文件名为 `A{your-plugin-id}.dll`（由 `AssemblyName` 决定）。

将生成的 DLL 放入 PotatoVN 的插件目录即可加载使用。

## AI开发指南
脚手架内准备了详细的文档供ai阅读，经测试，5.2-thinking-high及以上的ai完全有能力在无任何人工介入的情况下开发一个功能完整的potatovn插件。

你只需要告诉ai：文档位于`doc`下，应该先阅读`doc/main.md`，以及你的需求即可~
