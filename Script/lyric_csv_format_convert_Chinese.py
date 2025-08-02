#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 将歌词时间轴CSV格式转换为包含翻译字段的格式 Convert lyric timeline CSV format to format with translation fields
# Author: claude 4 & 90135
# Creation date: 2025-07-19
# Version: 2025-08-02_01
# License: Bsd-3

import csv
import os
import sys
from pathlib import Path

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

def convert_lyric_csv(input_file, output_file):
    """
    将歌词时间轴CSV格式转换为包含翻译字段的格式
    支持多种输入格式：
    - 格式1: 开始时间(秒),结束时间(秒),歌词
    - 格式2: ID,開始時間,終了時間,ローカライズ用キー名

    输出格式: StartTime,EndTime,OriginalLyric,TranslatedLyric

    Args:
        input_file (str): 输入CSV文件路径
        output_file (str): 输出CSV文件路径
    """

    # 检测文件格式
    file_format = detect_csv_format(input_file)
    print(f"  检测到文件格式: {file_format}")

    try:
        # 使用utf-8-sig编码处理BOM
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.reader(infile)

            # 准备输出数据
            output_data = []

            for row_num, row in enumerate(reader, 1):
                # 跳过空行
                if not row or len(row) < 3:
                    continue

                # 根据格式处理数据
                if file_format == 'format2':
                    # 格式2: ID,開始時間,終了時間,ローカライズ用キー名
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
                        # ID不是数字，可能是表头或无效行，跳过
                        continue

                else:
                    # 格式1: 开始时间(秒),结束时间(秒),歌词
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
                    # 尝试将开始时间和结束时间转换为浮点数
                    float(start_time)
                    float(end_time)
                except ValueError:
                    # 如果不能转换为数字，跳过这一行
                    continue

                # 跳过空歌词
                if not original_lyric:
                    continue

                # 创建新的行数据（翻译字段留空）
                new_row = {
                    'StartTime': start_time,
                    'EndTime': end_time,
                    'OriginalLyric': original_lyric,
                    'TranslatedLyric': ''  # 翻译字段留空
                }

                output_data.append(new_row)

        # 写入输出文件
        with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
            if output_data:
                writer = csv.DictWriter(outfile, fieldnames=['StartTime', 'EndTime', 'OriginalLyric', 'TranslatedLyric'])
                writer.writeheader()
                writer.writerows(output_data)
                return len(output_data)
            else:
                return 0

    except Exception as e:
        print(f"处理文件 {input_file} 时发生错误: {e}")
        return -1

def process_folder(input_folder, output_folder=None, output_suffix="", recursive=True):
    """
    批量处理文件夹中的所有CSV文件（包括子文件夹）

    Args:
        input_folder (str): 输入文件夹路径
        output_folder (str): 输出文件夹路径（可选，默认为输入文件夹）
        output_suffix (str): 输出文件名后缀（默认为空）
        recursive (bool): 是否递归处理子文件夹（默认为True）
    """

    input_path = Path(input_folder)

    # 检查输入文件夹是否存在
    if not input_path.exists():
        print(f"错误：输入文件夹 {input_folder} 不存在")
        return

    if not input_path.is_dir():
        print(f"错误：{input_folder} 不是一个文件夹")
        return

    # 设置输出文件夹
    if output_folder is None:
        output_path = input_path
    else:
        output_path = Path(output_folder)
        # 如果输出文件夹不存在，创建它
        output_path.mkdir(parents=True, exist_ok=True)

    # 查找所有CSV文件（递归或非递归）
    if recursive:
        csv_files = list(input_path.rglob("*.csv"))  # 递归查找所有子文件夹
        print(f"递归搜索文件夹 {input_folder} 及其子文件夹...")
    else:
        csv_files = list(input_path.glob("*.csv"))   # 只查找当前文件夹
        print(f"搜索文件夹 {input_folder}...")

    if not csv_files:
        search_type = "及其子文件夹" if recursive else ""
        print(f"在文件夹 {input_folder} {search_type}中没有找到CSV文件")
        return

    print(f"找到 {len(csv_files)} 个CSV文件，开始处理...")
    print("-" * 50)

    total_processed = 0
    successful_files = 0

    for csv_file in csv_files:
        # 计算相对路径，用于保持文件夹结构
        relative_path = csv_file.relative_to(input_path)

        # 生成输出文件路径，保持原有的文件夹结构
        if output_folder is None:
            # 在原位置生成文件
            output_filename = "lyric" + output_suffix + ".csv"
            output_file = csv_file.parent / output_filename
        else:
            # 在指定输出文件夹中保持相同的文件夹结构
            output_filename = "lyric" + output_suffix + ".csv"
            output_file = output_path / relative_path.parent / output_filename

            # 确保输出文件夹存在
            output_file.parent.mkdir(parents=True, exist_ok=True)

        # 显示相对路径，便于理解文件位置
        print(f"处理文件: {relative_path}")

        # 转换文件
        record_count = convert_lyric_csv(csv_file, output_file)

        if record_count > 0:
            print(f"  ✓ 成功转换 {record_count} 条记录")
            print(f"  ✓ 输出文件: {output_file.relative_to(Path.cwd()) if output_file.is_relative_to(Path.cwd()) else output_file}")
            total_processed += record_count
            successful_files += 1
        elif record_count == 0:
            print(f"  ⚠ 文件中没有有效数据")
        else:
            print(f"  ✗ 处理失败")

        print()

    print("-" * 50)
    print(f"批量处理完成！")
    print(f"成功处理文件: {successful_files}/{len(csv_files)}")
    print(f"总共转换记录: {total_processed} 条")

