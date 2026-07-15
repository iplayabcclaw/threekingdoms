#!/usr/bin/env python3
"""Split generated chroma-key atlases into normalized transparent map markers."""

from __future__ import annotations

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = Path("/tmp/three-kingdoms-map-markers")
OUTPUT_DIR = ROOT / "godot/assets/runtime/map-markers"
CANVAS_SIZE = 512
CONTENT_SIZE = 478
GUTTER = 12

SHEETS = (
    ("city-central-alpha.png", 2, 1, ("city-central-1-v1.webp", "city-central-2-v1.webp")),
    ("city-north-alpha.png", 2, 1, ("city-north-1-v1.webp", "city-north-2-v1.webp")),
    ("city-northeast-alpha.png", 2, 1, ("city-northeast-1-v1.webp", "city-northeast-2-v1.webp")),
    ("city-jiangnan-alpha.png", 2, 1, ("city-jiangnan-1-v1.webp", "city-jiangnan-2-v1.webp")),
    ("city-south-alpha.png", 2, 1, ("city-south-1-v1.webp", "city-south-2-v1.webp")),
    ("city-xiliang-alpha.png", 2, 1, ("city-xiliang-1-v1.webp", "city-xiliang-2-v1.webp")),
    ("city-nanman-alpha.png", 2, 1, ("city-nanman-1-v1.webp", "city-nanman-2-v1.webp")),
    (
        "pass-markers-alpha.png",
        2,
        2,
        (
            "pass-mountain-v1.webp",
            "pass-northern-v1.webp",
            "pass-river-v1.webp",
            "pass-southern-v1.webp",
        ),
    ),
)


def extract_marker(sheet: Image.Image, column: int, row: int, columns: int, rows: int) -> Image.Image:
    cell_width = sheet.width / columns
    cell_height = sheet.height / rows
    left = round(column * cell_width) + (GUTTER if column else 0)
    top = round(row * cell_height) + (GUTTER if row else 0)
    right = round((column + 1) * cell_width) - (GUTTER if column < columns - 1 else 0)
    bottom = round((row + 1) * cell_height) - (GUTTER if row < rows - 1 else 0)
    cell = sheet.crop((left, top, right, bottom)).convert("RGBA")
    bounds = cell.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"empty marker at cell ({column}, {row})")

    marker = cell.crop(bounds)
    scale = min(CONTENT_SIZE / marker.width, CONTENT_SIZE / marker.height)
    marker = marker.resize(
        (max(1, round(marker.width * scale)), max(1, round(marker.height * scale))),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - marker.width) // 2
    y = CANVAS_SIZE - marker.height - 14
    canvas.alpha_composite(marker, (x, y))
    return canvas


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    written = 0
    for source_name, columns, rows, output_names in SHEETS:
        sheet = Image.open(SOURCE_DIR / source_name).convert("RGBA")
        if len(output_names) != columns * rows:
            raise ValueError(f"{source_name}: output count does not match grid")
        for index, output_name in enumerate(output_names):
            marker = extract_marker(sheet, index % columns, index // columns, columns, rows)
            corners = (
                marker.getpixel((0, 0))[3],
                marker.getpixel((CANVAS_SIZE - 1, 0))[3],
                marker.getpixel((0, CANVAS_SIZE - 1))[3],
                marker.getpixel((CANVAS_SIZE - 1, CANVAS_SIZE - 1))[3],
            )
            if corners != (0, 0, 0, 0):
                raise ValueError(f"{output_name}: non-transparent corner detected: {corners}")
            output_path = OUTPUT_DIR / output_name
            temporary_path = output_path.with_name(f".{output_path.name}.tmp.webp")
            marker.save(temporary_path, "WEBP", quality=92, method=4)
            temporary_path.replace(output_path)
            written += 1
    print(f"wrote {written} normalized transparent map markers to {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
