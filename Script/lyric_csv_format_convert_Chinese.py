#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 将歌词时间轴CSV格式转换为 JAT 的歌词翻译文件（按歌曲ID分目录）
#           Convert lyric timeline CSV to JAT lyric files (one folder per song ID)
# Author: claude 4 & 90135
# Creation date: 2025-07-19
# Version: 2026-11-18_01
# License: Bsd-3
#
# 自 JAT v0.3.0 起，舞蹈歌词目录按 DanceData.ID 建立（Lyric\<ID>\lyric.csv），
# 不再按 musicName 建立。LBWtranslation 的 csv_rhythm_action\<歌曲内部名称>\ 无法
# 直接得到 ID，因此本脚本需要一个“名称 -> ID”的映射表：
#   - JAT 运行后自动生成的 danceInfos.csv / danceInfosKaraoke.csv
#   - 或由 dance_setting.nei 转换得到的 dance_setting.csv
# 用 --mapping 指定，可重复指定多个文件，脚本会自动识别列名。

import csv
import sys
from pathlib import Path

# 可能的 ID 列名（小写比较）
ID_COLUMNS = ('id', 'karaokemusicid', 'danceid')

# 可能的“歌词目录名/歌曲内部名称”列名（小写比较）
# 注意：不包含标题类字段，标题不是目录名
NAME_COLUMNS = (
    'csvfoldername',        # JAT danceInfos.csv / dance_setting.nei（英文字段名）
    'musicname',            # JAT danceInfos.csv，等于运行时实际使用的 csv_rhythm_action 目录名
    'karaokeoggfilename',   # JAT danceInfosKaraoke.csv
    'bgmfilename',          # JAT danceInfos.csv
    'dancedatabgmfilename', # JAT danceInfosKaraoke.csv
    'playbgmfile',          # dance_setting.nei 的 PlayBGMFile 列
    'playedoggfiles',       # JAT 运行时记录，多个值用 | 分隔
    'lyricfoldername',      # JAT 实际使用的目录名（便于重复运行）
    '譜面とかのcsvフォルダ名',  # dance_setting.nei 的原始日文列名
)

# 这些列的值可能包含多个条目
MULTI_VALUE_COLUMNS = {'playedoggfiles'}

# 用于生成映射键时要去掉的扩展名
STRIP_EXTENSIONS = ('.ogg', '.wav', '.mp3', '.csv', '.nei', '.txt')


def normalize_key(value):
    """把歌曲内部名称规范化为映射键（小写、去路径、去扩展名）"""
    if value is None:
        return ''
    text = str(value).strip()
    if not text:
        return ''
    text = text.replace('\\', '/')
    # 只保留最后一段
    text = text.rstrip('/')
    if '/' in text:
        text = text.rsplit('/', 1)[-1]
    lowered = text.lower()
    for ext in STRIP_EXTENSIONS:
        if lowered.endswith(ext):
            text = text[: -len(ext)]
            break
    return text.strip().lower()


