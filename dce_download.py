#!/usr/bin/env python3
"""
Download attachments from DCE (Dalian Commodity Exchange) notification page
by automating the "导出文本" (Export Text) button click.

URL: http://www.dce.com.cn/dce/channel/list/1018.html

Prerequisites:
    pip install playwright
    playwright install chromium

Usage:
    python3 dce_download.py
    python3 dce_download.py --download-dir /path/to/save
    python3 dce_download.py --headed          # show browser window (useful for debugging)
"""

import os
import sys
import time
import glob
import argparse
from playwright.sync_api import sync_playwright, TimeoutError as PlaywrightTimeout


PAGE_URL = "http://www.dce.com.cn/dce/channel/list/1018.html"
DOWNLOAD_DIR_NAME = "dce_downloads"


def find_export_button(page):
    """Find the '导出文本' element on the page using multiple strategies."""
    selectors = [
        "a:has-text('导出文本')",
        "button:has-text('导出文本')",
        "span:has-text('导出文本')",
        "text=导出文本",
        "a:has-text('导出')",
        "button:has-text('导出')",
        "[onclick*='export']",
    ]
    for sel in selectors:
        loc = page.locator(sel).first
        if loc.count() > 0:
            html = loc.evaluate("el => el.outerHTML")
            print(f"  Found with selector '{sel}':")
            print(f"    {html[:300]}")
            return loc
    return None


def try_download_via_click(page, locator, download_dir):
    """Click the button and handle: download event, new tab, or navigation."""

    # ── Attempt 1: expect a download event ──
    print("  Trying: expect download event...")
    try:
        with page.expect_download(timeout=8000) as dl_info:
            locator.click()
        download = dl_info.value
        filename = download.suggested_filename or "dce_export.txt"
        save_path = os.path.join(download_dir, filename)
        download.save_as(save_path)
        print(f"  [OK] Downloaded via download event: {save_path} "
              f"({os.path.getsize(save_path)} bytes)")
        return True
    except PlaywrightTimeout:
        print("    No download event fired.")

    # ── Attempt 2: expect a new popup / tab ──
    print("  Trying: expect new popup/tab...")
    try:
        with page.context.expect_page(timeout=5000) as new_page_info:
            locator.click()
        new_page = new_page_info.value
        new_page.wait_for_load_state("domcontentloaded", timeout=10000)
        url = new_page.url
        print(f"    New page opened: {url}")

        # If the new page is a file, its content is the download
        content = new_page.content()
        save_path = os.path.join(download_dir, "dce_export.txt")
        with open(save_path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"  [OK] Saved new page content: {save_path}")
        new_page.close()
        return True
    except PlaywrightTimeout:
        print("    No new page/popup opened.")

    # ── Attempt 3: plain click, check for navigation or JS-generated content ──
    print("  Trying: plain click...")
    old_url = page.url
    locator.click()
    page.wait_for_timeout(3000)

    new_url = page.url
    if new_url != old_url:
        print(f"    Page navigated to: {new_url}")
        content = page.content()
        save_path = os.path.join(download_dir, "dce_export.html")
        with open(save_path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"  [OK] Saved navigated page: {save_path}")
        return True

    # Check if any files appeared in download dir
    files = glob.glob(os.path.join(download_dir, "*"))
    non_debug = [f for f in files if not f.endswith((".html", ".png"))]
    if non_debug:
        latest = max(non_debug, key=os.path.getmtime)
        print(f"  [OK] File appeared in download dir: {latest}")
        return True

    print("    Plain click did not produce a visible result.")
    return False


