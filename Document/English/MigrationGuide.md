# JustAnotherTranslator Migration Guide

This guide is translated by AI.

If you have any questions, please refer to the Chinese version. 

We welcome any improvements and fixes.

repo url:  https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin

<br>

# Migrate from LBWtranslation/LBWmodifier

## Preparation before migration

After installing JustAnotherTranslator, start the game once and then exit. This is to allow the plugin to generate the necessary files.

<br>

The COM3D2 folder refers to your game's root directory, which is the folder where `COM3D2.exe` is located.

The `zh-CN` in the path is variable and depends on your settings.

The translation folder is located at `COM3D2\BepInEx\JustAnotherTranslator`.

<br>

The configuration file is located at `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`.

The `zh-CN` folder is used for placing translation files. Open the configuration file and find the setting below. The target language here determines which folder's translation files will be read.

You can change it to `en-US`, etc., and then restart the game.

This setting does not affect the actual language, which is determined by the translation files you provide. It only controls which folder's files are read.

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

## Start Migration

### Script Translations

Find the `COM3D2\LBWtranslation` folder.

Copy the files from `"COM3D2\LBWtranslation\Script"` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Text\`.

The default is `zh-CN`, so the path is `COM3D2\BepInEx\JustAnotherTranslator\zh-CN\Text`.

### Texture Translations

Find the `COM3D2\LBWtranslation` folder.

Copy the files from `COM3D2\LBWtranslation\Texture` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Texture\`.

### Dance Lyric Subtitles

You may not need to migrate this, as JAT is released with lyrics included.

Find the `COM3D2\LBWtranslation` folder.

Download the conversion script `lyric_csv_format_convert_English.py`

[https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script)

- Make sure you have Python 3 installed.
- Right-click in the blank area next to the script and select Open in Terminal.
- In the terminal, enter `python lyric_csv_format_convert_English.py COM3D2/LBWtranslation/DanceSubtitle/csv_rhythm_action COM3D2/BepInEx/JustAnotherTranslator/<your language>/Lyric`.
- Remember to replace the path with your actual path.
- For example, `python lyric_csv_format_convert_English.py X:/HG/maid/COM3D2/LBWtranslation/DanceSubtitle/csv_rhythm_action X:/HG/maid/COM3D2/BepInEx/JustAnotherTranslator/<your language>/Lyric`.
- You can just run `python lyric_csv_format_convert_English.py` and it will give you some instructions

For most users, using Python scripts can be daunting. If you don't know how, copy the script contents to AI and let it teach you.

## Cleanup

The file locations are not fixed. It is recommended to search for the filenames directly and delete `LBWmodifier.dll`, `LBWmodifier-Transator D3V.dll`, and `LBWmodifier-Transator D4V.dll`.

Delete the `COM3D2\BepInEx\plugins\LBWmodifier.dll"` file; this is where the plugin DLL is located.

Delete the `COM3D2\BepInEx\plugins\Translator D3V` folder; this is where the plugin DLL is located.

Delete the `COM3D2\BepInEx\plugins\LBWmodifier-Transator D3V.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\BepInEx\plugins\Translator D4V` folder; this is where the plugin DLL is located.

Delete the `COM3D2\BepInEx\plugins\LBWmodifier-Transator D4V.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\LBWtranslation`; this is your translation folder (optional).

# Migrate from COM3D2.i18nEx

## Preparation before migration

After installing JustAnotherTranslator, start the game once and then exit. This is to allow the plugin to generate the necessary files.

<br>

The COM3D2 folder refers to your game's root directory, which is the folder where `COM3D2.exe` is located.

The `zh-CN` in the path is variable and depends on your settings.

The translation folder is located at `COM3D2\BepInEx\JustAnotherTranslator`.

<br>

The configuration file is located at `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`.

The `zh-CN` folder is used for placing translation files. Open the configuration file and find the setting below. The target language here determines which folder's translation files will be read.

You can change it to `en-US`, etc., and then restart the game.

This setting does not affect the actual language, which is determined by the translation files you provide. It only controls which folder's files are read.

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

## Start Migration

### Script Translations

Find the `COM3D2\i18nEx\<Your set language>\Script` folder.

Usually, this is `COM3D2\i18nEx\English\Script`.

If you have a large number of .txt files, you can compress them and place the archive in `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Text`.

Broadly, the script translation format for i18nEx is the same as JAT, but there are some minor differences. For example, you don't need to replace `<E>` with a tab, [see here](https://github.com/ghorsington/COM3D2.i18nEx/wiki/How-to-translate#script-translations).

