# Comfort UI 0.3.2 安装说明

适用开发目标：Windows、ChilloutVR 2026r182-hf4 public-nightly、Mono MelonLoader 0.8.0-ci.2548、.NET Framework 4.8。其他分支和加载器版本需要自行验证。

## EXE 安装

从 [GitHub Release](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/tag/v0.3.2) 下载 `ComfortUI-Setup-0.3.2.exe`。正常退出游戏后运行，自动查找或手动选择包含 `ChilloutVR.exe` 的文件夹，再选择「安装 / 更新模组」。

缺少加载器时可选择加载器引导：程序下载并校验官方 MelonLoader 安装器，用户在官方界面选择 `ChilloutVR.exe` 并安装适合该游戏的 Mono 加载器，完成后返回本程序安装模组。程序不会替你结束或启动游戏，也不自动替换已存在的加载器。

## DLL 手动安装

下载独立 `CVRComfortUI.dll`（这是模组本身的 DLL）。先配置 Mono MelonLoader，退出游戏，把 DLL 放入 `ChilloutVR/Mods/`，再正常启动游戏。DLL 已包含界面、词典与样式，不需要另放 `assets` 文件夹。

只保留一份模组 DLL。EXE 与 DLL 是同一个模组的两种安装方式。

## 更新、停用与设置

使用 EXE 更新会备份旧 DLL，保存位置为 `UserData/ComfortUI/backups/`，偏好在 `UserData/ComfortUI/preferences.json`。使用本安装器安装的版本可通过「移除模组」移除并保留个人设置。纯手动安装可在退出游戏后自行移走 DLL。

首次启动没有偏好文件时，自动读取 Windows 当前用户的显示语言，选择简中/英/日/韩/西；不支持或检测失败回退英文，中文各地区使用简体中文。升级保留已有选择，不会每次启动覆盖语言。

设置 → 界面与显示：五种语言、字体、配色、动效形式/速度、关闭动效、玩家名牌统一/原版样式。
设置 → 翻译：Google/MyMemory/Bing 免费翻译/AI/本地模型分段选择、源语言、AI 接口/模型/密钥、本地模型准备与 CPU/显卡选项、系统代理/直连/手动 HTTP 代理、测试、缓存与各项翻译开关。
设置 → 操作：默认 F8，开启或关闭持续气泡翻译。每次启动默认关闭，文字输入和虚拟键盘使用时不会触发。

## 下载校验与问题报告

Release 提供 `SHA256SUMS.txt`。PowerShell 可用 `Get-FileHash -Algorithm SHA256 -LiteralPath .\CVRComfortUI.dll` 比较结果。

本版是公开测试版；遇到问题，请报告游戏与加载器版本、PC/VR、涉及页面和复现步骤。提供日志前请去除账号凭据、私聊正文及其他不相关个人信息。

翻译服务的接口、模型和密钥配置见 [翻译设置](TRANSLATION.zh-CN.md)。
