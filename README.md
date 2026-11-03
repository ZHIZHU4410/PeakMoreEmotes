# PEAK 更多动作 / Peak More Emotes

把机场变成舞池。直接扩展原版 **R 键动作轮盘**，保留前两页原版动作，并增加更多舞蹈与姿势。

## 功能

- **35 个额外轮盘选项**：27 个 SomeEmotesREPO 移植舞蹈/姿势，以及 3 个其他动作和 5 个变速版本。
- 默认共 **7 页、51 个可用选项**，含原版 16 个动作；空白位置禁用。
- 新增动作支持简体中文、繁体中文共用中文名称及英文名称，其他语言显示英文。
- 长动作完整播放，三个单帧姿势保持约 5 秒；移动、跳跃或攀爬可取消。
- 1.1.1 已修复渲染骨骼与布娃娃物理姿态冲突导致的抖动。

包含曳步舞、颤栗舞、鲨鱼宝宝、卡波耶拉、桑巴舞、爵士舞、机械嘻哈舞、骑马舞、定格姿势等。另包含竖中指手势。

**51 指全部轮盘选项，并非 51 个不同的原始动画。**

## 安装与使用

### Thunderstore Mod Manager / r2modman

1. 选择 **PEAK** 并建立或打开配置档。
2. 安装 **PeakMoreEmotes** 及其自动安装的 **BepInExPack_PEAK** 依赖。
3. 从模组管理器以模组模式启动游戏。
4. 按住 **R**，用原版轮盘箭头或鼠标滚轮翻页，指向动作后松开 R。

安装前请移走手动安装的旧版 `PeakMoreEmotes.dll`，避免同一插件在游戏目录和配置档中重复加载。也请勿同时安装旧版私人备份包。

### 本地导入 / 手动安装

可以将本 ZIP 导入模组管理器的本地模组安装功能进行测试，并单独安装清单所列的 BepInExPack_PEAK 依赖。

手动安装时，先安装 BepInExPack_PEAK，再把本 ZIP 的 `plugins/PeakMoreEmotes` 文件夹放入游戏的 `BepInEx/plugins` 中。DLL 与四个 `.bundle` 文件必须保持在同一目录。

本包已内置所需动作素材，**无需安装 R.E.P.O. 游戏插件、SomeEmotesREPO DLL、PEAKEmoteLib 或 PEAKLib**。

## 设置

首次启动后生成 `BepInEx/config/local.peak.moreemotes.cfg`：

|选项|默认|作用|
|---|---|---|
|Enabled|true|插件总开关|
|RepoEmotes|true|启用 27 个 REPO 移植动作/姿势|
|TempoVariations|true|启用之前动作的 5 个变速版本|

修改后完全退出并重新启动游戏。关闭选项后轮盘页数随实际动作数变化。

## 联机、兼容性与已知限制

- 同一私人房间中，所有玩家应安装同版本插件与素材，才能相互看见自定义动作；双客户端联机尚未验证。
- 动作保持原地，沿用 PEAK 的物理与角色比例。源模型根位移、完整腾空轨迹和手指细节未逐项复刻，部分姿势会有接触位置偏差。
- PEAKEmoteLib 或其他替换相同 `A_Scout_Emote_Dance2` 动画槽位的插件可能冲突。
- 本插件不提供第三人称镜头。自己的动作通常需要其他玩家或另行安装的镜头工具才能完整看见。
- 已验证环境：PEAK 3.0.1、Unity 6000.3.15f1、BepInEx 5.4.23.3。游戏更新后可能需要重新适配。

故障排查：检查 `BepInEx/LogOutput.log` 中的 `PEAK 更多动作` 日志。卸载时通过模组管理器卸载，或退出游戏后移走本插件文件夹。

## 验证

初次适配完成 119 项离线运行时检查。1.1.1 抖动专项对照完成 15 项检查，0 项失败，包含定格、曳步舞、颤栗舞、物理帧目标采样、移动取消与原版动作恢复。这些结果不代表所有模组组合或双客户端联机已验证。

## 素材、许可与致谢

