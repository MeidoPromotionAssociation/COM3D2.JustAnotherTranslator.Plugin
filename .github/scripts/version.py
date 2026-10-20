#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 读取插件版本号 / Print the plugin version
# License: BSD-3
"""打印插件版本号。

唯一来源是插件主类里的 `public const string PluginVersion` 常量：
- BepInEx 元数据、游戏里显示的版本取它；
- 程序集版本由 csproj 用同样的正则从该常量推导（GenerateAssemblyInfo=true + <Version>）。

所以发版时只需要改这一处，再打一个同名 tag。本脚本把它和 tag 比对，
避免漏改版本号却照样发出包。

The single source is the PluginVersion constant in the plugin class: BepInEx metadata
and the in-game version read it, and the csproj derives the assembly version from the
same constant. A release compares it against the tag so a forgotten bump fails the
build instead of shipping a wrong version.
"""

import os
import re
import sys

DEFAULT_SOURCE = os.path.join(
    "COM3D2.JustAnotherTranslator.Plugin", "COM3D2.JustAnotherTranslator.Plugin.cs")

# 与 csproj 里 <Version> 用的正则保持一致 / Keep in sync with the <Version> regex in the csproj
PATTERN = re.compile(
    r'public\s+const\s+string\s+PluginVersion\s*=\s*"([0-9][0-9.]*)"')


def main():
    source = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_SOURCE
    if not os.path.isfile(source):
        sys.exit(f"::error::source not found: {source}")

    with open(source, encoding="utf-8-sig") as f:
        match = PATTERN.search(f.read())

    if not match:
        sys.exit(f"::error::PluginVersion constant not found in {source}")

    print(match.group(1))


if __name__ == "__main__":
    main()
