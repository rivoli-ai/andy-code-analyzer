#!/usr/bin/env python3
"""
Data processing module with various utilities for handling different data types.
"""

import json
import asyncio
from typing import List, Dict, Any, Optional, Union
from dataclasses import dataclass
from datetime import datetime
from enum import Enum
import re


class ProcessingStatus(Enum):
    """Status of data processing operations."""
    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"
    FAILED = "failed"


@dataclass
class ProcessingResult:
    """Result of a data processing operation."""
    status: ProcessingStatus
    data: Optional[Any] = None
    error: Optional[str] = None
    timestamp: datetime = None

    def __post_init__(self):
        if self.timestamp is None:
            self.timestamp = datetime.now()


class DataValidator:
    """Validates various types of data."""
    
    @staticmethod
    def validate_email(email: str) -> bool:
        """Validate email address format."""
        pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
        return re.match(pattern, email) is not None
    
    @staticmethod
    def validate_phone(phone: str) -> bool:
        """Validate phone number format."""
        # Simple validation for US phone numbers
        pattern = r'^\+?1?\d{10,14}$'
        cleaned = re.sub(r'[\s\-\(\)]', '', phone)
        return re.match(pattern, cleaned) is not None
    
    @staticmethod
    def validate_json(json_str: str) -> tuple[bool, Optional[Dict]]:
        """Validate JSON string and return parsed data."""
        try:
            data = json.loads(json_str)
            return True, data
        except json.JSONDecodeError as e:
            return False, None


class DataProcessor:
    """Main data processor with various transformation capabilities."""
    
    def __init__(self, batch_size: int = 100):
        self.batch_size = batch_size
        self._processed_count = 0
        self._errors = []
    
    def process_text(self, text: str, operations: List[str]) -> str:
        """Apply a series of text operations."""
        result = text
        
        for op in operations:
            if op == "lowercase":
                result = result.lower()
            elif op == "uppercase":
                result = result.upper()
            elif op == "strip":
                result = result.strip()
            elif op == "remove_punctuation":
                result = re.sub(r'[^\w\s]', '', result)
            elif op == "normalize_whitespace":
                result = ' '.join(result.split())
        
        return result
    
    def extract_numbers(self, text: str) -> List[float]:
        """Extract all numbers from text."""
        # Find integers and decimals
        pattern = r'-?\d+\.?\d*'
        numbers = re.findall(pattern, text)
        return [float(n) for n in numbers]
    
    def process_batch(self, items: List[Dict], processor_func) -> List[ProcessingResult]:
        """Process items in batches."""
        results = []
        
        for i in range(0, len(items), self.batch_size):
            batch = items[i:i + self.batch_size]
            
            for item in batch:
                try:
                    processed = processor_func(item)
                    results.append(ProcessingResult(
                        status=ProcessingStatus.COMPLETED,
                        data=processed
                    ))
                    self._processed_count += 1
                except Exception as e:
                    error_result = ProcessingResult(
                        status=ProcessingStatus.FAILED,
                        error=str(e)
                    )
                    results.append(error_result)
                    self._errors.append(error_result)
        
        return results
    
    def get_statistics(self) -> Dict[str, Any]:
        """Get processing statistics."""
        return {
            "processed_count": self._processed_count,
            "error_count": len(self._errors),
            "success_rate": (self._processed_count - len(self._errors)) / self._processed_count if self._processed_count > 0 else 0
        }


class AsyncDataProcessor:
    """Asynchronous data processor for concurrent operations."""
    
    def __init__(self, max_concurrent: int = 10):
        self.max_concurrent = max_concurrent
        self._semaphore = asyncio.Semaphore(max_concurrent)
    
    async def process_url(self, url: str) -> ProcessingResult:
        """Simulate processing a URL asynchronously."""
        async with self._semaphore:
            try:
                # Simulate network request
                await asyncio.sleep(0.1)
                
                # Extract domain
                domain_match = re.match(r'https?://([^/]+)', url)
                domain = domain_match.group(1) if domain_match else None
                
                return ProcessingResult(
                    status=ProcessingStatus.COMPLETED,
                    data={"url": url, "domain": domain}
                )
            except Exception as e:
                return ProcessingResult(
                    status=ProcessingStatus.FAILED,
                    error=str(e)
                )
    
    async def process_urls(self, urls: List[str]) -> List[ProcessingResult]:
        """Process multiple URLs concurrently."""
        tasks = [self.process_url(url) for url in urls]
        return await asyncio.gather(*tasks)


class StreamProcessor:
    """Process data streams with buffering and windowing."""
    
    def __init__(self, window_size: int = 10):
        self.window_size = window_size
        self._buffer = []
        self._window_stats = []
    
    def add_value(self, value: float) -> Optional[Dict[str, float]]:
        """Add a value to the stream and return window statistics."""
        self._buffer.append(value)
        
        if len(self._buffer) >= self.window_size:
            window = self._buffer[-self.window_size:]
            stats = {
                "mean": sum(window) / len(window),
                "min": min(window),
                "max": max(window),
                "sum": sum(window)
            }
            self._window_stats.append(stats)
            return stats
        
        return None
    
    def get_trend(self) -> str:
        """Analyze trend based on window statistics."""
        if len(self._window_stats) < 2:
            return "insufficient_data"
        
        recent_means = [s["mean"] for s in self._window_stats[-5:]]
        if len(recent_means) < 2:
            return "insufficient_data"
        
        # Simple trend detection
        if all(recent_means[i] <= recent_means[i+1] for i in range(len(recent_means)-1)):
            return "increasing"
        elif all(recent_means[i] >= recent_means[i+1] for i in range(len(recent_means)-1)):
            return "decreasing"
        else:
            return "fluctuating"


def main():
    """Example usage of data processing classes."""
    # Text processing example
    processor = DataProcessor()
    text = "  Hello, World! This is a TEST.  "
    processed = processor.process_text(text, ["strip", "lowercase", "remove_punctuation"])
    print(f"Processed text: '{processed}'")
    
    # Number extraction
    numbers_text = "The temperature is 72.5 degrees and humidity is 65%"
    numbers = processor.extract_numbers(numbers_text)
    print(f"Extracted numbers: {numbers}")
    
    # Validation examples
    validator = DataValidator()
    print(f"Email valid: {validator.validate_email('user@example.com')}")
    print(f"Phone valid: {validator.validate_phone('+1 (555) 123-4567')}")
    
    # Stream processing
    stream = StreamProcessor(window_size=3)
    values = [10, 12, 11, 15, 18, 20, 19, 22, 25, 24]
    for val in values:
        stats = stream.add_value(val)
        if stats:
            print(f"Window stats: {stats}")
    
    print(f"Trend: {stream.get_trend()}")


if __name__ == "__main__":
    main()