#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: Convert lyric timeline CSV to JAT lyric files (one folder per song ID)
# Author: claude 4 & 90135
# Creation date: 2025-07-19
# Version: 2026-11-18_01
# License: BSD-3
#
# Since JAT v0.3.0 the dance lyric folder is named after DanceData.ID
# (Lyric\<ID>\lyric.csv) instead of musicName. LBWtranslation's
# csv_rhythm_action\<song internal name>\ cannot be turned into an ID directly,
# so this script needs a "name -> ID" mapping table:
#   - danceInfos.csv / danceInfosKaraoke.csv written by JAT at runtime
#   - or dance_setting.csv converted from dance_setting.nei
# Pass it with --mapping (repeatable), the script auto-detects the columns.

import csv
import sys
from pathlib import Path

# Possible ID column names (compared in lower case)
ID_COLUMNS = ('id', 'karaokemusicid', 'danceid')

# Possible "lyric folder name / song internal name" column names (lower case)
# Note: title-like fields are intentionally excluded, a title is not a folder name
NAME_COLUMNS = (
    'csvfoldername',        # JAT danceInfos.csv / dance_setting.nei (English field name)
    'musicname',            # JAT danceInfos.csv, the csv_rhythm_action folder actually used
    'karaokeoggfilename',   # JAT danceInfosKaraoke.csv
    'bgmfilename',          # JAT danceInfos.csv
    'dancedatabgmfilename', # JAT danceInfosKaraoke.csv
    'playbgmfile',          # dance_setting.nei column PlayBGMFile
    'playedoggfiles',       # recorded by JAT at runtime, multiple values separated by |
    'lyricfoldername',      # the folder name JAT actually used (makes re-runs idempotent)
    '譜面とかのcsvフォルダ名',  # original Japanese column name in dance_setting.nei
)

# Columns whose value may contain multiple entries
MULTI_VALUE_COLUMNS = {'playedoggfiles'}

# Extensions removed when building a mapping key
STRIP_EXTENSIONS = ('.ogg', '.wav', '.mp3', '.csv', '.nei', '.txt')


