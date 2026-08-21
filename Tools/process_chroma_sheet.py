#!/usr/bin/env python3
"""Convert a 4x4 magenta-key sprite sheet into a transparent 1024px sheet."""

from pathlib import Path
import sys

from PIL import Image


def main() -> None:
    if len(sys.argv) not in (3, 4):
        raise SystemExit("usage: process_chroma_sheet.py INPUT OUTPUT [--reuse-first-as-fourth]")

    reuse_first = len(sys.argv) == 4 and sys.argv[3] == "--reuse-first-as-fourth"

    source = Image.open(sys.argv[1]).convert("RGBA")
    side = min(source.size)
    side -= side % 4
    left = (source.width - side) // 2
    top = (source.height - side) // 2
    source = source.crop((left, top, left + side, top + side))

    pixels = source.load()
    for y in range(source.height):
        for x in range(source.width):
            red, green, blue, _ = pixels[x, y]
            magenta_strength = min(red, blue) - green
            if red > 180 and blue > 180 and magenta_strength > 70:
                alpha = max(0, 255 - magenta_strength * 3)
                pixels[x, y] = (red, green, blue, alpha)

    sheet = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    cell = side // 4
    for row in range(4):
        for column in range(4):
            source_column = 0 if reuse_first and column == 3 else column
            frame = source.crop((source_column * cell, row * cell, (source_column + 1) * cell, (row + 1) * cell))
            frame = frame.resize((256, 256), Image.Resampling.NEAREST)
            sheet.alpha_composite(frame, (column * 256, row * 256))

    output = Path(sys.argv[2])
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, optimize=True)


if __name__ == "__main__":
    main()
