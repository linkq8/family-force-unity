#!/usr/bin/env python3
"""Convert a 4x4 magenta-key sprite sheet into a transparent 1024px sheet."""

from pathlib import Path
import sys

from PIL import Image


def keep_largest_component(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    width, height = image.size
    occupied = [value > 12 for value in alpha.getdata()]
    visited = bytearray(width * height)
    components = []

    for start in range(width * height):
        if not occupied[start] or visited[start]:
            continue
        visited[start] = 1
        stack = [start]
        component = []
        while stack:
            index = stack.pop()
            component.append(index)
            x, y = index % width, index // width
            for neighbor in (index - 1 if x else -1, index + 1 if x + 1 < width else -1,
                             index - width if y else -1, index + width if y + 1 < height else -1):
                if neighbor >= 0 and occupied[neighbor] and not visited[neighbor]:
                    visited[neighbor] = 1
                    stack.append(neighbor)
        components.append(component)

    largest = max(components, key=len, default=[])
    if not largest:
        return image
    main_x = [index % width for index in largest]
    main_y = [index // width for index in largest]
    main_box = (min(main_x), min(main_y), max(main_x), max(main_y))
    keep = bytearray(width * height)
    for component in components:
        xs = [index % width for index in component]
        ys = [index // width for index in component]
        box = (min(xs), min(ys), max(xs), max(ys))
        horizontal_gap = max(0, main_box[0] - box[2], box[0] - main_box[2])
        vertical_gap = max(0, main_box[1] - box[3], box[1] - main_box[3])
        if component is largest or (len(component) >= 3 and horizontal_gap <= 12 and vertical_gap <= 12):
            for index in component:
                keep[index] = 255
    cleaned = image.copy()
    original_alpha = image.getchannel("A").tobytes()
    cleaned.putalpha(Image.frombytes("L", image.size,
        bytes(alpha_value if keep[index] else 0 for index, alpha_value in enumerate(original_alpha))))
    return cleaned


def main() -> None:
    if len(sys.argv) not in (3, 4):
        raise SystemExit("usage: process_chroma_sheet.py INPUT OUTPUT [--normalize-frames]")

    normalize_frames = len(sys.argv) == 4 and sys.argv[3] == "--normalize-frames"

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
            if max(red, green, blue) < 28:
                pixels[x, y] = (0, 0, 0, 0)
            elif red > 180 and blue > 180 and magenta_strength > 70:
                alpha = max(0, 255 - magenta_strength * 3)
                pixels[x, y] = (red, green, blue, alpha)

    sheet = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    cell = side // 4
    x_cuts = [0, cell, cell * 2, cell * 3, side]
    y_cuts = [0, cell, cell * 2, cell * 3, side]
    if normalize_frames:
        alpha = source.getchannel("A")

        def least_occupied_cut(nominal: int, vertical: bool) -> int:
            candidates = range(max(1, nominal - 18), min(side - 1, nominal + 19))
            if vertical:
                return min(candidates, key=lambda value: sum(alpha.crop((value, 0, value + 1, side)).getdata()))
            return min(candidates, key=lambda value: sum(alpha.crop((0, value, side, value + 1)).getdata()))

        x_cuts[1:4] = [least_occupied_cut(cell * index, True) for index in range(1, 4)]
        y_cuts[1:4] = [least_occupied_cut(cell * index, False) for index in range(1, 4)]

    for row in range(4):
        for column in range(4):
            frame = source.crop((x_cuts[column], y_cuts[row], x_cuts[column + 1], y_cuts[row + 1]))
            if normalize_frames:
                bounds = frame.getchannel("A").getbbox()
                if bounds is not None:
                    frame = frame.crop(bounds)
                    scale = min(224 / frame.width, 236 / frame.height)
                    frame = frame.resize((max(1, round(frame.width * scale)), max(1, round(frame.height * scale))), Image.Resampling.NEAREST)
                    normalized = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
                    normalized.alpha_composite(frame, ((256 - frame.width) // 2, 248 - frame.height))
                    frame = keep_largest_component(normalized)
            else:
                frame = frame.resize((256, 256), Image.Resampling.NEAREST)
            sheet.alpha_composite(frame, (column * 256, row * 256))

    output = Path(sys.argv[2])
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, optimize=True)


if __name__ == "__main__":
    main()
