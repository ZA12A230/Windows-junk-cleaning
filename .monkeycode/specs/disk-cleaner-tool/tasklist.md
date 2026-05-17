# DiskCleaner 实施任务列表

## 项目信息

- **需求文档**: `requirements.md`
- **设计文档**: `design.md`
- **创建日期**: 2026-05-17
- **预计工期**: 10-14 天

---

## 阶段一：项目初始化 (1-2 天)

### 任务 1.1: 创建项目结构
- [ ] 创建解决方案文件 `DiskCleaner.sln`
- [ ] 创建 WPF 项目 `src/DiskCleaner/DiskCleaner.csproj`
- [ ] 创建目录结构（Core/, UI/, Services/, Models/, Helpers/）
- [ ] 配置 `.gitignore`
- [ ] 配置 `Directory.Build.props`

### 任务 1.2: 配置项目依赖
- [ ] 添加 Windows App SDK NuGet 包
- [ ] 添加 Microsoft.Extensions.DependencyInjection
- [ ] 添加 Microsoft.Extensions.Logging
- [ ] 添加 System.Text.Json
- [ ] 配置 app.manifest（管理员权限）

### 任务 1.3: 实现基础框架
- [ ] 创建 `App.xaml` 和 `App.xaml.cs`
- [ ] 配置依赖注入容器
- [ ] 实现全局异常处理
- [ ] 创建日志系统（Serilog 或 Microsoft.Extensions.Logging）

### 任务 1.4: 创建主窗口框架
- [ ] 创建 `MainWindow.xaml` 基础布局
- [ ] 实现左侧导航栏（NavigationView）
- [ ] 实现 ContentFrame 导航
- [ ] 应用 Acrylic/Mica 背景效果

---

## 阶段二：核心模块实现 (3-4 天)

### 任务 2.1: 实现文件扫描引擎
- [ ] 创建 `Core/Scanning/ScannerEngine.cs`
- [ ] 实现 `ScanAllDrivesAsync` 方法
- [ ] 实现 `ScanDirectoryAsync` 异步遍历
- [ ] 使用 `Parallel.ForEachAsync` 控制并发
- [ ] 实现进度上报 `IProgress<ScanProgress>`
- [ ] 实现取消扫描功能

### 任务 2.2: 实现文件分类器
- [ ] 创建 `Core/Scanning/FileClassifier.cs`
- [ ] 实现垃圾文件识别规则
- [ ] 实现不常用文件判定（LastAccessTime）
- [ ] 实现大文件检测
- [ ] 实现不重要文件检测（重复文件、空文件夹等）
- [ ] 实现 MD5 前 4KB 快速比对

### 任务 2.3: 实现 Native API 封装
- [ ] 创建 `Helpers/NativeMethods.cs`
- [ ] 封装 NtQuerySystemInformation
- [ ] 封装 MoveFileEx
- [ ] 封装 DeleteFile
- [ ] 封装 CloseHandle
- [ ] 封装进程令牌和权限操作

### 任务 2.4: 实现权限提升
- [ ] 创建 `Helpers/PrivilegeHelper.cs`
- [ ] 实现申请 SE_DEBUG_NAME 特权
- [ ] 实现申请 SE_BACKUP_NAME 特权
- [ ] 实现申请 SE_RESTORE_NAME 特权

### 任务 2.5: 实现文件服务
- [ ] 创建 `Services/FileService.cs`
- [ ] 实现普通删除
- [ ] 实现强制删除（API + Handle.exe）
- [ ] 实现重启删除回退
- [ ] 集成 Handle.exe 工具调用

### 任务 2.6: 实现进程服务
- [ ] 创建 `Services/ProcessService.cs`
- [ ] 实现获取文件占用进程
- [ ] 实现关闭文件句柄
- [ ] 实现进程句柄枚举

---

## 阶段三：软件卸载模块 (2-3 天)

### 任务 3.1: 实现注册表服务
- [ ] 创建 `Services/RegistryService.cs`
- [ ] 实现读取已安装软件列表
- [ ] 实现扫描 HKLM 和 HKCU 路径
- [ ] 实现 WOW6432Node 分支支持
- [ ] 实现注册表项导出为 .reg

### 任务 3.2: 实现卸载引擎
- [ ] 创建 `Core/Uninstall/UninstallEngine.cs`
- [ ] 实现执行官方卸载字符串
- [ ] 实现静默卸载参数检测
- [ ] 实现等待卸载完成
- [ ] 实现强制卸载模式

