#!/usr/bin/env python3
"""
Download attachments from DCE (Dalian Commodity Exchange) by loading the
SPA iframe page directly and intercepting/clicking the "导出文本" button.

The parent page http://www.dce.com.cn/dce/channel/list/1018.html embeds an
iframe SPA at: http://www.dce.com.cn/frontend/dcereport/#/zh/queryDayTradPara

This script loads the SPA directly, intercepts all network requests to
discover the backend API, then clicks "导出文本" and captures the download.

Prerequisites:
    pip install playwright playwright-stealth
    playwright install chromium

Usage:
    python3 dce_download.py
    python3 dce_download.py --headed
    python3 dce_download.py --variety all --trade-type 1
"""

import os
import json
import glob
import argparse
from playwright.sync_api import sync_playwright, TimeoutError as PlaywrightTimeout

def stealth_sync(page):
    """Apply anti-detection patches without requiring playwright-stealth."""
    page.add_init_script("""
        Object.defineProperty(navigator, 'webdriver', {get: () => undefined});
        window.chrome = {runtime: {}, loadTimes: function(){}, csi: function(){}};
        Object.defineProperty(navigator, 'plugins', {
            get: () => [1, 2, 3, 4, 5]
        });
        Object.defineProperty(navigator, 'languages', {
            get: () => ['zh-CN', 'zh', 'en']
        });
        const originalQuery = window.navigator.permissions.query;
        window.navigator.permissions.query = (parameters) =>
            parameters.name === 'notifications'
                ? Promise.resolve({state: Notification.permission})
                : originalQuery(parameters);
    """)


# Load the SPA iframe page directly (bypasses parent page issues)
SPA_URL = "http://www.dce.com.cn/frontend/dcereport/#/zh/queryDayTradPara?variety={variety}&tradeType={trade_type}"
DOWNLOAD_DIR_NAME = "dce_downloads"