- **SomeEmotesREPO 2.0.1 — ImGogole / d4liln**：[Thunderstore](https://thunderstore.io/c/repo/p/ImGogole/SomeEmotesREPO/)、[源码与 Apache-2.0 许可证](https://github.com/d4liln/some-emotes-repo)。提供 27 个动作/姿势及源人形参考骨架。本包 `repo_emotes.bundle` 与该仓库的 `Emotes/emotes` 文件一致。本项目为 PEAK 的独立适配，并非原作者的官方 PEAK 版本。
- **WaporVave**：[Fortnite Default Dance Emote](https://thunderstore.io/c/peak/p/WaporVave/Fortnite_Default_Dance_Emote/)、[Twerk Emote](https://thunderstore.io/c/peak/p/WaporVave/Twerk_Emote/)、[MiddleFingerEmote](https://thunderstore.io/c/peak/p/WaporVave/MiddleFingerEmote/)，保留各原包 MIT 许可证。
- 动作槽替换思路参考 [PEAKEmoteLib](https://github.com/WaporVave/PEAKEmoteLib)，MIT；没有引入其运行时依赖。
- BepInEx / Harmony 提供加载与补丁支持。
- 插件代码采用 MIT，署名与第三方许可证随包保留。原素材的权利归相应作者或权利人所有。
- 插件代码、图标及制作文档由 Codex 协助编写。PEAK 为 Aggro Crab / Landfall 的作品，本项目为非官方社区 mod。

## English

Extends the native **R emote wheel** with 35 extra options. With the default settings there are **7 pages and 51 selectable entries**, including the 16 vanilla entries. The extras consist of 27 imported dances/poses, 3 other source animations and 5 tempo variants.

Hold **R**, browse using the arrows or mouse wheel, hover an emote and release R. Movement/jumping/climbing cancels playback. All clients should install the same version to see custom emotes; two-client multiplayer has not been verified. Version 1.1.1 integrates retargeting into PEAK's physics pose capture to fix the previous render/physics bone conflict.

Only BepInExPack_PEAK is required. All bundled assets stay next to the plugin DLL. No REPO, PEAKLib or PEAKEmoteLib plugin DLL is needed. Preserve the included third-party notices and licenses when redistributing.

## 下载与源码构建

当前版本 **1.1.1**，包含动作抖动修复。仓库中的 [Thunderstore 安装包](downloads/PeakMoreEmotes-1.1.1-Thunderstore.zip) 可下载后导入模组管理器；实际在 Thunderstore 上架是独立步骤，本仓库公开不代表商店已经上架。

源码位于 [`src/PeakMoreEmotes`](src/PeakMoreEmotes)。构建需要 .NET SDK（8.0 或更新版本），以及你自己安装的 PEAK 和 BepInExPack_PEAK。本仓库不提供游戏程序集或 BepInEx/Harmony DLL，构建时从本机游戏目录引用。

PowerShell 示例（将路径替换为自己的安装目录）：

```powershell
$env:PEAK_GAME_DIR = 'D:/SteamLibrary/steamapps/common/PEAK'
dotnet build src/PeakMoreEmotes/PeakMoreEmotes.csproj -c Release
```

打包还需要 Python 3.10 或更新版本，无第三方 Python 依赖：

```powershell
python scripts/package.py --game-dir $env:PEAK_GAME_DIR
```

生成 `dist/PeakMoreEmotes-1.1.1-Thunderstore.zip`。脚本编译 DLL 并收集四个动作包、许可证、图标、清单及说明，不会修改本机已安装的插件。

目录说明：`assets/` 是带许可的第三方动作包，`licenses/` 保留完整上游许可，`thunderstore/` 是商店元数据，`scripts/package.py` 是可复用打包脚本。自己修改后重新打包时，请同步更新插件版本、项目版本、商店清单和更新日志。

源码中的 `RepoRetarget.cs` 负责将人形动画映射至 PEAK 的 Scout 骨架；`Plugin.cs` 将重定向姿态接入原版固定物理帧的动画目标捕获，避免渲染帧和物理帧同时争用骨骼造成抖动。

**License:** authored plugin code is MIT; third-party bundles retain their own MIT / Apache-2.0 terms in [NOTICE](NOTICE) and [licenses/](licenses). The sponsorship card belongs to the creator and is supplied for display with this project; it is not a third-party animation asset. Codex assisted with implementation and documentation.

## 支持作者 / Support the creator

作者：**布洛芬4410（ZHIZHU4410）**。如果你喜欢这个 mod，欢迎通过爱发电支持后续制作。赞助完全自愿，不影响 mod 的下载与使用。

扫描下方爱发电二维码，为我发电：

![布洛芬4410的爱发电赞助二维码](docs/images/afdian-sponsor.jpg)
