# DiskCleaner 需求规格说明书

## 文档信息

- **版本号**: 1.0
- **创建日期**: 2026-05-17
- **最后更新**: 2026-05-17
- **状态**: 已批准

---

## 1. 产品概述

### 1.1 产品定位

DiskCleaner 是一款面向 Windows 系统的免安装单文件 EXE 系统清理与深度卸载工具，旨在帮助用户安全、高效地清理系统垃圾文件、管理大文件、彻底卸载软件并清除残留。

### 1.2 目标用户

- 个人用户：希望清理系统垃圾、释放磁盘空间
- 高级用户：需要深度卸载软件并清除所有残留
- IT 管理员：批量维护多台 Windows 设备

### 1.3 核心价值主张

- **免安装**: 单文件 EXE，双击即可运行
- **深度清理**: 智能识别垃圾文件、不常用文件、大文件
- **彻底卸载**: 清除软件本体、残留文件与注册表项
- **安全可靠**: 三重保护机制避免误删系统文件
- **现代界面**: 毛玻璃 + 液态玻璃动态效果

---

## 2. 功能需求

### 2.1 智能磁盘扫描引擎

#### 2.1.1 垃圾文件识别

**FR-SCAN-001**: 系统应扫描以下系统临时目录：
- `%TEMP%`
- `%WINDIR%\Temp`
- `%WINDIR%\Prefetch`
- `%WINDIR%\SoftwareDistribution\Download`

**FR-SCAN-002**: 系统应扫描以下浏览器缓存目录：
- Chrome: `%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache`
- Edge: `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache`
- Firefox: `%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*.default\cache2`

**FR-SCAN-003**: 系统应识别以下类型的垃圾文件：
- 回收站内容（`$Recycle.Bin`）
- 日志文件（`*.log`、`*.etl`、`*.dmp`）
- Windows 更新缓存
- 缩略图缓存（`thumbcache_*.db`）
- 字体缓存（`%WINDIR%\ServiceProfiles\LocalService\AppData\Local\FontCache`）

**FR-SCAN-004**: 系统应提供可配置的扫描规则，用户可启用/禁用各类垃圾文件扫描。

#### 2.1.2 不常用文件识别

**FR-SCAN-005**: 系统应通过 NTFS 最后访问时间（LastAccessTime）判定不常用文件。

**FR-SCAN-006**: 系统应将超过 90 天（可配置）未访问且非系统/只读文件归类为不常用文件。

**FR-SCAN-007**: 系统应排除以下扩展名文件不被标记为不常用文件：
- `.exe`、`.dll`、`.sys`（可执行及系统库）
- 仅针对文档、媒体、压缩包等用户数据文件

#### 2.1.3 大文件扫描

**FR-SCAN-008**: 系统应递归遍历所有固定磁盘（DriveType.Fixed）。

**FR-SCAN-009**: 系统应统计大于 500MB（可配置）的单文件并单独标记。

**FR-SCAN-010**: 系统应显示大文件的完整路径、大小、修改时间。

#### 2.1.4 不重要文件识别

**FR-SCAN-011**: 系统应识别以下不重要文件：
- 重复文件（按 MD5 前 4KB + 文件大小快速比对）
- 空文件夹
- `Thumbs.db`、`.DS_Store`、`desktop.ini`（冗余）
- 临时 Office 恢复文件（`~$*.docx`、`~$*.xlsx` 等）

#### 2.1.5 扫描性能要求

**FR-SCAN-012**: 系统应使用多线程异步扫描，分磁盘并行执行。

**FR-SCAN-013**: 系统应实时展示扫描进度，包括：
- 当前扫描目录
- 已发现文件数
- 已扫描总大小百分比

**FR-SCAN-014**: 系统应支持取消扫描操作。

**FR-SCAN-015**: 系统应收集以下文件信息：
- 完整路径
- 大小（字节）
- 类型（垃圾/不常用/大文件/不重要）
- 最后访问时间
- 最后修改时间
- 占用状态（是否被进程锁定）

### 2.2 结果展示与操作

#### 2.2.1 列表视图

