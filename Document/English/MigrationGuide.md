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

> **In most cases you do not need to migrate LBW's dance lyric subtitles at all.**
>
> The v0.5.8 release ships a `Lyric.zip` that already contains the lyric timelines of some songs together with their translations (the translations are in Chinese).
>
> How to use it: extract the contents of `Lyric.zip` into `COM3D2\BepInEx\JustAnotherTranslator\<your language>\Lyric\`. Inside the archive there are `<song ID>\lyric.csv` and `_Karaoke\<KaraokeMusicId>\lyric.csv` directly, **without** an outer `Lyric` folder, so make sure you extract into the `Lyric` directory, not into its parent.
>
> What is inside: every song that the game itself provides lyrics for (`dance_subtitle.nei`) is included. Songs without official lyrics (for example the various `-カラオケ` and `-ハイテンション` versions, and VR karaoke mode) only keep a header-only empty `lyric.csv` as a placeholder, with a `ThisLyricIsEmpty_这个歌词是空的.txt` note next to it; if you see that file, the timeline of that song still has to be built by hand.
>
> If `Lyric.zip` is missing from the current release, download it from the [v0.5.8 release](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/releases/tag/v0.5.8).
>
> Only continue with the migration steps below if you are sure that `Lyric.zip` does not contain the songs you need, or if your LBW lyrics are of higher quality and you want to override the bundled ones.
>
> Note: extracting overwrites files with the same name. Back up any `lyric.csv` you have already translated yourself.

**Note: since JAT v0.3.0 the lyric folder is named after the song ID, not the song internal name.** The old instruction "just convert LBW's `csv_rhythm_action` and copy it over" no longer works.

LBW keeps its lyrics in `COM3D2\LBWtranslation\DanceSubtitle\csv_rhythm_action\<song internal name>\`, while JAT now expects:

`COM3D2\BepInEx\JustAnotherTranslator\<your language>\Lyric\<song ID>\lyric.csv`

There is no direct conversion between `<song internal name>` and `<song ID>`, so the conversion script needs a "song internal name -> song ID" mapping table.

#### Step 1: Prepare a mapping table

Option 1 (recommended): let JAT generate `danceInfos.csv`

-   The file is located at `COM3D2\BepInEx\JustAnotherTranslator\<your language>\Lyric\danceInfos.csv`
-   JAT writes a row for a song as soon as you enter dance mode with it, no extra setting is required (`EnableDumpDanceInfo` is enabled by default)
-   So: let JAT take over first, then enter each song whose lyrics you want to migrate once (entering dance mode is enough, you do not have to finish the dance)
-   In `danceInfos.csv`, the `MusicName` column is LBW's folder name and the `Id` column is JAT's folder name

Option 2: generate it from `dance_setting.nei`

-   Extract `dance_setting.nei` from `COM3D2\GameData\csv.arc` and convert it to `dance_setting.csv` with [MeidoSerialization](https://github.com/MeidoPromotionAssociation/MeidoSerialization)
-   Its `譜面とかのcsvフォルダ名` column is LBW's folder name
-   Downside: this column is empty for the base songs (their folder name is baked into the scene assets), so it only covers DLC songs

Both options can be used together, the script merges the mappings.

#### Step 2: Run the conversion script

Download the conversion script `lyric_csv_format_convert_English.py`

[https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script)

- Make sure you have Python 3 installed.
- Right-click in the blank area next to the script and select Open in Terminal.
- In the terminal, enter `python lyric_csv_format_convert_English.py COM3D2/LBWtranslation/DanceSubtitle/csv_rhythm_action COM3D2/BepInEx/JustAnotherTranslator/<your language>/Lyric --mapping COM3D2/BepInEx/JustAnotherTranslator/<your language>/Lyric/danceInfos.csv`.
- Remember to replace the path with your actual path.
- For example, `python lyric_csv_format_convert_English.py X:/HG/maid/COM3D2/LBWtranslation/DanceSubtitle/csv_rhythm_action X:/HG/maid/COM3D2/BepInEx/JustAnotherTranslator/English/Lyric --mapping X:/HG/maid/COM3D2/BepInEx/JustAnotherTranslator/English/Lyric/danceInfos.csv`.
- If you also prepared `dance_setting.csv`, add another `--mapping dance_setting.csv`.
- You can just run `python lyric_csv_format_convert_English.py` and it will give you some instructions

#### Notes

-   One LBW folder may map to several IDs (the normal, theater and DLC versions of a song share the same chart), the script writes the same lyrics into every matching ID folder.
-   The script also overwrites the `lyric.csv` of the same ID that came from `Lyric.zip`; back it up first if you want to keep the bundled version.
-   Folders that cannot be mapped are written to `Lyric\_Unmapped\<folder name>\lyric.csv`, which JAT never reads. The script lists them at the end; add the missing mapping and run it again.
-   If you migrated with the old script before, folders like `Lyric\<song internal name>\` are no longer read and can be deleted.
-   VR karaoke lyrics live in `Lyric\_Karaoke\<KaraokeMusicId>\lyric.csv`, the mapping table is `danceInfosKaraoke.csv`, and you should pass `--karaoke`.
-   If you still use a JAT version older than v0.3.0, add `--legacy` to keep the old behaviour of naming folders after the source folder.

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