def convert_single_file(input_file, output_file=None):
    """
    转换单个文件

    Args:
        input_file (str): 输入文件路径
        output_file (str): 输出文件路径（可选）
    """

    input_path = Path(input_file)

    # 检查输入文件是否存在
    if not input_path.exists():
        print(f"错误：输入文件 {input_file} 不存在")
        return

    # 如果未指定输出文件，则在同目录生成
    if output_file is None:
        output_file = input_path.parent / f"{input_path.stem}_converted.csv"

    print(f"转换文件: {input_file}")
    print(f"输出文件: {output_file}")

    # 转换文件
    record_count = convert_lyric_csv(input_file, output_file)

    if record_count > 0:
        print(f"✓ 成功转换 {record_count} 条记录")
    elif record_count == 0:
        print(f"⚠ 文件中没有有效数据")
    else:
        print(f"✗ 处理失败")

def main():
    """主函数，处理命令行参数"""

    # 检查是否有--no-recursive标志
    recursive = True
    args = sys.argv.copy()
    if "--no-recursive" in args:
        recursive = False
        args.remove("--no-recursive")

    if len(args) < 2:
        print("歌词CSV格式转换工具")
        print("将歌词时间轴CSV格式转换为包含翻译字段的格式")
        print()
        print("支持的输入格式:")
        print("  格式1: 开始时间(秒),结束时间(秒),歌词")
        print("  格式2: ID,開始時間,終了時間,ローカライズ用キー名")
        print()
        print("使用方法:")
        print("  转换单个文件:")
        print("    python script.py <输入文件> [输出文件]")
        print("  批量转换文件夹:")
        print("    python script.py <输入文件夹> [输出文件夹] [输出后缀] [--no-recursive]")
        print()
        print("示例:")
        print("  python script.py song.csv")
        print("  python script.py song.csv converted_song.csv")
        print("  python script.py ./lyrics_folder")
        print("  python script.py ./lyrics_folder ./output_folder")
        print("  python script.py ./lyrics_folder ./output_folder _new")
        print("  python script.py ./lyrics_folder --no-recursive")
        print()
        print("参数说明:")
        print("  输入文件/文件夹: 要转换的CSV文件或包含CSV文件的文件夹路径")
        print("  输出文件夹: 输出文件夹路径（可选，默认为输入文件夹）")
        print("  输出后缀: 输出文件名后缀（可选，默认为空）")
        print("  --no-recursive: (可选标志) 如果使用，将不会递归处理子文件夹")
        print()
        print("输出格式: StartTime,EndTime,OriginalLyric,TranslatedLyric")
        print()
        return
    
    input_path = Path(args[1])
    
    # 判断是文件还是文件夹
    if input_path.is_file():
        # 单文件处理
        output_file = args[2] if len(args) > 2 else None
        convert_single_file(args[1], output_file)
    elif input_path.is_dir():
        # 文件夹批量处理
        output_folder = args[2] if len(args) > 2 else None
        output_suffix = args[3] if len(args) > 3 else ""
        
        if not recursive:
            print("注意：已设置不递归处理子文件夹。")
        
        process_folder(args[1], output_folder, output_suffix, recursive)
    else:
        print(f"错误：路径 {args[1]} 不存在")

if __name__ == "__main__":
    # 使用命令行参数
    if len(sys.argv) > 1:
        main()
    else:
        print("歌词CSV格式转换工具")
        print("将歌词时间轴CSV格式转换为包含翻译字段的格式")
        print()
        print("使用方法:")
        print("  转换单个文件:")
        print("    python script.py <输入文件> [输出文件]")
        print("  批量转换文件夹:")
        print("    python script.py <输入文件夹> [输出文件夹] [输出后缀] [--no-recursive]")
        print()
        print("示例:")
        print("  python script.py song.csv")
        print("  python script.py song.csv converted_song.csv")
        print("  python script.py ./lyrics_folder")
        print("  python script.py ./lyrics_folder ./output_folder")
        print("  python script.py ./lyrics_folder ./output_folder _new")
        print("  python script.py ./lyrics_folder --no-recursive")
        print()
        print("参数说明:")
        print("  输入文件/文件夹: 要转换的CSV文件或包含CSV文件的文件夹路径")
        print("  输出文件夹: 输出文件夹路径（可选，默认为输入文件夹）")
        print("  输出后缀: 输出文件名后缀（可选，默认为空）")
        print("  --no-recursive: (可选标志) 如果使用，将不会递归处理子文件夹")
        print()
        print("支持的输入格式:")
        print("  格式1: 开始时间(秒),结束时间(秒),歌词")
        print("  格式2: ID,開始時間,終了時間,ローカライズ用キー名")
        print()
        print("输出格式: StartTime,EndTime,OriginalLyric,TranslatedLyric")
        print()
