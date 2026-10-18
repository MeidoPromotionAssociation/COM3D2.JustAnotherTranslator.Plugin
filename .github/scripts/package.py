#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 组装单个目标（COM3D2 / COM3D25）的发布 zip / Assemble the release zip for one target
# License: BSD-3
"""组装发布 zip。

解压到游戏根目录后，期望的布局与插件运行时读取的路径一致：

    BepInEx/plugins/<PluginDir>/...         插件 DLL + 依赖 DLL + README
    BepInEx/JustAnotherTranslator/Document-文档/...
    BepInEx/JustAnotherTranslator/JAT-README.md / .txt
    JAT-README.md / .txt

歌词数据（BepInEx/JustAnotherTranslator/<语言>/Lyric）**故意不打包**，
它以单独的资产形式发布，避免把歌词放进仓库。

The zip mirrors the layout the plugin expects after extracting into the game root.
Lyric data is intentionally NOT part of this archive; it ships as its own asset.
"""

import argparse
import os
import sys
import zipfile

# 目标定义 / Target definitions
TARGETS = {
    "COM3D2": {
        "plugin_dir": "COM3D2.JustAnotherTranslator",
        "plugin_dll": "COM3D2.JustAnotherTranslator.Plugin.dll",
        "build_dir": os.path.join("COM3D2", "net35"),
        "zip_name": "COM3D2.JustAnotherTranslator.Plugin",
    },
    "COM3D25": {
        "plugin_dir": "COM3D25.JustAnotherTranslator",
        "plugin_dll": "COM3D25.JustAnotherTranslator.Plugin.dll",
        "build_dir": os.path.join("COM3D25", "net48"),
        "zip_name": "COM3D25.JustAnotherTranslator.Plugin",
    },
}

# 固定时间戳，保证同样的输入产出同样的 zip / Fixed timestamp so identical inputs give identical zips
FIXED_DATE_TIME = (2020, 1, 1, 0, 0, 0)

DOC_FOLDER_NAME = "Document-文档"
TRANSLATION_ROOT = "BepInEx/JustAnotherTranslator"


def add_dir(zf, name):
    info = zipfile.ZipInfo(name.rstrip("/") + "/", date_time=FIXED_DATE_TIME)
    info.external_attr = (0o40755 << 16) | 0x10
    info.compress_type = zipfile.ZIP_STORED
    zf.writestr(info, b"")


def add_file(zf, src, arcname):
    info = zipfile.ZipInfo(arcname, date_time=FIXED_DATE_TIME)
    info.external_attr = 0o100644 << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    with open(src, "rb") as f:
        zf.writestr(info, f.read(), compresslevel=9)


def add_tree(zf, src_dir, arc_prefix, skip=()):
    """递归加入目录，按名称排序以保证输出稳定 / Recursively add a directory, sorted for stable output."""
    added = 0
    for root, dirs, files in os.walk(src_dir):
        dirs.sort()
        rel_root = os.path.relpath(root, src_dir).replace(os.sep, "/")
        rel_root = "" if rel_root == "." else rel_root + "/"
        add_dir(zf, arc_prefix + "/" + rel_root)
        for name in sorted(files):
            if name in skip:
                continue
            add_file(zf, os.path.join(root, name), arc_prefix + "/" + rel_root + name)
            added += 1
    return added


