"""Render the office's signs and nameplates to PNGs (text on quads respects depth,
unlike Unity's legacy 3D text). Usage: python Tools/make_office_signs.py"""
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Office")
os.makedirs(OUT, exist_ok=True)
BOLD = "C:/Windows/Fonts/arialbd.ttf"
REG = "C:/Windows/Fonts/arial.ttf"


def sign(name, lines, size, bg, fg, pad=24, font=BOLD, line_gap=1.25, scale=None):
    fonts = [ImageFont.truetype(f, s) for (_, s, f) in lines]
    widths = [ImageDraw.Draw(Image.new("RGB", (1, 1))).textlength(t, font=fn) for (t, _, _), fn in zip(lines, fonts)]
    w = int(max(widths) + pad * 2) if size is None else size[0]
    h = int(sum(s * line_gap for (_, s, _) in lines) + pad * 2) if size is None else size[1]
    im = Image.new("RGBA", (w, h), bg)
    d = ImageDraw.Draw(im)
    y = pad
    for ((t, s, _), fn, tw) in zip(lines, fonts, widths):
        d.text(((w - tw) / 2, y), t, font=fn, fill=fg)
        y += s * line_gap
    im.save(os.path.join(OUT, name + ".png"))
    print(name, im.size)


plate_bg, plate_fg = (34, 52, 74, 255), (240, 240, 232, 255)
for n in ["YOUR DESK", "DANA", "OMAR", "KIM"]:
    sign("plate_" + n.split()[-1].lower() if n != "YOUR DESK" else "plate_you", [(n, 56, BOLD)], (420, 96), plate_bg, plate_fg, pad=16)
sign("sign_company", [("BRIGHTLINE", 120, BOLD), ("HOME SUPPORT", 64, BOLD)], None, (0, 0, 0, 0), (245, 245, 238, 255))
sign("sign_reference", [("REFERENCE", 52, BOLD)], None, (240, 240, 232, 255), (40, 40, 48, 255), pad=12)
sign("sign_break", [("BREAK AREA", 64, BOLD), ("(5 minutes means 5 minutes)", 36, REG)], None, (240, 236, 222, 255), (50, 50, 56, 255))
sign("sign_whiteboard", [("TONIGHT", 64, BOLD), ("", 24, REG), ("Never promise a delivery date.", 44, REG),
                         ("Over 30 days = warranty, not refund.", 44, REG), ("RESOLVED means RESOLVED.", 44, BOLD)],
     (1240, 760), (0, 0, 0, 0), (30, 50, 150, 255), pad=60)
