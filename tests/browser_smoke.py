"""Run against the built game. Requires Python Playwright and Chromium.
Usage: python tests/browser_smoke.py http://localhost:8088
Screenshots are saved under docs/screenshots for release evidence.
"""
import json
import sys
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

base = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8088"
out = Path(__file__).resolve().parents[1] / "docs" / "screenshots"
out.mkdir(parents=True, exist_ok=True)
errors = []
results = []

with sync_playwright() as p:
    browser = p.chromium.launch(args=["--enable-webgl", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--no-sandbox"])
    page = browser.new_page(viewport={"width": 1280, "height": 960})
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.goto(base, wait_until="networkidle", timeout=120000)
    page.wait_for_function("!document.getElementById('start').disabled", timeout=120000)
    page.screenshot(path=str(out / "desktop-ready.png"), full_page=True)
    results.append("Unity loader completed")
    page.locator("#start").click()
    page.wait_for_function("document.getElementById('overlay').classList.contains('hidden')")
    page.wait_for_function("document.getElementById('time').textContent !== '۶۰'", timeout=15000)
    page.keyboard.down("ArrowRight")
    page.wait_for_timeout(450)
    page.keyboard.up("ArrowRight")
    page.keyboard.down("ArrowLeft")
    page.wait_for_timeout(450)
    page.keyboard.up("ArrowLeft")
    page.locator("#pause").click()
    page.wait_for_function("document.getElementById('pause').textContent === 'ادامه'")
    paused_time = page.locator("#time").inner_text()
    page.wait_for_timeout(1500)
    assert page.locator("#time").inner_text() == paused_time, "Pause did not freeze timer"
    results.append("Start, keyboard input dispatch, pause and frozen timer")
    page.locator("#start").click()
    page.wait_for_function("document.getElementById('overlay').classList.contains('hidden')")
    page.locator("#sound").click()
    assert page.locator("#sound").get_attribute("aria-pressed") == "true"
    results.append("Resume and sound toggle")
    page.screenshot(path=str(out / "desktop-playing.png"), full_page=True)
    page.wait_for_function("document.getElementById('start').textContent === 'دوباره بازی کن'", timeout=85000)
    results.append("Round reaches end state")
    page.locator("#start").click()
    page.wait_for_function("document.getElementById('score').textContent === '۰' && document.getElementById('time').textContent === '۶۰'")
    results.append("Restart resets score and timer")
    page.close()

    context = browser.new_context(viewport={"width": 390, "height": 844}, is_mobile=True, has_touch=True, device_scale_factor=1)
    mobile = context.new_page()
    mobile.on("pageerror", lambda error: errors.append(str(error)))
    mobile.goto(base, wait_until="networkidle", timeout=120000)
    mobile.wait_for_function("!document.getElementById('start').disabled", timeout=120000)
    assert mobile.evaluate("document.documentElement.scrollWidth <= innerWidth"), "Horizontal overflow"
    mobile.screenshot(path=str(out / "mobile-ready.png"), full_page=True)
    mobile.locator("#start").tap()
    mobile.wait_for_function("document.getElementById('overlay').classList.contains('hidden')")
    box = mobile.locator("#unity-canvas").bounding_box()
    mobile.touchscreen.tap(box["x"] + box["width"] * .8, box["y"] + box["height"] * .8)
    mobile.wait_for_timeout(1200)
    mobile.locator("#pause").tap()
    mobile.wait_for_function("document.getElementById('pause').textContent === 'ادامه'")
    results.append("Mobile emulation: load, responsive width, touch dispatch, pause")
    browser.close()

report = {"results": results, "browser_errors": errors, "note": "Mobile is emulated Chromium, not a physical Android/iOS device."}
(out.parent / "browser-test-results.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False), flush=True)
assert not errors, errors