### 任务 3.3: 实现残留扫描
- [ ] 实现残留文件扫描（ProgramFiles、AppData 等）
- [ ] 实现残留注册表扫描
- [ ] 实现注册表树形展示数据结构
- [ ] 实现智能匹配（按发布者、应用名）

### 任务 3.4: 实现注册表备份
- [ ] 创建 `Core/Registry/RegistryBackup.cs`
- [ ] 实现备份到 .reg 文件
- [ ] 实现备份文件管理（7 天自动清理）
- [ ] 实现手动恢复功能

---

## 阶段四：清理引擎实现 (1-2 天)

### 任务 4.1: 实现清理引擎
- [ ] 创建 `Core/Cleaning/CleanerEngine.cs`
- [ ] 实现批量删除
- [ ] 实现删除策略模式
- [ ] 实现删除进度跟踪
- [ ] 实现删除结果汇总

### 任务 4.2: 实现白名单保护
- [ ] 创建硬编码系统路径白名单
- [ ] 实现文件属性检测（SYSTEM、READONLY）
- [ ] 实现 ACL 权限检查
- [ ] 实现用户自定义白名单加载
- [ ] 实现受保护文件删除二次确认

---

## 阶段五：UI 层实现 (3-4 天)

### 任务 5.1: 实现清理页面
- [ ] 创建 `UI/Views/CleanView.xaml`
- [ ] 实现扫描按钮和进度条
- [ ] 实现文件列表（ListView + 虚拟化）
- [ ] 实现列排序功能
- [ ] 实现多选和全选
- [ ] 实现右键菜单

### 任务 5.2: 实现清理 ViewModel
- [ ] 创建 `UI/ViewModels/CleanViewModel.cs`
- [ ] 实现 ScanAsync 命令
- [ ] 实现 DeleteAsync 命令
- [ ] 实现 CancelAsync 命令
- [ ] 实现文件列表 ObservableCollection
- [ ] 实现进度属性绑定

### 任务 5.3: 实现卸载页面
- [ ] 创建 `UI/Views/UninstallView.xaml`
- [ ] 实现软件列表展示
- [ ] 实现软件详情面板
- [ ] 实现残留项树形展示
- [ ] 实现卸载进度对话框

### 任务 5.4: 实现卸载 ViewModel
- [ ] 创建 `UI/ViewModels/UninstallViewModel.cs`
- [ ] 实现 LoadSoftwareListAsync
- [ ] 实现 UninstallAsync 命令
- [ ] 实现 ExportToCsvAsync
- [ ] 实现残留项勾选状态管理

### 任务 5.5: 实现设置页面
- [ ] 创建 `UI/Views/SettingsView.xaml`
- [ ] 实现扫描规则配置
- [ ] 实现阈值配置（天数、大小）
- [ ] 实现白名单管理
- [ ] 实现语言和主题切换
- [ ] 实现配置保存

### 任务 5.6: 实现值转换器
- [ ] 创建 `UI/Converters/FileSizeConverter.cs`
- [ ] 创建 `UI/Converters/FileTypeToColorConverter.cs`
- [ ] 创建 `UI/Converters/BoolToVisibilityConverter.cs`
- [ ] 创建 `UI/Converters/DateTimeFormatConverter.cs`

### 任务 5.7: 实现自定义控件
- [ ] 创建 `UI/Controls/FileListItem.xaml`
- [ ] 创建 `UI/Controls/SoftwareListItem.xaml`
- [ ] 创建 `UI/Controls/ProgressRing.xaml`
- [ ] 实现骨架屏加载动画

### 任务 5.8: 实现液态动画效果
- [ ] 创建 `Helpers/AcrylicHelper.cs`
- [ ] 使用 Composition API 实现按钮悬停动画
- [ ] 实现滚动弹性效果
- [ ] 实现窗口大小变化平滑过渡

---

## 阶段六：配置和工具类 (1 天)

### 任务 6.1: 实现配置服务
- [ ] 创建 `Services/ConfigService.cs`
- [ ] 实现 config.json 读写
- [ ] 实现配置验证
- [ ] 实现配置变更通知

