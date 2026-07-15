#!/usr/bin/env python3
"""Build normalized terrain backgrounds and transparent 4x2 troop atlases."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "godot/assets/generated/battle"
BACKGROUND_DIR = ROOT / "godot/assets/runtime/battle/backgrounds"
TROOP_DIR = ROOT / "godot/assets/runtime/battle/troops"
OFFICER_DIR = ROOT / "godot/assets/runtime/battle/officers"

BACKGROUNDS = (
    "battlefield-plain-bg-v1",
    "battlefield-hill-bg-v1",
    "battlefield-river-bg-v1",
    "battlefield-mountain-bg-v1",
    "siege-central-bg-v1",
    "siege-xiliang-bg-v1",
    "siege-jiangnan-bg-v1",
    "siege-nanman-bg-v1",
)

TROOPS = {
    "infantry": SOURCE_DIR / "infantry-sprite-alpha-v1.png",
    "spears": SOURCE_DIR / "spears-sprite-alpha-v1.png",
    "archers": SOURCE_DIR / "archers-sprite-alpha-v1.png",
    "cavalry": SOURCE_DIR / "cavalry-sprite-alpha-v1.png",
    "siege": SOURCE_DIR / "siege-sprite-alpha-v1.png",
}

OFFICERS = {
    name: SOURCE_DIR / "officers" / f"{name}-mounted-alpha-v1.png"
    for name in (
        "generic-vanguard",
        "generic-commander",
        "generic-strategist",
        "liu-bei",
        "guan-yu",
        "zhang-fei",
        "lu-bu",
    )
}

CELL_SIZE = 256
CONTENT_SIZE = 238


def build_troop_atlas(source_path: Path, output_path: Path) -> None:
    with Image.open(source_path) as source:
        sheet = source.convert("RGBA")
    source_cell_width = sheet.width / 4
    source_cell_height = sheet.height / 2
    atlas = Image.new("RGBA", (CELL_SIZE * 4, CELL_SIZE * 2), (0, 0, 0, 0))

    for frame in range(8):
        column, row = frame % 4, frame // 4
        cell = sheet.crop((
            round(column * source_cell_width),
            round(row * source_cell_height),
            round((column + 1) * source_cell_width),
            round((row + 1) * source_cell_height),
        ))
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise ValueError(f"{source_path.name}: empty frame {frame + 1}")
        subject = cell.crop(bounds)
        scale = min(CONTENT_SIZE / subject.width, CONTENT_SIZE / subject.height)
        subject = subject.resize((max(1, round(subject.width * scale)), max(1, round(subject.height * scale))), Image.Resampling.LANCZOS)
        frame_canvas = Image.new("RGBA", (CELL_SIZE, CELL_SIZE), (0, 0, 0, 0))
        x = (CELL_SIZE - subject.width) // 2
        y = CELL_SIZE - subject.height - 7
        frame_canvas.alpha_composite(subject, (x, y))
        atlas.alpha_composite(frame_canvas, (column * CELL_SIZE, row * CELL_SIZE))

    atlas.save(output_path, "WEBP", quality=92, method=6)


def main() -> None:
    BACKGROUND_DIR.mkdir(parents=True, exist_ok=True)
    TROOP_DIR.mkdir(parents=True, exist_ok=True)
    OFFICER_DIR.mkdir(parents=True, exist_ok=True)
    for name in BACKGROUNDS:
        with Image.open(SOURCE_DIR / f"{name}.png") as source:
            runtime = ImageOps.fit(source.convert("RGB"), (1600, 900), method=Image.Resampling.LANCZOS, centering=(0.5, 0.5))
            runtime.save(BACKGROUND_DIR / f"{name}.webp", "WEBP", quality=87, method=6)
    for troop_id, source_path in TROOPS.items():
        build_troop_atlas(source_path, TROOP_DIR / f"{troop_id}-battle-sprite-v1.webp")
    for officer_id, source_path in OFFICERS.items():
        build_troop_atlas(source_path, OFFICER_DIR / f"{officer_id}-mounted-sprite-v1.webp")
    print(f"wrote {len(BACKGROUNDS)} backgrounds, {len(TROOPS)} troop atlases and {len(OFFICERS)} mounted officer atlases")


if __name__ == "__main__":
    main()
