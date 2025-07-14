#!/usr/bin/env python3
"""
File system utilities for various file operations.
"""

import os
import json
import csv
import hashlib
import shutil
from pathlib import Path
from typing import List, Dict, Optional, Union, Iterator
from datetime import datetime
import mimetypes


class FileInfo:
    """Container for file information."""
    
    def __init__(self, path: Union[str, Path]):
        self.path = Path(path)
        self._stat = None
    
    @property
    def stat(self):
        """Lazy load file stats."""
        if self._stat is None and self.path.exists():
            self._stat = self.path.stat()
        return self._stat
    
    @property
    def size(self) -> int:
        """File size in bytes."""
        return self.stat.st_size if self.stat else 0
    
    @property
    def size_human(self) -> str:
        """Human-readable file size."""
        size = self.size
        for unit in ['B', 'KB', 'MB', 'GB', 'TB']:
            if size < 1024.0:
                return f"{size:.2f} {unit}"
            size /= 1024.0
        return f"{size:.2f} PB"
    
    @property
    def modified_time(self) -> datetime:
        """Last modification time."""
        return datetime.fromtimestamp(self.stat.st_mtime) if self.stat else None
    
    @property
    def created_time(self) -> datetime:
        """Creation time."""
        return datetime.fromtimestamp(self.stat.st_ctime) if self.stat else None
    
    @property
    def mime_type(self) -> str:
        """MIME type of the file."""
        mime_type, _ = mimetypes.guess_type(str(self.path))
        return mime_type or 'application/octet-stream'
    
    def to_dict(self) -> Dict:
        """Convert to dictionary."""
        return {
            'path': str(self.path),
            'name': self.path.name,
            'size': self.size,
            'size_human': self.size_human,
            'modified': self.modified_time.isoformat() if self.modified_time else None,
            'mime_type': self.mime_type,
            'is_dir': self.path.is_dir(),
            'is_file': self.path.is_file()
        }


class FileScanner:
    """Scan directories and find files based on patterns."""
    
    def __init__(self, root_path: Union[str, Path]):
        self.root_path = Path(root_path)
    
    def find_files(self, pattern: str = "*", recursive: bool = True) -> List[Path]:
        """Find files matching a pattern."""
        if recursive:
            return list(self.root_path.rglob(pattern))
        else:
            return list(self.root_path.glob(pattern))
    
    def find_by_extension(self, extensions: List[str], recursive: bool = True) -> List[Path]:
        """Find files with specific extensions."""
        files = []
        for ext in extensions:
            if not ext.startswith('.'):
                ext = '.' + ext
            files.extend(self.find_files(f"*{ext}", recursive))
        return files
    
    def find_large_files(self, size_mb: float = 100, recursive: bool = True) -> List[FileInfo]:
        """Find files larger than specified size."""
        size_bytes = size_mb * 1024 * 1024
        large_files = []
        
        for file_path in self.find_files("*", recursive):
            if file_path.is_file():
                info = FileInfo(file_path)
                if info.size > size_bytes:
                    large_files.append(info)
        
        return sorted(large_files, key=lambda f: f.size, reverse=True)
    
    def find_duplicates(self, recursive: bool = True) -> Dict[str, List[Path]]:
        """Find duplicate files based on content hash."""
        hash_map = {}
        
        for file_path in self.find_files("*", recursive):
            if file_path.is_file():
                file_hash = self._calculate_hash(file_path)
                if file_hash:
                    if file_hash not in hash_map:
                        hash_map[file_hash] = []
                    hash_map[file_hash].append(file_path)
        
        # Return only duplicates
        return {h: paths for h, paths in hash_map.items() if len(paths) > 1}
    
    @staticmethod
    def _calculate_hash(file_path: Path, chunk_size: int = 8192) -> Optional[str]:
        """Calculate MD5 hash of a file."""
        try:
            md5_hash = hashlib.md5()
            with open(file_path, 'rb') as f:
                while chunk := f.read(chunk_size):
                    md5_hash.update(chunk)
            return md5_hash.hexdigest()
        except Exception:
            return None
    
    def get_directory_stats(self) -> Dict[str, any]:
        """Get statistics about the directory."""
        total_size = 0
        file_count = 0
        dir_count = 0
        extension_stats = {}
        
        for item in self.root_path.rglob("*"):
            if item.is_file():
                file_count += 1
                size = item.stat().st_size
                total_size += size
                
                ext = item.suffix.lower()
                if ext:
                    if ext not in extension_stats:
                        extension_stats[ext] = {'count': 0, 'size': 0}
                    extension_stats[ext]['count'] += 1
                    extension_stats[ext]['size'] += size
            elif item.is_dir():
                dir_count += 1
        
        return {
            'total_size': total_size,
            'total_size_human': FileInfo._format_size(total_size),
            'file_count': file_count,
            'directory_count': dir_count,
            'extensions': extension_stats
        }
    
    @staticmethod
    def _format_size(size: int) -> str:
        """Format size in human-readable format."""
        for unit in ['B', 'KB', 'MB', 'GB', 'TB']:
            if size < 1024.0:
                return f"{size:.2f} {unit}"
            size /= 1024.0
        return f"{size:.2f} PB"


