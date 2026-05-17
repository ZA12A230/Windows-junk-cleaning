# SystemCleanerPro

Windows 系统清理与深度卸载工具

## 功能特性

### 🧹 智能磁盘扫描
- **垃圾文件清理**: 系统临时文件、浏览器缓存、日志文件、Windows 更新缓存、缩略图缓存
- **不常用文件**: 自动识别 90 天以上未访问的文件
- **大文件扫描**: 快速定位超过 500MB 的文件
- **不重要文件**: 重复文件、空文件夹、临时文件清理

### 🔒 强力删除
- **移动到回收站**: 普通删除，可恢复
- **永久删除**: 直接删除，不可恢复
- **强力删除**: 多次覆写，军事级安全删除

### ⚙️ 深度卸载
- 扫描已安装软件
- 执行程序自带卸载程序
- 清理残留文件和文件夹
- 清理注册表残留项

### 🎨 界面特色
- **毛玻璃效果**: 半透明磨砂背景
- **液态玻璃动画**: 动态渐变效果
- **现代化设计**: 深色主题，电光青强调色

## 技术栈

- **.NET 8.0** - 运行时框架
- **WPF** - 用户界面框架
- **CommunityToolkit.Mvvm** - MVVM 模式工具包

## 构建说明

### 前置要求
- .NET 8.0 SDK
- Windows 10/11

### 构建命令

```bash
# 还原依赖
dotnet restore SystemCleanerPro/SystemCleanerPro.csproj

# 构建项目
dotnet build SystemCleanerPro/SystemCleanerPro.csproj --configuration Release

# 发布单文件 EXE
dotnet publish SystemCleanerPro/SystemCleanerPro.csproj \
  --configuration Release \
  --output ./build \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true
```

### CI/CD 自动构建

推送到 main 分支或创建 tag 时会自动触发 GitHub Actions 构建：

```bash
# 创建 tag 触发发布
git tag v1.0.0
git push origin v1.0.0
```

构建产物将自动发布到 GitHub Releases。

## 使用说明

1. 运行 `SystemCleanerPro.exe`
2. 点击「开始扫描」扫描所有磁盘
3. 选择要清理的文件类型
4. 选择删除模式
5. 点击「开始清理」

## 项目结构

```
SystemCleanerPro/
├── Models/           # 数据模型
│   ├── FileItem.cs
│   ├── ScanConfiguration.cs
│   └── SoftwareInfo.cs
├── Services/         # 业务逻辑
│   ├── ScanService.cs
│   ├── CleanService.cs
│   └── UninstallService.cs
├── ViewModels/       # 视图模型
│   └── MainViewModel.cs
├── App.xaml          # 应用资源
├── App.xaml.cs
├── MainWindow.xaml   # 主窗口
├── MainWindow.xaml.cs
└── SystemCleanerPro.csproj
```

## 许可证

MIT License