def main():
    parser = argparse.ArgumentParser(description="Download DCE '导出文本' data")
    parser.add_argument("--download-dir", default=os.path.join(
        os.path.dirname(os.path.abspath(__file__)), DOWNLOAD_DIR_NAME
    ))
    parser.add_argument("--headed", action="store_true",
                        help="Show browser window for debugging")
    parser.add_argument("--variety", default="all",
                        help="Variety parameter (default: all)")
    parser.add_argument("--trade-type", default="1",
                        help="Trade type: 0=futures, 1=options (default: 1)")
    args = parser.parse_args()

    download_dir = os.path.abspath(args.download_dir)
    os.makedirs(download_dir, exist_ok=True)

    url = SPA_URL.format(variety=args.variety, trade_type=args.trade_type)

    print(f"[1] Starting {'headed' if args.headed else 'headless'} browser...")
    print(f"    Download dir: {download_dir}")
    print(f"    Target URL:   {url}")

    # Track all API requests the SPA makes
    api_requests = []

    with sync_playwright() as p:
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
        stealth_sync(page)

        # ── Intercept all network requests to discover API endpoints ──
        def on_request(request):
            req_url = request.url
            # Skip static assets
            if any(ext in req_url for ext in ['.js', '.css', '.png', '.jpg', '.gif', '.ico', '.woff', '.svg']):
                return
            api_requests.append({
                "method": request.method,
                "url": req_url,
                "post_data": request.post_data,
                "headers": dict(request.headers),
            })

        def on_response(response):
            req_url = response.url
            if any(ext in req_url for ext in ['.js', '.css', '.png', '.jpg', '.gif', '.ico', '.woff', '.svg']):
                return
            content_type = response.headers.get("content-type", "")
            content_disp = response.headers.get("content-disposition", "")
            if "json" in content_type or "octet" in content_type or "excel" in content_type or content_disp:
                print(f"    [API] {response.request.method} {req_url}")
                print(f"           Content-Type: {content_type}")
                if content_disp:
                    print(f"           Content-Disposition: {content_disp}")
                if response.request.post_data:
                    print(f"           POST data: {response.request.post_data[:200]}")

        page.on("request", on_request)
        page.on("response", on_response)

        # ── Load the SPA page directly ──
        print(f"\n[2] Loading SPA page...")
        try:
            page.goto(url, wait_until="networkidle", timeout=30000)
        except PlaywrightTimeout:
            print("    networkidle timeout, continuing...")

        print(f"    Title: {page.title()}")
        print(f"    URL:   {page.url}")

        # Wait for SPA to render
        page.wait_for_timeout(5000)

        body_len = page.evaluate("document.body ? document.body.innerText.length : 0")
        print(f"    Body text length: {body_len} chars")

        if body_len < 50:
            print("\n[!] SPA page appears blank. Saving debug info...")
            page.screenshot(path=os.path.join(download_dir, "screenshot.png"), full_page=True)
            with open(os.path.join(download_dir, "page_source.html"), "w", encoding="utf-8") as f:
                f.write(page.content())
            save_api_log(api_requests, download_dir)
            print_manual_instructions()

            if args.headed:
                print("\n[Paused] Browser open. Press Enter to close...")
                input()
            browser.close()
            return

        # ── Find and click "导出文本" ──
        print(f"\n[3] Looking for '导出文本' button...")

        selectors = [
            "text=导出文本",
            "button:has-text('导出文本')",
            "a:has-text('导出文本')",
            "span:has-text('导出文本')",
            "div:has-text('导出文本') >> button",
            "text=导出",
            "button:has-text('导出')",
            "a:has-text('导出')",
            "[class*='export']",
            "[class*='download']",
        ]

        export_el = None
        for sel in selectors:
            try:
                loc = page.locator(sel).first
                if loc.count() > 0 and loc.is_visible():
                    html = loc.evaluate("el => el.outerHTML")
                    print(f"  Found: '{sel}'")
                    print(f"    HTML: {html[:300]}")
                    export_el = loc
                    break
            except Exception:
                continue

        if not export_el:
            print("  Not found! Dumping all visible text and elements...")
            visible_text = page.evaluate("document.body.innerText")
            print(f"  Page text (first 1000 chars):\n{visible_text[:1000]}")
            page.screenshot(path=os.path.join(download_dir, "screenshot.png"), full_page=True)
            with open(os.path.join(download_dir, "page_source.html"), "w", encoding="utf-8") as f:
                f.write(page.content())
            save_api_log(api_requests, download_dir)

            if args.headed:
                print("\n[Paused] Browser open. Press Enter to close...")
                input()
            browser.close()
            return

        # ── Click and capture the download ──
        print(f"\n[4] Clicking '导出文本' and capturing response...")

        # Clear api_requests to only capture export-related ones
        pre_click_count = len(api_requests)

        # Try expect_download first
        downloaded = False
        try:
            with page.expect_download(timeout=10000) as dl_info:
                export_el.click()
            download = dl_info.value
            filename = download.suggested_filename or "dce_export.txt"
            save_path = os.path.join(download_dir, filename)
            download.save_as(save_path)
            print(f"\n[OK] Downloaded: {save_path} ({os.path.getsize(save_path):,} bytes)")
            downloaded = True
        except PlaywrightTimeout:
            print("  No download event. Checking for new API calls...")

        if not downloaded:
            # Check what new API requests were made after click
            page.wait_for_timeout(3000)
            new_requests = api_requests[pre_click_count:]
            if new_requests:
                print(f"\n  New API requests after click ({len(new_requests)}):")
                for r in new_requests:
                    print(f"    {r['method']} {r['url']}")
                    if r['post_data']:
                        print(f"      POST: {r['post_data'][:300]}")

            # Try popup
            try:
                new_pages = context.pages
                if len(new_pages) > 1:
                    new_page = new_pages[-1]
                    print(f"  New tab opened: {new_page.url}")
                    new_page.wait_for_load_state("domcontentloaded", timeout=5000)
                    content = new_page.content()
                    save_path = os.path.join(download_dir, "dce_export.html")
                    with open(save_path, "w", encoding="utf-8") as f:
                        f.write(content)
                    print(f"  Saved: {save_path}")
                    downloaded = True
            except Exception:
                pass

            # Check download dir for new files
            if not downloaded:
                files = [f for f in glob.glob(os.path.join(download_dir, "*"))
                         if not f.endswith((".html", ".png", ".json"))]
                if files:
                    latest = max(files, key=os.path.getmtime)
                    print(f"\n[OK] File found: {latest} ({os.path.getsize(latest):,} bytes)")
                    downloaded = True

        if not downloaded:
            print("\n[!] No download captured.")
            page.screenshot(path=os.path.join(download_dir, "after_click.png"), full_page=True)
            with open(os.path.join(download_dir, "after_click_source.html"), "w", encoding="utf-8") as f:
                f.write(page.content())

        # ── Save API log ──
        save_api_log(api_requests, download_dir)

        print(f"\n[5] Total API requests captured: {len(api_requests)}")
        print("    See api_requests.json for full details.")

        if args.headed:
            print("\n[Paused] Browser open. Press Enter to close...")
            input()

        browser.close()

    # List files
    print(f"\nFiles in {download_dir}:")
    for fname in sorted(os.listdir(download_dir)):
        fpath = os.path.join(download_dir, fname)
        print(f"  {fname} ({os.path.getsize(fpath):,} bytes)")


def save_api_log(api_requests, download_dir):
    """Save captured API requests to JSON for analysis."""
    log_path = os.path.join(download_dir, "api_requests.json")
    with open(log_path, "w", encoding="utf-8") as f:
        json.dump(api_requests, f, indent=2, ensure_ascii=False)
    print(f"  API log saved: {log_path}")


def print_manual_instructions():
    print("""
================================================================
MANUAL APPROACH (if automated browser is blocked)
================================================================

The site blocks automated browsers. Try this instead:

1. Open Chrome manually, go to:
   http://www.dce.com.cn/frontend/dcereport/#/zh/queryDayTradPara?variety=all&tradeType=1

2. Open DevTools (F12) -> Network tab -> check "Preserve log"

3. Click "导出文本"

4. Look at the Network tab for the new request:
   - Note the URL (e.g., /publicweb/...)
   - Note the Method (GET or POST)
   - Click the request -> "Payload" tab for POST data
   - Click "Headers" tab for request headers
   - Right-click the request -> "Copy as cURL"

5. Paste the cURL command here, and I'll convert it to a
   Python script that works without a browser.
""")


if __name__ == "__main__":
    main()