class FileOrganizer:
    """Organize files based on various criteria."""
    
    def __init__(self, source_dir: Union[str, Path], target_dir: Union[str, Path]):
        self.source_dir = Path(source_dir)
        self.target_dir = Path(target_dir)
    
    def organize_by_type(self, move: bool = False) -> Dict[str, List[Path]]:
        """Organize files by their type/extension."""
        organized = {}
        
        for file_path in self.source_dir.rglob("*"):
            if file_path.is_file():
                # Determine category
                category = self._get_file_category(file_path)
                target_category_dir = self.target_dir / category
                target_category_dir.mkdir(parents=True, exist_ok=True)
                
                # Move or copy file
                target_path = target_category_dir / file_path.name
                if move:
                    shutil.move(str(file_path), str(target_path))
                else:
                    shutil.copy2(str(file_path), str(target_path))
                
                if category not in organized:
                    organized[category] = []
                organized[category].append(target_path)
        
        return organized
    
    def organize_by_date(self, date_format: str = "%Y/%m", move: bool = False) -> Dict[str, List[Path]]:
        """Organize files by modification date."""
        organized = {}
        
        for file_path in self.source_dir.rglob("*"):
            if file_path.is_file():
                # Get modification date
                mod_time = datetime.fromtimestamp(file_path.stat().st_mtime)
                date_folder = mod_time.strftime(date_format)
                
                target_date_dir = self.target_dir / date_folder
                target_date_dir.mkdir(parents=True, exist_ok=True)
                
                # Move or copy file
                target_path = target_date_dir / file_path.name
                if move:
                    shutil.move(str(file_path), str(target_path))
                else:
                    shutil.copy2(str(file_path), str(target_path))
                
                if date_folder not in organized:
                    organized[date_folder] = []
                organized[date_folder].append(target_path)
        
        return organized
    
    @staticmethod
    def _get_file_category(file_path: Path) -> str:
        """Determine file category based on extension."""
        ext = file_path.suffix.lower()
        
        categories = {
            'images': ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.svg', '.webp'],
            'videos': ['.mp4', '.avi', '.mkv', '.mov', '.wmv', '.flv'],
            'audio': ['.mp3', '.wav', '.flac', '.aac', '.ogg', '.wma'],
            'documents': ['.pdf', '.doc', '.docx', '.txt', '.odt', '.rtf'],
            'spreadsheets': ['.xls', '.xlsx', '.csv', '.ods'],
            'presentations': ['.ppt', '.pptx', '.odp'],
            'archives': ['.zip', '.rar', '.7z', '.tar', '.gz', '.bz2'],
            'code': ['.py', '.js', '.java', '.cpp', '.c', '.cs', '.rb', '.go', '.rs']
        }
        
        for category, extensions in categories.items():
            if ext in extensions:
                return category
        
        return 'others'


