"""Draws the application icon: a black tile holding a 3 x 3 table whose cells run through the
heat colours of the banner. Writes src/HRE.App/app.ico with the sizes Windows asks for.

Needs Pillow: python tools/research/make_icon.py
"""
from pathlib import Path

from PIL import Image, ImageDraw

HEAT = ["#0E3E7A", "#0096AA", "#3CBE50", "#0096AA", "#3CBE50", "#F0C828", "#3CBE50", "#F0C828", "#EB3C2D"]
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def draw(size: int) -> Image.Image:
    scale = 8  # drawn large and reduced, for smooth corners
    full = size * scale
    image = Image.new("RGBA", (full, full), (0, 0, 0, 0))
    pen = ImageDraw.Draw(image)
    pen.rounded_rectangle((0, 0, full - 1, full - 1), radius=full * 0.2, fill="#000000", outline="#2A2A2A", width=max(scale // 2, full // 64))
    margin, gap = full * 0.17, full * 0.05
    cell = (full - 2 * margin - 2 * gap) / 3
    for index, colour in enumerate(HEAT):
        row, column = divmod(index, 3)
        left = margin + column * (cell + gap)
        top = margin + (2 - row) * (cell + gap)  # the hottest cell at the top right
        pen.rounded_rectangle((left, top, left + cell, top + cell), radius=cell * 0.18, fill=colour)
    return image.resize((size, size), Image.Resampling.LANCZOS)


def main() -> None:
    target = Path(__file__).resolve().parents[2] / "src" / "HRE.App" / "app.ico"
    largest = draw(SIZES[-1])
    largest.save(target, format="ICO", sizes=[(s, s) for s in SIZES], append_images=[draw(s) for s in SIZES[:-1]])
    print(target, target.stat().st_size, "bytes")


if __name__ == "__main__":
    main()
