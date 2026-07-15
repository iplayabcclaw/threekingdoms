#!/usr/bin/env python3
"""Build lightweight officer portraits and labeled review sheets for every officer."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "godot/assets/generated/officer-portraits"
RUNTIME_DIR = ROOT / "godot/assets/runtime/officer-portraits"
METADATA_TOOL = ROOT / "tools/list-officer-portrait-metadata.py"
PREVIEW_DIR = ROOT / "godot/assets/generated"

RUNTIME_SIZE = (384, 576)
PREVIEW_PORTRAIT_SIZE = (160, 240)
PREVIEW_COLUMNS = 7
PREVIEW_PAGE_SIZE = 28


def load_portraits() -> list[tuple[str, str, int]]:
    result = subprocess.run(
        [sys.executable, str(METADATA_TOOL)],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    metadata = json.loads(result.stdout)
    return [(entry["slug"], entry["name"], entry["age"]) for entry in metadata]


def edge_color(image: Image.Image) -> tuple[int, int, int]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    inset_x = max(1, width // 40)
    inset_y = max(1, height // 40)
    samples = (
        rgb.getpixel((inset_x, inset_y)),
        rgb.getpixel((width - inset_x - 1, inset_y)),
        rgb.getpixel((inset_x, height - inset_y - 1)),
        rgb.getpixel((width - inset_x - 1, height - inset_y - 1)),
    )
    return tuple(sum(sample[channel] for sample in samples) // len(samples) for channel in range(3))


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (
        "/System/Library/Fonts/PingFang.ttc",
        "/System/Library/Fonts/STHeiti Medium.ttc",
        "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
    )
    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size=size)
    return ImageFont.load_default()


def build_runtime_portraits(portraits: list[tuple[str, str, int]]) -> list[tuple[str, int, Path]]:
    RUNTIME_DIR.mkdir(parents=True, exist_ok=True)
    outputs: list[tuple[str, int, Path]] = []

    for slug, name, age in portraits:
        source_path = SOURCE_DIR / f"{slug}-source-v1.png"
        if not source_path.exists():
            raise FileNotFoundError(f"missing officer portrait source: {source_path.relative_to(ROOT)}")
        output_path = RUNTIME_DIR / f"{slug}-portrait-v1.webp"
        with Image.open(source_path) as source:
            portrait = ImageOps.pad(
                source.convert("RGB"),
                RUNTIME_SIZE,
                method=Image.Resampling.LANCZOS,
                color=edge_color(source),
                centering=(0.5, 0.5),
            )
            portrait.save(output_path, "WEBP", quality=84, method=6)
        outputs.append((name, age, output_path))

    return outputs


def build_preview_page(outputs: list[tuple[str, int, Path]], page_number: int) -> Path:
    gap = 14
    outer = 22
    label_height = 38
    cell_width, cell_image_height = PREVIEW_PORTRAIT_SIZE
    cell_height = cell_image_height + label_height
    rows = (len(outputs) + PREVIEW_COLUMNS - 1) // PREVIEW_COLUMNS
    width = outer * 2 + PREVIEW_COLUMNS * cell_width + (PREVIEW_COLUMNS - 1) * gap
    height = outer * 2 + rows * cell_height + (rows - 1) * gap
    canvas = Image.new("RGB", (width, height), "#151713")
    draw = ImageDraw.Draw(canvas)
    font = load_font(20)

    for index, (name, age, path) in enumerate(outputs):
        row, column = divmod(index, PREVIEW_COLUMNS)
        x = outer + column * (cell_width + gap)
        y = outer + row * (cell_height + gap)
        with Image.open(path) as portrait:
            preview = portrait.resize(PREVIEW_PORTRAIT_SIZE, Image.Resampling.LANCZOS)
            canvas.paste(preview, (x, y))
        draw.rectangle((x, y + cell_image_height, x + cell_width, y + cell_height), fill="#24271f")
        label = f"{name} · {age}岁"
        box = draw.textbbox((0, 0), label, font=font)
        text_width = box[2] - box[0]
        text_height = box[3] - box[1]
        draw.text(
            (x + (cell_width - text_width) / 2, y + cell_image_height + (label_height - text_height) / 2 - box[1]),
            label,
            font=font,
            fill="#eadbb9",
        )

    path = PREVIEW_DIR / f"officer-portrait-preview-{page_number:02d}-v1.webp"
    canvas.save(path, "WEBP", quality=86, method=6)
    return path


def build_previews(outputs: list[tuple[str, int, Path]]) -> list[Path]:
    return [
        build_preview_page(outputs[start : start + PREVIEW_PAGE_SIZE], page_number)
        for page_number, start in enumerate(range(0, len(outputs), PREVIEW_PAGE_SIZE), start=1)
    ]


def main() -> None:
    portraits = load_portraits()
    outputs = build_runtime_portraits(portraits)
    previews = build_previews(outputs)
    total_bytes = sum(path.stat().st_size for _, _, path in outputs)
    print(f"runtime portraits: {len(outputs)} files, {total_bytes / 1024:.1f} KiB total")
    for path in previews:
        print(f"preview: {path.relative_to(ROOT)} ({path.stat().st_size / 1024:.1f} KiB)")


if __name__ == "__main__":
    main()