def normalize_key(value):
    """Normalize a song internal name into a mapping key (lower case, no path, no extension)"""
    if value is None:
        return ''
    text = str(value).strip()
    if not text:
        return ''
    text = text.replace('\\', '/')
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
    Detect the format of the CSV file

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

            # Check for format 2 (contains ID and Japanese header names)
            if (len(first_row) >= 4 and
                ('ID' in first_row[0] or first_row[0].strip() == 'ID') and
                ('開始時間' in first_row[1] or '終了時間' in first_row[2] or 'ローカライズ用キー名' in first_row[3])):
                return 'format2'

            # Check for format 1 (original format)
            if (len(first_row) >= 3 and
                (first_row[0].strip() in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                 first_row[1].strip() in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                 first_row[2].strip() in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric'])):
                return 'format1'

            # If the first row is not a header, try to detect the data format
            # Format 2 usually starts with a numeric ID
            try:
                int(first_row[0])
                if len(first_row) >= 4:
                    return 'format2'
            except ValueError:
                pass

            # Format 1 usually starts directly with time
            try:
                float(first_row[0])
                float(first_row[1])
                if len(first_row) >= 3:
                    return 'format1'
            except ValueError:
                pass

    except Exception as e:
        print(f"Error while detecting file format: {e}")

    return 'unknown'


def read_lyric_csv(input_file):
    """
    Read a lyric timeline CSV and return the record list

    Supported input formats:
    - Format 1: start_time(seconds),end_time(seconds),lyric
    - Format 2: ID,開始時間,終了時間,ローカライズ用キー名

    Returns: [{'StartTime':.., 'EndTime':.., 'OriginalLyric':.., 'TranslatedLyric':..}, ...]
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

                # Skip the header row
                if (id_field in ['ID', 'id'] or
                    start_time in ['開始時間', 'StartTime', 'Start Time'] or
                    end_time in ['終了時間', 'EndTime', 'End Time'] or
                    original_lyric in ['ローカライズ用キー名', 'OriginalLyric', 'Lyric']):
                    continue

                # Validate that the ID is numeric (optional check)
                try:
                    int(id_field)
                except ValueError:
                    continue

            else:
                start_time = row[0].strip()
                end_time = row[1].strip()
                original_lyric = row[2].strip() if len(row) > 2 else ""

                # Skip the header row
                if (start_time in ['開始時間(秒)', '開始時間', 'StartTime', 'Start Time', 'start_time'] or
                    end_time in ['結束時間(秒)', '結束時間', 'EndTime', 'End Time', 'end_time'] or
                    original_lyric in ['歌詞', 'Lyric', 'Lyrics', 'OriginalLyric', 'Original Lyric']):
                    continue

            # Common validation: check the time format
            try:
                float(start_time)
                float(end_time)
            except ValueError:
                continue

            # Skip empty lyrics
            if not original_lyric:
                continue

            records.append({
                'StartTime': start_time,
                'EndTime': end_time,
                'OriginalLyric': original_lyric,
                'TranslatedLyric': '',  # Translation field left empty
            })

    return file_format, records


def write_lyric_csv(output_file, records):
    """Write a JAT lyric file (UTF-8-BOM with header)"""
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8-sig', newline='') as outfile:
        writer = csv.DictWriter(
            outfile,
            fieldnames=['StartTime', 'EndTime', 'OriginalLyric', 'TranslatedLyric'])
        writer.writeheader()
        writer.writerows(records)


def load_mapping(mapping_files):
    """
    Load "song internal name -> song ID" from mapping tables

    Args:
        mapping_files (list[str]): Mapping CSV paths

    Returns:
        dict[str, list[str]]: Normalized name -> list of song IDs
    """
    mapping = {}

    for mapping_file in mapping_files:
        path = Path(mapping_file)
        if not path.is_file():
            print(f"  Warning: mapping table {mapping_file} does not exist, skipped")
            continue

        try:
            with open(path, 'r', encoding='utf-8-sig', newline='') as f:
                reader = csv.DictReader(f)
                if not reader.fieldnames:
                    print(f"  Warning: mapping table {mapping_file} has no header, skipped")
                    continue

                # Build a "lower case column name -> original column name" index
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
                    print(f"  Warning: no ID column found in mapping table {mapping_file} "
                          f"(available: {', '.join(ID_COLUMNS)}), skipped")
                    continue

                name_columns = []
                for candidate in NAME_COLUMNS:
                    if candidate in header_index:
                        name_columns.append(header_index[candidate])

                if not name_columns:
                    print(f"  Warning: no name column found in mapping table {mapping_file}, skipped")
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

                print(f"  Loaded mapping table {path.name}: "
                      f"{len(name_columns)} name column(s), {added} new mapping(s)")
        except Exception as e:
            print(f"  Warning: failed to read mapping table {mapping_file}: {e}")

    return mapping


def resolve_song_ids(dir_key, mapping):
    """
    Resolve the song ID from the relative directory of the input file

    Tries the full relative path first, then the first level folder, then the last folder.
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
    Batch process all CSV files in a folder (including subfolders)

    Args:
        input_folder (str): Input folder path
        output_folder (str): Output folder path (optional, defaults to input folder)
        mapping (dict): Name -> list of song IDs
        karaoke (bool): Write to Lyric/_Karaoke/<ID>/lyric.csv
        legacy (bool): Use the pre-v0.3.0 layout named after the source folder
        recursive (bool): Whether to recursively process subfolders (defaults to True)
    """
    input_path = Path(input_folder)

    if not input_path.exists():
        print(f"Error: Input folder {input_folder} does not exist")
        return
    if not input_path.is_dir():
        print(f"Error: {input_folder} is not a folder")
        return

    output_path = Path(output_folder) if output_folder else input_path
    output_path.mkdir(parents=True, exist_ok=True)

    if recursive:
        csv_files = sorted(input_path.rglob("*.csv"))
        print(f"Recursively searching folder {input_folder} and its subfolders...")
    else:
        csv_files = sorted(input_path.glob("*.csv"))
        print(f"Searching folder {input_folder}...")

    if not csv_files:
        search_type = " and its subfolders" if recursive else ""
        print(f"No CSV files found in folder {input_folder}{search_type}")
        return

    print(f"Found {len(csv_files)} CSV file(s), processing...")
    print("-" * 50)

    # Target folder -> lyric records
    targets = {}
    # Unresolved folder -> lyric records
    unmatched = {}
    # Folder -> resolved song IDs, used for the summary
    resolved_summary = []

    for csv_file in csv_files:
        relative_path = csv_file.relative_to(input_path)
        relative_dir = relative_path.parent
        # When the input folder is the song folder itself, use the input folder name as key
        dir_key = relative_dir.as_posix() if relative_dir.parts else input_path.name

        try:
            file_format, records = read_lyric_csv(csv_file)
        except Exception as e:
            print(f"Error occurred while processing file {relative_path}: {e}")
            continue

        print(f"Processing file: {relative_path}")
        print(f"  Detected file format: {file_format}")

        if not records:
            print("  ⚠ No valid data in file")
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
            print(f"  ✗ Cannot map {dir_key or '.'} to a song ID")
            print("    Please pass danceInfos.csv or dance_setting.csv with --mapping")
            print()
            continue

        print(f"  ✓ Read {len(records)} record(s)")
        for folder in target_dirs:
            if legacy:
                print(f"  → {folder}")
            else:
                print(f"  → song ID {folder}")
            bucket = targets.setdefault(folder, [])
            bucket.extend(records)
        print()

    print("-" * 50)

    # Write results
    total_written = 0
    written_folders = 0
    unmatched_records = 0

    for folder, records in sorted(targets.items()):
        # De-duplicate (one song may receive records from several source files)
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

    print("Batch processing finished!")
    print(f"Lyric folders written: {written_folders}")
    print(f"Records written: {total_written}")
    if unmatched_records:
        print(f"Records that could not be mapped: {unmatched_records}")

    if legacy:
        print()
        print("Note: you used --legacy (folders named after the source folder).")
        print("      This only works with JAT versions older than v0.3.0.")
        print("      For JAT v0.3.0 and later, drop --legacy and pass --mapping.")

    if unmatched:
        print()
        print(f"{len(unmatched)} folder(s) could not be mapped to a song ID, "
              f"written to {output_path / '_Unmapped'}:")
        for folder in sorted(unmatched):
            print(f"  - {folder}")
        print("JAT will not read these folders. Enter the corresponding song once so that")
        print("JAT writes danceInfos.csv, or use dance_setting.csv as the mapping table,")
        print("then run this script again.")

    if resolved_summary:
        print()
        print("Folder -> song ID:")
        for folder, song_ids in resolved_summary:
            print(f"  {folder} -> {', '.join(song_ids)}")


def convert_single_file(input_file, output_file=None):
    """
    Convert a single file

    Args:
        input_file (str): Input file path
        output_file (str): Output file path (optional)
    """
    input_path = Path(input_file)

    if not input_path.exists():
        print(f"Error: Input file {input_file} does not exist")
        return

    if output_file is None:
        output_file = input_path.parent / f"{input_path.stem}_converted.csv"

    print(f"Converting file: {input_file}")
    print(f"Output file: {output_file}")

    try:
        file_format, records = read_lyric_csv(input_path)
    except Exception as e:
        print(f"Error occurred while processing file {input_file}: {e}")
        return

    print(f"Detected file format: {file_format}")

    if not records:
        print("⚠ No valid data in file")
        return

    write_lyric_csv(Path(output_file), records)
    print(f"✓ Successfully converted {len(records)} record(s)")


def print_usage():
    print("Lyric CSV Format Converter Tool")
    print("Convert lyric timeline CSV to JAT lyric files")
    print()
    print("Supported input formats:")
    print("  Format1: start_time(seconds),end_time(seconds),lyric")
    print("  Format2: ID,開始時間,終了時間,ローカライズ用キー名")
    print()
    print("Output format: StartTime,EndTime,OriginalLyric,TranslatedLyric")
    print()
    print("Since JAT v0.3.0 the dance lyric folder is named after the song ID:")
    print("  COM3D2/BepInEx/JustAnotherTranslator/<language>/Lyric/<song ID>/lyric.csv")
    print("So batch conversion requires a mapping table passed with --mapping:")
    print("  - danceInfos.csv / danceInfosKaraoke.csv written by JAT")
    print("  - or dance_setting.csv converted from dance_setting.nei")
    print()
    print("Usage:")
    print("  Convert single file:")
    print("    python script.py <input_file> [output_file]")
    print("  Batch convert folder:")
    print("    python script.py <input_folder> [output_folder] --mapping <mapping> [options]")
    print()
    print("Examples:")
    print("  python script.py song.csv")
    print("  python script.py song.csv converted_song.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv --mapping ./dance_setting.csv")
    print("  python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfosKaraoke.csv --karaoke")
    print("  python script.py ./csv_rhythm_action ./Lyric --legacy   (only for JAT before v0.3.0)")
    print()
    print("Parameters:")
    print("  input_file/folder: CSV file or folder containing CSV files to convert")
    print("  output_folder: Output folder path (optional, defaults to input folder)")
    print("  --mapping <file>: Name to song ID mapping table, can be repeated")
    print("  --karaoke: Write to <output_folder>/_Karaoke/<song ID>/lyric.csv")
    print("  --legacy: Name folders after the source folder, only for JAT before v0.3.0")
    print("  --no-recursive: (optional flag) If used, will not recursively process subfolders")
    print()


def main():
    """Main function to handle command line arguments"""

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
                print("Error: --mapping must be followed by a mapping table path")
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
        print(f"Error: path {positional[0]} does not exist")
        return

    output_folder = positional[1] if len(positional) > 1 else None

    mapping = {}
    if legacy:
        print("Note: --legacy is enabled (folders named after the source folder), "
              "only for JAT before v0.3.0.")
    else:
        if not mapping_files:
            print("Error: since JAT v0.3.0 lyric folders are named after the song ID, "
                  "batch conversion needs a mapping table.")
            print("       Pass danceInfos.csv or dance_setting.csv with --mapping, e.g.")
            print("       python script.py ./csv_rhythm_action ./Lyric --mapping ./danceInfos.csv")
            print("       If you still use a JAT version before v0.3.0, add --legacy.")
            return
        print("Loading mapping table(s)...")
        mapping = load_mapping(mapping_files)
        if not mapping:
            print("Error: no mapping could be read from the mapping table(s), stopped.")
            return
        print(f"Mapping loaded, {len(mapping)} name(s).")
        print()

    if not recursive:
        print("Note: subfolders will not be processed recursively.")

    process_folder(positional[0], output_folder, mapping, karaoke, legacy, recursive)


if __name__ == "__main__":
    main()