def main():
    parser = argparse.ArgumentParser(description="Assemble a release zip")
    parser.add_argument("--target", required=True, choices=sorted(TARGETS))
    parser.add_argument("--version", required=True, help="例如 0.5.7 / e.g. 0.5.7")
    parser.add_argument("--build-root", required=True,
                        help="dotnet build 的输出根目录（bin/Release）/ dotnet build output root")
    parser.add_argument("--repo-root", default=".", help="仓库根目录 / repository root")
    parser.add_argument("--assets-dir", default=os.path.join(".github", "release-assets"),
                        help="发布辅助文件目录 / release helper assets directory")
    parser.add_argument("--output-dir", default="dist", help="zip 输出目录 / output directory")
    args = parser.parse_args()

    target = TARGETS[args.target]
    repo_root = os.path.abspath(args.repo_root)
    assets_dir = os.path.join(repo_root, args.assets_dir)
    build_dir = os.path.join(os.path.abspath(args.build_root), target["build_dir"])
    plugin_dir = target["plugin_dir"]

    if not os.path.isdir(build_dir):
        sys.exit(f"::error::build output not found: {build_dir}")

    dlls = sorted(n for n in os.listdir(build_dir) if n.lower().endswith(".dll"))
    if not dlls:
        sys.exit(f"::error::no dll found in {build_dir}")
    expected_plugin_dll = target["plugin_dll"]
    if expected_plugin_dll not in dlls:
        sys.exit(f"::error::{expected_plugin_dll} not found in {build_dir}, got {dlls}")

    i18n_dir = os.path.join(assets_dir, "i18n", args.target)
    if not os.path.isdir(i18n_dir):
        sys.exit(f"::error::I18N assemblies not found: {i18n_dir}")

    readme_txt = os.path.join(assets_dir, "JAT-README.txt")
    readme_md = os.path.join(repo_root, "README.md")
    # 包里带了 Mono 的 I18N 程序集，MIT 要求许可声明随副本分发
    # The archive carries Mono's I18N assemblies, and MIT requires the notice to travel with copies
    notices = os.path.join(repo_root, "THIRD-PARTY-NOTICES.md")
    for path in (readme_txt, readme_md, notices):
        if not os.path.isfile(path):
            sys.exit(f"::error::missing file: {path}")

    docs_src = os.path.join(repo_root, "Document")
    if not os.path.isdir(docs_src):
        sys.exit(f"::error::missing docs directory: {docs_src}")

    os.makedirs(os.path.join(repo_root, args.output_dir), exist_ok=True)
    zip_path = os.path.join(
        repo_root, args.output_dir, f"{target['zip_name']}-{args.version}.zip")

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        # 目录条目 / directory entries
        add_dir(zf, "BepInEx")
        add_dir(zf, "BepInEx/plugins")
        add_dir(zf, f"BepInEx/plugins/{plugin_dir}")
        add_dir(zf, TRANSLATION_ROOT)
        # Document-文档 的目录条目由 add_tree 自己补 / the Document-文档 entry comes from add_tree

        # 插件本体与依赖 / plugin and its dependencies
        for name in dlls:
            add_file(zf, os.path.join(build_dir, name),
                     f"BepInEx/plugins/{plugin_dir}/{name}")

        # 游戏 Mono 缺少的 .NET I18N 程序集 / .NET I18N assemblies missing from the game's Mono
        for name in sorted(os.listdir(i18n_dir)):
            if name.lower().endswith(".dll"):
                add_file(zf, os.path.join(i18n_dir, name),
                         f"BepInEx/plugins/{plugin_dir}/{name}")

        # README：md 直接取仓库根目录，保证随文档更新 / README: taken from the repo root so it stays current
        add_file(zf, readme_md, f"BepInEx/plugins/{plugin_dir}/JAT-README.md")
        add_file(zf, readme_txt, f"BepInEx/plugins/{plugin_dir}/JAT-README.txt")

        # 第三方声明跟插件 DLL 放在一起，包里带了谁的 DLL 就写清谁的许可
        # Third-party notices sit next to the plugin dlls, so whatever dll is in the archive is covered
        add_file(zf, notices, f"BepInEx/plugins/{plugin_dir}/THIRD-PARTY-NOTICES.md")

        # 本地文档 / local documentation
        add_tree(zf, docs_src, f"{TRANSLATION_ROOT}/{DOC_FOLDER_NAME}")

        add_file(zf, readme_md, f"{TRANSLATION_ROOT}/JAT-README.md")
        add_file(zf, readme_txt, f"{TRANSLATION_ROOT}/JAT-README.txt")

        # 顶层再放一份，方便解压前阅读 / one more copy at the archive root
        add_file(zf, readme_md, "JAT-README.md")
        add_file(zf, readme_txt, "JAT-README.txt")

    # 自检：不允许出现 pdb，也不允许出现歌词目录
    # Sanity check: no pdb, no lyric folder
    with zipfile.ZipFile(zip_path) as zf:
        names = zf.namelist()
    bad = [n for n in names if n.lower().endswith(".pdb")]
    lyric = [n for n in names if "/Lyric/" in n]
    if bad or lyric:
        sys.exit(f"::error::unexpected entries in {zip_path}: {bad + lyric}")

    size = os.path.getsize(zip_path)
    print(f"built {zip_path} ({len(names)} entries, {size} bytes)")
    print("  plugin dlls: " + ", ".join(dlls))


if __name__ == "__main__":
    main()
