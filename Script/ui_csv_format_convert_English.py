#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: Convert csv file format extracted from official website or I18nEx to the format used by JustAnotherTranslator
# Author: claude 4 & 90135
# Creation date: 2025-07-04
# Version: 2025-08-02_01
# License: Bsd-3

import csv
import os
import sys
from pathlib import Path

def convert_single_csv(input_file, output_file, add_prefix=True):
    """
    Convert single multilingual CSV format to simplified terminology table format
    
    Input format: Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)
    or: Key,Type,Desc,Japanese,English
    Output format: Term,Original,Translation
    
    Args:
        input_file (str): Input CSV file path
        output_file (str): Output CSV file path
        add_prefix (bool): Whether to add filename prefix to Term field
    """
    
    try:
        # Get filename (without extension) for prefix
        file_prefix = Path(input_file).stem
        
        # Use utf-8-sig encoding to handle BOM
        with open(input_file, 'r', encoding='utf-8-sig') as infile:
            reader = csv.DictReader(infile)
            
            # Prepare output data
            output_data = []
            
            for row_num, row in enumerate(reader, 1):
                # Extract key information, handle possible BOM characters
                key_field = 'Key'
                if key_field not in row:
                    # Find column name containing 'Key' (may have BOM prefix)
                    for col_name in row.keys():
                        if col_name.endswith('Key'):
                            key_field = col_name
                            break
                
                key = row.get(key_field, '')
                japanese = row.get('Japanese', '')
                english = row.get('English', '')
                
                # Skip empty rows or invalid data
                if not key:
                    continue
                
                # Determine Term format based on add_prefix parameter
                term_value = f"{file_prefix}/{key}" if add_prefix else key
                
                # Create new row data
                new_row = {
                    'Term': term_value,
                    'Original': japanese,
                    'Translation': english
                }
                
                output_data.append(new_row)
    
        # Write output file
        with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
            if output_data:
                writer = csv.DictWriter(outfile, fieldnames=['Term', 'Original', 'Translation'])
                writer.writeheader()
                writer.writerows(output_data)
                return len(output_data)
            else:
                return 0
                
    except Exception as e:
        print(f"Error processing file {input_file}: {e}")
        return -1

def process_folder(input_folder, output_folder=None, output_suffix="", add_prefix=True, recursive=True):
    """
    Batch process all CSV files in folder (including subfolders)
    
    Args:
        input_folder (str): Input folder path
        output_folder (str): Output folder path (optional, defaults to input folder)
        output_suffix (str): Output filename suffix (defaults to empty)
        add_prefix (bool): Whether to add filename prefix to Term field
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
        # Create output folder if it doesn't exist
        output_path.mkdir(parents=True, exist_ok=True)
    
    # Find all CSV files (recursive or non-recursive)
    if recursive:
        csv_files = list(input_path.rglob("*.csv"))  # Recursively search all subfolders
        print(f"Recursively searching folder {input_folder} and its subfolders...")
    else:
        csv_files = list(input_path.glob("*.csv"))   # Only search current folder
        print(f"Searching folder {input_folder}...")
    
    if not csv_files:
        search_type = "and its subfolders" if recursive else ""
        print(f"No CSV files found in folder {input_folder} {search_type}")
        return
    
    print(f"Found {len(csv_files)} CSV files, starting processing...")
    if not add_prefix:
        print("Note: Set to not add filename prefix to Term.")
    print("-" * 50)
    
    total_processed = 0
    successful_files = 0
    
    for csv_file in csv_files:
        # Calculate relative path to maintain folder structure
        relative_path = csv_file.relative_to(input_path)
        
        # Generate output file path, maintaining original folder structure
        if output_folder is None:
            # Generate file in original location
            output_filename = csv_file.stem + output_suffix + ".csv"
            output_file = csv_file.parent / output_filename
        else:
            # Maintain same folder structure in specified output folder
            output_filename = csv_file.stem + output_suffix + ".csv"
            output_file = output_path / relative_path.parent / output_filename
            
            # Ensure output folder exists
            output_file.parent.mkdir(parents=True, exist_ok=True)
        
        # Display relative path for easier understanding of file location
        print(f"Processing file: {relative_path}")
        
        # Convert file, pass add_prefix parameter
        record_count = convert_single_csv(csv_file, output_file, add_prefix=add_prefix)
        
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

def convert_single_file(input_file, output_file=None, add_prefix=True):
    """
    Convert single file
    
    Args:
        input_file (str): Input file path
        output_file (str): Output file path (optional)
        add_prefix (bool): Whether to add filename prefix to Term field
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
    if not add_prefix:
        print("Note: Set to not add filename prefix to Term.")
    
    # Convert file
    record_count = convert_single_csv(input_file, output_file, add_prefix=add_prefix)
    
    if record_count > 0:
        print(f"✓ Successfully converted {record_count} records")
    elif record_count == 0:
        print(f"⚠ No valid data in file")
    else:
        print(f"✗ Processing failed")

