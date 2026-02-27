# JustAnotherTranslator Translation Guide

This document will detail how to create and manage translation files for the JustAnotherTranslator(JAT) plugin.

This guide is translated by AI.

If you have any questions, please refer to the Chinese version.

We welcome any improvements and fixes.

repo url:  https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin

<br>

JAT adopts a modular design, where each module can be enabled or disabled independently. However, the standard subtitles module depends on the general text translation module.

- General Text Translation
- Texture Replacement
- UI Translation
- Lyrics Subtitles
- Standard Subtitles
- Fix patch

<br>

- **Configuration File:** `COM3D2\BepInEx\config\Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg`
- **Root Translation Folder:** `COM3D2\BepInEx\JustAnotherTranslator`

All the in-game configuration file modifications described in the document need to be done using [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), whose default shortcut key is F1, and will not be elaborated further below.

<br>

# Integration with JAT

## Interaction between JAT and XUnity.AutoTranslator

JAT automatically integrates with [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator).

JAT actively intervenes in XUnity's translation decisions; text translated by JAT will not be translated by XUnity again, and text translated by XUnity is unlikely to be in the JAT dictionary.

If you have developed a plugin that you wish to integrate with JAT, please check https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/COM3D2.JustAnotherTranslator.Plugin/Utils/XUATInterop.cs

JAT does not use special tags. You can refer to the `XUAT_IsTranslatable_Prefix` method in `XUATInterop.cs` to check if text has been JAT translated. Sprite sheets/atlases and textures replaced by JAT also have their own special prefixes.

<br>

All Harmony patches for JAT have fixed IDs. Please check https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/COM3D2.JustAnotherTranslator.Plugin/Manger

Harmony patches are registered in the `Init()` method.

JAT also includes detailed code comments to facilitate your exploration of its code.

If you have any other needs, please submit an issue.

# Hot Reload Resources

If you have modified translation files in-game, you can hot reload translation resources by pressing the shortcut key with `No Default Shortcut`.

Please set your own keyboard shortcut in the `ReloadTranslateResource/重载翻译资源` option.

After pressing the shortcut key, the following resources will be hot reloaded:

- General Text Translation
- Texture Replacement
- UI Translation (Text and Sprites)
- Regular Subtitles (Current subtitles will be destroyed)
- Speaker color configuration (SubtitleColors.json)

Unlike the modules corresponding to the toggle switch, this will not uninstall and reload the corresponding modules; it will only reload the translation files.

Text and UI translations are still loaded asynchronously. Please pay attention to console messages; JAT will return the original text before loading is complete.

Resources will not be hot-reloaded if the corresponding module is not enabled.

<br>

# General Text Translation

The General Text Translation module is where the main text translation occurs. It can translate content including daily ADV dialogues, NGUI text, uGUI text, and the game's built-in multi-language support text.

Subtitles, excluding lyrics, also depend on this module.

Unlike COM3D2.i18nEx, JAT does not care about translation filenames; you do not need to create translation files according to scripts.

JAT loads all translation files into a single, large in-memory database upon startup and then translates based on this database during runtime.

This approach has its advantages and disadvantages.

The most notable is that it will consume more memory than i18nEx.

After toggling the `EnableGeneralTextTranslation/启用通用文本翻译` option in-game, translated text will be hot-reloaded.

## 1. Translation File Location

All text translation files should be placed in the following directory:

`COM3D2\BepInEx\JustAnotherTranslator\<Your_Language_Setting>\Text`

The plugin will automatically load all supported translation files from this directory at startup.

## 2. File Reading Order and Override Rules

Understanding the file reading order is crucial because it determines which translation takes effect when multiple translations for the same original text exist.

**Core Rule: Later-loaded translations will override earlier-loaded ones.**

The plugin's loading order is as follows:

1.  **File Types**: The plugin supports `.txt` and `.zip` file formats.
2.  **Directory Structure**: The plugin recursively reads files from the `COM3D2\BepInEx\JustAnotherTranslator\<Your_Language_Setting>\Text` directory and all its subdirectories.
3.  **Sorting Rules**:
    *   First, the plugin reads and loads all files in the root of the translation folder, sorted by their full path in **Unicode character order**.
    *   Then, the plugin gets all subdirectories, also sorted by their directory path in **Unicode character order**.
    *   Finally, it iterates through these sorted subdirectories and loads the files within them. Files in each subdirectory also follow the rule of being sorted by filename.
    *   In short, the loading order is **"root files -> subdirectory1 files -> subdirectory2 files..."**, and at each level, files and directories are sorted by name.

4.  **Order Inside ZIP Archives**:
    *   By default, `.txt` files inside a ZIP archive are read in the order they are stored within the archive (a zip file has an internal file list), which is not necessarily sorted by filename and depends on your compression software.
    *   If the `AllowFilesInZipLoadInOrder` option is enabled in the plugin settings, `.txt` files inside the ZIP package will also be sorted and loaded in **Unicode character order**. However, this slightly reduces loading speed, so it is disabled by default.

**Override Example**:
Assuming the following file structure:
- `Text\A_translations.txt`
- `Text\B_translations.txt`
- `Text\Overrides\C_translations.txt`

The loading order will be `A_translations.txt` -> `B_translations.txt` -> `C_translations.txt`.
If both `A_translations.txt` and `C_translations.txt` contain a translation for "Hello", the translation from `C_translations.txt` will ultimately be used.

**Complex File Structure Example**:
Assuming a more complex directory structure:
```
Text/
├── 1_core.txt
├── 2_dlc.zip
│   ├── b_dlc2.txt
│   └── a_dlc1.txt
├── Z_patch.txt
├── A_Mods/
│   ├── Mod_A.txt
│   └── Mod_B.zip
│       └── translations.txt
└── B_Overrides/
    └── my_override.txt
```

The plugin will load files in the following order:
1.  `Text/1_core.txt` (root files sorted by name)
2.  `Text/2_dlc.zip` (root files sorted by name)
    - If `AllowFilesInZipLoadInOrder` is enabled, the internal order will be `a_dlc1.txt` -> `b_dlc2.txt`.
    - Otherwise, the order depends on the storage order within the ZIP file.
3.  `Text/Z_patch.txt` (root files sorted by name)
4.  `Text/A_Mods/Mod_A.txt` (subdirectories are processed after root files, sorted by name)
5.  `Text/A_Mods/Mod_B.zip` -> `translations.txt`
6.  `Text/B_Overrides/my_override.txt` (subdirectory `B_Overrides` is processed after `A_Mods`)

Therefore, translations in `B_Overrides/my_override.txt` have the highest priority and can override all other identical translations in other files.

