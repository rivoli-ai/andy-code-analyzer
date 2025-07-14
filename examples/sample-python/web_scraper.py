#!/usr/bin/env python3
"""
Web scraping utilities with various parsers and extractors.
"""

import re
import asyncio
from typing import List, Dict, Optional, Set, Tuple
from urllib.parse import urljoin, urlparse
from dataclasses import dataclass, field
from datetime import datetime
import logging


# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@dataclass
class WebPage:
    """Represents a scraped web page."""
    url: str
    title: Optional[str] = None
    content: Optional[str] = None
    links: List[str] = field(default_factory=list)
    images: List[str] = field(default_factory=list)
    metadata: Dict[str, str] = field(default_factory=dict)
    scraped_at: datetime = field(default_factory=datetime.now)


class URLValidator:
    """Validates and normalizes URLs."""
    
    @staticmethod
    def is_valid_url(url: str) -> bool:
        """Check if URL is valid."""
        try:
            result = urlparse(url)
            return all([result.scheme, result.netloc])
        except:
            return False
    
    @staticmethod
    def normalize_url(url: str) -> str:
        """Normalize URL by removing fragments and converting to lowercase."""
        parsed = urlparse(url.lower())
        # Remove fragment and rebuild URL
        return f"{parsed.scheme}://{parsed.netloc}{parsed.path}"
    
    @staticmethod
    def is_same_domain(url1: str, url2: str) -> bool:
        """Check if two URLs belong to the same domain."""
        domain1 = urlparse(url1).netloc
        domain2 = urlparse(url2).netloc
        return domain1 == domain2


class HTMLParser:
    """Parse HTML content and extract various elements."""
    
    def __init__(self):
        self.url_validator = URLValidator()
    
    def extract_title(self, html: str) -> Optional[str]:
        """Extract page title from HTML."""
        match = re.search(r'<title>(.*?)</title>', html, re.IGNORECASE | re.DOTALL)
        return match.group(1).strip() if match else None
    
    def extract_links(self, html: str, base_url: str) -> List[str]:
        """Extract all links from HTML."""
        links = []
        # Find all anchor tags
        href_pattern = r'<a[^>]+href=["\']([^"\']+)["\']'
        matches = re.findall(href_pattern, html, re.IGNORECASE)
        
        for match in matches:
            # Convert relative URLs to absolute
            absolute_url = urljoin(base_url, match)
            if self.url_validator.is_valid_url(absolute_url):
                links.append(absolute_url)
        
        return list(set(links))  # Remove duplicates
    
    def extract_images(self, html: str, base_url: str) -> List[str]:
        """Extract all image URLs from HTML."""
        images = []
        # Find img tags
        img_pattern = r'<img[^>]+src=["\']([^"\']+)["\']'
        matches = re.findall(img_pattern, html, re.IGNORECASE)
        
        for match in matches:
            absolute_url = urljoin(base_url, match)
            images.append(absolute_url)
        
        return list(set(images))
    
    def extract_text(self, html: str) -> str:
        """Extract plain text from HTML."""
        # Remove script and style elements
        html = re.sub(r'<script[^>]*>.*?</script>', '', html, flags=re.DOTALL | re.IGNORECASE)
        html = re.sub(r'<style[^>]*>.*?</style>', '', html, flags=re.DOTALL | re.IGNORECASE)
        
        # Remove HTML tags
        text = re.sub(r'<[^>]+>', ' ', html)
        
        # Clean up whitespace
        text = ' '.join(text.split())
        
        return text
    
    def extract_metadata(self, html: str) -> Dict[str, str]:
        """Extract metadata from HTML meta tags."""
        metadata = {}
        
        # Find all meta tags
        meta_pattern = r'<meta\s+([^>]+)>'
        matches = re.findall(meta_pattern, html, re.IGNORECASE)
        
        for match in matches:
            # Extract name/property and content
            name_match = re.search(r'(?:name|property)=["\']([^"\']+)["\']', match)
            content_match = re.search(r'content=["\']([^"\']+)["\']', match)
            
            if name_match and content_match:
                metadata[name_match.group(1)] = content_match.group(1)
        
        return metadata


