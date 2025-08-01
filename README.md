[English](#english) | [简体中文](#%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87)

[![Github All Releases](https://img.shields.io/github/downloads/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/total.svg)]() [![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin)

# English

## COM3D2.JustAnotherTranslator.Plugin

This plugin is still under active development and is currently in an early beta stage.

Please report issues via Discord: [https://discord.com/channels/297072643797155840/1398579642599870564](https://discord.com/channels/297072643797155840/1398579642599870564)

## Why another translation plugin?

There are some things I'm not satisfied with in the existing translation plugins.

### LBWtranslation/LBWmodifier

- It is not public, hidden in a forum that does not open for registration, and requires points to obtain, so it does not exist for most people.
- It has a built-in modifier, and many modifiers are enabled by default, such as AllMaidInPrivate, which may cause the game to crash, and some modifiers that may cause NTR are enabled by default.
- It is not open source.
- It is not internationalized.
- It has no documentation.

### i18nEX

- It uses too many in-game resources, for example, the csv format is the same as the official one, and there are too many redundancies and unclear places.
- Although it can be partially internationalized, it is mainly designed for English users, and there are some behaviors to obtain built-in resources in _en.
- It is not compatible with existing translation resources, and due to the design of loading by script, migrating existing files is a difficult task.
- It is highly coupled with the game implementation. For users of other languages, after enabling the built-in internationalization of COM3D2, you can choose English, Simplified Chinese, and Traditional Chinese in the game, while the translation file needs to use the English entry, which is confusing for users of other languages.
- It can only use the official subtitle system.

### YATranslator

- It is not maintained and seems to be broken in recent versions.
- It has no subtitle system.

## Highlights of JAT

- It has a self-implemented, highly customizable subtitle system, including a dance subtitle system.
- It can provide you with subtitles in various scenes without subtitles, such as dancing, fishing minigames, VR karaoke, shooting minigames, night battles, etc.
- It has specially designed VR subtitles, which are suspended in world space like other games.
- It has a dedicated UI translation system inspired by i18nEX, which is almost unaffected by version updates. There is no need to redo the UI translation due to game version updates, because we have seen too many users who cannot use the UI translation after the game is updated.
- Its translation files are loaded asynchronously, which means it will not slow down your game startup speed.
- It does not use any in-game resources.
- It does not enable the in-game multi-language support.

However, JAT does load all translated text into memory, which does lead to higher memory usage.

## Getting Started

### Version Compatibility

Open `COM3D2.exe` to see your game version in the upper right corner.

Tested and passed on COM3D2 2.44.0 / COM3D2 2.44.5.

Versions of COM3D2.5 greater than 3.41.0 will be supported after the official release. Versions below 3.41.0 have not been tested, but should theoretically work.

### Installation

This is a BepinEX plugin. If you have a `BepinEX` folder in your game folder, you're good to go.

Otherwise, we recommend using [CMI](https://github.com/krypto5863/COM-Modular-Installer) to get a basic plugin environment.

<br>

Please download the zip archive from the [Release](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/releases) page.

Go to the Release page to download the compressed package.

For COM3D2 2.xx and COM3D2.5 below 3.41, please download `COM3D2.JustAnotherTranslator.Plugin.zip`

For COM3D2.5 3.41 and above, please download `COM3D2_5.JustAnotherTranslator.Plugin.zip`

<br>

To modify configuration files in-game, install the Configuration Manager plugin at https://github.com/BepInEx/BepInEx.ConfigurationManager . The default shortcut key is F1.

The ConfigurationManagerWrapper.dll provided here (https://github.com/DeathWeasel1337/COM3D2_Plugins) displays a button in the game's gear menu to open the Configuration Manager.

<br>

For legal and copyright reasons, this plugin does not provide translation files. Please obtain them from other sources.

<br>

The compressed package has been organized by folder, just put it into the corresponding folder in the COM3D2 directory.

That is, `COM3D2.JustAnotherTranslator.Plugin.dll` should be located at the following path

`COM3D2\BepInEx\plugins\COM3D2.JustAnotherTranslator\COM3D2.JustAnotherTranslator.Plugin.dll`

The COM3D2 folder at the beginning refers to your game root directory, which is the folder where `COM3D2.exe` is located.

After installation, start the game once, and the plugin will automatically generate the required files.

The configuration file is located at `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`

The translation folder is located at `COM3D2\BepInEx\JustAnotherTranslator`

### Initial Configuration

After starting the game once, a Dump folder and a zh-CN folder will be generated in the translation folder.

The Dump folder is used to dump in-game files, and general users don't need to care about it.

The zh-CN folder is used to place translation files. Open the configuration file and find the following configuration. The target language here is which folder to read the translation files from.

You can change it to en-US, etc., and then restart the game.

This configuration does not affect the actual language. The actual language is determined by the translation file you provide. It only controls which folder to read the files from.

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

### How the plugin reads translation files

For detailed instructions, please refer to the documentation here [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Document)

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

- The `Text` folder is where the main text translations are located. The translatable content includes daily ADV dialogues, NGUI text, uGUI text, and in-game multi-language support text.
- The `Texture` folder is used to replace in-game textures (various images).
- The `Lyric` folder is for files dedicated to lyric subtitles.
- The `UI` folder is a dedicated folder for interface translation. Sprite is used to replace a single sprite, and Text is used to translate UI text.

## Credits

Part of the code is from [https://github.com/Pain-Brioche/COM3D2.i18nEx](https://github.com/Pain-Brioche/COM3D2.i18nEx) under the MIT license

Part of the code is from [https://github.com/ghorsington/CM3D2.YATranslator](https://github.com/ghorsington/CM3D2.YATranslator) under The Unlicense license (for compatibility)


<br>
<br>
<br>

---

<br>
<br>
<br>

# 简体中文

## COM3D2.JustAnotherTranslator.Plugin

只是另一个 COM3D2 翻译插件

此插件仍在积极开发者，目前处于早期测试版状态。

请通过 Discord 反馈问题：[https://discord.com/channels/297072643797155840/1398579642599870564](https://discord.com/channels/297072643797155840/1398579642599870564)

## 为什么需要另一个翻译插件

现有翻译插件都有存在一些我不满意的地方

### LBWtranslation/LBWmodifier

- 它不是公开的，藏在不开放注册的论坛，且需要积分获取，因此对大多数人来说它不存在
- 它内置了修改器，且很多修改器是默认开启的，例如 AllMaidInPrivate 可能会导致游戏崩溃，以及一些可能导致 NTR 的修改器是默认开启的。
- 它不是开源的
- 它不是国际化的
- 它没有文档

### i18nEX 

- 它使用了太多的游戏内资源，例如 csv 格式与官方相同，有太多的冗余和不清楚的地方
- 虽然可以部分国际化，但它主要是为英文用户设计的，存在一些获取 _en 内置资源的行为
- 它与现有翻译资源不兼容，且由于按脚本加载的设计，迁移现有文件是一个艰巨的任务
- 它与游戏实现高度耦合，对于其他语言用户来说，启用 COM3D2 内置国际化后游戏中可以选择 英文、简体中文、繁体中文，而翻译文件需要使用 English 条目，这对其他语言用户来说是迷惑的
- 它只能使用官方的字幕系统

### YATranslator

- 它未被维护，在最近的版本中似乎坏了
- 它没有字幕系统

## JAT 的亮点

- 它拥有一个自行实现的高度可自定义的字幕系统，包括一个舞蹈字幕系统。
- 可以在各种无字幕场景下为您提供字幕，例如舞蹈、钓鱼小游戏、VR卡拉OK、射击小游戏、夜战等。
- 它拥有专门设计的 VR 字幕，就像其他游戏一样，它是悬浮在世界空间中的。
- 它拥有一个受 i18nEX 启发的专用 UI 翻译系统，它几乎不受版本更新影响，不会因为游戏版本更新而需要重做 UI 翻译，因为我们看到了平时太多用户因为游戏更新后 UI 翻译不可用。
- 它的翻译文件是异步加载的，这意味着它不会减慢你的游戏启动速度。
- 它不使用任何游戏内资源
- 它不启用游戏内的多语言支持

不过，JAT 确实会将所有翻译文本加载到内存，这确实会导致较高的内存占用。

## 入门

### 版本兼容性

打开 COM3D2.exe 即可在右上角看见您的游戏版本。

于 COM3D2 2.44.0 / COM3D2 2.44.5 测试通过。

COM3D2.5 大于 3.41.0 的版本将在正式版发布后支持，低于 3.41.0 的版本未经测试，理论上可用。

### 安装

这是一个 BepinEX 插件，如果您的游戏文件夹中有 `BepinEX` 文件夹，那么您就可以开始了。

否则推荐使用 [CMI](https://github.com/krypto5863/COM-Modular-Installer) 来获得一个基本的插件环境（有中文说明）。

<br>

请前往 [Release](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/releases) 页面下载压缩包。

COM3D2 2.xx 以及 COM3D2.5 3.41 以下版本请下载 COM3D2.JustAnotherTranslator.Plugin.zip

COM3D2 3.41 及以上版本请下载 COM3D2_5.JustAnotherTranslator.Plugin.zip

<br>

要在游戏内修改配置文件，请安装 https://github.com/BepInEx/BepInEx.ConfigurationManager 配置管理器插件，其默认快捷键是 F1

[这里](https://github.com/DeathWeasel1337/COM3D2_Plugins)提供的 ConfigurationManagerWrapper.dll 可以在游戏的齿轮菜单显示一个按钮以打开配置管理器。

<br>

由于法律与版权原因，本插件不提供翻译文件，请自行从其他地方获取。

<br>

压缩包内已按文件夹组织好，放入 COM3D2 目录对应文件夹即可。

即 `COM3D2.JustAnotherTranslator.Plugin.dll` 应该位于以下路径

`COM3D2\BepInEx\plugins\COM3D2.JustAnotherTranslator\COM3D2.JustAnotherTranslator.Plugin.dll`

开头的 COM3D2 文件夹指的是您的游戏根目录，也就是 COM3D2.exe 的所在文件夹。

安装后启动一次游戏，插件会自动生成所需文件。

配置文件位于 `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`

翻译文件夹位于 `COM3D2\BepInEx\JustAnotherTranslator`

### 初始化配置

启动一次游戏后，翻译文件夹内会生成一个 Dump 文件夹，一个 zh-CN 文件夹。

Dump 文件夹是用于转储游戏内文件的，一般用户不用管。

zh-CN 文件夹是用于放置翻译文件的，打开配置文件，找到下面的配置，这里的目标语言即为读取哪一个文件夹内的翻译文件。

你可以改为 en-US 等，然后重启游戏。

这个配置不影响实际语言，实际语言由您提供的翻译文件决定，它只控制读取哪一个文件夹里的文件。

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

### 插件如何读取翻译文件

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

对应语言的翻译文件夹中有 Text、Texture、UI、Lyric 4 个主要文件夹

- `Text` 文件夹是主要的文本翻译所在，可翻译内容包括日常 ADV 对话、NGUI 文本、uGUI 文本、游戏内置多语言支持文本。
- `Texture` 文件夹用于替换游戏内纹理（各种图片）
- `Lyric` 文件夹放置歌词字幕专用的文件。
- `UI` 文件夹是界面翻译专用的文件夹，Sprite 用于替换单个精灵图、Text 则用于翻译 UI 文本。



## Credits

部分代码来自 [https://github.com/Pain-Brioche/COM3D2.i18nEx](https://github.com/Pain-Brioche/COM3D2.i18nEx) 基于 MIT 许可证

部分代码来自 [https://github.com/ghorsington/CM3D2.YATranslator](https://github.com/ghorsington/CM3D2.YATranslator) 基于 The Unlicense 许可证（为了保持兼容性）
