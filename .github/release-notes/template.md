# Note

Please check the repository [homepage](https://github.com/{{REPO}}) for information on which package to download and how to install it.

Please report any issues in the [Discord](https://discord.com/channels/297072643797155840/1398579642599870564) subchannel.

Thanks in advance.

### Please Note

**Important for none Chinese, Japanese and Korean languages users:** Please manually enable the `EnableNoneCJKFix/启用非CJK语言修复` option under `[10Fixer]`.

If this optional fix is not enabled, some interfaces may exhibit text that is crowded together, overlapping, or has spaces automatically inserted between text.

**Recommended for Chinese, Japanese, and Korean users:** Due to regional font inconsistencies, replacing the font with your region's preferred font will provide a better experience.

Please manually enable the `EnableUIFontReplace/启用UI字体替换` option under `[10Fixer]`.

You can find the usage instructions and default font download link [here](https://github.com/{{REPO}}/blob/main/Document/English/TranslationGuide.md#ui-font-replacement) (This function is invalid if the corresponding font is not installed).

# Incompatible changes

**In version 0.5.4, the UI translation module support for the Maid Attributes panel has been moved from the default enabled UITextExtraPatch to UITextDangerPatch. You need to enable `EnableUITextDangerPatch/Enable UI Text Translation Danger Patch` in the settings for this translation scope to reappear.**

Version 0.3.0 modifies the way dance lyrics are read. The reason is that the newly released official dances use a multi-audio file synthesis method for playback, making the old categorization method no longer applicable.

Therefore, both the regular dance and karaoke modes are now divided into folders by song ID. You will need to relocate your old lyrics files.

The `Lyric.zip` contains information on all currently available songs that I extracted. You can refer to it for reference. The document has been updated accordingly and includes detailed explanations.

If you cannot find `Lyric.zip`, please download it from the v0.5.8 release.

[Online Documents](https://github.com/{{REPO}}/blob/main/Document/English/TranslationGuide.md#dance-mode-lyric-subtitles)

# Changes

{{CHANGELOG}}

<br>
<br>
<br>

# 说明

请查看[仓库主页](https://github.com/{{REPO}})以获取该下载哪一个包和如何安装等信息

如有任何问题，请在 [Discord](https://discord.com/channels/297072643797155840/1398579642599870564) 子频道中报告。

提前感谢！

### 请注意

**非中日韩语言用户请注意：** 请手动启用 `[10Fixer]` 下的 `EnableNoneCJKFix/启用非CJK语言修复` 选项。

如果未启用此可选修复，部分界面可能会出现文字挤在一起、重叠，或者文字之间被自动插入空格的情况。

**推荐中日韩语言用户开启字体替换：** 由于各地区字形不一致，替换为您地区的首选字体可以获得更佳体验。

请手动启用 `[10Fixer]` 下的 `EnableUIFontReplace/启用UI字体替换` 选项。

可以在[这里](https://github.com/{{REPO}}/blob/main/Document/%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87_SimplifiedChinese/%E7%BF%BB%E8%AF%91%E6%8C%87%E5%8D%97.md#ui-%E5%AD%97%E4%BD%93%E6%9B%BF%E6%8D%A2)查看使用说明以及默认字体下载链接（未安装对应字体则此功能无效）。

# 不兼容更改

**v0.5.4 版本中将 女仆属性 面板的 UI 翻译模块支持从默认开启的 UITextExtraPatch 移动到了 UITextDangerPatch，需要在设置中启用 `EnableUITextDangerPatch/启用UI文本翻译危险补丁` 此翻译作用域才会重新生效。**

v0.3.0 修改了读取舞蹈歌词的方式，原因是官方新出的舞蹈采用多个音频文件合成的方式播放音频，旧有的分类方式不再适用。

因此普通舞蹈和卡拉OK模式都修改为按歌曲ID分文件夹，您需要重新放置您的旧歌词文件。

`Lyric.zip` 中已包含我提取的目前所有歌曲的信息，您可以对照使用，文档已相应更新并说明详细信息。

如果找不到 `Lyric.zip`，请到 v0.5.8 的 release 下载。

[在线文档](https://github.com/{{REPO}}/blob/main/Document/%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87_SimplifiedChinese/%E7%BF%BB%E8%AF%91%E6%8C%87%E5%8D%97.md#%E8%88%9E%E8%B9%88%E6%A8%A1%E5%BC%8F%E6%AD%8C%E8%AF%8D%E5%AD%97%E5%B9%95)

# 更改

{{CHANGELOG_ZH}}

**Full Changelog**: {{FULL_CHANGELOG_URL}}
