# SystemCleanerPro - Windows 系统清理与深度卸载工具

## 1. 项目概述

**项目名称:** SystemCleanerPro  
**项目类型:** Windows 桌面应用程序  
**核心功能:** 免安装单文件 EXE 的系统清理与深度卸载工具，具备智能扫描、毛玻璃界面、强力删除和软件深度卸载功能  
**目标用户:** Windows 系统用户，需要清理磁盘空间和彻底卸载软件的用户

### 技术栈
- **框架:** WPF (.NET 8.0)
- **UI设计:** 毛玻璃 (Frosted Glass) + 液态玻璃 (Liquid Glass) 动态效果
- **构建工具:** dotnet publish，单文件自包含 EXE
- **CI/CD:** GitHub Actions

---

## 2. UI/UX 规格

### 2.1 布局结构

**主窗口:**
- 尺寸: 1200 x 800 像素（最小尺寸 900 x 600）
- 支持最大化、最小化、关闭
- 自定义标题栏（毛玻璃效果）
- 可拖拽移动

**主要区域:**
```
┌─────────────────────────────────────────────────────────┐
│  标题栏 (毛玻璃) - 系统图标 + 标题 + 最小化/最大化/关闭   │
├──────────────┬──────────────────────────────────────────┤
│              │                                          │
│   左侧导航   │           主内容区域                      │
│   (180px)    │      (扫描结果列表/详情显示)              │
│              │                                          │
│  ○ 磁盘扫描  │                                          │
│  ○ 垃圾清理  │                                          │
│  ○ 大文件    │                                          │
│  ○ 软件卸载  │                                          │
│              │                                          │
├──────────────┴──────────────────────────────────────────┤
│  底部状态栏 - 扫描进度 + 统计信息 + 操作按钮              │
└─────────────────────────────────────────────────────────┘
```

### 2.2 视觉设计

**配色方案:**
- 主色: #2D5AFC (活力蓝)
- 次色: #1A1A2E (深空黑)
- 强调色: #00D9FF (电光青)
- 背景色: rgba(30, 30, 50, 0.85) (半透明深蓝)
- 文字色: #FFFFFF (白) / #B0B0C0 (次要)
- 危险色: #FF4757 (删除/卸载)
- 成功色: #2ED573 (完成)

**毛玻璃效果:**
- 背景: 40% 不透明度
- 模糊半径: 20px
- 边框: 1px rgba(255, 255, 255, 0.1)
- 阴影: 0 8px 32px rgba(0, 0, 0, 0.3)

**液态玻璃效果:**
- 动态渐变动画 (3s 循环)
- 渐变色: #2D5AFC → #00D9FF → #2ED573
- 柔和发光效果
- hover 时波纹扩散

**字体:**
- 标题: Microsoft YaHei UI Bold, 20px
- 正文: Microsoft YaHei UI, 14px
- 辅助: Microsoft YaHei UI Light, 12px

**间距系统:**
- 基础单位: 8px
- 内边距: 16px / 24px
- 元素间距: 8px / 16px
- 圆角: 8px (小) / 12px (中) / 16px (大)

### 2.3 组件规格

**导航按钮:**
- 尺寸: 160 x 48px
- 默认: 透明背景 + 白色文字
- Hover: 毛玻璃背景 + 强调色边框
- Active: 渐变背景 + 白色文字
- 图标: 20x20px 左侧对齐

**文件列表项:**
- 高度: 56px
- 显示: 文件图标 + 名称 + 路径 + 大小 + 修改时间
- 多选框: 左侧 24x24px
- 复选框样式: 圆角方形，选中时渐变填充
- Hover: 背景亮度+10%
- 选中: 左边框 3px 强调色

