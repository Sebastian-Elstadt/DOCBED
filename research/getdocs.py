#!/usr/bin/env python3
"""
Improved Test Document Downloader for Multimodal RAG
Downloads ~30 diverse complex documents (10-K filings, technical papers with diagrams, reports).

Run with: python download_test_documents.py
"""

import time
from pathlib import Path
from tqdm import tqdm
import requests

# Optional dependencies
try:
    from sec_edgar_downloader import Downloader
    HAS_EDGAR = True
except ImportError:
    HAS_EDGAR = False
    print("sec-edgar-downloader not installed. Financial reports will be skipped.")

try:
    import arxiv
    HAS_ARXIV = True
except ImportError:
    HAS_ARXIV = False
    print("arxiv-py not installed. Some papers will be skipped.")

OUTPUT_DIR = Path("test_documents")
OUTPUT_DIR.mkdir(exist_ok=True)

# Stable, high-value PDFs (complex layouts, charts, diagrams)
DIRECT_URLS = [
    ("https://arxiv.org/pdf/2307.09288.pdf", "attention_is_all_you_need.pdf"),
    ("https://arxiv.org/pdf/2403.05530.pdf", "llama3_technical_report.pdf"),
    ("https://arxiv.org/pdf/2501.12948.pdf", "deepseek_r1.pdf"),
    ("https://arxiv.org/pdf/2305.13048.pdf", "gpt4_technical_report.pdf"),
    ("https://arxiv.org/pdf/2210.03629.pdf", "blip2_vision_language.pdf"),
    ("https://arxiv.org/pdf/2404.05850.pdf", "nemotron_4_technical_report.pdf"),  # Recent NVIDIA paper
    ("https://www.nasa.gov/wp-content/uploads/2025/01/nasa-2025-strategic-plan.pdf", "nasa_strategic_plan_2025.pdf"),
]

HEADERS = {
    "User-Agent": "Mozilla/5.0 (compatible; TestDocumentCollector/1.0; https://github.com/yourname/rag-project)"
}

def download_file(url: str, filename: str) -> bool:
    """Download with proper headers and progress."""
    filepath = OUTPUT_DIR / filename
    if filepath.exists():
        print(f"✓ Already exists: {filename}")
        return True

    try:
        resp = requests.get(url, headers=HEADERS, stream=True, timeout=30)
        resp.raise_for_status()
        total = int(resp.headers.get("content-length", 0))

        with open(filepath, "wb") as f, tqdm(
            desc=filename[:30].ljust(30),
            total=total,
            unit="iB",
            unit_scale=True,
            leave=False,
        ) as pbar:
            for chunk in resp.iter_content(chunk_size=8192):
                size = f.write(chunk)
                pbar.update(size)

        print(f"✓ Downloaded: {filename}")
        time.sleep(0.8)
        return True
    except Exception as e:
        print(f"✗ Failed {filename}: {e}")
        return False


def download_edgar_reports():
    """Download real 10-Ks — best for dense tables and charts."""
    if not HAS_EDGAR:
        print("Skipping EDGAR downloads (install with: pip install sec-edgar-downloader)")
        return

    print("\nDownloading real 10-K financial reports (excellent test data)...")
    dl = Downloader("MacchiaRAGTest", "macchia@example.com", str(OUTPUT_DIR))

    for ticker in ["NVDA", "TSLA", "AAPL", "MSFT", "GOOGL", "AMZN"]:
        try:
            dl.get("10-K", ticker, limit=1, download_details=False)
            print(f"✓ Downloaded 10-K for {ticker}")
        except Exception as e:
            print(f"  Warning for {ticker}: {e}")


def download_arxiv_papers():
    """Download recent technical papers with diagrams using modern API."""
    if not HAS_ARXIV:
        print("Skipping arXiv downloads (install with: pip install arxiv)")
        return

    print("\nDownloading technical papers with diagrams...")
    client = arxiv.Client()

    search = arxiv.Search(
        query="multimodal OR RAG OR vision-language OR diagram OR chart OR blueprint",
        max_results=12,
        sort_by=arxiv.SortCriterion.Relevance,
    )

    for result in client.results(search):
        try:
            pdf_url = result.pdf_url
            if not pdf_url:
                continue
            safe_title = "".join(c for c in result.title.lower()[:40] if c.isalnum() or c in " -_")
            filename = f"arxiv_{result.get_short_uid()}_{safe_title.replace(' ', '_')}.pdf"
            download_file(pdf_url, filename)
        except Exception as e:
            continue  # Skip problematic ones silently


def main():
    print(f"Starting download into ./{OUTPUT_DIR}/\n")

    # Direct stable PDFs
    print("1. Downloading core technical papers and reports...")
    for url, name in DIRECT_URLS:
        download_file(url, name)

    # High-value financial reports
    download_edgar_reports()

    # Additional technical papers
    download_arxiv_papers()

    count = len(list(OUTPUT_DIR.glob("**/*.pdf")))
    print("\n" + "=" * 70)
    print(f"DONE — Collected {count} PDF documents in ./test_documents/")
    print("This set is diverse: dense 10-Ks (tables/charts), AI papers (diagrams), strategic reports.")
    print("\nNext step: Use nemotron-page-elements-v3 on these files.")


if __name__ == "__main__":
    main()