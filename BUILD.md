# 构建说明

## 项目概览

本项目包含两个主要构建目标：

| 目标 | 项目 | 框架 | 输出 |
|------|------|------|------|
| **桌面客户端** | `GalgameManager` | .NET 8 + WinUI 3 | MSIX 安装包 |
| **后端服务** | `GalgameManager.Server` | .NET 8 ASP.NET Core | Docker 镜像 / 自部署 |

其他支持项目：`GalgameManager.Core`（公共库）、`GalgameManager.WinApp.Base`（WinUI 基类库）、`GalgameManager.Test` / `GalgameManager.Server.Test`（测试）、`GalgameManager.Tool`（C++ 数据处理工具）。

---

## 环境要求

### 桌面客户端

- **Visual Studio 2022**（17.5+），需安装以下工作负载：
  - .NET 桌面开发（`.NET desktop development`）
  - Windows 应用 SDK（`Windows App SDK` / `MSIX Packaging`）
  - 或直接导入 `.vsconfig`（打开解决方案时 VS 会提示安装缺失组件）
- **.NET 8 SDK**
- **Windows 10 SDK 10.0.19041+**
- **Windows App SDK 2.1.3**（NuGet 自动还原）

### 后端服务

- **.NET 8 SDK**（仅 `dotnet` CLI 即可，无需 Visual Studio）
- **Docker**（可选，用于容器化部署）

---

## 构建桌面客户端

### 使用 Visual Studio

1. 打开 `PotatoVN\GalgameManager.sln`
2. 选择配置：`Release | x64`（或 `x86` / `arm64`）
3. 生成 → 生成解决方案

### 使用命令行（MSBuild）

```powershell
# 以 x64 Release 为例，生成侧载 MSIX 包
msbuild PotatoVN\GalgameManager\GalgameManager.csproj `
  /restore `
  /p:Platform=x64 `
  /p:Configuration=Release `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxPackageDir=..\publish\ `
  /p:GenerateAppxPackageOnBuild=true `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile=GalgameManager_TemporaryKey.pfx
```

输出路径：`publish\`，包含 `.msix` 和 `.cer`（证书）文件。

### 使用 `dotnet` CLI

> 注意：MSIX 打包依赖 MSBuild 的 `GenerateAppxPackageOnBuild` 属性，`dotnet build` 不完全支持。如需完整打包，建议使用 MSBuild。

```powershell
# 仅编译（不生成 MSIX 包）
dotnet build PotatoVN\GalgameManager\GalgameManager.csproj `
  -c Release `
  -p:Platform=x64
```

### 构建产物

- `*.msix` — 侧载安装包，双击即可安装
- `*.cer` — 自签名证书，首次安装需先导入到"受信任的根证书颁发机构"

---

## 构建后端服务

### 使用 `dotnet` CLI

```powershell
# 还原并编译
dotnet build PotatoVN\GalgameManager.Server\GalgameManager.Server.csproj -c Release

# 发布到指定目录
dotnet publish PotatoVN\GalgameManager.Server\GalgameManager.Server.csproj `
  -c Release `
  -o .\server-publish `
  /p:UseAppHost=false
```

### 使用 Docker

```powershell
# 构建镜像
docker build -f PotatoVN\GalgameManager.Server\Dockerfile -t potatovn-server .

# 使用 docker-compose 启动（含 PostgreSQL）
docker-compose -f PotatoVN\GalgameManager.Server\docker-compose.yml up -d
```

Docker 构建支持通过 `CI_BUILD_SUFFIX` 参数注入版本号后缀：

```powershell
docker build `
  -f PotatoVN\GalgameManager.Server\Dockerfile `
  -t potatovn-server:latest `
  --build-arg CI_BUILD_SUFFIX=12345 `
  .
```

---

## 运行测试

```powershell
# 运行所有测试
dotnet test PotatoVN\GalgameManager.Test\GalgameManager.Test.csproj
dotnet test PotatoVN\GalgameManager.Server.Test\GalgameManager.Server.Test.csproj

# 或通过解决方案运行
dotnet test PotatoVN\GalgameManager.sln
```

注意：桌面客户端测试依赖 WinUI 环境，测试项目已通过 `Directory.Build.props` 配置禁用 WinAppSDK 自动初始化。

---

## 常见问题

### 1. MSBuild 找不到 `GenerateAppxPackageOnBuild`

确保已安装 MSBuild 工具，并使用 Visual Studio 的 Developer PowerShell（或 Developer Command Prompt）。

### 2. 构建失败："Windows SDK 版本不匹配"

在 `GalgameManager.csproj` 中检查 `TargetPlatformMinVersion`，或安装对应版本的 Windows SDK。

### 3. 测试运行失败："The specified runtimeconfig.json does not exist"

确保测试项目以 `x64` 平台运行（默认 `.runsettings` 已配置）。如使用命令行：

```powershell
dotnet test PotatoVN\GalgameManager.Test\GalgameManager.Test.csproj -p:Platform=x64
```

### 4. NuGet 包还原慢

项目已配置 NuGet 缓存路径（`NuGet.Config`），首次构建会自动还原依赖。如遇网络问题，可尝试：

```powershell
dotnet restore PotatoVN\GalgameManager.sln
```