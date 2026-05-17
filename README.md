# DiskCleaner

Windows 系统清理与深度卸载工具 - 单文件免安装 EXE

## 功能特性

### 🧹 智能磁盘扫描
- **垃圾文件识别**: 系统临时文件、浏览器缓存、回收站、日志文件、Windows 更新缓存
- **不常用文件**: 超过 90 天未访问的非系统文件
- **大文件检测**: 自动扫描大于 500MB 的文件
- **不重要文件**: 重复文件、空文件夹、冗余系统文件

### 🔧 强力删除
- 解除文件占用强制删除
- 支持重启删除（MoveFileEx）
- 集成 Sysinternals Handle 工具

### 🗑️ 软件深度卸载
- 读取已安装程序列表
- 执行官方卸载程序
- 扫描并删除残留文件和注册表项
- 自动备份注册表供恢复

### 🎨 现代化界面
- 毛玻璃（Acrylic/Mica）动态效果
- 液态动画过渡
- 跟随系统深色/浅色模式
- 中英文切换支持

## 系统要求

- **操作系统**: Windows 10 1809+ (推荐 Windows 11)
- **架构**: x64
- **.NET**: 无需安装（自包含 EXE）
- **权限**: 管理员权限（UAC 自动请求）

## 使用方法

### 下载
从 [GitHub Releases](https://github.com/yourusername/DiskCleaner/releases) 下载最新版本的 `DiskCleaner.exe`

### 运行
1. 双击运行 `DiskCleaner.exe`
2. 允许 UAC 提权请求
3. 点击"开始扫描"按钮
4. 等待扫描完成后选择要清理的文件
5. 点击"清理选中"或"清理全部"

### 软件卸载
1. 切换到"软件卸载"标签页
2. 选择要卸载的软件
3. 点击"卸载"按钮
4. 确认删除残留文件和注册表项

## 构建说明

### 环境要求
- .NET 8 SDK
- Windows 10/11
- Visual Studio 2022 (可选)

### 本地构建

```bash
# 还原依赖
dotnet restore

# 发布单文件 EXE
dotnet publish src/DiskCleaner/DiskCleaner.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:WindowsPackageType=None `
  -o build
```

构建产物位于 `build/DiskCleaner.exe`

### GitHub Actions

本项目配置了自动构建工作流：
- Push 到 main 分支自动触发构建
- 构建失败自动重试 3 次
- 成功后提交 EXE 到 `build/` 目录
- 自动创建 GitHub Release

## 项目结构

```
DiskCleaner/
├── src/DiskCleaner/
│   ├── Core/              # 核心业务逻辑
│   │   ├── Scanning/      # 扫描引擎
│   │   ├── Cleaning/      # 清理引擎
│   │   └── Uninstall/     # 卸载引擎
│   ├── UI/                # 界面层
│   │   ├── Views/         # XAML 页面
│   │   ├── ViewModels/    # MVVM 视图模型
│   │   └── Converters/    # 值转换器
│   ├── Services/          # 系统服务
│   ├── Models/            # 数据模型
│   └── Helpers/           # 工具类
├── .github/workflows/     # GitHub Actions
├── build/                 # 构建输出
└── .monkeycode/specs/     # 需求与设计文档
```

## 技术栈

- **UI 框架**: WinUI 3 (Windows App SDK)
- **语言**: C# 12 (.NET 8)
- **架构**: MVVM
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **打包**: Single File EXE (Self-contained)

## 安全说明

- 内置系统关键路径白名单
- 动态检测文件属性和 ACL 权限
- 删除受保护文件时二次确认
- 注册表操作前自动备份

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

## 致谢

- [Windows App SDK](https://docs.microsoft.com/windows/apps/windows-app-sdk/)
- [Sysinternals Handle](https://learn.microsoft.com/sysinternals/downloads/handle)
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/dotnet/communitytoolkit/mvvm/)