class WebScraper:
    """Main web scraper class."""
    
    def __init__(self, max_depth: int = 2, same_domain_only: bool = True):
        self.max_depth = max_depth
        self.same_domain_only = same_domain_only
        self.parser = HTMLParser()
        self.validator = URLValidator()
        self._visited_urls: Set[str] = set()
        self._scraped_pages: List[WebPage] = []
    
    async def scrape_page(self, url: str) -> Optional[WebPage]:
        """Scrape a single page (simulated)."""
        if url in self._visited_urls:
            return None
        
        self._visited_urls.add(url)
        logger.info(f"Scraping: {url}")
        
        # Simulate fetching HTML content
        await asyncio.sleep(0.1)  # Simulate network delay
        
        # Simulated HTML content
        html = f"""
        <html>
        <head>
            <title>Sample Page - {url}</title>
            <meta name="description" content="This is a sample page for {url}">
            <meta property="og:title" content="Sample Page">
        </head>
        <body>
            <h1>Welcome to {url}</h1>
            <p>This is sample content with <a href="/about">internal link</a> and 
               <a href="https://example.com">external link</a>.</p>
            <img src="/images/logo.png" alt="Logo">
            <img src="https://example.com/banner.jpg" alt="Banner">
        </body>
        </html>
        """
        
        # Parse the page
        page = WebPage(url=url)
        page.title = self.parser.extract_title(html)
        page.content = self.parser.extract_text(html)
        page.links = self.parser.extract_links(html, url)
        page.images = self.parser.extract_images(html, url)
        page.metadata = self.parser.extract_metadata(html)
        
        self._scraped_pages.append(page)
        return page
    
    async def scrape_website(self, start_url: str) -> List[WebPage]:
        """Scrape a website starting from the given URL."""
        await self._scrape_recursive(start_url, 0)
        return self._scraped_pages
    
    async def _scrape_recursive(self, url: str, depth: int):
        """Recursively scrape pages up to max_depth."""
        if depth > self.max_depth:
            return
        
        page = await self.scrape_page(url)
        if not page:
            return
        
        # Scrape linked pages
        tasks = []
        for link in page.links:
            if self.same_domain_only and not self.validator.is_same_domain(url, link):
                continue
            
            if link not in self._visited_urls:
                tasks.append(self._scrape_recursive(link, depth + 1))
        
        if tasks:
            await asyncio.gather(*tasks, return_exceptions=True)
    
    def get_statistics(self) -> Dict[str, int]:
        """Get scraping statistics."""
        total_links = sum(len(page.links) for page in self._scraped_pages)
        total_images = sum(len(page.images) for page in self._scraped_pages)
        
        return {
            "pages_scraped": len(self._scraped_pages),
            "unique_urls_visited": len(self._visited_urls),
            "total_links_found": total_links,
            "total_images_found": total_images,
            "average_links_per_page": total_links // len(self._scraped_pages) if self._scraped_pages else 0
        }


class LinkChecker:
    """Check links for various issues."""
    
    def __init__(self):
        self._results: Dict[str, str] = {}
    
    async def check_link(self, url: str) -> Tuple[str, str]:
        """Check a single link (simulated)."""
        await asyncio.sleep(0.05)  # Simulate network check
        
        # Simulate various link statuses
        if "404" in url:
            return url, "404 Not Found"
        elif "redirect" in url:
            return url, "301 Redirect"
        elif not URLValidator.is_valid_url(url):
            return url, "Invalid URL"
        else:
            return url, "200 OK"
    
    async def check_links(self, urls: List[str]) -> Dict[str, str]:
        """Check multiple links concurrently."""
        tasks = [self.check_link(url) for url in urls]
        results = await asyncio.gather(*tasks)
        
        self._results = dict(results)
        return self._results
    
    def get_broken_links(self) -> List[str]:
        """Get list of broken links."""
        return [url for url, status in self._results.items() 
                if "404" in status or "Invalid" in status]


# Example usage
async def main():
    """Example of using the web scraper."""
    scraper = WebScraper(max_depth=1)
    
    # Scrape a website
    start_url = "https://example.com"
    pages = await scraper.scrape_website(start_url)
    
    print(f"Scraped {len(pages)} pages")
    for page in pages:
        print(f"\nPage: {page.url}")
        print(f"Title: {page.title}")
        print(f"Links found: {len(page.links)}")
        print(f"Images found: {len(page.images)}")
    
    # Get statistics
    stats = scraper.get_statistics()
    print(f"\nStatistics: {stats}")
    
    # Check links
    checker = LinkChecker()
    all_links = [link for page in pages for link in page.links]
    link_results = await checker.check_links(all_links[:10])  # Check first 10 links
    
    print(f"\nLink check results:")
    for url, status in link_results.items():
        print(f"  {url}: {status}")


if __name__ == "__main__":
    asyncio.run(main())