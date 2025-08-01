# COM3D2.JustAnotherTranslator.Plugin

Just Another COM3D2 Translator Plugin

Part of the code is from https://github.com/Pain-Brioche/COM3D2.i18nEx under the MIT license

Part of the code is from https://github.com/ghorsington/CM3D2.YATranslator under The Unlicense license (for
compatibility)


<br>

# COM3D2.JustAnotherTranslator.Plugin

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

前往 Release 页面下载压缩包。

压缩包内已按文件夹组织好，放入 COM3D2 目录对应文件夹即可。

即 COM3D2.JustAnotherTranslator.Plugin.dll 应该位于以下路径

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



## 鸣谢

部分代码来自 [https://github.com/Pain-Brioche/COM3D2.i18nEx](https://github.com/Pain-Brioche/COM3D2.i18nEx) 基于 MIT 许可证

部分代码来自 [https://github.com/ghorsington/CM3D2.YATranslator](https://github.com/ghorsington/CM3D2.YATranslator) 基于 The Unlicense 许可证（为了保持兼容性）
