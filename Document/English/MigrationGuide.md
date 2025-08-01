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

Download the conversion script [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/script/lyric_csv_format_convert.py](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/script/lyric_csv_format_convert.py)

Place the script in the `COM3D2\LBWtranslation` folder.

Create two folders, one named `output` and one named `input`.

Run the script.

`python lyric_csv_format_convert.py ./input ./output`

Then copy the files from `./output` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Lyric\`.

Alternatively, after manual conversion, copy the converted files to `COM3D2\BepInEx\JustAnotherTranslator\Lyric\`.

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

Find the `COM3D2\i18nEx\<Your set language>` folder.

Usually, this is `COM3D2\i18nEx\English`.

You will need to perform some format conversion. Since I do not use i18nEx, I cannot provide assistance. Pull requests are welcome.

### Texture Translations

Find the `COM3D2\i18nEx\<Your set language>\Textures` folder.

Usually, this is `COM3D2\i18nEx\English\Textures`.

Copy the files from `COM3D2\i18nEx\<Your set language>\Textures` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\Texture\`.

### UI Translations

Find the `COM3D2\i18nEx\<Your set language>\UI` folder.

Download the conversion script [https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/script/ui_csv_format_convert.py](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/script/ui_csv_format_convert.py)

Place the script in the `COM3D2\i18nEx\<Your set language>` folder.

Create two folders, one named `output` and one named `input`.

Run the script in the `COM3D2\i18nEx\<Your set language>` folder.

`python ui_csv_format_convert.py ./input ./output`

Then copy the files from `./output` to `COM3D2\BepInEx\JustAnotherTranslator\<Your set language>\UI`.

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