def analyze_page(page, download_dir):
    """Dump debug info: screenshot, source, all export-related elements."""
    print("\n[DEBUG] Saving page artifacts...")

    screenshot = os.path.join(download_dir, "screenshot.png")
    page.screenshot(path=screenshot, full_page=True)
    print(f"  Screenshot: {screenshot}")

    source = os.path.join(download_dir, "page_source.html")
    with open(source, "w", encoding="utf-8") as f:
        f.write(page.content())
    print(f"  Page source: {source}")

    # List all elements with '导出' text
    elements = page.query_selector_all("//*[contains(text(), '导出')]")
    print(f"\n  All elements containing '导出' ({len(elements)}):")
    for i, el in enumerate(elements):
        info = el.evaluate("""el => ({
            tag: el.tagName,
            text: el.textContent.trim().substring(0, 60),
            onclick: el.getAttribute('onclick') || '',
            href: el.getAttribute('href') || '',
            html: el.outerHTML.substring(0, 200)
        })""")
        print(f"    [{i}] <{info['tag']}> text='{info['text']}'")
        if info["onclick"]:
            print(f"        onclick: {info['onclick']}")
        if info["href"]:
            print(f"        href: {info['href']}")
        print(f"        html: {info['html']}")

    # Search JS for export-related functions
    print("\n  Searching inline scripts for 'export'/'导出'...")
    scripts = page.query_selector_all("script:not([src])")
    for i, s in enumerate(scripts):
        content = s.evaluate("el => el.textContent")
        if content and any(kw in content.lower() for kw in ["export", "导出", "blob", "saveas"]):
            print(f"    Script #{i}: {content[:500]}")

    # Also list external script URLs
    ext_scripts = page.query_selector_all("script[src]")
    print(f"\n  External scripts ({len(ext_scripts)}):")
    for s in ext_scripts:
        src = s.evaluate("el => el.src")
        print(f"    {src}")

    # Check iframes
    frames = page.frames
    if len(frames) > 1:
        print(f"\n  Iframes ({len(frames) - 1}):")
        for f in frames[1:]:
            print(f"    {f.url}")

    print(f"\n[TIP] Open {screenshot} and {source} to inspect the page visually.")
    print("[TIP] Run with --headed to see the browser and interact manually.")


def main():
    parser = argparse.ArgumentParser(description="Download DCE '导出文本' attachment")
    parser.add_argument("--download-dir", default=os.path.join(
        os.path.dirname(os.path.abspath(__file__)), DOWNLOAD_DIR_NAME
    ))
    parser.add_argument("--headed", action="store_true",
                        help="Show browser window (useful for debugging)")
    args = parser.parse_args()

    download_dir = os.path.abspath(args.download_dir)
    os.makedirs(download_dir, exist_ok=True)

    print(f"[1] Starting {'headed' if args.headed else 'headless'} browser...")
    print(f"    Download dir: {download_dir}")

    with sync_playwright() as p:
        browser = p.chromium.launch(
            headless=not args.headed,
        )
        context = browser.new_context(
            accept_downloads=True,
            locale="zh-CN",
        )
        # Set download path for headless mode
        page = context.new_page()

        print(f"\n[2] Loading: {PAGE_URL}")
        try:
            page.goto(PAGE_URL, wait_until="networkidle", timeout=30000)
        except PlaywrightTimeout:
            print("    networkidle timeout, continuing anyway...")

        print(f"    Title: {page.title()}")
        print(f"    URL:   {page.url}")

        # Give dynamic content time to load
        page.wait_for_timeout(2000)

        print(f"\n[3] Searching for '导出文本' button...")
        locator = find_export_button(page)

        if locator:
            print(f"\n[4] Attempting download...")
            success = try_download_via_click(page, locator, download_dir)
            if success:
                print("\n[Done] Download successful!")
            else:
                print("\n[!] Click did not produce a download.")
                analyze_page(page, download_dir)
        else:
            print("  Button not found!")
            analyze_page(page, download_dir)

        if args.headed:
            print("\n[Paused] Browser is open. Press Enter to close...")
            input()

        browser.close()

    # List final download dir contents
    print(f"\nFiles in {download_dir}:")
    for fname in sorted(os.listdir(download_dir)):
        fpath = os.path.join(download_dir, fname)
        size = os.path.getsize(fpath)
        print(f"  {fname} ({size:,} bytes)")


if __name__ == "__main__":
    main()
