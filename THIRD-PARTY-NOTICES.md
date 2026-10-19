# Third-Party Notices

The source code in this repository is licensed under the
[BSD 3-Clause License](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/LICENSE).
The plugin archive also ships the third-party components listed below. They are redistributed
unmodified and remain under their own licenses.

## Bundled components

| Component | Version | Shipped in | License | Copyright |
| --- | --- | --- | --- | --- |
| `CsvHelper` | 2.16.3 / 33.1.0 | both, version differs | MS-PL or Apache-2.0 | Copyright (c) Josh Close |
| `Microsoft.Bcl.AsyncInterfaces` | 8.0.0 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `Microsoft.Bcl.HashCode` | 1.1.1 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Buffers` | 4.4.0 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Memory` | 4.5.0 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Numerics.Vectors` | 4.4.0 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Runtime.CompilerServices.Unsafe` | 4.5.3 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Threading.Tasks.Extensions` | 4.5.4 | COM3D2.5 only | MIT | Copyright (c) .NET Foundation and Contributors |
| `I18N.dll`, `I18N.CJK.dll`, `I18N.MidEast.dll`, `I18N.Other.dll`, `I18N.Rare.dll`, `I18N.West.dll` | 2.0.0.0 | both | MIT (Mono) | Copyright (c) Mono project contributors |

The `System.*` and `Microsoft.Bcl.*` assemblies are transitive dependencies of CsvHelper and come from
the corresponding NuGet packages. CsvHelper itself is dual-licensed: this distribution relies on the
MS-PL option, and the Apache-2.0 alternative is published in
[CsvHelper's LICENSE.txt](https://github.com/JoshClose/CsvHelper/blob/master/LICENSE.txt).

## MIT License

Applies to the Microsoft .NET packages above and to Mono's I18N assemblies.
Mono's LICENSE states: "In general, the runtime and its class libraries are licensed under the terms
of the MIT license". The per-file copyright notices remain in the Mono sources.

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

Project: https://github.com/mono/mono & https://github.com/Unity-Technologies/mono

## Microsoft Public License (MS-PL)

Applies to CsvHelper.

```
Microsoft Public License (MS-PL)