Remember: Later-loaded translations will override earlier-loaded ones.

Therefore, it is generally recommended to create several folders to categorize translations by quality, for example:

```
Text/
├──0000AvoidConflicts
├──1MachineTranslation
├──2LowQualityAITranslation
├──3HighQualityAITranslation
├──4HumanTranslation
├──5HumanUITranslation
├──6Overrides
└──9999AvoidConflicts
```

This naming convention cleverly utilizes the plugin's rule of loading directories in **Unicode order**, allowing you to finely control translation priority:

- This naming ensures the loading order is strictly from smaller numbers to larger ones (`0` -> `1` -> `2`...). Folders with smaller numbers are loaded earlier.
- You can place the most basic, lowest-quality translations (e.g., `1MachineTranslation`) in lower-numbered folders. Then, place higher-quality, proofread translations (e.g., `4HumanTranslation`) in higher-numbered folders.
- Because later-loaded translations override earlier ones, the content in the `4HumanTranslation` folder will automatically override translations for the same original text from the `1MachineTranslation` folder.
- A folder like `6Overrides` can be used for temporary, highest-priority personal corrections, ensuring they take effect over all other translations, for instance, if you want to replace "Guest" with "Master".
- Folders like `0000AvoidConflicts` and `9999AvoidConflicts` can be used to ensure certain non-translatable texts are not translated to avoid conflicts.

With this structure, you can easily manage and layer your translation files, ensuring the highest quality version is ultimately applied in the game.

## 3. Translation File Format (`.txt`)

`.txt` files must be **UTF-8** encoded.

### 1. Basic Format

Each line represents a single translation entry, in the following format:

`Original Text[Tab]Translated Text`

-   **Original Text**: The original text in the game to be replaced.
-   **Tab**: The original and translated text must be separated by one or more tab characters (`\t`).
-   **Translated Text**: The translated text to replace the original.

Since it is uncertain whether the separator in the document will be rendered as `\t`, please do not copy the content directly from the document.

If you use Notepad++, you can enable `View -> Show Symbol -> Show White Space and TAB` to see the tab characters. Tabs will be displayed as `->`, and spaces as `·`.


Example:
```
你好			Hello
早上好	早上好
```