**FR-UI-001**: 系统应以列表视图展示扫描结果。

**FR-UI-002**: 系统应支持以下排序方式：
- 按文件大小（升序/降序）
- 按文件名称（升序/降序）
- 按文件类型（升序/降序）
- 按文件路径（升序/降序）

**FR-UI-003**: 系统应支持多选和全选操作。

**FR-UI-004**: 列表每项应显示以下信息：
- 复选框
- 文件名
- 大小（自动格式化为 KB/MB/GB）
- 完整目录路径
- 类型标签（带颜色区分：垃圾 - 红色、不常用 - 黄色、大文件 - 蓝色、不重要 - 灰色）
- 最后访问时间

#### 2.2.2 右键菜单

**FR-UI-005**: 系统应提供右键菜单，包含以下选项：
- 打开文件位置
- 强行删除（解除占用后删除）
- 属性

#### 2.2.3 删除确认

**FR-CLEAN-001**: 系统应在删除前弹窗二次确认。

**FR-CLEAN-002**: 删除确认对话框应显示：
- 待删除文件数量
- 释放空间总量
- 待删除文件列表（可展开查看详情）
- 警告提示（如包含系统文件）

**FR-CLEAN-003**: 用户可取消删除操作。

#### 2.2.4 强制删除机制

**FR-CLEAN-004**: 系统应调用 Windows API 关闭文件句柄以解除占用。

**FR-CLEAN-005**: 系统应申请以下特权以支持强制删除：
- `SE_BACKUP_NAME`
- `SE_RESTORE_NAME`
- `SE_DEBUG_NAME`

**FR-CLEAN-006**: 若 API 方式失败，系统应使用 `MoveFileEx` 标记为重启删除。

**FR-CLEAN-007**: 若仍失败，系统应调用 Sysinternals Handle 工具释放占用后删除。

**FR-CLEAN-008**: 系统应记录删除操作日志，包括成功/失败状态及原因。

### 2.3 软件深度卸载模块

#### 2.3.1 已安装程序列表

**FR-UNINSTALL-001**: 系统应从以下注册表路径读取已安装程序：
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

**FR-UNINSTALL-002**: 系统应展示以下程序信息：
- 名称（DisplayName）
- 发布者（Publisher）
- 安装日期（InstallDate）
- 大小（EstimatedSize）
- 版本（DisplayVersion）
- 卸载字符串（UninstallString）
- 安装位置（InstallLocation）

**FR-UNINSTALL-003**: 系统应支持导出已安装软件列表为 CSV 文件。

#### 2.3.2 深度卸载流程

**FR-UNINSTALL-004**: 系统应首先运行原版卸载字符串（UninstallString）。

**FR-UNINSTALL-005**: 若已知静默参数，系统应使用静默模式执行卸载（如 `/quiet`、`/silent`、`/S`）。

**FR-UNINSTALL-006**: 卸载完成后，系统应扫描并删除以下位置的残留文件夹：
- InstallLocation 指定路径
- DisplayIcon 路径推断的目录
- `%ProgramFiles%` 下同名或发布者文件夹
- `%AppData%` 下同名或发布者文件夹
- `%LocalAppData%` 下同名或发布者文件夹
- `%ProgramData%` 下同名或发布者文件夹

