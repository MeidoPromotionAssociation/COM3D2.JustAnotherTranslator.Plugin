# JustAnotherTranslator Migration Guide

This guide is translated by AI.

If you have any questions, please refer to the Chinese version. 

We welcome any improvements and fixes.

repo url:  https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin


## I don't know which plugin I'm using

If you're unsure which translation plugin you're using, you can quickly determine it by following the path characteristics below. Search for these file names in the game's root directory (where COM3D2.exe is located).

- If you see any of the following, LBWtranslation/LBWmodifier is being used.
  - `LBWmodifier.dll`
  - `LBWmodifier-Transator D3V.dll` or `LBWmodifier-Transator D4V.dll` or `LBWmodifier-Transator D2.dll` or `LBWmodifier-Transator D3.dll`
- If you see any of the following, COM3D2.i18nEx is being used.
  - `COM3D2\BepInEx\plugins\I18N\` folder
- If you see any of the following, COM3D2.YATranslator / CM3D2.YATranslator is being used.
  - `COM3D2.YATranslator.Plugin.dll`
  - `CM3D2.YATranslator.Plugin.dll`
  - `COM3D2.YATranslator.Hook.dll`
  - `CM3D2.YATranslator.Hook.dll`
  - `COM3D2.YATranslator.Sybaris.Patcher.dll`
  - `CM3D2.YATranslator.Sybaris.Patcher.dll`

Assisted judgment (check where the translation content is located):
- `COM3D2\LBWtranslation\` exists → LBWtranslation/LBWmodifier
- `COM3D2\i18nEx\` exists → i18nEx
- `COM3D2\Sybaris\UnityInjector\Config\Strings\` exists → YATranslator

Next steps:
- After confirmation, proceed to the corresponding section of this article to begin the migration:
  - Migrate from LBWtranslation/LBWmodifier
  - Migrate from COM3D2.i18nEx
  - Migrate from COM3D2.YATranslator / CM3D2.YATranslator
- If multiple sets of artifacts exist in your environment, prioritize migrating the one with the most translation files/most recently updated. After the migration is complete, follow the corresponding "Cleanup" section to delete any remaining artifacts.

Note:
- The JAT coexists with XUnity.AutoTranslator. XUAT is typically located in `COM3D2\BepInEx\plugins\XUnity.AutoTranslator\`. It is not one of the three libraries listed above and does not require migration according to this page.
- IMGUITranslationLoader is a plugin specifically designed for translating the IMGUI window. JAT does not have this function and does not need to be removed.

<br>
<br>
<br>
<br>
<br>
<br>


# Migrate from LBWtranslation/LBWmodifier

## Preparation before migration

Please first complete the initial configuration of the plugin by following the instructions in the [README](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin).

That means the files should already exist in these two directories.

- The configuration file is located at: `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- The translation folder root is located at: `COM3D2\BepInEx\JustAnotherTranslator`

The `COM3D2\` at the beginning of the path refers to your game's root directory, which is where `COM3D2.exe` is located.

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

## LBW Modifier

Newer versions of LBWmodifier are divided into translators and modifiers.

DLLs containing `LBWmodifier-Modifier` are modifier plugins (e.g., `LBWmodifier-Modifier D2.dll`), which can be kept.

DLLs containing `LBWmodifier-Transator` are translation plugins (e.g., `LBWmodifier-Transator D2.dll`), which need to be deleted.

## Cleanup

The file locations are not fixed. It is recommended to directly search for the filenames and delete `LBWmodifier.dll`, `LBWmodifier-Transator D3V.dll`, `LBWmodifier-Transator D4V.dll`, `LBWmodifier-Transator D2.dll`, and `LBWmodifier-Transator D3.dll`.

Delete the file `COM3D2\BepInEx\plugins\LBWmodifier.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\LBWmodifier-Transator D3V.dll`. This file is the plugin DLL.

Delete the folder `COM3D2\BepInEx\plugins\Translator D4V`. This file is the plugin DLL.

Delete the folder `COM3D2\BepInEx\plugins\Translator D3V` This folder contains the plugin DLLs.

Delete the folder `COM3D2\BepInEx\plugins\Translator D2` This folder contains the plugin DLLs.

Delete the folder `COM3D2\BepInEx\plugins\Translator D3` This folder contains the plugin DLLs.

Delete the file `COM3D2\BepInEx\plugins\LBWmodifier-Transator D4V.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\Transator D2\LBWmodifier-Transator D2.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\Modifier D2\LBWmodifier-Transator D2.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\Modifier D2\LBWmodifier-Transator D2.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\Transator D3\LBWmodifier-Transator D3.dll`. This file is the plugin DLL.

Delete the file `COM3D2\BepInEx\plugins\Modifier D3\LBWmodifier-Transator D3.dll`. This file is the plugin DLL.

Delete `COM3D2\LBWtranslation`, which is your translation folder (optional).

<br>
<br>
<br>

---

<br>
<br>
<br>

# Migrate from COM3D2.i18nEx

## Preparation before migration

Please first complete the initial configuration of the plugin by following the instructions in the [README](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin).

That means the files should already exist in these two directories.

- The configuration file is located at: `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- The translation folder root is located at: `COM3D2\BepInEx\JustAnotherTranslator`

The `COM3D2\` at the beginning of the path refers to your game's root directory, which is where `COM3D2.exe` is located.

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

<br>
<br>
<br>

---

<br>
<br>
<br>


# Migrate from COM3D2.YATranslator / CM3D2.YATranslator

## Preparation before migration

Please first complete the initial configuration of the plugin by following the instructions in the [README](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin).

That means the files should already exist in these two directories.

- The configuration file is located at: `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- The translation folder root is located at: `COM3D2\BepInEx\JustAnotherTranslator`

The `COM3D2\` at the beginning of the path refers to your game's root directory, which is where `COM3D2.exe` is located.

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

Delete `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Plugin.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Plugin.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Hook.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Hook.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\COM3D2.YATranslator.Sybaris.Patcher.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\CM3D2.YATranslator.Sybaris.Patcher.dll` file; this is where the plugin DLL is located.

Delete `COM3D2\Sybaris\UnityInjector\Config\Strings`; this is your translation folder (optional).

Delete `COM3D2\Sybaris\UnityInjector\Config\Textures`; this is your translation folder (optional).

Delete `COM3D2\Sybaris\UnityInjector\Config\Assets`; this is your translation folder (optional).