For compatibility, the basic translation format is ported from CM3D2.YATranslator. You can also refer to its [documentation](https://github.com/ghorsington/CM3D2.YATranslator/wiki/Translatable-resources).

### 2. Comments

Lines starting with a semicolon (`;`) are treated as comments and will be ignored by the plugin during loading.

Example:
```
;This is a comment line and will be ignored
```

### 3. Escape Characters

You can use standard escape characters in both the original and translated text, and they will be parsed correctly. For example:

| Escape code | Meaning                              |
|-------------|--------------------------------------|
| \\n         | Line break (aka. newline character)  |
| \t          | Tab                                  |
| \a          | Alarm (Hardware beep)                |
| \b          | Back-space (Return)                  |
| \f          | Form-feed                            |
| \r          | Carriage return                      |

Example:
```
Line 1\nLine 2	第一行\n第二行 (Line 1\nLine 2)
```

### 4. Regular Expression Translations

For text requiring more complex matching rules, the plugin supports translation using regular expressions.

For compatibility, regex translations are ported from CM3D2.YATranslator. You can also refer to its [documentation](https://github.com/ghorsington/CM3D2.YATranslator/wiki/Translatable-resources#regex-translations).

-   **Format**: Add a dollar sign `$` at the beginning of the line.
-   **Original Text**: The original text part after the `$` will be treated as a **.NET regular expression** to match game text.
-   **Translated Text**: The translated part can be used as a template, using `${group_name}` or `${group_index}` to reference capture groups from the regex.

**Advanced Feature**: If the content of a regex capture group (e.g., the value of `${name}`) also exists in a regular translation, it will be translated first before being substituted into the final translated template.

**Example**:

`$^Hello, (?<name>\w+)!$	你好, ${name}!`

Suppose the translation file also contains a regular translation:

`John	约翰`

When the game text is `Hello, John!`, the plugin first matches the regular expression and captures `name="John"`.

The plugin then discovers that `John` has a regular translation `约翰` and replaces it with the original.

The final result is `你好, 约翰!`


**More Regex Examples**:

1.  **Matching and translating text with numbers**
    -   **Target**: Translate `You have 5 apples.`
    -   **Rule**: `$^You have (\d+) apples\.$	你拥有 ${1} 个苹果。`
    -   `(\d+)` captures one or more digits, which are inserted into the translation via `${1}`.

2.  **Reordering text elements**
    -   **Target**: Translate `(Event) The story begins.` to `【活动】故事开始了。`
    -   **Rule**: `^\(Event\) (?<content>.+)$	【活动】${content}`
    -   **With regular translation**: If there is also a regular translation `The story begins.    故事开始了。`, the final result will be `【活动】故事开始了。` (【Event】The story has begun.)

3.  **Handling fixed prefixes and dynamic suffixes**
    -   **Target**: Translate all item names starting with `Item_`, like `Item_Potion`.
    -   **Rule**: `^Item_(?<id>.+)$	${id}`
    -   **With regular translation**: Assuming there is `Potion	恢复药水`, `Item_Potion` will be translated to `恢复药水`. This technique is often used to handle cases where internal game IDs are separate from display names.

4.  **Using multiple capture groups**
    -   **Target**: Translate `Character: Alice, Level: 99` to `角色：爱丽丝 (等级: 99)`
    -   **Rule**: `^Character: (?<name>.+), Level: (?<level>\d+)	$Character: ${name} (Level: ${level})`
    -   **With regular translation**: If there is also a regular translation `Alice	爱丽丝`, the final result will be `角色：爱丽丝 (等级: 99)` (Character: 爱丽丝 (Level: 99)). This is very useful for formatting complex strings.

### 5. Multi-language Strings

Sometimes, especially in other language versions of the game, a script text is written in the format `这是日文原文<e>This is English text<sc>这是简体中文文本`.

Text containing tags like `<e>` is actually a piece of official text containing multiple languages.

In this case, the official code will parse it into the corresponding language text and then decide which one to use based on the current game language.

JAT will translate using the original Japanese text *after* the official code has parsed it into the corresponding language; it cannot use the entire string.

For example, if the original text is `这是日文原文<e>This is English text<sc>这是简体中文文本`,

in JAT, it should be written as `这是日文原文         This is target language text`.

Note that this is a tab, not spaces.

The [i18nEx guide](https://github.com/ghorsington/COM3D2.i18nEx/wiki/How-to-translate#script-translations) mentions replacing `<E>` with a tab. This seems to treat only the Japanese part as the original and the English part as the translation, without considering other languages.

Once again, I believe this exposes the flaw of i18nEx being primarily aimed at English users.

## Performance recommendations and some notes

-   The plugin uses asynchronous loading and will not block the game's startup. If your game loads too quickly before the translation files are fully loaded, you might see some untranslated text.
-   If you have a large number of small `.txt` files, loading can be very slow. To improve performance, it is recommended to **merge them into one or a few larger `.txt` files**, or **pack them into a `.zip` archive**.
-   Note: Due to the nature of regular expressions, JAT must iterate through every loaded regex to find a valid match. Therefore, it is best to avoid using regular expressions as much as possible.
-   For text with special tags like `[HF]` and `[SF]`, just keep the tags in the correct position and the text will be replaced before being processed by the official code, which will then correctly replace them with the corresponding words.

# UI Translation

JAT's UI translation module aims to provide a flexible, precise, and game-update-resistant solution for UI translation. It is divided into two parts: UI text translation and UI image translation.

The official Chinese and English versions of COM3D2 use the Unity plugin [I2.Localization](https://assetstore.unity.com/packages/tools/localization/i2-localization-14884) 
to manage localization content. Since all versions share the same base code, the Japanese version we play also includes this plugin.

JAT's UI translation system works by hooking into the low-level functions where this plugin interacts with the official code. When the game requests a piece of UI text or an image, JAT intercepts this request, looks up the corresponding content in its own translation database, and returns the translated result.

We believe this "interception" model has advantages over other solutions. For example, the COM3D2.i18nEx plugin appends translations to the official database. This method is not only highly dependent on game resources but can also lead to unexpected issues.

In contrast, JAT maintains its translation resources independently, without relying on the official CSV format or internal game resources. This makes it more flexible, effectively mitigating the negative impacts of game updates and ensuring the accuracy and consistency of translations.

Please note that for a UI element (text or image) to be translatable by this module, it must first be added to the I2.Localization database by the developers and have a corresponding "Term".

Although some UI text can also be translated by the generic text translation module, using this dedicated module provides a more stable and accurate translation that can handle homonyms correctly.

<br>

UI translation is divided into two parts: **UI Text Translation** and **UI Image Translation**.

## UI Text Translation

### 1. File Location

UI text translation files should be placed in the following directory:

`COM3D2\BepInEx\JustAnotherTranslator\<Your_Language_Setting>\UI\Text`

### 2. File Format and Reading Order

-   **File Type**: Supports `.csv` and `.zip` files.
-   **File Format**: Must be a comma-separated CSV file, must be **UTF-8-BOM** encoded, must have a header, and ideally should conform to [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180).
-   **Header**: Must include the following fields:
    -   `Term`: The term to translate (cannot be empty)。
    -   `Original`: Original text, for reference only, plugins do not use this field (can be empty)。
    -   `Translation`: Translation (can be empty, otherwise the entry will be skipped)。
-   **Reading Order**: Exactly the same as general text translation. The plugin recursively loads all files and directories in Unicode order, and later-loaded `Term`s will override earlier ones.
-   **Filename**: Unlike i18nEx, JAT does not care about the filename.

**Example `SceneDaily.csv`**:
```csv
Term,Original,Translation
SceneDaily/モード選択,モード選択,Mode Selection
SceneDaily/リザルト/クラブ評価,クラブ評価,Club Evaluation
```

### 3. How It Works

The UI Translation module can be used to translate content that has been officially added to the `I2.Localization` database and has a corresponding Term, usually UI text or images.

Translations are retrieved using a unique "Term", for example, `SceneDaily/ボタン文字/男エディット`.

The format of the translation database extracted from the official source is:

config.csv:
```csv
Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)
FPS表示,Text,,FPS表示,,,
VR/VR空間優先,Text,,VR/VR空間優先,,,
```

Note that the first column is `Key`. `Filename + Key = Term`.

For example, the term for the second row (including the header) is `config.csv + FPS表示 = config/FPS表示`.

The term for the third row is `config.csv + VR/VR空間優先 = config/VR/VR空間優先`.

<br>

When designing JAT, we felt that using `Filename + Key` was not ideal. Therefore, the first column in JAT's CSV is `Term`.

When JAT translates:
1.  During game loading, JAT reads all CSV files and builds a unified translation database in memory.
2.  The value passed to JAT by the game is the full `Term` (`SceneDaily/ボタン文字/男エディット`), so JAT looks for a matching `Term` in its translation database.
3.  If not found, it removes the content before the first `/` and searches again with the remainder (`ボタン文字/男エディット`).
4.  After finding a match, it returns the corresponding `Translation` as the result.
5.  The translated text is marked with the same special tags as XUnity.AutoTranslator, so it won't be translated again by other JAT modules or XUAT.

<br>

### 4. Translation Guide

The game's main UI includes both pre-made buttons with text and blank buttons with text.

This is a partial translation extracted from the official documentation. You can see that most of the `ボタン文字(Button Text)` is empty.

However, the `ボタン画像（Button Image）` below are present, indicating that these buttons use icons.

<details>

<summary>Extracted translation</summary>

| Term                         | Original                | Translation                 |
|------------------------------|-------------------------|-----------------------------|
| SceneDaily/ボタン文字/LOAD        |                         |                             |
| SceneDaily/ボタン文字/NPCエディット    |                         |                             |
| SceneDaily/ボタン文字/SAVE        |                         |                             |
| SceneDaily/ボタン文字/イベント        |                         |                             |
| SceneDaily/ボタン文字/カジノ         |                         |                             |
| SceneDaily/ボタン文字/カラオケ        |                         |                             |
| SceneDaily/ボタン文字/クレジット       |                         |                             |
| SceneDaily/ボタン文字/ショップ        |                         |                             |
| SceneDaily/ボタン文字/スカウト        | スカウト                    | Scout                       |
| SceneDaily/ボタン文字/スケジュール      |                         |                             |
| SceneDaily/ボタン文字/スタジオモード     |                         |                             |
| SceneDaily/ボタン文字/ダンス         |                         |                             |
| SceneDaily/ボタン文字/トロフィー       |                         |                             |
| SceneDaily/ボタン文字/プライベートモード設定 | プライベートモード設定             | Private Mode Settings       |
| SceneDaily/ボタン文字/マイルームカスタム   |                         |                             |
| SceneDaily/ボタン文字/メイド管理       |                         |                             |
| SceneDaily/ボタン文字/回想モード       |                         |                             |
| SceneDaily/ボタン文字/執務室モード      |                         |                             |
| SceneDaily/ボタン文字/施設管理        |                         |                             |
| SceneDaily/ボタン文字/男エディット      |                         |                             |
| SceneDaily/ボタン文字/経営切替        |                         |                             |
| SceneDaily/ボタン画像/LOAD        | common_buttom_load      |                             |
| SceneDaily/ボタン画像/NPCエディット    | common_buttom_npcedit   |                             |
| SceneDaily/ボタン画像/SAVE        | common_buttom_save      |                             |
| SceneDaily/ボタン画像/イベント        | common_buttom_event     |                             |
| SceneDaily/ボタン画像/カジノ         | common_buttom_casino    |                             |
| SceneDaily/ボタン画像/カラオケ        | common_buttom_karaoke   |                             |
| SceneDaily/ボタン画像/クレジット       | common_buttom_credit    |                             |
| SceneDaily/ボタン画像/ショップ        | common_buttom_shop      |                             |
| SceneDaily/ボタン画像/スケジュール      | common_buttom_schedule  |                             |
| SceneDaily/ボタン画像/スタジオモード     | common_buttom_studio    |                             |
| SceneDaily/ボタン画像/ダンス         | common_buttom_dance     |                             |
| SceneDaily/ボタン画像/トロフィー       | common_buttom_trophy    |                             |
| SceneDaily/ボタン画像/マイルームカスタム   | common_buttom_myroom    |                             |
| SceneDaily/ボタン画像/メイド管理       | common_buttom_maidkanri |                             |
| SceneDaily/ボタン画像/回想モード       | common_buttom_kaisou    |                             |
| SceneDaily/ボタン画像/執務室モード      | common_buttom_shitsumu  |                             |
| SceneDaily/ボタン画像/施設管理        | common_buttom_shisetsu  |                             |
| SceneDaily/ボタン画像/男エディット      | common_buttom_medit     |                             |
| SceneDaily/ボタン画像/経営切替        | common_buttom_keiei     |                             |
| SceneDaily/メイドパラメータ          | メイドパラメータ                | Maid Parameter              |
| SceneDaily/メイド研修を実施しました      | メイド研修を実施しました            | Maid training was conducted |
</details>

Use a unpacking tool like [AssetStudio](https://github.com/Perfare/AssetStudio) to open the `COM3D2\COM3D2x64_Data` folder.

<details>

<summary>For readers unfamiliar with how UI works</summary>

A Sprite is part of a larger image called an Atlas.

During game development, multiple sprites are combined into a single large image, the Atlas, to reduce memory usage and improve performance.

However, the size of this Atlas is not fixed; it adjusts dynamically based on the number and size of its sub-images. Therefore, after a game update, the Atlas may change.

The traditional method of replacing the entire Atlas may fail after a game update, requiring the Atlas to be remade. This is why UI localizations often break.

For example, here is an Atlas named AtlasSceneDaily:

![img_1.png](../image/img_1.png)

And here is a sprite within that Atlas:

![img.png](../image/img.png)

Each sprite in an Atlas has a name. For example, this button is called `common_buttom_medit`. The game can call this name to get the button's icon.

Unfortunately, we cannot see the names of each sprite in unpacking tools, but we can find them by searching for resources of type Sprite and comparing them manually.

</details>


Search for the corresponding filename, for example, `common_buttom_medit` for `SceneDaily/ボタン文字/男エディット`. You will see a file of type Sprite, which is the button's icon.

This Sprite belongs to an Atlas named AtlasSceneDaily.

The game can directly get a small image (Sprite) from the Atlas by calling the sprite's name.

Therefore, to translate this button, you have 6 options:

1. Use the Texture Replacement module to replace the entire Atlas (old method).
2. Replace the sprite image in `SceneDaily/ボタン影像/男エディット` with the name of a blank button image, and then add the translation in the corresponding text `SceneDaily/ボタン文字/男エディット` (i18nEx method)
3. Replace the name of the sprite image specified in `SceneDaily/ボタン影像/男エディット` with a custom image name (excluding .png), and then place the replacement file with the same name in the `UI/Sprite` folder (the Sprite Replacement module can also add new images).
4. Replace the name of the sprite image specified in `SceneDaily/ボタン影像/男エディット` with the name of another existing sprite image in the game (some sprite images exist in other languages, usually in [language version]). Endings like `_en` and `_ch_s` are acceptable, but I don't recommend using them directly. I suggest using method 5 instead.
5. Replace the sprite image (JAT exclusive). Place the replacement file in the `UI/Sprite` folder.
6. Combine steps 2 and 3.

JAT supports whichever method you choose, but we recommend method 3 because it offers the highest quality.

For replacement details, please refer to the next section.


### **How to get the Term?**

Please consider using TranslationExtract.dll provided by https://github.com/Pain-Brioche/COM3D2.i18nEx and follow the instructions to extract: https://github.com/Pain-Brioche/COM3D2.i18nEx#extracting-translations-from-the-english-game

We provide a script to convert the extracted csv files to the format used by JAT `ui_csv_format_convert_English.py`:

[https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script)

> You can set the log level to `Debug` in the plugin configuration, then trigger the UI you want to translate in the game. You will see output like `LocalizationManager_GetTranslation_Prefix Term: SceneDaily/ボタン文字/男エディット` in the console or log file. This is the `Term` you need.

#### Dynamically Generated Terms

Some terms are dynamically generated, such as the terms for items in edit mode:

Item Name:
``` 
The expression is this.m_strCateName + "/" + Path.GetFileNameWithoutExtension(this.m_strMenuFileName).ToLower() + "|name"
m_strCateName is the first parameter of the category command in the menu, and m_strMenuFileName is the filename of the .menu file.
For example, the term for the item name dress789_wear_i_.menu is wear/dress789_wear_i_|name
Therefore, custom-made mods can also use terms for translation.
```

Item Description:
``` 
The expression is this.m_strCateName + "/" + Path.GetFileNameWithoutExtension(this.m_strMenuFileName).ToLower() + "|info"
m_strCateName is the category in the menu. The first parameter of the command, `m_strMenuFileName`, is the filename of the `.menu` file.
For example, the item description term for `dress789_wear_i_.menu` is `wear/dress789_wear_i_|info`.
Therefore, custom-made mods can also use `term` for translation.
```

## 2. UI Image (Sprite) Translation

JAT can also replace some image buttons or icons in the game.

### 1. File Location

UI image files should be placed in the following directory:

`COM3D2\BepInEx\JustAnotherTranslator\<Your_Language_Setting>\UI\Sprite`

### 2. File Format and Usage

-   **File Type**: Only `.png` image format is supported.
-   **Naming Convention**: The filename of the target image to be replaced (without the extension) is its "key".

**How to use**:
1.  Get the name of the image to be replaced. The method is similar to getting a `Term`. Find `UIButton_SetSprite_Postfix called with sp: some_sprite_name` in the `Debug` log. Here, `some_sprite_name` is the image name.
2.  Rename your prepared `.png` image to the obtained image name (e.g., `some_sprite_name.png`).
3.  Place the renamed image into the `UI\Sprite` directory.

The plugin automatically scans all `.png` files in this directory at startup. When the game needs to load a sprite named `some_sprite_name`, JAT will automatically replace it with your local image.

Note: If the Term for a UI text translation is of an image type, the replacement here will also be triggered.


ote: If the Term for a UI text translation is of an image type, the replacement here will also be triggered.

For performance reasons, newly added `.png` images will not take effect immediately after the plugin has loaded. You can toggle UI translation in the plugin settings to trigger a rescan (not guaranteed to work 100% correctly). If necessary, please restart the game.

### 3. How It Works

Unlike directly replacing game files, JAT uses a safer method:

When a UI image needs to be replaced, JAT dynamically creates a new, independent UI Atlas for your `.png` file and then applies this new atlas to the corresponding UI component. The advantage of this is that it does not touch the game's original atlas files at all, avoiding the problem of images becoming garbled due to layout changes in the original atlas after a game update.

However, creating a separate Atlas does increase draw calls, which can lead to a negligible performance decrease.

### **How do I obtain sprites? **

Use an unpacking tool like [AssetStudio](https://github.com/Perfare/AssetStudio) to open the `COM3D2\COM3D2x64_Data` folder.

Alternatively, you can set the log level to `Debug` in the plugin configuration, enable `EnableDumpSprite/是否启用精灵图导出` in the settings, and then trigger the UI you want to translate in-game.

# Texture Replacement

JAT can also replace some tutorial images, scene graphics, etc.

This includes Textures and Assets.

## 1. File Location

All texture replacement files should be placed in the following directory:

`COM3D2\BepInEx\JustAnotherTranslator\<Your_Language_Setting>\Texture`

The plugin will recursively scan for all `.png` files in this directory and its subdirectories.

**Note**: Filenames must be unique across all subdirectories. If there are files with the same name, the last one loaded will overwrite the previous ones, which may cause incorrect replacements.

## 2. File Format and Usage

-   **File Type**: Only `.png` format is supported.
-   **Naming Convention**: The filename of the target texture to be replaced (without the extension) is its "key". For example, to replace `item_icon.tex` in the game, you need to name your image `item_icon.png`.

**How to use**:
1.  **Get the name of the texture to be replaced**.
2.  Rename your prepared `.png` image to the obtained texture name (e.g., `some_texture.png`).
3.  Place the renamed image into the `Texture` directory.

The plugin automatically scans and caches the paths of all replacement files at startup. When the game loads a texture named `some_texture`, JAT will automatically intercept it and load your local image.

## 3. How to Get Texture Names

The easiest way is to enable the "texture dump" feature:

1.  In the plugin's configuration file, set `EnableTexturesDump` to `true`.
2.  Start the game and trigger the image you want to replace (e.g., open an interface that displays the image).
3.  The plugin will automatically save the currently loaded textures that do not have a corresponding replacement file in the `Texture` folder as `.png` files to the `COM3D2\BepInEx\JustAnotherTranslator\Dump\Texture` directory.
4.  You can find the "dumped" image in that directory. Its filename is the texture name you need. You can then replace this file with your own image.

> You can also set the log level to `Debug` in the plugin configuration, then trigger the UI you want to translate in the game. You will see output like `UIWidget_mainTexture_Getter_Postfix called: ..., mainTexture name: some_texture_name` in the console or log file. `some_texture_name` is the texture name you need.

## 4. How It Works

JAT uses Harmony patches to intercept several core texture loading functions in the game, including:
-   `ImportCM.LoadTexture` (for loading `.tex` files)
-   `UIWidget.mainTexture` (for NGUI widget textures)
-   `UITexture.mainTexture` (for NGUI raw texture widgets)
-   `UI2DSprite.mainTexture` (for NGUI 2D sprites)
-   `Image.sprite` (for uGUI sprite controls, this method is inherited from i18nEx, but it seems to be ineffective)

When the game attempts to get the texture for these components, JAT will:
1.  Check if the texture's name matches a `.png` filename you placed in the `Texture` folder.
2.  If it matches, JAT will read your `.png` file and load its data into the game's original `Texture2D` object, thus achieving the replacement.
3.  To avoid repeated replacements and infinite loops, the successfully replaced texture is renamed with a `JAT_` prefix.


# Dance Mode Lyric Subtitles

Lyric subtitles are listed separately because they require separate translation files.

As is not widely known, the official dance mode has its own lyric subtitle system, but it is only available in the Chinese and English versions. The Japanese version we usually play does not enable the game's multilingual support system at all, and thus has no subtitle system.

However, like other parts, JAT does not use any in-game resources. The dance subtitle system is implemented by JAT itself, is unrelated to the official options, and does not care what format the official subtitles use.

## 1. File Location

The dance mode lyrics translation file should be placed in the following directory:

`COM3D2\BepInEx\JustAnotherTranslator\<your language>\Lyric\<song's internal name (musicName)>\lyric.csv`

- `<song's internal name (musicName)>`: corresponds to the in-game song's internal name. See below for details.
- The file name must be `lyric.csv`.

The VR Karaoke mode lyrics translation file should be placed in the following directory:

VR karaoke mode refers to the mode where there is a tablet in front of you, and has nothing to do with whether you are actually playing with a VR device.

`COM3D2\BepInEx\JustAnotherTranslator\<your language>\Lyric\_Karaoke\<song's internal name (BgmFileName)>\lyric.csv`

- `<song's internal name (BgmFileName)>`: corresponds to the in-game song's internal name. See below for details.
- The file name must be `lyric.csv`.

## 2. How to Get the Music Name and Create Translation Files

Songs available in the English or Chinese versions will certainly have official timelines, while very few are included in the Japanese version.

Therefore, it is recommended to use SybarisArcEditor in the English or Chinese version to unpack the arc, search for the `Dance_subtitle.nei` file, which is the timeline file.

The folder containing the `Dance_subtitle.nei` file is the song's internal name.

If it doesn't exist, your only option is to create the timeline yourself.

Example from official extraction for `taiyoparadice`:

```csv
ID,開始時間,終了時間,ローカライズ用キー名
1,4,10,僕らの思いが溢れて
2,10.5,20,優しいメロディ奏でた
3,30,34,甘い紅茶に誘われた
4,35.4,42,優しさを滲ませた 陽射しのマキアート
```

We provide a script to convert the official timeline file into JAT's lyric translation file `lyric_csv_format_convert_English.py`:

[https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/tree/main/Script)

Please note that you need to convert the .nei file to a .csv file first, and then use the script for conversion.

<br>

For Dance Mode:

1. Enter dance mode in the game, select and start the song you want to create subtitles for.
2. The log will print `Mode: Dance, Current dance name (musicName)/模式：舞蹈，当前舞蹈名称（musicName）: {musicName}`.
3. The plugin will automatically create a folder named after the song's `musicName` in the `...\Lyric\` directory and generate a `lyric.csv` file with a header.
4. (When the dance information export option is enabled) the plugin will automatically create a `danceInfos.csv` file in the `...\Lyric\` directory. This file will dump the song name (musicName), song title, and other internal information for easy matching.

For VR Karaoke Mode (the one with the tablet in front of you):

1. Enter VR Karaoke mode in the game, select and start a song for which you want to create subtitles. 
2. The log will print `Mode: Karaoke, Current dance name (BgmFileName)/模式：卡拉OK，当前舞蹈名称（BgmFileName）: {bgmFileName}`.
3. The plugin will automatically create a folder named after the song's `BgmFileName` in the `...\Lyric\_Karaoke\` directory and generate a `lyric.csv` file with a header.
4. (When the dance information export option is enabled) the plugin will automatically create a file named `danceInfosKaraoke.csv` in the `...\Lyric\_Karaoke\` directory. This file will contain internal information such as the background music name (BgmFileName) and song title for easy matching.
5. Unlike dance mode, this mode does not have an official timeline file; you must create lyrics by your own. 
6. In addition, musicName is invalid in this mode, and ID is also invalid (0). Only BgmFileName is valid. In `danceInfosKaraoke.csv`, BgmFileName will overwrite musicName.


## 3. File Format

### lyric.csv

-   **File Type**: `.csv` files.
-   **File Format**: Must be a comma-separated CSV file, must be **UTF-8-BOM** encoded, must have a header, and ideally should conform to [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180).
-   **Header**: Must include the following fields:
    -   `StartTime`: The time the lyric starts to display (in seconds, supports decimals).
    -   `EndTime`: The time the lyric stops displaying (in seconds, supports decimals).
    -   `OriginalLyric`: The original lyric (can be empty).
    -   `TranslatedLyric`: The translated lyric (can be empty).

**Example `lyric.csv`**:
```csv
StartTime,EndTime,OriginalLyric,TranslatedLyric
10.5,15.2,Hello World,Hello World
16.0,20.8,This is a test lyric,This is a test lyric
```

### danceInfos.csv

- **File Type**: `.csv` file.
- **File Format**: Must be a `,`-delimited CSV file, must be in **UTF-8-BOM** encoding, must have a header, and ideally should conform to [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180).
- **Note**: The plugin reads this file to sort, deduplicate, and rewrite it. Do not modify it at will.
- **Header**: See the example. Only a few important fields are listed here. Most of the content is a direct dump of official information. For some list-type data, JAT automatically serializes it, separating it with `|`.
    -   `Id`: The song's ID.
    -   `MusicName`: The MusicName, which is the song's internal name, and JAT's folder name.
    -   `Title`: The song's title, which is the song's title displayed in-game.
    -   `TranslatedTitle`: The song's title translated using the General Text Translation module. - `CommentaryText`: The song's commentary text, typically artist information.
    -   `TranslatedCommentaryText`: The song's commentary text, translated using the general text translation module.
    -   `Mode`: The song's mode, either `Dance` or `Karaoke`.
    -   `SceneName`: The name of the dance scene (Unity Scene name).
    -   `BodyFilterMode`: The body filter mode, either `Both`, `OnlyOld`, `OnlyNew`, or `Either`. High-polygon bodies using KCES are treated as `new`.

**Example `danceInfos.csv`**:
```csv
Id,MusicName,Title,TranslatedTitle,CommentaryText,TranslatedCommentaryText,Mode,TitleFontSize,TitleOffsetY,SceneName,SelectCharaNum,SampleImageName,BgmFileName,PresetName,ScenarioProgress,Term,AppealCutinName,ReversalCutinName,DanceshowScene,DanceshowImage,MaidOrder,BgType,InitialPlayable,IsPlayable,RhythmGameCorrespond,SubtitleSheetName,IsShowSelectScene,CsvFolderName,KuchiPakuFileList,MotionFileList,MovieFileName,BinaryFolderName,SingPartList,PersonalityFilter,BodyFilterMode
100,dokidoki_fallinlove,ドキドキ ☆ Fallin' Love,DokiDoki ☆ Fallin' Love,Vocal1 . nao / Vocal2 . 美郷あき / Vocal3 . 佐咲紗花,Vocal1 . nao / Vocal2 . 美乡秋 / Vocal3 . 佐咲紗花,Dance,25,0,SceneDance_DDFL_Release,3,dance_select_image_ddfl_live,,||,6,VS外,Novice,Novice,SceneDance_DDFLT_Release,dance_select_image_ddfl_th,0|1|2,LiveStage,False,True,True,SceneDance_DDFL_Release,True,,,,,,,,Both
101,dokidoki_fallinlove,ドキドキ ☆ Fallin' Love-in劇場,DokiDoki ☆ Fallin' Love-in剧场,Vocal1 . nao / Vocal2 . 美郷あき / Vocal3 . 佐咲紗花,Vocal1 . nao / Vocal2 . 美乡秋 / Vocal3 . 佐咲紗花,Dance,25,0,SceneDance_DDFLT_Release,3,dance_select_image_ddfl_th,,||,6,VS外,Novice,Novice,SceneDance_DDFLT_Release,dance_select_image_ddfl_th,0|1|2,Theater,False,True,True,SceneDance_DDFL_Release,True,,,,,,,,Both
170,kimini_aijo_delicious,キミに愛情でりぃしゃす,爱的美味献给你,"Vocal1 . nao / Vocal2 . 中恵光城 / Vocal3 . 彩音 ","Vocal1 . nao / Vocal2 . 中恵光城 / Vocal3 . 彩音 ",Dance,25,0,SceneDance_KAD_Release,3,dance_select_image_kad_live,,||,2,レストラン,Novice,Novice,SceneDance_KADT_Release,dance_select_image_kad_th,0|1|2,LiveStage,False,True,True,SceneDance_KAD_Release,True,,,,,,,,Both
210,sakura_uraraka_harahirari,さくらうららか、はらひらり,樱花烂漫，飘落纷纷,"Vocal1 . nao ","Vocal1 . nao ",Dance,25,0,SceneDance_SUH_Release,1,dance_select_image_sakurara_live,,,0,VS外,Novice,Novice,SceneDance_SUHT_Release,dance_select_image_sakurara_th,,LiveStage,True,True,True,SceneDance_SUH_Release,True,,,,,,,,Both
260,1oy,1st only you ver.nao,1st only you ver.nao,"Vocal1 . nao ","Vocal1 . nao ",Dance,25,0,SceneDance_1OY_Release,1,dance_select_image_1oy_live,,,0,None,Novice,Novice,SceneDance_1OYT_Release,dance_select_image_1oy_th,,LiveStage,True,True,True,SceneDance_1OY_Release,True,,,,,,,,Both
```
### danceInfosBgmFileName.csv

Same as `danceInfos.csv`

## 4. How It Works

JAT uses Harmony patches to intercept the game's dance management module (`RhythmAction_Mgr`).

-   **Loading**: When you select a song and enter the dance scene, JAT gets the current song's music name and then tries to load the corresponding `...\Lyric\<Music_Name>\lyric.csv` file. This is loaded in real-time, so adding new files does not require restarting the game.
-   **Synchronization**: After the dance starts, JAT starts a monitoring program that displays the corresponding lyrics on the screen during the time period matching `StartTime` and `EndTime`, based on the dance's real-time playback time (`DanceTimer`).
-   **Display**: You can adjust the lyric display method in the plugin's configuration, for example, showing only the original, only the translation, or bilingual display.

## 5. Recommendations

-   Lyric files are only loaded when entering dance mode and have no performance impact on game startup or daily play.
-   Since it matches the timeline in real-time, ensure the accuracy of `StartTime` and `EndTime` for the best viewing experience.
-   Do not open files such as `lyric.csv` when entering the dance. For example, if you open it with Microsoft Excel, JAT will not be able to read the file.

# Other Subtitles

i18nEX enabled the game's built-in subtitle system by enabling the game's built-in multilingual system. JAT believed that enabling the multilingual system would cause many unnecessary problems, so JAT chose to implement the subtitle system on its own.

Like other parts, JAT does not use any in-game resources. The subtitle system is implemented by JAT itself, is unrelated to the official options, and does not care what format the official subtitles use.

As is not widely known, our game uses a modified Kirikiri KAG script for most of the story, i.e., .ks files.

Translation extraction software should unpack arc files, scan all .ks files, and extract the text.

The subtitle module depends on the general text translation module and will only be displayed when there is a translation. JAT will not display the original text when there is no translation.

## How It Works Overview

JAT's subtitle system uses Harmony patches to intercept various methods in the game that handle story scripts (KAG). The core manager is `SubtitleManager`, which is responsible for coordinating the display of all subtitles.

1.  **Event Interception**: The plugin hooks native game methods that handle tags like `@talk`, `@PlayVoice`, etc.
2.  **Information Capture**:
    -   For tags with explicit text (like `@talk`), the plugin captures both the voice ID (`voiceId`) and the corresponding dialogue text.
    -   For tags with only voice (like `@PlayVoice`), the plugin can only capture the `voiceId`.
3.  **Coroutine Monitoring**: `SubtitleManager` starts a monitoring coroutine for the currently speaking character. This coroutine continuously checks the playback status of the character's `AudioSource` component.
4.  **Subtitle Display**:
    -   When audio playback is detected, the coroutine looks up the corresponding translation in the general translation files based on the captured `voiceId` (usually the audio filename).
    -   After finding the translation, it calls `SubtitleComponentManager` to create and display the subtitle on the screen.
    -   When audio playback ends, it hides the subtitle.


In JAT, there are basically 4 situations where subtitles appear:

### @talk and @hitret tags
For example, in `a1_ft_00002.ks`:
```
@talk voice=H0_04530 name=[HF]
あ、あたし、その……初めて、ですから……優しくして下さい……ご主人様……？　あたしも、そのっ……が、頑張ります、からっ……
@hitret
```

If the game uses the `@talk` tag, 99% of the time there is text, and when the `@hitret` tag is activated, JAT can get this original text.

**Technical Details**: The plugin hooks the `TagTalk` and `TagHitRet` methods of `ADVKagManager` via `AdvSubtitlePatch.cs`. When the `TagTalk` tag is parsed, the plugin captures the `voiceId` from the `voice` attribute; when the script executes the `@hitret` tag, the plugin captures the currently displayed text. `SubtitleManager` then associates this `voiceId` with the text and stores it in a temporary map. When the monitoring coroutine detects that the audio for this `voiceId` is playing, it displays this translated text.

Therefore, as long as there is a corresponding translation in the general text translations, the subtitle will be displayed.

So the general text translation file should contain:
```
あ、あたし、その……初めて、ですから……優しくして下さい……ご主人様……？　あたしも、そのっ……が、頑張ります、からっ……					Ah, it's my first time, so please be gentle... master...? I'll try my best too, so...
```

### @PlayVoice tag, Case A
For example, in `a1_casino_0001.ks`:
```
*L9|
@PlayVoice maid=0 voice=H0_13123 wait
;ふふ～ん、悩んでますね～。
;@hitret
@s
```

In this case, the text is just a comment, and JAT cannot read it directly.

Unless I intercept the game's file reading calls and parse it manually, it cannot be obtained in real-time in the game, and I don't want to do that.

**Technical Details**: The plugin hooks the `TagPlayVoice` method of `BaseKagManager` via `BaseVoiceSubtitlePatch.cs`. This patch can only get the value of the `voice` attribute (`H0_13123`). It cannot read the comment lines in the `.ks` file. Therefore, the subsequent monitoring coroutine can only use `H0_13123` as the original text to look for a translation.

In this case, the translation extraction software should do something to extract this text for translation, but the original text needs to be set to the voiceID.


JAT supports using the voiceID to match translations, so the general text translation file should contain:
```
H0_13123				Hmm, you seem troubled~.
```

### @PlayVoice tag, Case B
For example, in `mc01_0001.ks`:
```
@SetMcSkip label=*スキップ用

@PlayVoice maid=1 voice=MC_t2

@Wait time=6000 skip=false
@face maid=3 name=にっこり
```

For voices without comments, consider using voice recognition tools or AI language recognition tools to extract the text.

**Technical Details**: Exactly the same as Case A. The plugin can only capture the `voiceId` (`MC_t2`). `voice` + `.ogg` = filename, so here it is `MC_t2.ogg`. You can use SybarisArcEditor to unpack the arc and find this file.

So the general text translation file should contain:
```
MC_T2				Well, well, uh, then—our first concert, let's go for it!
```

### Maid Cafe DLC

The story scripts for the Maid Cafe DLC are written in a special way, usually as a single long audio file, and then the `@wait` tag is used to synchronize the text display, rather than playing one line at a time.

**Technical Details**: Before starting the monitoring coroutine, `SubtitleManager` checks if it is currently in the Maid Cafe's special playback mode via the `MaidCafeManagerHelper.IsStreamingPart()` method. If so, JAT will proactively disable its own subtitle system to avoid conflicts with the native subtitle system designed for this mode, which cannot be reliably intercepted by JAT.

Therefore, JAT cannot handle this situation. Please use the official subtitles.

However, the official subtitles still use the `@talk` tag, so it is the same as the `@talk and @hitret tags` case, and the official subtitles can be translated by JAT.

For example, in `stream001_adv_0002.ks`:
```
*L0|
@talk
『エンパイアメイドカフェ』とは？
@hitret
```


# DUMP

JAT supports exporting untranslated text and terms, along with unreplaced textures and sprites, to facilitate subsequent translation. Enabling these features can reduce performance, so it's recommended to only enable them when needed.

## Output Location

- Text: `BepInEx/JustAnotherTranslator/Dump/Text/`
  - `yyyy-MM-dd-HH-mm_untranslate.txt` (year-month-day-hour-minute.txt at game startup)
  - `yyyy-MM-dd-HH-mm_untranslate_normalized.txt` (Text with \r, \n, \t, and whitespace characters removed and converted to uppercase can be used directly to create translation files)
- Textures: `BepInEx/JustAnotherTranslator/Dump/Texture/*.png`
- Sprites: `BepInEx/JustAnotherTranslator/Dump/Sprite/*.png`
- UI 文本（Term）：`BepInEx/JustAnotherTranslator/Dump/UI/Text/yyyy-MM-dd-HH-mm_untranslate_term.csv`

## Switches and Dependencies (Configuration Section [9Dump])

- `EnableDumpText/是否启用文本转储`
  - Only collects "untranslated" original text; writes each line to `yyyy-MM-dd-HH-mm_untranslate.txt` at game startup
  - Requires `General Text Translation` to be enabled
- `TextDumpThreshold/文本转储阈值 (default 20)`
  - Write to disk only when the number of untranslated term cache entries reaches the threshold or when switching scenes. Setting it to 1 indicates real-time writing.
- `FlushTextDumpNow/立即写出文本`
  - Write out the cache immediately when the option status changes.
- `EnableDumpTexture/是否启用纹理转储`
  - Exports the original texture as a PNG only if a texture with "no corresponding replacement file" is found.
  - Requires the `Texture Replacement` feature to be enabled.
- `EnableDumpSprite/是否启用精灵图转储`
  - When texture dumping is enabled, the underlying textures used by sprites are exported. If you use the `Texture Replacement` feature to replace the atlas that a sprite belongs to, the sprites from the replaced atlas will also be exported.
- `EnableDumpDanceInfo/是否启用舞蹈信息转储`
  - See the `Dance Mode Lyrics and Subtitles` section.
- `EnableDumpTerm/是否启用Term转储`
  - Only collect "untranslated" terms; write them line by line to `yyyy-MM-dd-HH-mm_untranslate_term.csv` at game startup
  - Requires enabling the `Enable UI Text Translation` function
- `TermDumpThreshold/Term转储阈值 (default 20)`
  - Write to disk only when the number of untranslated term cache entries reaches the threshold or when switching scenes. Setting it to 1 indicates real-time writing.
- `FlushTermDumpNow/立即写出Term`
  - Write the cache immediately when the state of options in the game changes.

## Details

- Text
  - Text that does not match the original translation will be cached; it will be written to the file when the threshold is reached, when the game changes, or when the game exits normally.
  - Cached text that does not meet the threshold may be lost in case of an abnormal exit.
  - Simple deduplication is performed; duplicate lines are generally not exported.
  - The file is "append-only."
- Textures
  - When the game accesses a texture that supports replacement, the unreplaced texture will be exported.
  - If a file with the same name already exists, it will not be overwritten.
  - Note: JAT supports replacing .tex textures, such as those used for clothing, but these textures will not be dumped.
- Sprites
  - Only NGUI sprites can be replaced.
  - When the game accesses a sprite that supports replacement, the unreplaced sprite will be exported.
  - If a file with the same name already exists, it will not be overwritten.
  - If you use the texture replacement feature to replace the atlas to which a sprite belongs, the sprites from the replaced atlas will also be exported.
- UI Text (Terms)
  - Terms without a matching translation are added to the cache; they are written to a file when a threshold is reached, a scene is switched, or the game exits normally.
  - Cached text that has not reached the threshold may be lost upon abnormal exit.
  - Simple deduplication has been performed; duplicate lines are generally not exported.
  - The file is appended.
  - Not all terms need to be translated.

## Debugging and Performance Tips

- Switching to Debug in the `Log Level` setting can help troubleshoot issues.
- Dumping increases disk IO and memory usage: Enable only when needed; too low a threshold will cause frequent disk writes and degrade game performance, while too high a threshold will cause more cache loss during crashes.
- Dumped textures and sprites can be used directly with the replacement feature; do not modify their size.
- To avoid dumping duplicate content, JAT records all dumped content. This content is not cleaned up during game runtime, and its memory usage depends on the number of untranslated texts.

## FAQ

- Will files be overwritten?
  - Text: Always append
  - Texture: Not overwrite if the target file already exists
  - Sprite: Not overwrite if the target file already exists
  - UI Text: Always append



# Fix Patch

## Maid Cafe Line Break Fix

This fixes a bug where the line breaks in the Maid Cafe DLC sometimes stop scrolling after translation.

Due to inconsistent replacement timing, sometimes the line breaks only occur after the official text length check, causing errors.

Function is the same as [maid_cafe_line_break_fix.cs](https://github.com/90135/COM3D2_Scripts_901), and JAT is automatically compatible.

## UI Font Replacement

Don't ask me why it's in the patch category, because it's the most suitable place here.

JAT supports replacing the fonts for uGUI, NGUI, and JAT's own subtitles. Switching the `PrintOSFont/打印系统字体` option in the dump settings within the game will print installed and usable fonts to the console and logs.

The game defaults to `NotoSansCJKjp-DemiLight`. Although it's a CJK font, the glyphs are Japanese, which users in other regions may find unfamiliar. You can replace it with another font.

For example, you can download [NotoSansCJKsc-DemiLight.otf](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-DemiLight.otf) and set the font to `Noto Sans CJK SC`, which will give you Simplified Chinese fonts.

You can use the fonts shown in the logs. Unity 5.6 generally only supports .otf and .ttf formats, **and you must right-click the font and select "Install for all users"**.