# Third-Party Notices

The source code in this repository is licensed under the [LICENSE](LICENSE) (BSD 3-Clause).
The files listed below are not part of this project and remain under their own licenses.

## Mono I18N assemblies

### Files

```
.github/release-assets/i18n/COM3D2/I18N.dll
.github/release-assets/i18n/COM3D2/I18N.CJK.dll
.github/release-assets/i18n/COM3D2/I18N.MidEast.dll
.github/release-assets/i18n/COM3D2/I18N.Other.dll
.github/release-assets/i18n/COM3D2/I18N.Rare.dll
.github/release-assets/i18n/COM3D2/I18N.West.dll
.github/release-assets/i18n/COM3D25/I18N.dll
.github/release-assets/i18n/COM3D25/I18N.CJK.dll
.github/release-assets/i18n/COM3D25/I18N.MidEast.dll
.github/release-assets/i18n/COM3D25/I18N.Other.dll
.github/release-assets/i18n/COM3D25/I18N.Rare.dll
.github/release-assets/i18n/COM3D25/I18N.West.dll
```

### Origin

| Directory | Source |
| --- | --- |
| `.github/release-assets/i18n/COM3D2/` | Unity 5.6.7f1: `Editor/Data/Mono/lib/mono/unity/` |
| `.github/release-assets/i18n/COM3D25/` | Unity 2022.3.62f1: `Editor/Data/MonoBleedingEdge/lib/mono/unity/` |

All twelve files are taken unmodified from the Mono `unity` profile class library bundled with the
corresponding Unity Editor, and every file matches its source by MD5. The assembly identity is
`PublicKeyToken=0738eb9f132ed756`, Mono's signing key.

### License

MIT. Mono's LICENSE states: "In general, the runtime and its class libraries are licensed under the
terms of the MIT license". The per-file copyright notices remain in the Mono sources.

```
MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

Project: https://github.com/mono/mono

### Why these files are here

Some translation packs are archived on Windows with a legacy, non-UTF-8 code page: the entry names
inside the zip are stored in the system ANSI code page and the archive carries no UTF-8 flag. JAT
does not ship its own zip library — it references SharpZipLib at compile time only
(`IncludeAssets="compile"`) and reuses the `ICSharpCode.SharpZipLib.dll` that the game already loads.
That reader decodes such names through `ZipConstants.DefaultCodePage`, which resolves the code page
via `Encoding.GetEncoding`. The game's Mono runtime does not ship the I18N assemblies that implement
those code pages, so reading such an archive fails. These assemblies are bundled so that those packs
can be read; JAT itself always reads translation files as UTF-8.

# 第三方声明

本仓库的源码按根目录 [LICENSE](LICENSE)（BSD 3-Clause）授权。
下列文件不是本项目的作品，按各自的许可授权。

## Mono 的 I18N 程序集

### 文件

```
.github/release-assets/i18n/COM3D2/I18N.dll
.github/release-assets/i18n/COM3D2/I18N.CJK.dll
.github/release-assets/i18n/COM3D2/I18N.MidEast.dll
.github/release-assets/i18n/COM3D2/I18N.Other.dll
.github/release-assets/i18n/COM3D2/I18N.Rare.dll
.github/release-assets/i18n/COM3D2/I18N.West.dll
.github/release-assets/i18n/COM3D25/I18N.dll
.github/release-assets/i18n/COM3D25/I18N.CJK.dll
.github/release-assets/i18n/COM3D25/I18N.MidEast.dll
.github/release-assets/i18n/COM3D25/I18N.Other.dll
.github/release-assets/i18n/COM3D25/I18N.Rare.dll
.github/release-assets/i18n/COM3D25/I18N.West.dll
```

### 来源

| 目录 | 来源 |
| --- | --- |
| `.github/release-assets/i18n/COM3D2/` | Unity 5.6.7f1：`Editor/Data/Mono/lib/mono/unity/` |
| `.github/release-assets/i18n/COM3D25/` | Unity 2022.3.62f1：`Editor/Data/MonoBleedingEdge/lib/mono/unity/` |

十二个文件均取自对应 Unity 编辑器自带的 Mono `unity` profile 类库，未作任何修改，与源文件 MD5 完全一致。
程序集标识 `PublicKeyToken=0738eb9f132ed756`，即 Mono 的签名密钥。

### 许可

MIT。Mono 的 LICENSE 原文为 “In general, the runtime and its class libraries are licensed under the
terms of the MIT license”，各源文件的版权声明保留在 Mono 仓库对应文件中。

```
MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

项目地址：https://github.com/mono/mono

### 为什么在这里

部分翻译包是在 Windows 上打包的，用的是旧式（非 UTF-8）代码页：压缩包内条目的文件名按系统 ANSI
代码页存储，且不带 UTF-8 标志位。JAT 不另外携带解压库，只以 `IncludeAssets="compile"` 在编译期引用
SharpZipLib，运行时复用游戏已经加载的 `ICSharpCode.SharpZipLib.dll`。该库会通过
`ZipConstants.DefaultCodePage` 解码这类文件名，而它最终调用 `Encoding.GetEncoding` 解析代码页。
游戏的 Mono 运行时没有附带实现这些代码页的 I18N 程序集，读取这类压缩包就会失败。
因此在这里附带这些程序集；JAT 自身读取翻译文件始终使用 UTF-8。