def main():
    """Main function to handle command line arguments"""
    
    # Check for --no-prefix and --no-recursive flags
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
        print("Multilingual CSV Format Converter")
        print("Convert multilingual CSV format to terminology table format")
        print()
        print("Usage:")
        print("  Convert single file:")
        print("    python script.py <input_file> [output_file] [--no-prefix]")
        print("  Batch convert folder:")
        print("    python script.py <input_folder> [output_folder] [output_suffix] [--no-prefix] [--no-recursive]")
        print()
        print("Examples:")
        print("  python script.py terms.csv")
        print("  python script.py terms.csv converted_terms.csv")
        print("  python script.py ./input_folder")
        print("  python script.py ./input_folder ./output_folder")
        print("  python script.py ./input_folder ./output_folder _new")
        print("  python script.py ./input_folder --no-prefix")
        print("  python script.py ./input_folder --no-recursive")
        print("  python script.py ./input_folder --no-prefix --no-recursive")
        print()
        print("Parameters:")
        print("  input_file/folder: CSV file or folder containing CSV files to convert")
        print("  output_folder: Output folder path (optional, defaults to input folder)")
        print("  output_suffix: Output filename suffix (optional, defaults to empty)")
        print("  --no-prefix: (optional flag) If used, will not add filename prefix to Term")
        print("  --no-recursive: (optional flag) If used, will not recursively process subfolders")
        print()
        print("Input format: Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)")
        print("Output format: Term,Original,Translation")
        print()
        return
    
    input_path = Path(args[1])
    
    # Determine if it's a file or folder
    if input_path.is_file():
        # Single file processing
        output_file = args[2] if len(args) > 2 else None
        convert_single_file(args[1], output_file, add_prefix=add_prefix)
    elif input_path.is_dir():
        # Folder batch processing
        output_folder = args[2] if len(args) > 2 else None
        output_suffix = args[3] if len(args) > 3 else ""
        
        if not recursive:
            print("Note: Set to not recursively process subfolders.")
        
        process_folder(args[1], output_folder, output_suffix, add_prefix=add_prefix, recursive=recursive)
    else:
        print(f"Error: Path {args[1]} does not exist")


if __name__ == "__main__":
    # Use command line arguments
    if len(sys.argv) > 1:
        main()
    else:
        print("Multilingual CSV Format Converter")
        print("Convert multilingual CSV format to terminology table format")
        print()
        print("Usage:")
        print("  Convert single file:")
        print("    python script.py <input_file> [output_file] [--no-prefix]")
        print("  Batch convert folder:")
        print("    python script.py <input_folder> [output_folder] [output_suffix] [--no-prefix] [--no-recursive]")
        print()
        print("Examples:")
        print("  python script.py terms.csv")
        print("  python script.py terms.csv converted_terms.csv")
        print("  python script.py ./input_folder")
        print("  python script.py ./input_folder ./output_folder")
        print("  python script.py ./input_folder ./output_folder _new")
        print("  python script.py ./input_folder --no-prefix")
        print("  python script.py ./input_folder --no-recursive")
        print("  python script.py ./input_folder --no-prefix --no-recursive")
        print()
        print("Parameters:")
        print("  input_file/folder: CSV file or folder containing CSV files to convert")
        print("  output_folder: Output folder path (optional, defaults to input folder)")
        print("  output_suffix: Output filename suffix (optional, defaults to empty)")
        print("  --no-prefix: (optional flag) If used, will not add filename prefix to Term")
        print("  --no-recursive: (optional flag) If used, will not recursively process subfolders")
        print()
        print("Input format: Key,Type,Desc,Japanese,English,Chinese (Simplified),Chinese (Traditional)")
        print("Output format: Term,Original,Translation")
        print()