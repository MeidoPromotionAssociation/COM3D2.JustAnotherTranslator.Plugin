#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 将从官方提取的 csv 文件格式或 I18nEx 的 csv 文件格式转换为 JustAnotherTranslator 使用的格式 Convert the csv file format extracted from the official website or I18nEx to the format used by JustAnotherTranslator
# Author: claude 4 & 90135
# Creation date: 2025-07-04
# Version: 2025-07-19_01
# License: Bsd-3

import csv
import os
import sys
from pathlib import Path

def convert_single_csv(input_file, output_file, add_prefix=True):
    """
    将单个多语言CSV格式转换为简化的术语对照表格式
    
    输入格式: Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)
    或: Key,Type,Desc,Japanese,English
    输出格式: Term,Original,Translation
    
    Args:
        input_file (str): 输入CSV文件路径
        output_file (str): 输出CSV文件路径
        add_prefix (bool): 是否在Term字段前添加文件名前缀
    """
    
    try:
        # 获取文件名（不含扩展名）用作前缀
        file_prefix = Path(input_file).stem
        
        # 使用utf-8-sig编码处理BOM
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.DictReader(infile)
            
            # 准备输出数据
            output_data = []
            
            for row_num, row in enumerate(reader, 1):
                # 提取关键信息，处理可能的BOM字符
                key_field = 'Key'
                if key_field not in row:
                    # 查找包含'Key'的列名（可能有BOM前缀）
                    for col_name in row.keys():
                        if col_name.endswith('Key'):
                            key_field = col_name
                            break
                
                key = row.get(key_field, '')
                japanese = row.get('Japanese', '')
                english = row.get('English', '')
                
                # 跳过空行或无效数据
                if not key:
                    continue
                
                # 根据 add_prefix 参数决定Term的格式
                term_value = f"{file_prefix}/{key}" if add_prefix else key
                
                # 创建新的行数据
                new_row = {
                    'Term': term_value,
                    'Original': japanese,
                    'Translation': english
                }
                
                output_data.append(new_row)
    
        # 写入输出文件
        with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
            if output_data:
                writer = csv.DictWriter(outfile, fieldnames=['Term', 'Original', 'Translation'])
                writer.writeheader()
                writer.writerows(output_data)
                return len(output_data)
            else:
                return 0
                
    except Exception as e:
        print(f"处理文件 {input_file} 时发生错误: {e}")
        return -1

def process_folder(input_folder, output_folder=None, output_suffix="", add_prefix=True, recursive=True):
    """
    批量处理文件夹中的所有CSV文件（包括子文件夹）
    
    Args:
        input_folder (str): 输入文件夹路径
        output_folder (str): 输出文件夹路径（可选，默认为输入文件夹）
        output_suffix (str): 输出文件名后缀（默认为空）
        add_prefix (bool): 是否在Term字段前添加文件名前缀
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
    if not add_prefix:
        print("注意：已设置不在Term前添加文件名前缀。")
    print("-" * 50)
    
    total_processed = 0
    successful_files = 0
    
    for csv_file in csv_files:
        # 计算相对路径，用于保持文件夹结构
        relative_path = csv_file.relative_to(input_path)
        
        # 生成输出文件路径，保持原有的文件夹结构
        if output_folder is None:
            # 在原位置生成文件
            output_filename = csv_file.stem + output_suffix + ".csv"
            output_file = csv_file.parent / output_filename
        else:
            # 在指定输出文件夹中保持相同的文件夹结构
            output_filename = csv_file.stem + output_suffix + ".csv"
            output_file = output_path / relative_path.parent / output_filename
            
            # 确保输出文件夹存在
            output_file.parent.mkdir(parents=True, exist_ok=True)
        
        # 显示相对路径，便于理解文件位置
        print(f"处理文件: {relative_path}")
        
        # 转换文件，传入add_prefix参数
        record_count = convert_single_csv(csv_file, output_file, add_prefix=add_prefix)
        
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

def convert_single_file(input_file, output_file=None, add_prefix=True):
    """
    转换单个文件
    
    Args:
        input_file (str): 输入文件路径
        output_file (str): 输出文件路径（可选）
        add_prefix (bool): 是否在Term字段前添加文件名前缀
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
    if not add_prefix:
        print("注意：已设置不在Term前添加文件名前缀。")
    
    # 转换文件
    record_count = convert_single_csv(input_file, output_file, add_prefix=add_prefix)
    
    if record_count > 0:
        print(f"✓ 成功转换 {record_count} 条记录")
    elif record_count == 0:
        print(f"⚠ 文件中没有有效数据")
    else:
        print(f"✗ 处理失败")