class FileProcessor:
    """Process various file formats."""
    
    @staticmethod
    def read_json(file_path: Union[str, Path]) -> Dict:
        """Read JSON file."""
        with open(file_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    @staticmethod
    def write_json(data: Dict, file_path: Union[str, Path], indent: int = 2) -> None:
        """Write data to JSON file."""
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=indent, ensure_ascii=False)
    
    @staticmethod
    def read_csv(file_path: Union[str, Path]) -> List[Dict]:
        """Read CSV file and return as list of dictionaries."""
        with open(file_path, 'r', encoding='utf-8') as f:
            reader = csv.DictReader(f)
            return list(reader)
    
    @staticmethod
    def write_csv(data: List[Dict], file_path: Union[str, Path]) -> None:
        """Write list of dictionaries to CSV file."""
        if not data:
            return
        
        with open(file_path, 'w', encoding='utf-8', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=data[0].keys())
            writer.writeheader()
            writer.writerows(data)
    
    @staticmethod
    def read_lines(file_path: Union[str, Path]) -> List[str]:
        """Read file as list of lines."""
        with open(file_path, 'r', encoding='utf-8') as f:
            return [line.rstrip('\n') for line in f]
    
    @staticmethod
    def write_lines(lines: List[str], file_path: Union[str, Path]) -> None:
        """Write list of lines to file."""
        with open(file_path, 'w', encoding='utf-8') as f:
            for line in lines:
                f.write(line + '\n')
    
    @staticmethod
    def process_large_file(file_path: Union[str, Path], 
                          process_func: callable, 
                          chunk_size: int = 1024 * 1024) -> None:
        """Process large file in chunks."""
        with open(file_path, 'r', encoding='utf-8') as f:
            while chunk := f.read(chunk_size):
                process_func(chunk)


class BackupManager:
    """Manage file backups."""
    
    def __init__(self, backup_dir: Union[str, Path]):
        self.backup_dir = Path(backup_dir)
        self.backup_dir.mkdir(parents=True, exist_ok=True)
    
    def backup_file(self, file_path: Union[str, Path], 
                   versioning: bool = True) -> Path:
        """Create backup of a file."""
        file_path = Path(file_path)
        
        if versioning:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            backup_name = f"{file_path.stem}_{timestamp}{file_path.suffix}"
        else:
            backup_name = file_path.name
        
        backup_path = self.backup_dir / backup_name
        shutil.copy2(str(file_path), str(backup_path))
        
        return backup_path
    
    def backup_directory(self, dir_path: Union[str, Path], 
                        compress: bool = True) -> Path:
        """Create backup of entire directory."""
        dir_path = Path(dir_path)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        if compress:
            backup_name = f"{dir_path.name}_{timestamp}"
            backup_path = self.backup_dir / backup_name
            shutil.make_archive(str(backup_path), 'zip', str(dir_path))
            return Path(str(backup_path) + '.zip')
        else:
            backup_name = f"{dir_path.name}_{timestamp}"
            backup_path = self.backup_dir / backup_name
            shutil.copytree(str(dir_path), str(backup_path))
            return backup_path
    
    def list_backups(self, pattern: str = "*") -> List[FileInfo]:
        """List all backups."""
        backups = []
        for backup_path in self.backup_dir.glob(pattern):
            backups.append(FileInfo(backup_path))
        return sorted(backups, key=lambda b: b.modified_time, reverse=True)
    
    def cleanup_old_backups(self, days: int = 30) -> List[Path]:
        """Remove backups older than specified days."""
        removed = []
        cutoff_date = datetime.now().timestamp() - (days * 24 * 60 * 60)
        
        for backup_path in self.backup_dir.iterdir():
            if backup_path.stat().st_mtime < cutoff_date:
                if backup_path.is_file():
                    backup_path.unlink()
                else:
                    shutil.rmtree(backup_path)
                removed.append(backup_path)
        
        return removed


def main():
    """Example usage of file utilities."""
    # File scanning example
    scanner = FileScanner(".")
    
    # Find Python files
    py_files = scanner.find_by_extension(['.py'])
    print(f"Found {len(py_files)} Python files")
    
    # Get directory statistics
    stats = scanner.get_directory_stats()
    print(f"Directory stats: {stats}")
    
    # File processing example
    sample_data = [
        {'name': 'Alice', 'age': 30, 'city': 'New York'},
        {'name': 'Bob', 'age': 25, 'city': 'London'},
        {'name': 'Charlie', 'age': 35, 'city': 'Paris'}
    ]
    
    # Write and read CSV
    FileProcessor.write_csv(sample_data, 'sample.csv')
    read_data = FileProcessor.read_csv('sample.csv')
    print(f"Read {len(read_data)} records from CSV")
    
    # Cleanup
    if Path('sample.csv').exists():
        Path('sample.csv').unlink()


if __name__ == "__main__":
    main()