def detect_csv_format(input_file):
    """
    检测CSV文件的格式类型

    Args:
        input_file (str): 输入CSV文件路径

    Returns:
        str: 格式类型 ('format1', 'format2', 'unknown')
    """
    try:
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.reader(infile)
            first_row = next(reader, None)

            if not first_row:
                return 'unknown'

            # 检查是否为格式2 (包含ID和日文列名)
            if (len(first_row) >= 4 and
                ('ID' in first_row[0] or first_row[0].strip() == 'ID') and
                ('開始時間' in first_row[1] or '終了時間' in first_row[2] or 'ローカライズ用キー名' in first_row[3])):
                return 'format2'

            # 检查是否为格式1 (原始格式)
            if (len(first_row) >= 3 and
                (first_row[0].strip() in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                 first_row[1].strip() in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                 first_row[2].strip() in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric'])):
                return 'format1'

            # 如果第一行不是表头，尝试检测数据格式
            # 格式2通常第一列是数字ID
            try:
                int(first_row[0])
                if len(first_row) >= 4:
                    return 'format2'
            except ValueError:
                pass

            # 格式1通常直接以时间开始
            try:
                float(first_row[0])
                float(first_row[1])
                if len(first_row) >= 3:
                    return 'format1'
            except ValueError:
                pass

    except Exception as e:
        print(f"检测文件格式时发生错误: {e}")

    return 'unknown'


def read_lyric_csv(input_file):
    """
    读取歌词时间轴CSV，返回记录列表

    支持多种输入格式：
    - 格式1: 开始时间(秒),结束时间(秒),歌词
    - 格式2: ID,開始時間,終了時間,ローカライズ用キー名

    返回: [{'StartTime':.., 'EndTime':.., 'OriginalLyric':.., 'TranslatedLyric':..}, ...]
    """
    file_format = detect_csv_format(input_file)
    records = []

    with open(input_file, 'r', encoding='utf-8-sig') as infile:
        reader = csv.reader(infile)

        for row in reader:
            if not row or len(row) < 3:
                continue

            if file_format == 'format2':
                if len(row) < 4:
                    continue

                id_field = row[0].strip()
                start_time = row[1].strip()
                end_time = row[2].strip()
                original_lyric = row[3].strip()

                # 跳过表头行
                if (id_field in ['ID', 'id'] or
                    start_time in ['開始時間', 'StartTime', 'Start Time'] or
                    end_time in ['終了時間', 'EndTime', 'End Time'] or
                    original_lyric in ['ローカライズ用キー名', 'OriginalLyric', 'Lyric']):
                    continue

                # 验证ID是否为数字（可选验证）
                try:
                    int(id_field)
                except ValueError:
                    continue

            else:
                start_time = row[0].strip()
                end_time = row[1].strip()
                original_lyric = row[2].strip() if len(row) > 2 else ""

                # 跳过表头行
                if (start_time in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                    end_time in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                    original_lyric in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric']):
                    continue

            # 共同验证：检查时间格式
            try:
                float(start_time)
                float(end_time)
            except ValueError:
                continue

            # 跳过空歌词
            if not original_lyric:
                continue

            records.append({
                'StartTime': start_time,
                'EndTime': end_time,
                'OriginalLyric': original_lyric,
                'TranslatedLyric': '',  # 翻译字段留空
            })

    return file_format, records


def write_lyric_csv(output_file, records):
    """写出 JAT 歌词文件（UTF-8-BOM，带表头）"""
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
        writer = csv.DictWriter(
            outfile,
            fieldnames=['StartTime', 'EndTime', 'OriginalLyric', 'TranslatedLyric'])
        writer.writeheader()
        writer.writerows(records)


def load_mapping(mapping_files):
    """
    从映射表中读取 “歌曲内部名称 -> 歌曲ID”

    Args:
        mapping_files (list[str]): 映射表CSV路径列表

    Returns:
        dict[str, list[str]]: 规范化后的名称 -> 歌曲ID列表
    """
    mapping = {}

    for mapping_file in mapping_files:
        path = Path(mapping_file)
        if not path.is_file():
            print(f"  警告：映射表 {mapping_file} 不存在，已跳过")
            continue

        try:
            with open(path, 'r', encoding='utf-8-sig', newline='') as f:
                reader = csv.DictReader(f)
                if not reader.fieldnames:
                    print(f"  警告：映射表 {mapping_file} 没有表头，已跳过")
                    continue

                # 建立 “小写列名 -> 原始列名” 的索引
                header_index = {}
                for name in reader.fieldnames:
                    if name is None:
                        continue
                    header_index.setdefault(name.strip().lower(), name)

                id_column = None
                for candidate in ID_COLUMNS:
                    if candidate in header_index:
                        id_column = header_index[candidate]
                        break

                if id_column is None:
                    print(f"  警告：映射表 {mapping_file} 中找不到 ID 列"
                          f"（可用列：{', '.join(ID_COLUMNS)}），已跳过")
                    continue

                name_columns = []
                for candidate in NAME_COLUMNS:
                    if candidate in header_index:
                        name_columns.append(header_index[candidate])

                if not name_columns:
                    print(f"  警告：映射表 {mapping_file} 中找不到名称列，已跳过")
                    continue

                added = 0
                for row in reader:
                    song_id = (row.get(id_column) or '').strip()
                    if not song_id:
                        continue

                    for column in name_columns:
                        raw_value = (row.get(column) or '').strip()
                        if not raw_value:
                            continue

                        if column.strip().lower() in MULTI_VALUE_COLUMNS:
                            values = [v for v in raw_value.split('|') if v.strip()]
                        else:
                            values = [raw_value]

                        for value in values:
                            key = normalize_key(value)
                            if not key:
                                continue
                            ids = mapping.setdefault(key, [])
                            if song_id not in ids:
                                ids.append(song_id)
                                added += 1

                print(f"  已载入映射表 {path.name}："
                      f"{len(name_columns)} 个名称列，新增 {added} 条映射")
        except Exception as e:
            print(f"  警告：读取映射表 {mapping_file} 失败：{e}")

    return mapping


def resolve_song_ids(dir_key, mapping):
    """
    根据输入文件所在的相对目录解析歌曲ID

    先尝试完整相对路径，再尝试第一层目录名，最后尝试最后一段目录名。
    """
    parts = [part for part in dir_key.replace('\\', '/').split('/') if part]

    candidates = []
    if parts:
        candidates.append('/'.join(parts))
        if len(parts) > 1:
            candidates.append(parts[0])
        candidates.append(parts[-1])

    for candidate in candidates:
        key = normalize_key(candidate)
        if key and key in mapping:
            return mapping[key]

    return None


def process_folder(input_folder, output_folder, mapping, karaoke=False, legacy=False,
                   recursive=True):
    """
    批量处理文件夹中的所有CSV文件（包括子文件夹）

    Args:
        input_folder (str): 输入文件夹路径
        output_folder (str): 输出文件夹路径（可选，默认为输入文件夹）
        mapping (dict): 名称 -> 歌曲ID 列表
        karaoke (bool): 输出到 Lyric/_Karaoke/<ID>/lyric.csv
        legacy (bool): 使用 JAT v0.3.0 之前的按目录名输出的布局
        recursive (bool): 是否递归处理子文件夹（默认为True）
    """
    input_path = Path(input_folder)

    if not input_path.exists():
        print(f"错误：输入文件夹 {input_folder} 不存在")
        return
    if not input_path.is_dir():
        print(f"错误：{input_folder} 不是一个文件夹")
        return

    output_path = Path(output_folder) if output_folder else input_path
    output_path.mkdir(parents=True, exist_ok=True)

    if recursive:
        csv_files = sorted(input_path.rglob("*.csv"))
        print(f"递归搜索文件夹 {input_folder} 及其子文件夹...")
    else:
        csv_files = sorted(input_path.glob("*.csv"))
        print(f"搜索文件夹 {input_folder}...")

    if not csv_files:
        search_type = "及其子文件夹" if recursive else ""
        print(f"在文件夹 {input_folder} {search_type}中没有找到CSV文件")
        return

    print(f"找到 {len(csv_files)} 个CSV文件，开始处理...")
    print("-" * 50)

    # 目标目录 -> 歌词记录列表
    targets = {}
    # 无法映射的目录 -> 歌词记录列表
    unmatched = {}
    # 目录 -> 解析到的歌曲ID，用于打印摘要
    resolved_summary = []

    for csv_file in csv_files:
        relative_path = csv_file.relative_to(input_path)
        relative_dir = relative_path.parent
        # 输入文件夹本身就是某首歌的目录时，用输入文件夹名作为键
        dir_key = relative_dir.as_posix() if relative_dir.parts else input_path.name

        try:
            file_format, records = read_lyric_csv(csv_file)
        except Exception as e:
            print(f"处理文件 {relative_path} 时发生错误: {e}")
            continue

        print(f"处理文件: {relative_path}")
        print(f"  检测到文件格式: {file_format}")

        if not records:
            print("  ⚠ 文件中没有有效数据")
            print()
            continue

        if legacy:
            target_dirs = [relative_dir.as_posix() if relative_dir.parts else '']
        else:
            song_ids = resolve_song_ids(dir_key, mapping)
            if song_ids:
                target_dirs = list(song_ids)
                resolved_summary.append((dir_key, song_ids))
            else:
                target_dirs = []

        if not target_dirs:
            bucket = unmatched.setdefault(dir_key or '.', [])
            bucket.extend(records)
            print(f"  ✗ 无法把 {dir_key or '.'} 映射到歌曲ID")
            print("    请用 --mapping 指定 danceInfos.csv 或 dance_setting.csv")
            print()
            continue

        print(f"  ✓ 读取 {len(records)} 条记录")
        for folder in target_dirs:
            if legacy:
                print(f"  → {folder}")
            else:
                print(f"  → 歌曲ID {folder}")
            bucket = targets.setdefault(folder, [])
            bucket.extend(records)
        print()

    print("-" * 50)

    # 写出结果
    total_written = 0
    written_folders = 0
    unmatched_records = 0

    for folder, records in sorted(targets.items()):
        # 去重（同一首歌可能被多个源文件贡献）
        seen = set()
        unique_records = []
        for record in records:
            marker = (record['StartTime'], record['EndTime'],
                      record['OriginalLyric'], record['TranslatedLyric'])
            if marker in seen:
                continue
            seen.add(marker)
            unique_records.append(record)

        if karaoke:
            output_file = output_path / "_Karaoke" / folder / "lyric.csv"
        else:
            output_file = output_path / folder / "lyric.csv"

        write_lyric_csv(output_file, unique_records)
        total_written += len(unique_records)
        written_folders += 1

    for folder, records in sorted(unmatched.items()):
        output_file = output_path / "_Unmapped" / folder / "lyric.csv"
        write_lyric_csv(output_file, records)
        unmatched_records += len(records)

    print("批量处理完成！")
    print(f"成功写出歌词目录: {written_folders} 个")
    print(f"总共写出记录: {total_written} 条")
    if unmatched_records:
        print(f"未能映射的记录: {unmatched_records} 条")

    if legacy:
        print()
        print("注意：你使用了 --legacy（按目录名输出），这只适用于 JAT v0.3.0 之前的版本。")
        print("      JAT v0.3.0 及之后请去掉 --legacy 并使用 --mapping 指定映射表。")

    if unmatched:
        print()
        print(f"有 {len(unmatched)} 个目录无法映射到歌曲ID，已写入 {output_path / '_Unmapped'}：")
        for folder in sorted(unmatched):
            print(f"  - {folder}")
        print("这些目录不会被 JAT 读取。请先进入对应歌曲让 JAT 生成 danceInfos.csv，")
        print("或用 dance_setting.csv 作为映射表，然后重新运行本脚本。")

    if resolved_summary:
        print()
        print("目录 -> 歌曲ID 对照：")
        for folder, song_ids in resolved_summary:
            print(f"  {folder} -> {', '.join(song_ids)}")


def convert_single_file(input_file, output_file=None):
    """
    转换单个文件

    Args:
        input_file (str): 输入文件路径
        output_file (str): 输出文件路径（可选）
    """
    input_path = Path(input_file)

    if not input_path.exists():
        print(f"错误：输入文件 {input_file} 不存在")
        return

    if output_file is None:
        output_file = input_path.parent / f"{input_path.stem}_converted.csv"

    print(f"转换文件: {input_file}")
    print(f"输出文件: {output_file}")

    try:
        file_format, records = read_lyric_csv(input_path)
    except Exception as e:
        print(f"处理文件 {input_file} 时发生错误: {e}")
        return

    print(f"检测到文件格式: {file_format}")

    if not records:
        print("⚠ 文件中没有有效数据")
        return

    write_lyric_csv(Path(output_file), records)
    print(f"✓ 成功转换 {len(records)} 条记录")


def print_usage():
    print("歌词CSV格式转换工具")
    print("将歌词时间轴CSV格式转换为 JAT 的歌词翻译文件")
    print()
    print("支持的输入格式:")
    print("  格式1: 开始时间(秒),结束时间(秒),歌词")
    print("  格式2: ID,開始時間,終了時間,ローカライズ用キー名")
    print()
    print("输出格式: StartTime,EndTime,OriginalLyric,TranslatedLyric")
    print()
    print("JAT v0.3.0 起，舞蹈歌词目录按歌曲ID建立：")
    print("  COM3D2/BepInEx/JustAnotherTranslator/<语言>/Lyric/<歌曲ID>/lyric.csv")
    print("因此批量转换时必须用 --mapping 指定映射表：")
    print("  - JAT 自动生成的 danceInfos.csv / danceInfosKaraoke.csv")
    print("  - 或 dance_setting.nei 转换得到的 dance_setting.csv")
    print()
    print("使用方法:")
    print("  转换单个文件:")
    print("    python script.py <输入文件> [输出文件]")
    print("  批量转换文件夹:")
    print("    python script.py <输入文件夹> [输出文件夹] --mapping <映射表> [选项]")
    print()
    print("示例:")
    print("  python script.py song.csv")
    print("  python script.py song.csv converted_song.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv --mapping ./dance_setting.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfosKaraoke.csv --karaoke")
    print("  python script.py ./csv_rhythm_action ./Lyric --legacy   (仅 JAT v0.3.0 之前)")
    print()
    print("参数说明:")
    print("  输入文件/文件夹: 要转换的CSV文件或包含CSV文件的文件夹路径")
    print("  输出文件夹: 输出文件夹路径（可选，默认为输入文件夹）")
    print("  --mapping <文件>: 名称到歌曲ID的映射表，可重复指定")
    print("  --karaoke: 输出到 <输出文件夹>/_Karaoke/<歌曲ID>/lyric.csv")
    print("  --legacy: 按源目录名输出，仅适用于 JAT v0.3.0 之前")
    print("  --no-recursive: (可选标志) 如果使用，将不会递归处理子文件夹")
    print()


def main():
    """主函数，处理命令行参数"""

    recursive = True
    karaoke = False
    legacy = False
    mapping_files = []

    args = sys.argv[1:]
    positional = []

    index = 0
    while index < len(args):
        arg = args[index]
        if arg == "--no-recursive":
            recursive = False
        elif arg == "--karaoke":
            karaoke = True
        elif arg == "--legacy":
            legacy = True
        elif arg == "--mapping":
            if index + 1 >= len(args):
                print("错误：--mapping 后面需要跟映射表路径")
                return
            mapping_files.append(args[index + 1])
            index += 1
        elif arg in ("-h", "--help"):
            print_usage()
            return
        else:
            positional.append(arg)
        index += 1

    if not positional:
        print_usage()
        return

    input_path = Path(positional[0])

    if input_path.is_file():
        output_file = positional[1] if len(positional) > 1 else None
        convert_single_file(positional[0], output_file)
        return

    if not input_path.is_dir():
        print(f"错误：路径 {positional[0]} 不存在")
        return

    output_folder = positional[1] if len(positional) > 1 else None

    mapping = {}
    if legacy:
        print("注意：已启用 --legacy（按目录名输出），仅适用于 JAT v0.3.0 之前。")
    else:
        if not mapping_files:
            print("错误：JAT v0.3.0 起歌词目录按歌曲ID建立，批量转换需要映射表。")
            print("      请用 --mapping 指定 danceInfos.csv 或 dance_setting.csv，")
            print("      例如：python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv")
            print("      如果你仍在使用 JAT v0.3.0 之前的版本，请加 --legacy。")
            return
        print("载入映射表...")
        mapping = load_mapping(mapping_files)
        if not mapping:
            print("错误：映射表中没有读到任何映射，已停止。")
            return
        print(f"映射表载入完成，共 {len(mapping)} 个名称。")
        print()

    if not recursive:
        print("注意：已设置不递归处理子文件夹。")

    process_folder(positional[0], output_folder, mapping, karaoke, legacy, recursive)


if __name__ == "__main__":
    main()
