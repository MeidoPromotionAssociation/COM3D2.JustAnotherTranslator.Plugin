#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: Convert lyric timeline CSV format to format with translation fields
# Author: claude 4 & 90135
# Creation date: 2025-07-19
# Version: 2025-08-02_01
# License: BSD-3

import csv
import os
import sys
from pathlib import Path

def detect_csv_format(input_file):
    """
    Detect the format type of the CSV file

    Args:
        input_file (str): Input CSV file path

    Returns:
        str: Format type ('format1', 'format2', 'unknown')
    """
    try:
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.reader(infile)
            first_row = next(reader, None)

            if not first_row:
                return 'unknown'

            # Check if it's format2 (contains ID and Japanese column names)
            if (len(first_row) >= 4 and
                ('ID' in first_row[0] or first_row[0].strip() == 'ID') and
                ('開始時間' in first_row[1] or '終了時間' in first_row[2] or 'ローカライズ用キー名' in first_row[3])):
                return 'format2'

            # Check if it's format1 (original format)
            if (len(first_row) >= 3 and
                (first_row[0].strip() in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                 first_row[1].strip() in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                 first_row[2].strip() in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric'])):
                return 'format1'

            # If the first row is not a header, try to detect data format
            # Format2 usually has numeric ID in the first column
            try:
                int(first_row[0])
                if len(first_row) >= 4:
                    return 'format2'
            except ValueError:
                pass

            # Format1 usually starts directly with time
            try:
                float(first_row[0])
                float(first_row[1])
                if len(first_row) >= 3:
                    return 'format1'
            except ValueError:
                pass

    except Exception as e:
        print(f"Error occurred while detecting file format: {e}")

    return 'unknown'

def convert_lyric_csv(input_file, output_file):
    """
    Convert lyric timeline CSV format to format with translation fields
    Supports multiple input formats:
    - Format1: start_time(seconds),end_time(seconds),lyric
    - Format2: ID,開始時間,終了時間,ローカライズ用キー名

    Output format: StartTime,EndTime,OriginalLyric,TranslatedLyric

    Args:
        input_file (str): Input CSV file path
        output_file (str): Output CSV file path
    """

    # Detect file format
    file_format = detect_csv_format(input_file)
    print(f"  Detected file format: {file_format}")

    try:
        # Use utf-8-sig encoding to handle BOM
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.reader(infile)

            # Prepare output data
            output_data = []

            for row_num, row in enumerate(reader, 1):
                # Skip empty rows
                if not row or len(row) < 3:
                    continue

                # Process data according to format
                if file_format == 'format2':
                    # Format2: ID,開始時間,終了時間,ローカライズ用キー名
                    if len(row) < 4:
                        continue

                    id_field = row[0].strip()
                    start_time = row[1].strip()
                    end_time = row[2].strip()
                    original_lyric = row[3].strip()

                    # Skip header row
                    if (id_field in ['ID', 'id'] or
                        start_time in ['開始時間', 'StartTime', 'Start Time'] or
                        end_time in ['終了時間', 'EndTime', 'End Time'] or
                        original_lyric in ['ローカライズ用キー名', 'OriginalLyric', 'Lyric']):
                        continue

                    # Validate if ID is numeric (optional validation)
                    try:
                        int(id_field)
                    except ValueError:
                        # ID is not numeric, might be header or invalid row, skip
                        continue

                else:
                    # Format1: start_time(seconds),end_time(seconds),lyric
                    start_time = row[0].strip()
                    end_time = row[1].strip()
                    original_lyric = row[2].strip() if len(row) > 2 else ""

                    # Skip header row
                    if (start_time in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                        end_time in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                        original_lyric in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric']):
                        continue

                # Common validation: check time format
                try:
                    # Try to convert start time and end time to float
                    float(start_time)
                    float(end_time)
                except ValueError:
                    # If cannot convert to number, skip this row
                    continue

                # Skip empty lyrics
                if not original_lyric:
                    continue

                # Create new row data (translation field left empty)
                new_row = {
                    'StartTime': start_time,
                    'EndTime': end_time,
                    'OriginalLyric': original_lyric,
                    'TranslatedLyric': ''  # Translation field left empty
                }

                output_data.append(new_row)

        # Write to output file
        with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
            if output_data:
                writer = csv.DictWriter(outfile, fieldnames=['StartTime', 'EndTime', 'OriginalLyric', 'TranslatedLyric'])
                writer.writeheader()
                writer.writerows(output_data)
                return len(output_data)
            else:
                return 0

    except Exception as e:
        print(f"Error occurred while processing file {input_file}: {e}")
        return -1

def process_folder(input_folder, output_folder=None, output_suffix="", recursive=True):
    """
    Batch process all CSV files in a folder (including subfolders)

    Args:
        input_folder (str): Input folder path
        output_folder (str): Output folder path (optional, defaults to input folder)
        output_suffix (str): Output filename suffix (defaults to empty)
        recursive (bool): Whether to recursively process subfolders (defaults to True)
    """

    input_path = Path(input_folder)

    # Check if input folder exists
    if not input_path.exists():
        print(f"Error: Input folder {input_folder} does not exist")
        return

    if not input_path.is_dir():
        print(f"Error: {input_folder} is not a folder")
        return

    # Set output folder
    if output_folder is None:
        output_path = input_path
    else:
        output_path = Path(output_folder)
        # If output folder doesn't exist, create it
        output_path.mkdir(parents=True, exist_ok=True)

    # Find all CSV files (recursive or non-recursive)
    if recursive:
        csv_files = list(input_path.rglob("*.csv"))  # Recursively find all subfolders
        print(f"Recursively searching folder {input_folder} and its subfolders...")
    else:
        csv_files = list(input_path.glob("*.csv"))   # Only search current folder
        print(f"Searching folder {input_folder}...")

    if not csv_files:
        search_type = "and its subfolders" if recursive else ""
        print(f"No CSV files found in folder {input_folder} {search_type}")
        return

    print(f"Found {len(csv_files)} CSV files, starting processing...")
    print("-" * 50)

    total_processed = 0
    successful_files = 0

    for csv_file in csv_files:
        # Calculate relative path to maintain folder structure
        relative_path = csv_file.relative_to(input_path)

        # Generate output file path, maintaining original folder structure
        if output_folder is None:
            # Generate file in original location
            output_filename = "lyric" + output_suffix + ".csv"
            output_file = csv_file.parent / output_filename
        else:
            # Maintain same folder structure in specified output folder
            output_filename = "lyric" + output_suffix + ".csv"
            output_file = output_path / relative_path.parent / output_filename

            # Ensure output folder exists
            output_file.parent.mkdir(parents=True, exist_ok=True)

        # Display relative path for better understanding of file location
        print(f"Processing file: {relative_path}")

        # Convert file
        record_count = convert_lyric_csv(csv_file, output_file)

        if record_count > 0:
            print(f"  ✓ Successfully converted {record_count} records")
            print(f"  ✓ Output file: {output_file.relative_to(Path.cwd()) if output_file.is_relative_to(Path.cwd()) else output_file}")
            total_processed += record_count
            successful_files += 1
        elif record_count == 0:
            print(f"  ⚠ No valid data in file")
        else:
            print(f"  ✗ Processing failed")

        print()

    print("-" * 50)
    print(f"Batch processing completed!")
    print(f"Successfully processed files: {successful_files}/{len(csv_files)}")
    print(f"Total converted records: {total_processed}")

def convert_single_file(input_file, output_file=None):
    """
    Convert a single file

    Args:
        input_file (str): Input file path
        output_file (str): Output file path (optional)
    """

    input_path = Path(input_file)

    # Check if input file exists
    if not input_path.exists():
        print(f"Error: Input file {input_file} does not exist")
        return

    # If output file not specified, generate in same directory
    if output_file is None:
        output_file = input_path.parent / f"{input_path.stem}_converted.csv"

    print(f"Converting file: {input_file}")
    print(f"Output file: {output_file}")

    # Convert file
    record_count = convert_lyric_csv(input_file, output_file)

    if record_count > 0:
        print(f"✓ Successfully converted {record_count} records")
    elif record_count == 0:
        print(f"⚠ No valid data in file")
    else:
        print(f"✗ Processing failed")

def main():
    """Main function to handle command line arguments"""

    # Check for --no-recursive flag
    recursive = True
    args = sys.argv.copy()
    if "--no-recursive" in args:
        recursive = False
        args.remove("--no-recursive")

    if len(args) < 2:
        print("Lyric CSV Format Converter Tool")
        print("Convert lyric timeline CSV format to format with translation fields")
        print()
        print("Supported input formats:")
        print("  Format1: start_time(seconds),end_time(seconds),lyric")
        print("  Format2: ID,開始時間,終了時間,ローカライズ用キー名")
        print()
        print("Usage:")
        print("  Convert single file:")
        print("    python script.py <input_file> [output_file]")
        print("  Batch convert folder:")
        print("    python script.py <input_folder> [output_folder] [output_suffix] [--no-recursive]")
        print()
        print("Examples:")
        print("  python script.py song.csv")
        print("  python script.py song.csv converted_song.csv")
        print("  python script.py ./lyrics_folder")
        print("  python script.py ./lyrics_folder ./output_folder")
        print("  python script.py ./lyrics_folder ./output_folder _new")
        print("  python script.py ./lyrics_folder --no-recursive")
        print()
        print("Parameter description:")
        print("  input_file/folder: CSV file or folder containing CSV files to convert")
        print("  output_folder: Output folder path (optional, defaults to input folder)")
        print("  output_suffix: Output filename suffix (optional, defaults to empty)")
        print("  --no-recursive: (optional flag) If used, will not recursively process subfolders")
        print()
        print("Output format: StartTime,EndTime,OriginalLyric,TranslatedLyric")
        print()
        return
    
    input_path = Path(args[1])
    
    # Determine if it's a file or folder
    if input_path.is_file():
        # Single file processing
        output_file = args[2] if len(args) > 2 else None
        convert_single_file(args[1], output_file)
    elif input_path.is_dir():
        # Folder batch processing
        output_folder = args[2] if len(args) > 2 else None
        output_suffix = args[3] if len(args) > 3 else ""
        
        if not recursive:
            print("Note: Set to not recursively process subfolders.")
        
        process_folder(args[1], output_folder, output_suffix, recursive)
    else:
        print(f"Error: Path {args[1]} does not exist")

if __name__ == "__main__":
    # Use command line arguments
    if len(sys.argv) > 1:
        main()
    else:
        print("Lyric CSV Format Converter Tool")
        print("Convert lyric timeline CSV format to format with translation fields")
        print()
        print("Usage:")
        print("  Convert single file:")
        print("    python script.py <input_file> [output_file]")
        print("  Batch convert folder:")
        print("    python script.py <input_folder> [output_folder] [output_suffix] [--no-recursive]")
        print()
        print("Examples:")
        print("  python script.py song.csv")
        print("  python script.py song.csv converted_song.csv")
        print("  python script.py ./lyrics_folder")
        print("  python script.py ./lyrics_folder ./output_folder")
        print("  python script.py ./lyrics_folder ./output_folder _new")
        print("  python script.py ./lyrics_folder --no-recursive")
        print()
        print("Parameter description:")
        print("  input_file/folder: CSV file or folder containing CSV files to convert")
        print("  output_folder: Output folder path (optional, defaults to input folder)")
        print("  output_suffix: Output filename suffix (optional, defaults to empty)")
        print("  --no-recursive: (optional flag) If used, will not recursively process subfolders")
        print()
        print("Supported input formats:")
        print("  Format1: start_time(seconds),end_time(seconds),lyric")
        print("  Format2: ID,開始時間,終了時間,ローカライズ用キー名")
        print()
        print("Output format: StartTime,EndTime,OriginalLyric,TranslatedLyric")
        print()