**FR-UNINSTALL-007**: 系统应扫描并清理以下注册表残留：
- `HKLM\Software\[Publisher]\[AppName]`
- `HKCU\Software\[Publisher]\[AppName]`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall` 残余键
- `HKCR` 中的文件关联（需谨慎删除）
- 相关服务注册项

**FR-UNINSTALL-008**: 系统应提供强制卸载模式，跳过官方卸载，直接执行残留文件和注册表清理。

**FR-UNINSTALL-009**: 系统应在卸载前自动备份注册表项到临时文件（`.reg` 格式），供手动恢复。

**FR-UNINSTALL-010**: 备份文件应保存在 `%TEMP%\DiskCleaner\backup\` 目录。

**FR-UNINSTALL-011**: 备份文件命名格式：`Uninstall_{AppName}_{yyyyMMdd_HHmmss}.reg`。

**FR-UNINSTALL-012**: 系统应保留备份文件 7 天后自动清理。

#### 2.3.3 卸载确认与安全

**FR-UNINSTALL-013**: 系统应在卸载前展示待清理的注册表项和文件列表。

**FR-UNINSTALL-014**: 用户应可勾选/取消勾选待清理项（默认全选）。

**FR-UNINSTALL-015**: 系统应提供可选的系统还原点创建功能（需系统保护已开启）。

**FR-UNINSTALL-016**: 系统应记录完整操作日志到 `%TEMP%\DiskCleaner\logs\`。

### 2.4 系统安全与保护

#### 2.4.1 管理员权限

**FR-SEC-001**: 应用程序启动时应自动请求管理员权限。

**FR-SEC-002**: 应用应通过 `app.manifest` 设置 `requestedExecutionLevel level="requireAdministrator"`。

#### 2.4.2 白名单保护机制

**FR-SEC-003**: 系统应内置硬编码系统关键路径白名单：
- `C:\Windows\System32`
- `C:\Windows\WinSxS`
- `C:\Program Files\WindowsApps`
- `C:\ProgramData\Microsoft\Windows`
- 其他系统关键目录

**FR-SEC-004**: 系统应动态检测文件属性，跳过以下文件：
- 具有 `FILE_ATTRIBUTE_SYSTEM` 属性的文件
- 具有 `FILE_ATTRIBUTE_READONLY` 属性的文件

**FR-SEC-005**: 系统应检查文件 ACL 权限，无写入权限的文件应跳过。

**FR-SEC-006**: 系统应支持用户自定义白名单，保存到配置文件。

**FR-SEC-007**: 删除受保护文件时，系统应强制二次确认并显示警告提示。

#### 2.4.3 审计模式

**FR-SEC-008**: 系统应提供"仅扫描、不删除"的审计模式。

**FR-SEC-009**: 审计模式下应生成详细报告，但不执行任何删除操作。

### 2.5 用户界面与交互

#### 2.5.1 窗口效果

**FR-UI-010**: 窗口背景应使用 Acrylic 或 Mica 材料，实现半透明模糊效果。

**FR-UI-011**: 模糊效果应随桌面壁纸颜色变化。

**FR-UI-012**: 窗口大小改变时，模糊层应平滑过渡（液态效果）。

**FR-UI-013**: 按钮悬停时应有流体光波动画效果。

**FR-UI-014**: 滚动列表时，内容应有弹性缓出动画。

**FR-UI-015**: 界面应跟随系统深色/浅色模式自动切换。

**FR-UI-016**: 系统应支持手动切换主题。

#### 2.5.2 窗口样式

**FR-UI-017**: 主窗口应为无标题栏或自定义标题栏设计。

**FR-UI-018**: 窗口应为圆角设计（Win11 风格）。

**FR-UI-019**: 窗口应融入模糊背景，通过 `DwmExtendFrameIntoClientArea` 实现。

#### 2.5.3 界面布局

**FR-UI-020**: 界面应包含左侧导航栏（可收起），包含以下图标按钮：
- 清理
- 软件卸载
- 设置

**FR-UI-021**: 右侧应为内容面板。

**FR-UI-022**: 顶部应显示扫描按钮与状态指示器。

**FR-UI-023**: 下方应为文件列表区域。

**FR-UI-024**: 底部应显示信息栏，包括：
- 已选文件大小
- 总大小
- 文件数量统计

**FR-UI-025**: 扫描中列表项应显示轻量骨架屏动画。

#### 2.5.4 国际化

**FR-UI-026**: 系统应支持中英文切换。

**FR-UI-027**: 语言设置应保存到配置文件。

### 2.6 配置与持久化

**FR-CONFIG-001**: 系统应使用 JSON 格式配置文件，保存在 `%APPDATA%\DiskCleaner\config.json`。

**FR-CONFIG-002**: 配置文件应包含以下设置：
- 扫描规则启用/禁用状态
- 不常用文件判定天数阈值
- 大文件大小阈值
- 自定义白名单路径列表
- 界面语言设置
- 主题设置（自动/深色/浅色）

---

## 3. 非功能需求

### 3.1 性能要求

**NFR-PERF-001**: 扫描 500GB 磁盘应在 5 分钟内完成初步扫描。

**NFR-PERF-002**: 界面响应时间应小于 100ms（扫描过程中除外）。

**NFR-PERF-003**: 删除 1000 个文件应在 30 秒内完成（不含大文件）。

**NFR-PERF-004**: 应用程序启动时间应小于 3 秒。

### 3.2 兼容性要求

**NFR-COMPAT-001**: 系统应支持 Windows 10 版本 1809 及以上。

**NFR-COMPAT-002**: 毛玻璃效果在 Windows 10 1903+ 上完整支持，早期版本降级为普通背景。

**NFR-COMPAT-003**: 系统应支持 x64 架构。

### 3.3 可靠性要求

**NFR-REL-001**: 系统崩溃时不应损坏用户数据。

**NFR-REL-002**: 删除操作应具有原子性，失败时回滚。

**NFR-REL-003**: 系统应记录详细日志以便故障排查。

### 3.4 安全性要求

**NFR-SEC-001**: 系统不应收集或上传用户隐私数据。

**NFR-SEC-002**: 注册表备份文件应设置适当权限，防止未授权访问。

**NFR-SEC-003**: 系统不应执行任何网络操作（除可选的版本检查外）。

### 3.5 可维护性要求

**NFR-MAINT-001**: 代码应遵循 MVVM 模式，便于单元测试。

**NFR-MAINT-002**: 核心业务逻辑应无 UI 依赖。

**NFR-MAINT-003**: 代码注释覆盖率应大于 30%。

### 3.6 打包与发布要求

**NFR-DEPLOY-001**: 应用程序应打包为单个 EXE 文件。

**NFR-DEPLOY-002**: EXE 应为自包含（self-contained），无需安装 .NET 运行时。

**NFR-DEPLOY-003**: 构建过程应通过 GitHub Actions 自动化。

**NFR-DEPLOY-004**: 构建失败时应自动重试 3 次，间隔 10 秒。

**NFR-DEPLOY-005**: 构建成功后应：
- 提交 EXE 到 `build/` 目录
- 创建 GitHub Release 并上传 EXE 作为 Asset

**NFR-DEPLOY-006**: Release 版本号格式：`Build-{YYYYMMDD}-{commit_sha_short}`。

---

## 4. 验收标准

### 4.1 功能验收

- [ ] 扫描所有磁盘分区并正确分类文件
- [ ] 支持多选、排序、右键操作
- [ ] 强制删除功能正常工作（包括占用文件）
- [ ] 软件卸载功能正常，注册表和残留文件清理彻底
- [ ] 白名单机制有效防止误删系统文件
- [ ] 审计模式仅扫描不删除

### 4.2 界面验收

- [ ] 毛玻璃效果正常显示
- [ ] 液态动画流畅（按钮悬停、滚动弹性）
- [ ] 主题切换正常工作
- [ ] 中英文切换正常

### 4.3 构建验收

- [ ] GitHub Actions 自动构建成功
- [ ] EXE 文件可在 `build/` 目录下载
- [ ] GitHub Release 自动创建并包含 EXE

---

## 5. 术语表

| 术语 | 定义 |
|------|------|
| Acrylic | Windows 10/11 的亚克力模糊效果材质 |
| Mica | Windows 11 的云母效果材质 |
| NTFS | Windows 文件系统，支持 LastAccessTime 等元数据 |
| ACL | 访问控制列表（Access Control List） |
| UAC | 用户账户控制（User Account Control） |
| MVVM | 模型 - 视图 - 视图模型设计模式 |

---

## 6. 参考文献

- [Windows App SDK 文档](https://docs.microsoft.com/windows/apps/windows-app-sdk/)
- [WinUI 3 设计指南](https://docs.microsoft.com/windows/apps/winui/winui3/)
- [.NET 8 发布文档](https://docs.microsoft.com/dotnet/core/deploying/single-file/)
- [Sysinternals Handle 工具](https://learn.microsoft.com/sysinternals/downloads/handle)