Text containing tags like `<E>` is actually official text that includes multiple languages, for example: `这是日文原文<e>This is English text<sc>这是简体中文文本`.

In this situation, the official game code parses it into the corresponding language text and then decides which one to use based on the current game language.

JAT translates using the original Japanese text *after* the official code has parsed it; it cannot use the entire string.

For example, if the original text is `这是日文原文<e>This is English text<sc>这是简体中文文本`,

in JAT, it should be written as `这是日文原文         This is target language text`.

Note that this is a tab, not spaces.

Currently, there is no script to handle this situation directly; it needs to be done manually.

<br>

For subtitles, i18nEx uses the same tag format as the official game. JAT does not use this; please refer to the translation guide.

<br>

If you have a .zip or .zst file containing a .bson file,

you will need to perform some format conversion. Since I do not use i18nEx, I cannot provide assistance. Pull requests are welcome.

### Texture Translations

Find the `COM3D2\i18nEx\<Your set language>\Textures` folder.

Usually, this is `COM3D2\i18nEx\English\Textures`.

Copy the files from `COM3D2\i18nEx\<Your set language>\Textures` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Texture\`.

### UI Translations

Find the `COM3D2\i18nEx\<Your set language>\UI` folder.

Please note that this script only handles .csv files.

If you have a .zip or .zst file with a .bson file in it, the script won't handle it and you'll need to do some format conversion. Since I don't use i18nEX, I can't help you there, but you're welcome to submit a PR.

Download the conversion script `ui_csv_format_convert_English.py`

[https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script)

- Make sure you have Python 3 installed.
- Right-click in the blank area next to the script and select Open in Terminal.
- In the terminal, enter `python ui_csv_format_convert_English.py COM3D2/i18nEx/<your language>/UI COM3D2/BepInEx/JustAnotherTranslator/<your language>/UI/Text`
- Remember to replace the path with your actual path.
- For example, `python ui_csv_format_convert_English.py X:/HG/maid/COM3D2/i18nEx/English/UI X:/HG/maid/COM3D2/BepInEx/JustAnotherTranslator/English/UI/Text`
- You can just run `python ui_csv_format_convert_English.py` and it will give you some instructions

For most users, using Python scripts can be daunting. If you don't know how, copy the script contents to AI and let it teach you.

## Cleanup

Delete the `COM3D2\BepInEx\plugins\I18N` folder; this is where the plugin DLL is located.

Delete `COM3D2\i18nEx`; this is your translation folder (optional).

# Migrate from COM3D2.YATranslator / CM3D2.YATranslator

## Preparation before migration

After installing JustAnotherTranslator, start the game once and then exit. This is to allow the plugin to generate the necessary files.

<br>

The COM3D2 folder refers to your game's root directory, which is the folder where `COM3D2.exe` is located.

The `zh-CN` in the path is variable and depends on your settings.

The translation folder is located at `COM3D2\BepInEx\JustAnotherTranslator`.

<br>

The configuration file is located at `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`.

The `zh-CN` folder is used for placing translation files. Open the configuration file and find the setting below. The target language here determines which folder's translation files will be read.

You can change it to `en-US`, etc., and then restart the game.

This setting does not affect the actual language, which is determined by the translation files you provide. It only controls which folder's files are read.

```
[2General]

## Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径
# Setting type: String
# Default value: zh-CN
TargetLanguage/目标语言 = zh-CN
```

## Start Migration

### Script Translations

Find the `COM3D2\Sybaris\UnityInjector\Config\Strings` folder.

Copy the files from `COM3D2\Sybaris\UnityInjector\Config\Strings` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Text\`.

### Texture Translations

Find the `COM3D2\Sybaris\UnityInjector\Config\Textures` folder.

Copy the files from `COM3D2\Sybaris\UnityInjector\Config\Textures` to
`COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Texture\`.

Find the `"COM3D2\Sybaris\UnityInjector\Config\Assets"` folder.

Copy the files from `"COM3D2\Sybaris\UnityInjector\Config\Assets"` to
`COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Texture\`.

## Integration with XUnity.AutoTranslator

JAT automatically integrates with [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator). Text translated by JAT will not be translated again by XUAT.

There is no need to process translated text as before.

## Cleanup

Delete the `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Plugin.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Plugin.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Hook.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Hook.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Sybaris.Patcher.dll` file; this is where the plugin DLL is located.

Delete the `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Sybaris.Patcher.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\Config\Strings`; this is your translation folder (optional).

Delete `COM3D2\Sybaris\UnityInjector\Config\Textures`; this is your translation folder (optional).

Delete `COM3D2\Sybaris\UnityInjector\Config\Assets`; this is your translation folder (optional).