This license governs use of the accompanying software. If you use the software, you accept this
license. If you do not accept the license, do not use the software.

   1. Definitions
      The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same
      meaning here as under U.S. copyright law. A "contribution" is the original software, or any
      additions or changes to the software. A "contributor" is any person that distributes its
      contribution under this license. "Licensed patents" are a contributor's patent claims that
      read directly on its contribution.
   2. Grant of Rights
      (A) Copyright Grant- Subject to the terms of this license, including the license conditions
      and limitations in section 3, each contributor grants you a non-exclusive, worldwide,
      royalty-free copyright license to reproduce its contribution, prepare derivative works of its
      contribution, and distribute its contribution or any derivative works that you create.
      (B) Patent Grant- Subject to the terms of this license, including the license conditions and
      limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free
      license under its licensed patents to make, have made, use, sell, offer for sale, import,
      and/or otherwise dispose of its contribution in the software or derivative works of the
      contribution in the software.
   3. Conditions and Limitations
      (A) No Trademark License- This license does not grant you rights to use any contributors'
      name, logo, or trademarks.
      (B) If you bring a patent claim against any contributor over patents that you claim are
      infringed by the software, your patent license from such contributor to the software ends
      automatically.
      (C) If you distribute any portion of the software, you must retain all copyright, patent,
      trademark, and attribution notices that are present in the software.
      (D) If you distribute any portion of the software in source code form, you may do so only
      under this license by including a complete copy of this license with your distribution. If you
      distribute any portion of the software in compiled or object code form, you may only do so
      under a license that complies with this license.
      (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no
      express warranties, guarantees, or conditions. You may have additional consumer rights under
      your local laws which this license cannot change. To the extent permitted under your local
      laws, the contributors exclude the implied warranties of merchantability, fitness for a
      particular purpose and non-infringement.
```

## Mono I18N assemblies

### Origin

| Directory | Source |
| --- | --- |
| `.github/release-assets/i18n/COM3D2/` | Unity 5.6.7f1: `Editor/Data/Mono/lib/mono/unity/` |
| `.github/release-assets/i18n/COM3D25/` | Unity 2022.3.62f1: `Editor/Data/MonoBleedingEdge/lib/mono/unity/` |

All twelve files are taken unmodified from the Mono `unity` profile class library bundled with the
corresponding Unity Editor, and every file matches its source by MD5. The assembly identity is
`PublicKeyToken=0738eb9f132ed756`, Mono's signing key.

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

本仓库的源码按 [BSD 3-Clause 许可](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin/blob/main/LICENSE) 授权。
插件包同时附带下列第三方组件，它们均按原样分发，各自沿用自身的许可。

## 随包分发的组件

| 组件 | 版本 | 出现在 | 许可 | 版权 |
| --- | --- | --- | --- | --- |
| `CsvHelper` | 2.16.3 / 33.1.0 | 两者，版本不同 | MS-PL 或 Apache-2.0 | Copyright (c) Josh Close |
| `Microsoft.Bcl.AsyncInterfaces` | 8.0.0 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `Microsoft.Bcl.HashCode` | 1.1.1 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Buffers` | 4.4.0 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Memory` | 4.5.0 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Numerics.Vectors` | 4.4.0 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Runtime.CompilerServices.Unsafe` | 4.5.3 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `System.Threading.Tasks.Extensions` | 4.5.4 | 仅 COM3D2.5 | MIT | Copyright (c) .NET Foundation and Contributors |
| `I18N.dll`、`I18N.CJK.dll`、`I18N.MidEast.dll`、`I18N.Other.dll`、`I18N.Rare.dll`、`I18N.West.dll` | 2.0.0.0 | 两者 | MIT（Mono） | Copyright (c) Mono project contributors |

`System.*` 与 `Microsoft.Bcl.*` 这些程序集是 CsvHelper 的传递依赖，来自对应的 NuGet 包。
CsvHelper 本身是双许可，本分发采用其中的 MS-PL，Apache-2.0 备选文本见
[CsvHelper 的 LICENSE.txt](https://github.com/JoshClose/CsvHelper/blob/master/LICENSE.txt)。

## 许可

- 上表中的 Microsoft .NET 组件与 Mono 的 I18N 程序集：**MIT**。
- CsvHelper：**MS-PL**（或 Apache-2.0）。

两种许可的全文见上文英文部分。Mono 的 LICENSE 原文为
“In general, the runtime and its class libraries are licensed under the terms of the MIT license”，
各源文件的版权声明保留在 Mono 仓库对应文件中。
项目地址：https://github.com/mono/mono & https://github.com/Unity-Technologies/mono

## Mono 的 I18N 程序集

### 来源

| 目录 | 来源 |
| --- | --- |
| `.github/release-assets/i18n/COM3D2/` | Unity 5.6.7f1：`Editor/Data/Mono/lib/mono/unity/` |
| `.github/release-assets/i18n/COM3D25/` | Unity 2022.3.62f1：`Editor/Data/MonoBleedingEdge/lib/mono/unity/` |

十二个文件均取自对应 Unity 编辑器自带的 Mono `unity` profile 类库，未作任何修改，与源文件 MD5 完全一致。
程序集标识 `PublicKeyToken=0738eb9f132ed756`，即 Mono 的签名密钥。

### 为什么在这里

部分翻译包是在 Windows 上打包的，用的是旧式（非 UTF-8）代码页：压缩包内条目的文件名按系统 ANSI
代码页存储，且不带 UTF-8 标志位。JAT 不另外携带解压库，只以 `IncludeAssets="compile"` 在编译期引用
SharpZipLib，运行时复用游戏已经加载的 `ICSharpCode.SharpZipLib.dll`。该库会通过
`ZipConstants.DefaultCodePage` 解码这类文件名，而它最终调用 `Encoding.GetEncoding` 解析代码页。
游戏的 Mono 运行时没有附带实现这些代码页的 I18N 程序集，读取这类压缩包就会失败。
因此在这里附带这些程序集；JAT 自身读取翻译文件始终使用 UTF-8。