**操作按钮:**
- 主按钮: 渐变背景 (#2D5AFC → #00D9FF)
- 次按钮: 毛玻璃背景 + 边框
- 危险按钮: #FF4757 背景
- 尺寸: 高度 40px，宽度自适应（最小 100px）
- Hover: 亮度+10%
- 禁用: 50% 透明度

**进度条:**
- 高度: 8px
- 背景: rgba(255, 255, 255, 0.1)
- 填充: 渐变动画 (左→右流动)
- 显示百分比文字

---

## 3. 功能模块详细规格

### 3.1 智能磁盘扫描引擎

**扫描范围:**
- 固定磁盘 (DriveType.Fixed): C:\, D:\, E:\ 等
- 排除: 可移动磁盘、网络磁盘

**垃圾文件识别规则 (可配置):**

| 类别 | 路径/规则 | 默认启用 |
|------|-----------|----------|
| 系统临时文件 | %TEMP%, %WINDIR%\Temp, %WINDIR%\Prefetch | ✓ |
| 浏览器缓存 | Chrome/Edge/Firefox Cache, Code Cache | ✓ |
| 回收站 | $Recycle.Bin | ✓ |
| 日志文件 | *.log, *.etl, *.dmp | ✓ |
| Windows 更新缓存 | SoftwareDistribution\Download | ✓ |
| 缩略图缓存 | thumbcache_*.db | ✓ |
| 用户临时文件 | %USERPROFILE%\AppData\Local\Temp | ✓ |

**不常用文件判定:**
- 条件: LastAccessTime > 90 天
- 排除: .exe, .dll, .sys, .ini (系统文件)
- 仅针对: 文档、媒体、压缩包等用户文件

**大文件扫描:**
- 阈值: > 500MB
- 递归遍历所有固定磁盘
- 单独分类标记

**不重要文件识别:**
- 重复文件: MD5(前4KB) + 文件大小
- 空文件夹
- Thumbs.db, .DS_Store, desktop.ini
- 临时 Office 恢复文件 (*.asd, ~$*)

### 3.2 文件清理模块

**清理模式:**
1. **标准删除:** 使用 RecycleBin (可恢复)
2. **永久删除:** 覆盖后删除 (不可恢复)
3. **强力删除:** 多次覆写 (军事级)

**文件占用解除:**
- 检测文件占用进程
- 可选终止进程后删除
- 支持强制解锁

**批量操作:**
- 全选/取消全选
- 按类型选择
- 按大小范围选择

### 3.3 软件深度卸载模块

**卸载流程:**
1. 读取注册表 Uninstall 信息
2. 执行程序自带的卸载程序
3. 扫描残留文件 (AppData, ProgramData)
4. 扫描残留注册表项
5. 用户确认后彻底清理

**注册表扫描位置:**
- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall
- HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall

**残留文件扫描:**
- %APPDATA% (Roaming)
- %LOCALAPPDATA%
- %PROGRAMDATA%
- 程序安装目录

---

## 4. 数据结构

### 4.1 扫描结果模型

```csharp
public class FileItem
{
    public string FullPath { get; set; }
    public string FileName { get; set; }
    public string Extension { get; set; }
    public long Size { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public FileCategory Category { get; set; }
    public bool IsSelected { get; set; }
    public string IconKey { get; set; }
}

public enum FileCategory
{
    Junk,           // 垃圾文件
    Unused,         // 不常用文件
    Large,          // 大文件
    Unimportant,    // 不重要文件
    Duplicate       // 重复文件
}
```

### 4.2 软件信息模型

```csharp
public class SoftwareInfo
{
    public string DisplayName { get; set; }
    public string Version { get; set; }
    public string Publisher { get; set; }
    public string InstallLocation { get; set; }
    public string UninstallString { get; set; }
    public DateTime InstallDate { get; set; }
    public long EstimatedSize { get; set; }
}
```

---

## 5. 构建与发布

### 5.1 单文件 EXE 配置

- **发布模式:** Release
- **运行时:** win-x64, self-contained
- **单个文件:** true
- **裁剪:** true (可选)
- **Ready2Run:** true

### 5.2 GitHub Actions

```yaml
触发条件: push 到 main 分支 + tag push
重试机制: 失败自动重试 3 次
输出路径: build/SystemCleanerPro.exe
```

---

## 6. 验收标准

### 6.1 功能验收
- [ ] 自动扫描所有固定磁盘
- [ ] 正确分类显示: 垃圾/不常用/大文件/不重要
- [ ] 强力删除功能正常工作
- [ ] 文件占用解除功能正常
- [ ] 软件卸载列表正确显示
- [ ] 深度卸载清理残留

### 6.2 UI验收
- [ ] 毛玻璃效果正常显示
- [ ] 液态玻璃动画流畅
- [ ] 响应式布局正常
- [ ] 交互反馈及时

### 6.3 构建验收
- [ ] 成功生成单文件 EXE
- [ ] EXE 可在纯净 Windows 10/11 运行
- [ ] GitHub Actions 自动构建成功
