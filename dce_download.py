#!/usr/bin/env python3
"""
Download attachments from DCE (Dalian Commodity Exchange) notification page
by automating the "导出文本" (Export Text) button click.

URL: http://www.dce.com.cn/dce/channel/list/1018.html

Prerequisites:
    pip install playwright playwright-stealth
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
from playwright_stealth import stealth_sync


PAGE_URL = "http://www.dce.com.cn/dce/channel/list/1018.html"
DOWNLOAD_DIR_NAME = "dce_downloads"


def find_export_button(page):
    """Find the '导出文本' element on the page using multiple strategies."""
    # Also search inside iframes
    all_frames = [page] + page.frames

    for frame in all_frames:
        selectors = [
            "a:has-text('导出文本')",
            "button:has-text('导出文本')",
            "span:has-text('导出文本')",
            "text=导出文本",
            "a:has-text('导出')",
            "button:has-text('导出')",
            "[onclick*='export']",
            "[onclick*='Export']",
            "[onclick*='导出']",
        ]
        for sel in selectors:
            try:
                loc = frame.locator(sel).first
                if loc.count() > 0:
                    html = loc.evaluate("el => el.outerHTML")
                    frame_info = f" (in iframe: {frame.url})" if frame != page else ""
                    print(f"  Found with selector '{sel}'{frame_info}:")
                    print(f"    {html[:300]}")
                    return loc, frame
            except Exception:
                continue
    return None, None


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
        # Launch with anti-detection flags
        browser = p.chromium.launch(
            headless=not args.headed,
            args=[
                "--disable-blink-features=AutomationControlled",
                "--disable-infobars",
                "--no-first-run",
                "--no-default-browser-check",
            ],
        )
        context = browser.new_context(
            accept_downloads=True,
            locale="zh-CN",
            viewport={"width": 1920, "height": 1080},
            user_agent=(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/120.0.0.0 Safari/537.36"
            ),
        )
        page = context.new_page()

        # Apply stealth patches to avoid bot detection
        stealth_sync(page)

        print(f"\n[2] Loading: {PAGE_URL}")
        try:
            page.goto(PAGE_URL, wait_until="networkidle", timeout=30000)
        except PlaywrightTimeout:
            print("    networkidle timeout, trying domcontentloaded...")
            try:
                page.goto(PAGE_URL, wait_until="domcontentloaded", timeout=30000)
            except PlaywrightTimeout:
                print("    domcontentloaded also timed out, continuing anyway...")

        print(f"    Title: {page.title()}")
        print(f"    URL:   {page.url}")

        # Give dynamic content time to load
        page.wait_for_timeout(5000)

        # Check if page actually loaded (not blank/blocked)
        body_text = page.evaluate("document.body ? document.body.innerText.length : 0")
        print(f"    Page body text length: {body_text} chars")

        if body_text < 50:
            print("\n[!] Page appears blank or blocked.")
            print("    The site may be using advanced bot protection (WAF/JS challenge).")
            print("\n    Saving debug artifacts...")
            analyze_page(page, download_dir)

            print("\n" + "=" * 60)
            print("ALTERNATIVE: Use your real browser + DevTools")
            print("=" * 60)
            print("""
Since the site blocks automated browsers, try this approach:

1. Open the page manually in Chrome:
   http://www.dce.com.cn/dce/channel/list/1018.html

2. Open DevTools (F12) -> Console tab

3. Paste this script to find the export button:

   document.querySelectorAll('*').forEach(el => {
     let t = el.textContent;
     if (t.includes('导出') && el.children.length === 0) {
       console.log('TAG:', el.tagName, 'TEXT:', el.textContent.trim());
       console.log('HTML:', el.outerHTML);
       console.log('onclick:', el.getAttribute('onclick'));
       console.log('href:', el.getAttribute('href'));
       console.log('---');
     }
   });

4. Then paste the output here, and I'll write a direct
   requests-based script using the exact URL/params.

ALTERNATIVE 2: Use your system Chrome profile (not Playwright's):

   Run this in terminal:
   google-chrome --remote-debugging-port=9222

   Then re-run this script with:
   python3 dce_download.py --use-cdp ws://127.0.0.1:9222
""")
        else:
            print(f"\n[3] Searching for '导出文本' button...")
            locator, frame = find_export_button(page)

            if locator:
                print(f"\n[4] Attempting download...")
                target_page = page if frame == page else frame
                success = try_download_via_click(target_page, locator, download_dir)
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
