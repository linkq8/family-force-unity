#!/usr/bin/env python3
"""Convert a 4x4 magenta-key sprite sheet into a transparent 1024px sheet."""

from pathlib import Path
import sys

from PIL import Image, ImageFilter


def remove_edge_connected_black(image: Image.Image) -> None:
    """Remove only dark background reachable from the sheet edge, preserving black costume/hair pixels."""
    width, height = image.size
    pixels = image.load()
    visited = bytearray(width * height)
    stack = []
    for x in range(width):
        stack.extend((x, (height - 1) * width + x))
    for y in range(height):
        stack.extend((y * width, y * width + width - 1))
    while stack:
        index = stack.pop()
        if visited[index]:
            continue
        visited[index] = 1
        x, y = index % width, index // width
        red, green, blue, alpha = pixels[x, y]
        traversable = alpha == 0 or max(red, green, blue) < 42
        if not traversable:
            continue
        if alpha != 0:
            pixels[x, y] = (0, 0, 0, 0)
        if x: stack.append(index - 1)
        if x + 1 < width: stack.append(index + 1)
        if y: stack.append(index - width)
        if y + 1 < height: stack.append(index + width)


def keep_largest_component(image: Image.Image, focus_box=None) -> Image.Image:
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
        center_x = (box[0] + box[2]) * 0.5
        center_y = (box[1] + box[3]) * 0.5
        centered = focus_box is None or (focus_box[0] <= center_x < focus_box[2] and focus_box[1] <= center_y < focus_box[3])
        nearby = horizontal_gap <= 12 and vertical_gap <= 12
        # A separated head, hand, or shoe is still a substantial component. Tiny components are
        # chroma/black-key debris (usually visible as dots beneath the feet) and must not survive.
        meaningful_detail = len(component) >= 48
        if component is largest or (centered and meaningful_detail and (focus_box is not None or nearby)):
            for index in component:
                keep[index] = 255
    cleaned = image.copy()
    original_alpha = image.getchannel("A").tobytes()
    cleaned.putalpha(Image.frombytes("L", image.size,
        bytes(alpha_value if keep[index] else 0 for index, alpha_value in enumerate(original_alpha))))
    return cleaned


def restore_enclosed_dark_details(image: Image.Image) -> Image.Image:
    """Restore dark clothing holes enclosed by the visible silhouette without filling open body gaps."""
    alpha = image.getchannel("A")
    binary = alpha.point(lambda value: 255 if value > 12 else 0)
    closed = binary.filter(ImageFilter.MaxFilter(15)).filter(ImageFilter.MinFilter(15))
    width, height = image.size
    mask = closed.load()
    outside = bytearray(width * height)
    stack = []
    for x in range(width): stack.extend((x, (height - 1) * width + x))
    for y in range(height): stack.extend((y * width, y * width + width - 1))
    while stack:
        index = stack.pop()
        if outside[index]: continue
        x, y = index % width, index // width
        if mask[x, y] != 0: continue
        outside[index] = 1
        if x: stack.append(index - 1)
        if x + 1 < width: stack.append(index + 1)
        if y: stack.append(index - width)
        if y + 1 < height: stack.append(index + width)

    restored = image.copy()
    pixels = restored.load()
    for index in range(width * height):
        x, y = index % width, index // width
        if pixels[x, y][3] == 0 and (mask[x, y] != 0 or not outside[index]):
            pixels[x, y] = (8, 8, 8, 255)
    return restored


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
            if red > 180 and blue > 180 and magenta_strength > 70:
                alpha = max(0, 255 - magenta_strength * 3)
                pixels[x, y] = (red, green, blue, alpha)

    remove_edge_connected_black(source)

    sheet = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    cell = side // 4
    x_cuts = [0, cell, cell * 2, cell * 3, side]
    y_cuts = [0, cell, cell * 2, cell * 3, side]
    # Keep the authored grid boundaries exact. Searching for low-occupancy cuts can move a row
    # boundary into a dark-haired head and was the cause of missing heads during punch frames.

    for row in range(4):
        for column in range(4):
            if normalize_frames:
                margin = 42
                crop_left = max(0, x_cuts[column] - margin)
                crop_top = max(0, y_cuts[row] - margin)
                frame = source.crop((crop_left, crop_top,
                    min(side, x_cuts[column + 1] + margin), min(side, y_cuts[row + 1] + margin)))
                focus = (x_cuts[column] - crop_left, y_cuts[row] - crop_top,
                    x_cuts[column + 1] - crop_left, y_cuts[row + 1] - crop_top)
                frame = keep_largest_component(frame, focus)
            else:
                frame = source.crop((x_cuts[column], y_cuts[row], x_cuts[column + 1], y_cuts[row + 1]))
            if normalize_frames:
                bounds = frame.getchannel("A").getbbox()
                if bounds is not None:
                    frame = frame.crop(bounds)
                    scale = min(224 / frame.width, 236 / frame.height)
                    frame = frame.resize((max(1, round(frame.width * scale)), max(1, round(frame.height * scale))), Image.Resampling.NEAREST)
                    normalized = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
                    normalized.alpha_composite(frame, ((256 - frame.width) // 2, 248 - frame.height))
                    frame = restore_enclosed_dark_details(normalized)
            else:
                frame = frame.resize((256, 256), Image.Resampling.NEAREST)
            sheet.alpha_composite(frame, (column * 256, row * 256))

    output = Path(sys.argv[2])
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, optimize=True)


if __name__ == "__main__":
    main()