def main():
    """主函数，处理命令行参数"""
    
    # 检查是否有--no-prefix和--no-recursive标志
    add_prefix = True
    recursive = True
    args = sys.argv.copy()
    
    if "--no-prefix" in args:
        add_prefix = False
        args.remove("--no-prefix")
    
    if "--no-recursive" in args:
        recursive = False
        args.remove("--no-recursive")

    if len(args) < 2:
        print("多语言CSV格式转换工具")
        print("将多语言CSV格式转换为术语对照表格式")
        print()
        print("使用方法:")
        print("  转换单个文件:")
        print("    python script.py <输入文件> [输出文件] [--no-prefix]")
        print("  批量转换文件夹:")
        print("    python script.py <输入文件夹> [输出文件夹] [输出后缀] [--no-prefix] [--no-recursive]")
        print()
        print("示例:")
        print("  python script.py terms.csv")
        print("  python script.py terms.csv converted_terms.csv")
        print("  python script.py ./input_folder")
        print("  python script.py ./input_folder ./output_folder")
        print("  python script.py ./input_folder ./output_folder _new")
        print("  python script.py ./input_folder --no-prefix")
        print("  python script.py ./input_folder --no-recursive")
        print("  python script.py ./input_folder --no-prefix --no-recursive")
        print()
        print("参数说明:")
        print("  输入文件/文件夹: 要转换的CSV文件或包含CSV文件的文件夹路径")
        print("  输出文件夹: 输出文件夹路径（可选，默认为输入文件夹）")
        print("  输出后缀: 输出文件名后缀（可选，默认为空）")
        print("  --no-prefix: (可选标志) 如果使用，将不会在Term前添加文件名前缀")
        print("  --no-recursive: (可选标志) 如果使用，将不会递归处理子文件夹")
        print()
        print("输入格式: Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)")
        print("输出格式: Term,Original,Translation")
        print()
        return
    
    input_path = Path(args[1])
    
    # 判断是文件还是文件夹
    if input_path.is_file():
        # 单文件处理
        output_file = args[2] if len(args) > 2 else None
        convert_single_file(args[1], output_file, add_prefix=add_prefix)
    elif input_path.is_dir():
        # 文件夹批量处理
        output_folder = args[2] if len(args) > 2 else None
        output_suffix = args[3] if len(args) > 3 else ""
        
        if not recursive:
            print("注意：已设置不递归处理子文件夹。")
        
        process_folder(args[1], output_folder, output_suffix, add_prefix=add_prefix, recursive=recursive)
    else:
        print(f"错误：路径 {args[1]} 不存在")

# 如果直接运行脚本，也可以在这里设置默认文件夹
if __name__ == "__main__":
    # 方式一：使用命令行参数
    if len(sys.argv) > 1:
        main()
    else:
        # 方式二：使用默认文件夹（可根据需要修改）
        input_folder = "./csv_files"          # 修改为您的文件夹路径
        output_folder = "./converted_files"   # 可选：指定输出文件夹
        add_prefix_default = True             # 修改为 False 则不添加前缀
        
        print(f"使用默认设置:")
        print(f"输入文件夹: {input_folder}")
        print(f"输出文件夹: {output_folder}")
        print(f"在Term前添加文件名前缀: {add_prefix_default}")
        print()
        
        process_folder(input_folder, output_folder, add_prefix=add_prefix_default)