### 任务 6.2: 实现系统信息服务
- [ ] 创建 `Services/SystemInfoService.cs`
- [ ] 实现获取磁盘信息
- [ ] 实现获取系统版本
- [ ] 实现检查 Windows 版本兼容性

### 任务 6.3: 实现文件哈希辅助
- [ ] 创建 `Helpers/FileHashHelper.cs`
- [ ] 实现增量 MD5 计算（前 4KB）
- [ ] 实现重复文件检测

---

## 阶段七：GitHub Actions 配置 (0.5 天)

### 任务 7.1: 配置工作流
- [ ] 创建 `.github/workflows/build-exe.yml`
- [ ] 配置 .NET 8 SDK
- [ ] 配置 retry 动作（3 次重试）
- [ ] 配置构建命令
- [ ] 配置 EXE 验证

### 任务 7.2: 配置发布流程
- [ ] 配置 Artifact 上传
- [ ] 配置 build 目录提交（[skip ci]）
- [ ] 配置 GitHub Release 创建
- [ ] 配置 Release Asset 上传

---

## 阶段八：测试和调试 (2-3 天)

### 任务 8.1: 单元测试
- [ ] 编写 FileClassifier 测试
- [ ] 编写扫描引擎测试（模拟目录）
- [ ] 编写配置读写测试
- [ ] 编写 ViewModel 命令测试

### 任务 8.2: 集成测试
- [ ] 测试真实目录扫描
- [ ] 测试文件删除功能
- [ ] 测试注册表备份恢复
- [ ] 测试软件卸载流程

### 任务 8.3: UI 测试
- [ ] 测试毛玻璃效果
- [ ] 测试液态动画流畅度
- [ ] 测试主题切换
- [ ] 测试中英文切换

### 任务 8.4: 性能测试
- [ ] 测试 500GB 磁盘扫描时间
- [ ] 测试大量文件删除性能
- [ ] 测试 UI 响应时间
- [ ] 优化慢查询和瓶颈

---

## 阶段九：文档和优化 (1 天)

### 任务 9.1: 代码注释
- [ ] 补充 Core 层 XML 注释
- [ ] 补充 Services 层 XML 注释
- [ ] 补充 ViewModel 层注释

### 任务 9.2: README 文档
- [ ] 编写项目介绍
- [ ] 编写功能特性说明
- [ ] 编写使用方法
- [ ] 编写构建说明
- [ ] 编写截图展示

### 任务 9.3: 性能优化
- [ ] 分析内存使用
- [ ] 优化扫描速度
- [ ] 优化 UI 渲染性能
- [ ] 优化启动时间

---

## 交付物清单

- [ ] `build/DiskCleaner.exe` - 单文件可执行程序
- [ ] GitHub Release - 包含 EXE 的正式发行版
- [ ] 完整源代码 - 清晰注释
- [ ] 需求文档 - `requirements.md`
- [ ] 设计文档 - `design.md`
- [ ] README.md - 项目说明文档

---

## 风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| WinUI 3 毛玻璃效果兼容性问题 | 高 | 中 | 准备降级方案（普通背景） |
| Handle.exe 工具集成失败 | 中 | 低 | 准备纯 API 方案备选 |
| 扫描性能不达标 | 中 | 中 | 提前性能测试，优化并发策略 |
| GitHub Actions 构建超时 | 低 | 中 | 使用 retry 机制，增加超时时间 |
| 管理员权限导致用户体验差 | 中 | 高 | 添加 UAC 说明，提供免管理员模式（功能受限） |

---

## 验收标准

1. **功能验收**
   - [ ] 扫描功能正常，分类准确
   - [ ] 删除功能正常，包括占用文件
   - [ ] 软件卸载功能正常，残留清理彻底
   - [ ] 白名单机制有效
   - [ ] 审计模式正常工作

2. **界面验收**
   - [ ] 毛玻璃效果显示正常
   - [ ] 动画流畅（60 FPS）
   - [ ] 主题切换正常
   - [ ] 中英文切换正常

3. **构建验收**
   - [ ] GitHub Actions 自动构建成功
   - [ ] EXE 可在 build 目录下载
   - [ ] Release 自动创建

4. **性能验收**
   - [ ] 500GB 磁盘扫描 < 5 分钟
   - [ ] UI 响应 < 100ms
   - [ ] 启动时间 < 3 秒

---

## 下一步行动

**立即执行**: 任务 1.1 - 创建项目结构
