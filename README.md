[English](#english) | [简体中文](#%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87)

[![Github All Releases](https://img.shields.io/github/downloads/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/total.svg)]() [![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin)

# English

# COM3D2.JustAnotherTranslator.Plugin

Just Another COM3D2 Translator plugin, or JAT for short.

This plugin is still under active development and is currently in an early beta stage, so no official release is available for download yet. The download link below is just a placeholder.

Please join our Discord channel: [https://discord.gg/custommaid](https://discord.gg/custommaid)

And report issues in the JAT sub-channel: [https://discord.com/channels/297072643797155840/1398579642599870564](https://discord.com/channels/297072643797155840/1398579642599870564)

You can also raise issues or start discussions in the Issues or Discussions tabs.

When reporting an issue, please set the plugin's log level to DEBUG, restart the game to reproduce the issue, and then attach the log file located at `COM3D2\COM3D2x64_Data\output_log.txt` with your submission.

## Highlights of JAT

**Robust and Independent UI Translation**
- A dedicated UI translation system inspired by i18nEx. JAT hooks into the game's underlying I2.Localization to intercept requests for UI text and images, rather than modifying or appending to the official translation database.
- This approach does not rely on internal game resources, making UI translations resilient to game updates and avoiding issues caused by version changes.

**Advanced Sprite Replacement**
- For UI sprite replacement, JAT dynamically creates a new, independent UIAtlas for your `.png` files instead of replacing the game's original atlas.
- This completely avoids touching native game resources, fundamentally preventing issues like misplaced images or broken UI layouts caused by changes to the original atlas after a game update.

**Flexible Translation Override System**
- JAT recursively loads translation files from the translation folder, sorting both files and folders by Unicode character order. Translations loaded later will override those loaded earlier.
- Users can create folders like `01-MachineTrans`, `02-AITrans`, `03-Proofread`, `99-PersonalFixes` to precisely control translation priority, easily managing and layering translations of different quality.

**Lyrics and Subtitles**
- A completely self-implemented subtitle system: The subtitle feature (including general subtitles and dance lyrics) is fully implemented by JAT and does not depend on any in-game resources or official systems, requiring no official support.
- It can provide subtitles in various scenes that lack them, such as dances, the fishing minigame, VR karaoke, the shooting minigame, night scenes (Yotogi), etc.
- It also features specially designed VR subtitles, with options to display them on a virtual tablet or in world space, floating just like in other VR games.

**Performance Optimizations**
- Translation files for UI and general text are loaded asynchronously, so it won't slow down your game's startup.
- Supports using `.zip` archives to speed up the loading of many small files.

**Other**
- JAT does not enable the game's built-in multilingual support, reducing user confusion and potential bugs.
- JAT does not use any in-game resources, avoiding some potential bugs.
- JAT automatically integrates with [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator), so you don't need to worry about the text being translated repeatedly by XUAT and no need to add special mark to the text in advance. (Please do not modify the default `RedirectedResourceDetectionStrategy`  setting of XUAT, That setting must be set to `AppendMongolianVowelSeparatorAndRemoveAll`.)

**Drawbacks:**
- JAT loads all translation text into memory, which does lead to higher memory usage.

## Why another translation plugin?

By observing questions from new community members, we've discovered some issues with existing translation plugins.

These include not being open source, UI translations being susceptible to updates, and a focus on English translation.

Meido Promotion Association is committed to making Meidou more accessible to everyone, so we've decided to develop a new
open source translation plugin.

### LBWtranslation/LBWmodifier

- It is not public, hidden in a forum that does not open for registration, and requires points to obtain, so it does not
  exist for most people.
- It has a built-in modifier, For example, private mode allows all characters, which may cause the game to crash, and
  some mods that may cause NTR are enabled by default. (In the latest version, it seems that the modifier and translator
  have finally been separated)
- UI translation uses the old YATranslator method, and there are issues with UI translation implementation caused by
  version updates.
- It is not open source.
- It is not internationalized.
- It has no documentation.
- It has no dump.

### i18nEX

- It uses too many in-game resources, for example, the csv format is the same as the official one, and there are too
  many redundancies and unclear places.
- Although it can be partially internationalized, it is mainly designed for English users, and there are some behaviors
  to obtain built-in resources in _en.
- It is not compatible with existing translation resources, and due to the design of loading by script, migrating
  existing files is a difficult task.
- It is highly coupled with the game implementation. For users of other languages, after enabling the built-in
  internationalization of COM3D2, you can choose English, Simplified Chinese, and Traditional Chinese in the game, while
  the translation file needs to use the English entry, which is confusing for users of other languages.
- It can only use the official subtitle system.
- It has poor support for languages other than English.

### YATranslator

- It is not maintained and seems to be broken in recent versions.
- It has no subtitle system.

## Getting Started

### Prerequisites

You can check your game version in the top-right corner of the game launcher (`COM3D2.exe`).

**BepInEx Environment (Required):**
- This is a BepInEx plugin, so you need to have [BepInEx](https://github.com/BepInEx/BepInEx) installed. If you have a `BepInEx` folder in your game's root directory (where `COM3D2.exe` is located), you are good to go.
- If you don't have it installed, we recommend using [**COM-Modular-Installer (CMI)**](https://github.com/krypto5863/COM-Modular-Installer) to install BepInEx and other essential plugins with one click. CMI provides English instructions.

**In-Game Configuration (Optional):**
- To modify the configuration in-game, please install [**BepInEx.ConfigurationManager**](https://github.com/BepInEx/BepInEx.ConfigurationManager).
- Download `BepInEx.ConfigurationManager_BepInEx5_vxx.xx.x.zip`. The archive is already structured, so just extract and merge its `BepInEx` folder with the one in your game's root directory.
- The default hotkey to open it is `F1`.

**Quick Access to Configuration Manager (Optional):**
- The `ConfigurationManagerWrapper.dll` provided [**here**](https://github.com/DeathWeasel1337/COM3D2_Plugins) can add a button to the in-game settings menu (the gear icon) to open the Configuration Manager.
- The `COM3D2.ConfigurationManagerWrapper.v1.0.zip` archive is already structured, so just extract and merge its `BepInEx` folder with the one in your game's root directory.

### Installation Steps

Go to the [**Releases**](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/releases) page to download the plugin.

- COM3D2:`2.44.0` or lower -> Download `COM3D2.JustAnotherTranslator.Plugin.zip` (Untested, should work in theory)
- COM3D2: `2.44.0` or higher -> Download `COM3D2.JustAnotherTranslator.Plugin.zip`
- COM3D2.5: `3.41.0` or lower -> Download `COM3D2.JustAnotherTranslator.Plugin.zip` (Untested, should work in theory)
- COM3D2.5: `3.41.0` or higher -> Download `COM3D25.JustAnotherTranslator.Plugin.zip` (Note it is: COM3D25)

<br>

**Install the Plugin:**
- Extract the downloaded archive and merge its `BepInEx` folder with the one in your game's root directory (where `COM3D2.exe` is located).
- If prompted to merge folders, select "Yes".
- The correct installation path for the plugin should be: `COM3D2\BepInEx\plugins\COM3D2.JustAnotherTranslator\COM3D2.JustAnotherTranslator.Plugin.dll`

The `COM3D2\` at the beginning of the path refers to your game's root directory, which is where `COM3D2.exe` is located.

### Initial Setup

#### File Generation:

After completing the installation, **launch the game once**.

The plugin will automatically generate the default configuration file and translation folders inside the `COM3D2\BepInEx` directory.

- **Configuration File:** `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- **Root Translation Folder:** `COM3D2\BepInEx\JustAnotherTranslator`

After the first launch, a default `zh-CN` folder and a `Dump` folder will be created inside `JustAnotherTranslator`. (The `Dump` folder is for translators to dump game assets and can be ignored by regular users).

#### Set Target Language

Open the configuration file and find the `TargetLanguage/目标语言` option under `[2General]`.

This setting determines which language folder the plugin will load from the `COM3D2\BepInEx\JustAnotherTranslator\` directory. The default value is `zh-CN`.

For example, if you want to use English translations, you can place your translation files in a folder named `COM3D2\BepInEx\JustAnotherTranslator\en-US` and then change the value of this setting to `en-US`.

**Note:** This setting only specifies the folder path. The language actually displayed in-game depends entirely on the content of the translation files you provide. Therefore, you can also keep the setting as `zh-CN` and simply place your translation files into that folder.

Example Configuration:

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

## Installing Translation Files

- Due to legal and copyright reasons, this plugin does not provide translation files beyond the base translations. You will need to obtain them from elsewhere.
- Place the translation files you obtained (usually folders like `Text`, `UI`, and `Texture`) in the `COM3D2\BepInEx\JustAnotherTranslator\<target language>\` directory.
- This plugin's text translation and texture replacement features are compatible with `LBWtranslation` and `YATranslator`. If you have obtained translation files for these plugins, refer to the "How Plugins Read Translation Files" section and place them in the corresponding directories.
- For a more detailed migration guide, please refer to the [Migration Guide](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)

## Migrating from other plugins

Please refer to the documentation here [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)

## How the plugin reads translation files

For detailed instructions, please refer to the documentation
here [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)

Including translation instructions and instructions for migrating from other plugins.

```
COM3D2\BepInEx\JustAnotherTranslator\<your target language>
├─Lyric
├─Text
├─Texture
└─UI
    ├─Sprite
    └─Text
```

There are 4 main folders in the translation folder of the corresponding language: Text, Texture, UI, and Lyric.

- The `Text` folder is where the main text translations are located. The translatable content includes daily ADV
  dialogues, NGUI text, uGUI text, and in-game multi-language support text.
- The `Texture` folder is used to replace in-game textures (various images).
- The `Lyric` folder is for files dedicated to lyric subtitles.
- The `UI` folder is a dedicated folder for interface translation. Sprite is used to replace a single sprite, and Text
  is used to translate UI text.

<br>
<br>
<br>

---

<br>
<br>
<br>

# 简体中文

# COM3D2.JustAnotherTranslator.Plugin

只是另一个 COM3D2 翻译插件，JustAnotherTranslator，简称 JAT。

此插件仍在积极开发中，目前处于早期测试版状态，因此暂未提供正式版本下载，下面的下载链接只是提前写完了。

请加入 Discord 频道：[https://discord.gg/custommaid](https://discord.gg/custommaid)

并通过 JAT 子区反馈问题：[https://discord.com/channels/297072643797155840/1398579642599870564](https://discord.com/channels/297072643797155840/1398579642599870564)

您也可以在 Issue 或 Discussions 中提出问题。

在您反馈问题时，请将插件日志等级调至 DEBUG，重启游戏后复现该问题，然后在提交时附上位于 `COM3D2\COM3D2x64_Data\output_log.txt` 的日志文件。

## JAT 的亮点

健壮且独立的 UI 翻译：
- 受 i18nEX 启发的专用 UI 翻译系统，JAT 通过挂钩（Hook）游戏底层的 I2.Localization 来拦截 UI 文本和图片的请求，而不是修改或追加到官方的翻译数据库。
- 这种方式不依赖游戏内部资源，使得 UI 翻译能有效抵抗游戏更新带来的影响，避免了因游戏版本更新导致的翻译失效问题。

精灵图（Sprite）替换：
- 对于 UI 精灵图的替换，JAT 会为用户的 `.png` 文件动态创建一个全新的、独立的 UI 图集（UIAtlas），而不是替换游戏原有的图集。
- 完全不触及游戏原生资源，从根本上避免了因游戏更新导致原图集布局变化而引发的图片错位、UI 错乱等问题。

灵活的翻译覆盖系统：
- JAT 会递归加载翻译文件夹内的翻译文件，并根据 Unicode 字符顺序对文件和文件夹进行排序。后加载的翻译会覆盖先加载的。
- 用户可以通过创建如 `01-机翻`, `02-AI翻译`, `03-人工校对`, `99-个人修正` 这样的文件夹，来精确控制翻译的优先级，轻松管理和分层不同质量的翻译。

歌词与字幕：
- 完全自实现的字幕系统：字幕功能（包括普通字幕和舞蹈歌词）完全由 JAT 实现，不依赖任何游戏内资源或官方系统，无需官方系统支持。
- 可以在各种无字幕场景下为您提供字幕，例如舞蹈、钓鱼小游戏、VR卡拉OK、射击小游戏、夜伽等。
- 它还拥有专门设计的 VR 字幕，可选显示在虚拟平板电脑上或世界空间中，就像其他 VR 游戏一样，它是悬浮在世界空间中的。

性能优化：
- UI 与通用文本翻译的翻译文件是异步加载的，它不会减慢你的游戏启动速度。
- 支持使用 `.zip` 压缩包来加快小文件加载速度。

其他：
- JAT 不启用游戏内的多语言支持，减少了使用户困惑的内容和可能出现的 BUG。
- JAT 不使用任何游戏内资源，避免了一些可能出现的 BUG。
- JAT 自动与 [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) 集成，无需担心文本被 XUAT 重复翻译，无需预先在文本中添加特殊标记。（请勿修改 XUAT 的默认设置 `RedirectedResourceDetectionStrategy`，该设置必须为 `AppendMongolianVowelSeparatorAndRemoveAll`）

缺点：
- JAT 确实会将所有翻译文本加载到内存，这确实会导致较高的内存占用。

## 为什么需要另一个翻译插件

我们通过观察社区新手问题，发现了一些现有翻译插件所存在的问题。

包括翻译插件不开源、UI 翻译易受更新影响、主攻英文翻译等问题。

MeidoPromotionAssociation 始终致力于让每个人都能更容易的享受妹抖，因此我们决定开发新的开源翻译插件。

### LBWtranslation/LBWmodifier

- 它不是公开的，藏在不开放注册的论坛，且需要积分获取，因此对大多数人来说它不存在
- 它内置了修改器，且很多修改器是默认开启的，例如私人模式允许所有性格可能会导致游戏崩溃，以及一些可能导致 NTR
  的修改器是默认开启的（在最近的版本中似乎终于将修改器与翻译器分离了）
- UI 翻译沿用旧的 YATranslator 方法，存在版本更新导致 UI 翻译实现问题
- 它不是开源的
- 它不是国际化的
- 它没有文档
- 他没有转储功能

### i18nEX

- 它使用了太多的游戏内资源，例如 csv 格式与官方相同，有太多的冗余和不清楚的地方
- 虽然可以部分国际化，但它主要是为英文用户设计的，存在一些获取 _en 内置资源的行为
- 它与现有翻译资源不兼容，且由于按脚本加载的设计，迁移现有文件是一个艰巨的任务
- 它与游戏实现高度耦合，对于其他语言用户来说，启用 COM3D2 内置国际化后游戏中可以选择 英文、简体中文、繁体中文，而翻译文件需要使用
  English 条目，这对其他语言用户来说是迷惑的
- 它只能使用官方的字幕系统
- 它对于英文以外的语言支持较差

### YATranslator

- 它未被维护，在最近的版本中似乎坏了
- 它没有字幕系统

## 入门

### 前置需求

打开启动器（COM3D2.exe），在启动器右上角即可查看您的游戏版本。

**BepInEx 环境（必须）**:
- 这是一个 BepInEx 插件，您需要安装 [BepInEx](https://github.com/BepInEx/BepInEx)，如果您的游戏根目录（COM3D2.exe 所在位置）下有 BepInEx 文件夹，那么您应该已经安装了。
- 如果您没有安装，推荐使用 [COM-Modular-Installer](https://github.com/krypto5863/COM-Modular-Installer) (CMI) 来一键安装 BepInEx 和其他基础插件（CMI 提供中文说明）。

**游戏内修改配置文件（可选）**:
- 要在游戏内修改配置文件，请安装 [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)（配置管理器插件）
- 下载 `BepInEx.ConfigurationManager_BepInEx5_vxx.xx.x.zip` 压缩包内已按照文件夹组织，将解压出的 BepInEx 文件夹覆盖至游戏根目录即可。
- 其默认快捷键是 F1

**快捷访问配置管理器（可选）**:
- [这里](https://github.com/DeathWeasel1337/COM3D2_Plugins)提供的 ConfigurationManagerWrapper.dll
- 可以在游戏的齿轮菜单显示一个按钮以打开配置管理器。
-  `COM3D2.ConfigurationManagerWrapper.v1.0.zip` 压缩包内已按照文件夹组织，将解压出的 BepInEx 文件夹覆盖至游戏根目录即可。

### 安装步骤

请前往 [Release](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/releases) 页面下载压缩包。

- COM3D2: `2.44.0`  或更低版本：请下载 `COM3D2.JustAnotherTranslator.Plugin.zip` (未经测试，理论可用)
- COM3D2: `2.44.0`  或更高版本：请下载 `COM3D2.JustAnotherTranslator.Plugin.zip`
- COM3D2.5: `3.41.0` 或更低版本：请下载 `COM3D2.JustAnotherTranslator.Plugin.zip` (未经测试，理论可用)
- COM3D2.5: `3.41.0` 或更高版本：请下载 `COM3D25.JustAnotherTranslator.Plugin.zip` (注意是 COM3D25)

<br>

**安装插件**:
- 解压下载的压缩包，将其中的 BepInEx 文件夹直接放入您的游戏根目录（即 COM3D2.exe 所在的文件夹）。
- 如果提示合并文件夹，请选择“是”。
- 正确的安装路径应为：`COM3D2\BepInEx\plugins\COM3D2.JustAnotherTranslator\COM3D2.JustAnotherTranslator.Plugin.dll`

路径开头的 `COM3D2\` 都是指您的游戏根目录，也就是 `COM3D2.exe` 所在的位置。

### 初始化配置

#### 生成文件:

完成上述安装后，启动一次游戏。

插件会自动在 `COM3D2\BepInEx` 文件夹内生成默认的配置文件和翻译文件夹 。

- **配置文件位于**: `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- **翻译文件夹根目录位于**: `COM3D2\BepInEx\JustAnotherTranslator`

首次启动后，JustAnotherTranslator 文件夹内会自动生成一个默认的 `zh-CN` 文件夹和一个 `Dump` 文件夹（用于翻译者转储资源，普通用户无需关心）。

#### 设置目标语言

打开配置文件，找到 `[2General]` 下的 `TargetLanguage/目标语言` 选项。

此设置项决定了插件会读取 `COM3D2\BepInEx\JustAnotherTranslator\` 目录下的哪一个语言文件夹。默认值为 `zh-CN`。

例如，如果您想使用英文翻译，可以将翻译文件放入 `COM3D2\BepInEx\JustAnotherTranslator\en-US` 文件夹，然后将此配置项的值修改为 `en-US`。

请注意：此设置仅用于指定文件夹路径，您游戏中实际显示的语言完全取决于您所提供的翻译文件内容。

因此您保留原始 `zh-CN` 设置也无妨，只需将翻译文件放到对于文件夹即可。

配置示例：

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

## 安装翻译文件

- 由于法律与版权原因，本插件不提供基础翻译以外的翻译文件，您需要自行从其他地方获取。
- 将您获取的翻译文件（通常是 `Text`, `UI`, `Texture` 等文件夹）放入 `COM3D2\BepInEx\JustAnotherTranslator\<目标语言>\` 路径下。
- 本插件的文本翻译和纹理替换功能兼容 `LBWtranslation` 和 `YATranslator`。如果你获取了适用于这些插件的翻译文件，参考《插件如何读取翻译文件》章节放入对应路径即可。
- 更详细的迁移指南请参考此处的 [迁移指南](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)


## 从其他插件迁移

请参考此处的文档 [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)


## 插件如何读取翻译文件

详细说明请参考此处的文档 [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)

包括翻译说明和从其他插件迁移的说明。

```
COM3D2\BepInEx\JustAnotherTranslator\<你设置的目标语言>
├─Lyric
├─Text
├─Texture
└─UI
    ├─Sprite
    └─Text
```

对应语言的翻译文件夹中有 `Text`、`Texture`、`UI`、`Lyric` 4 个主要文件夹

- `Text` 文件夹是主要的文本翻译所在，可翻译内容包括日常 ADV 对话、NGUI 文本、uGUI 文本、游戏内置多语言支持文本。
- `Texture` 文件夹用于替换游戏内纹理（各种图片）
- `Lyric` 文件夹放置歌词字幕专用的文件。
- `UI` 文件夹是界面翻译专用的文件夹，Sprite 用于替换单个精灵图、Text 则用于翻译 UI 文本。

<br>
<br>
<br>
<br>
<br>
<br>

---

<br>
<br>
<br>
<br>
<br>
<br>

# KISS Rule

*This Project is not owned or endorsed by KISS.

*MODs are not supported by KISS.

*KISS cannot be held responsible for any problems that may arise when using MODs.

*If any problem occurs, please do not contact KISS.

```
KISS 規約

・原作がMOD作成者にある場合、又は、原作が「カスタムメイド3D2」のみに存在する内部データの場合、又は、原作が「カスタムメイド3D2」と「カスタムオーダーメイド3D2」の両方に存在する内部データの場合。
※MODはKISSサポート対象外です。
※MODを利用するに当たり、問題が発生してもKISSは一切の責任を負いかねます。
※「カスタムメイド3D2」か「カスタムオーダーメイド3D2」か「CR EditSystem」を購入されている方のみが利用できます。
※「カスタムメイド3D2」か「カスタムオーダーメイド3D2」か「CR EditSystem」上で表示する目的以外の利用は禁止します。
※これらの事項は https://kisskiss.tv/kiss/diary.php?no=558 を優先します。

・原作が「カスタムオーダーメイド3D2(GP01含む)」の内部データのみにある場合。
※MODはKISSサポート対象外です。
※MODを利用するに当たり、問題が発生してもKISSは一切の責任を負いかねます。
※「カスタムオーダーメイド3D2」か「CR EditSystem」をを購入されている方のみが利用できます。
※「カスタムオーダーメイド3D2」か「CR EditSystem」上で表示する目的以外の利用は禁止します。
※「カスタムメイド3D2」上では利用しないで下さい。
※これらの事項は https://kisskiss.tv/kiss/diary.php?no=558 を優先します。

・原作が「CR EditSystem」の内部データのみにある場合。
※MODはKISSサポート対象外です。
※MODを利用するに当たり、問題が発生してもKISSは一切の責任を負いかねます。
※「CR EditSystem」を購入されている方のみが利用できます。
※「CR EditSystem」上で表示する目的以外の利用は禁止します。
※「カスタムメイド3D2」「カスタムオーダーメイド3D2」上では利用しないで下さい。
※これらの事項は https://kisskiss.tv/kiss/diary.php?no=558 を優先します。
```

<br>

# Disclaimer

By downloading this software, you agree to read, accept and abide by this Disclaimer, this is a developer protection
measure and we apologize for any inconvenience this may cause.

下载此软件即表示您已阅读且接受并同意遵守此免责声明，这是为了保护开发人员而采取的措施，对于由此造成的不便，我们深表歉意。

本ソフトウェアをダウンロードすることにより、利用者は本免責事項を読み、内容を理解し、全ての条項に同意し、遵守することを表明したものとみなされます。これは開発者保護のための措置であることをご理解いただき、ご不便をおかけする場合もあらかじめご了承ください。

```
English

In case of any discrepancy between the translated versions, the Simplified Chinese version shall prevail.

1. Tool Nature Statement
    This project is an open-source tool released under the BSD-3-Clause license. The developer(s) (hereinafter referred to as "the Author") are individual technical researchers only. The Author does not derive any commercial benefit from this tool and does not provide any form of online service or user account system.
    This tool is a purely local data processing tool with no content generation capabilities whatsoever. It possesses no online upload or download functionality.
    This tool is essentially a text replacer that receives a user-provided source text to target text translation table and replaces the source text with the user-provided translation in real time while the game is running. The tool itself does not generate, modify, or inject any new data content.

2. Usage restrictions
  This software shall not be used for any illegal purposes. This includes, but is not limited to, creating or disseminating obscene or illegal materials, infringing upon the intellectual property rights of others, violating platform user agreements, or any other actions that may contravene the laws and regulations of the user's jurisdiction.
    Users shall bear full responsibility for any consequences arising from violations of the law.
  
  Users must commit to:
      - Not creating, publishing, transmitting, disseminating, or storing any content that violates the laws and regulations of their jurisdiction.
      - Not creating, publishing, transmitting, disseminating, or storing obscene or illegal materials.
      - Not creating, publishing, transmitting, disseminating, or storing content that infringes upon the intellectual property rights of others.
      - Not creating, publishing, transmitting, disseminating, or storing content that violates platform user agreements.
      - Not using the tool for any activities that endanger national security or undermine social stability.
      - Not using the tool to conduct cyber attacks or crack licensed software.
      - The Author has no legal association with user-generated content.
      - Any content created using this tool that violates local laws and regulations (including but not limited to pornography, violence, or infringing content) entails legal liability borne solely by the content creator.

3. Liability exemption
  Given the nature of open-source projects:
      - The Author cannot monitor the use of all derivative code.
      - The Author is not responsible for modified versions compiled/distributed by users.
      - The Author assumes no liability for any legal consequences resulting from illegal use by users.
      - The Author provides no technical guarantee for content review or filtering.
      - The tool's operational mechanism inherently prevents it from recognizing or filtering content nature.
      - All data processing occurs solely on the user's local device; the Author cannot access or control any user data.

  Users acknowledge and agree that:
      - This tool does not generate any content; the final content is entirely dependent on its input files. The tool only performs replacement operations and cannot be held responsible for the legality, content nature, or usage scenarios of user input data.
      - This tool contains no data upload/download capabilities; all content processing is completed on the user's local device.
      - If illegal activities involving this tool are discovered, they must be reported immediately to the public security authorities.
      - The Author reserves the right to cease distribution of specific versions suspected of being abused.

4. Age and guardianship responsibility
  Users must be persons with full civil capacity (18 years of age or older). Minors are prohibited from downloading, installing or using this tool. Guardians must assume full management responsibility for device access.

5. Agreement Update
  The author has the right to update this statement through the GitHub repository. Continued use is deemed to accept the latest version of the terms.

6. Disclaimer of Warranty
  This tool is provided "AS IS" and the developer expressly disclaims any express or implied warranties, including but not limited to:
    - Warranty of merchantability
    - Warranty of fitness for a particular purpose
    - Warranty of code freedom from defects or potential risks
    - Warranty of continuous availability and technical support

7. Waiver of liability for damages
  Regardless of the use/inability to use this tool resulting in:
    - Direct/indirect property loss
    - Data loss or business interruption
    - Third-party claims or administrative penalties
  The developer shall not bear any civil, administrative or criminal liability

8. Waiver of liability for third-party reliance
  If the third-party libraries/components included or relied upon by this tool have:
    - Intellectual property disputes
    - Security vulnerabilities
    - Content that violates local laws
    - Subject to criminal or civil penalties
  The developer shall not bear joint and several liability, and users should review the relevant licenses on their own

9. Version iteration risk
  Users understand and accept:
    - Different versions of code may have compatibility issues
    - Developers are not obliged to maintain the security of old versions
    - Modifying the code on your own may lead to unforeseen legal risks

简体中文

1. 工具性质声明  
   本项目是基于 BSD-3-Clause 许可证的开源工具。开发者（以下简称"作者"）仅为个人技术研究者，不通过本工具获取任何商业利益，亦不提供任何形式的在线服务及用户账号体系。
   本工具为纯本地化数据处理工具，不具备任何内容生成能力，无任何在线上传下载功能。
   本工具本质上是一个文本替换器，其接收用户提供的原文到译文翻译表，并在游戏运行中实时替换原文为用户提供的译文，工具本身不产生、修改或注入任何新数据内容。

2. 使用限制
   本软件不得用于任何违法用途，包括但不限于制作、传播淫秽违法物品、侵害他人知识产权、违反平台用户协议的行为等可能违反所在地法律法规的违法行为。
   使用者因违反法律造成的后果需自行承担全部责任。

   用户必须承诺：  
     - 不制作、发布、传送、传播、储存任何违反所在地法律法规的内容
     - 不制作、发布、传送、传播、储存淫秽违法物品
     - 不制作、发布、传送、传播、储存侵害他人知识产权的内容
     - 不制作、发布、传送、传播、储存违反平台用户协议的内容
     - 不将工具用于任何危害国家安全或破坏社会稳定的活动
     - 不使用本工具实施网络攻击或破解正版软件
     - 开发者与用户生成内容无法律关联性
     - 任何使用本工具创建违反当地法律法规的内容（包括但不限于色情、暴力、侵权内容），其法律责任由内容创建者独立承担

3. 责任豁免  
   鉴于开源项目特性：  
     - 作者无法监控所有衍生代码的使用
     - 不负责用户自行编译/分发的修改版本
     - 不承担用户非法使用导致的任何法律责任
     - 不提供内容审核或过滤的技术保证
     - 工具运行机制决定其无法识别或过滤内容性质
     - 所有数据处理均在用户本地设备完成，开发者无法访问或控制任何用户数据

   用户知悉并同意：
     - 本工具不具备任何内容生成能力，最终内容完全取决于其输入文件。工具仅执行替换操作，无法对用户输入数据的合法性、内容性质及使用场景负责。
     - 本工具不包含任何数据上传/下载功能，所有内容生成均在用户本地设备完成
     - 如发现有人利用本工具从事违法活动，应立即向公安机关举报
     - 开发者保留停止分发涉嫌被滥用的特定版本的权利

4. 年龄及监护责任  
   用户须为完全民事行为能力人（18 周岁及以上），禁止未成年人下载、安装或使用。监护人须对设备访问承担完全管理责任。

5. 协议更新  
   作者有权通过 GitHub 仓库更新本声明，继续使用视为接受最新版本条款。

6. 担保免责  
  此工具按"原样"提供，不附带任何明示或暗示的保证，包括但不限于：
     - 适销性担保  
     - 特定用途适用性担保  
     - 代码无缺陷或潜在风险担保  
     - 持续可用性及技术支持担保  

7. 损害赔偿责任免除  
   无论使用/无法使用本工具导致：  
     - 直接/间接财产损失
     - 数据丢失或业务中断
     - 第三方索赔或行政处罚
     - 受到刑事或民事处罚
   开发者均不承担民事、行政或刑事责任  

8. 第三方依赖免责  
   本工具包含或依赖的第三方库/组件如存在：  
     - 知识产权纠纷  
     - 安全漏洞  
     - 违反当地法律的内容  
   开发者不承担连带责任，用户应自行审查相关许可  

9. 版本迭代风险  
    用户理解并接受：  
     - 不同版本代码可能存在兼容性问题  
     - 开发者无义务维护旧版本安全性  
     - 自行修改代码可能导致不可预见的法律风险


日本語

本声明の翻訳版（日本語を含む）と簡体中文原文に解釈上の相違がある場合は、簡体中文版が優先的に有効とします。

1. ツールの性質に関する声明
   本プロジェクトは、BSD-3-Clause ライセンスに基づくオープンソースツールです。開発者（以下「作者」）は個人の技術研究者に過ぎず、本ツールを通じていかなる商業的利益も得ておらず、いかなる形式のオンラインサービス及びユーザーアカウントシステムも提供しません。
   本ツールは純粋にローカル環境でのデータ処理ツールであり、いかなるコンテンツ生成能力も有しておらず、いかなるオンラインアップロード・ダウンロード機能も備えていません。
   このツールは、基本的にテキスト置換ツールであり、ユーザーが提供したソーステキストからターゲットテキストへの翻訳テーブルを受け取り、ゲームの実行中にリアルタイムでソーステキストをユーザーが提供した翻訳に置き換えます。ツール自体は、新しいデータコンテンツを生成、変更、または挿入することはありません。

2. 使用制限
   本ソフトウェアは、以下のような、所在地の法令に違反する可能性のある違法行為を含むがこれに限定されない、いかなる違法目的にも使用してはなりません：
     - わいせつ物や違法物の作成・頒布
     - 他人の知的財産権の侵害
     - プラットフォーム利用規約違反行為
   使用者は、法律違反によって生じた結果について、自ら全ての責任を負うものとします。

   ユーザーは以下を確約しなければなりません：
     - 所在地の法令に違反する内容を、作成、公開、送信、拡散、保存しないこと。
     - わいせつ物や違法物を、作成、公開、送信、拡散、保存しないこと。
     - 他人の知的財産権を侵害する内容を、作成、公開、送信、拡散、保存しないこと。
     - プラットフォーム利用規約に違反する内容を、作成、公開、送信、拡散、保存しないこと。
     - 本ツールを国家安全を脅かす、または社会の安定を破壊する活動に使用しないこと。
     - 本ツールを使用してネットワーク攻撃を実行したり、正規ソフトウェアのクラッキングを行わないこと。
     - 開発者はユーザー生成コンテンツとの法的関連性を一切有しないこと。
     - 本ツールを使用して作成された、当地の法令に違反するコンテンツ（ポルノ、暴力、著作権侵害等を含むがこれに限定されない）についての法的責任は、コンテンツ作成者が単独で負うこと。

3. 免責事項
   オープンソースプロジェクトの性質上：
     - 作者はすべての派生コードの使用状況を監視することはできません。
     - ユーザー自身がコンパイル/配布する修正版について責任を負いません。
     - ユーザーの違法使用に起因するいかなる法的責任も負いません。
     - コンテンツ審査やフィルタリングの技術的保証は提供しません。
     - ツールの動作メカニズム上、コンテンツの性質を識別またはフィルタリングすることはできません。
     - すべてのデータ処理はユーザーのローカルデバイス上で完了し、開発者はユーザーデータにアクセスまたは制御することはできません。

   ユーザーはこれを理解し同意するものとします：
     - このツールはコンテンツを生成しません。最終的なコンテンツは入力ファイルに完全に依存します。このツールは置換操作のみを実行するものであり、ユーザーが入力したデータの合法性、コンテンツの性質、または使用シナリオについては一切責任を負いません。
     - 本ツールにはいかなるデータアップロード/ダウンロード機能も含まれておらず、すべてのコンテンツ生成はユーザーのローカルデバイス上で完了します。
     - 本ツールを利用した違法行為を発見した場合は、直ちに公安機関に通報すること。
     - 開発者は、悪用の疑いのある特定バージョンの配布停止権利を留保します。

4. 年齢及び監督責任
   ユーザーは完全民事行為能力者（18歳以上）でなければなりません。未成年者のダウンロード、インストール、または使用は禁止されています。保護者はデバイスへのアクセスについて完全な管理責任を負うものとします。

5. 規約の更新
   作者は、GitHub リポジトリを通じて本声明を更新する権利を有します。継続的な使用は最新版の条項の受諾とみなされます。

6. 保証の免責
   本ツールは「現状のまま」提供され、商品性、特定目的への適合性、コードの欠陥や潜在リスクの不存在、継続的な利用可能性及び技術サポートの保証を含むがこれらに限定されない、明示または黙示を問わず、いかなる保証も付帯しません。

7. 損害賠償責任の免責
   本ツールの使用または使用不能によって生じた以下の事項について、開発者は民事、行政、または刑事上のいかなる責任も負いません：
     - 直接的または間接的な財産上の損害
     - データ損失または業務中断
     - 第三者からの請求または行政処分
     - 刑事罰または民事罰の適用

8. 第三者依存関係に関する免責
   本ツールに含まれる、または依存するサードパーティライブラリ/コンポーネントに関して：
     - 知的財産権に関する紛争
     - セキュリティ上の脆弱性
     - 当地の法律に違反する内容
   が存在する場合でも、開発者は連帯責任を負わず、ユーザーは関連ライセンスを自ら確認するものとします。

9. バージョン更新リスク
   ユーザーは以下を理解し受諾するものとします：
     - 異なるバージョンのコード間で互換性の問題が生じる可能性があること。
     - 開発者は旧バージョンのセキュリティを維持する義務を負わないこと。
     - コードの独自修正は予期せぬ法的リスクを招く可能性があること。
```

<br>
<br>

# Credits

Part of the code is from [https://github.com/Pain-Brioche/COM3D2.i18nEx](https://github.com/Pain-Brioche/COM3D2.i18nEx)
under the MIT license

Part of the code is
from [https://github.com/ghorsington/CM3D2.YATranslator](https://github.com/ghorsington/CM3D2.YATranslator) under The
Unlicense license (for compatibility)
