"""Rasterize the rectangle-only SVG to a Windows icon. Requires Pillow."""
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

assets = Path(__file__).resolve().parents[1] / 'src/ShrinkDarkerDisplay.WinUI/Assets'
svg = ET.parse(assets / 'ShrinkDarkerDisplay.svg').getroot()
scale = 16
image = Image.new('RGBA', (64 * scale, 64 * scale))
draw = ImageDraw.Draw(image)
for element in svg:
    if element.tag.rsplit('}', 1)[-1] != 'rect':
        raise ValueError('Only rectangle SVG elements are supported by this icon generator.')
    a = element.attrib
    x, y, w, h = (float(a[k]) for k in ('x', 'y', 'width', 'height'))
    stroke = float(a.get('stroke-width', 0))
    radius = float(a.get('rx', 0))
    if stroke:
        draw.rounded_rectangle(((x-stroke/2)*scale, (y-stroke/2)*scale, (x+w+stroke/2)*scale, (y+h+stroke/2)*scale), radius=(radius+stroke/2)*scale, fill=a['stroke'])
    draw.rounded_rectangle(((x+stroke/2)*scale, (y+stroke/2)*scale, (x+w-stroke/2)*scale, (y+h-stroke/2)*scale), radius=max(0, radius-stroke/2)*scale, fill=a['fill'])
image = image.resize((256, 256), Image.Resampling.LANCZOS)
image.save(assets / 'ShrinkDarkerDisplay.ico', sizes=[(n, n) for n in (16, 20, 24, 32, 40, 48, 64, 128, 256)])
