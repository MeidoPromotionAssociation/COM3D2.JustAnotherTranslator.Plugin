#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 读取插件版本号 / Print the plugin version
# License: BSD-3
"""打印 [BepInPlugin] 特性里的版本字符串。

游戏里显示的插件版本取的就是这个值，发版时用它和 tag 比对，避免漏改版本号。
Prints the version string from the [BepInPlugin] attribute, which is what the game shows.
"""

import os
import re
import sys

DEFAULT_SOURCE = os.path.join(
    "COM3D2.JustAnotherTranslator.Plugin", "COM3D2.JustAnotherTranslator.Plugin.cs")

PATTERN = re.compile(
    r'\[BepInPlugin\(\s*"[^"]*"\s*,\s*"[^"]*"\s*,\s*"([^"]+)"\s*\)\]')


def main():
    source = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_SOURCE
    if not os.path.isfile(source):
        sys.exit(f"::error::source not found: {source}")

    with open(source, encoding="utf-8-sig") as f:
        match = PATTERN.search(f.read())

    if not match:
        sys.exit(f"::error::BepInPlugin attribute not found in {source}")

    print(match.group(1))


if __name__ == "__main__":
    main()
