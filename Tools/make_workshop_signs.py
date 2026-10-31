"""Render the workshop's signs and the blueprint sheet to PNGs (text on quads
respects depth, like the office signs). Usage: python Tools/make_workshop_signs.py"""
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Workshop")
os.makedirs(OUT, exist_ok=True)
BOLD = "C:/Windows/Fonts/arialbd.ttf"
REG = "C:/Windows/Fonts/arial.ttf"
HAND = "C:/Windows/Fonts/comicbd.ttf" if os.path.exists("C:/Windows/Fonts/comicbd.ttf") else BOLD


def font(path, size):
    return ImageFont.truetype(path, size)


def taped_note(name, lines, size, paper=(244, 238, 214, 255), ink=(40, 40, 52, 255)):
    """A sheet of paper with marker writing and two strips of tape."""
    w, h = size
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([8, 14, w - 8, h - 6], fill=paper)
    for x in (w * 0.18, w * 0.82):
        d.rectangle([x - 34, 0, x + 34, 30], fill=(225, 215, 170, 220))
    y = 40
    for text, s, path in lines:
        f = font(path, s)
        tw = d.textlength(text, font=f)
        d.text(((w - tw) / 2, y), text, font=f, fill=ink)
        y += int(s * 1.25)
    im.save(os.path.join(OUT, name + ".png"))
    print(name, im.size)


taped_note("sign_workshop", [("THE WORKSHOP", 70, HAND), ("(do not touch the soldering iron)", 30, REG)], (720, 170))
taped_note("sign_parts", [("PARTS: SHARED", 58, HAND), ("take what you need, write it down (we won't)", 26, REG)], (720, 150))
taped_note("sign_rack", [("CHARGING RACK", 58, HAND), ("one gadget each in the bag", 28, REG)], (640, 150))
taped_note("sign_parcels", [("PARCELS", 60, HAND), ("here, not on my feet", 28, REG)], (460, 150), paper=(250, 246, 230, 255))

# The blueprint sheet over the bench: both recipes, the keyed ports, the steps.
W, H = 1400, 900
im = Image.new("RGBA", (W, H), (28, 66, 128, 255))
d = ImageDraw.Draw(im)
line = (220, 232, 250, 255)
faint = (150, 180, 225, 255)
for x in range(0, W, 50):
    d.line([(x, 0), (x, H)], fill=(40, 82, 146, 255), width=1)
for y in range(0, H, 50):
    d.line([(0, y), (W, y)], fill=(40, 82, 146, 255), width=1)
d.rectangle([16, 16, W - 16, H - 16], outline=line, width=4)
d.text((40, 30), "BLUEPRINTS", font=font(BOLD, 64), fill=line)
d.text((44, 104), "Questionable Components, assembled by hand. Look at the board to start one.", font=font(REG, 28), fill=faint)


def recipe(x, title, job, parts, ports):
    d.text((x, 160), title, font=font(BOLD, 48), fill=line)
    d.text((x, 216), job, font=font(REG, 26), fill=faint)
    # Casing with the board and its ports.
    d.rectangle([x, 270, x + 520, 520], outline=line, width=4)
    d.rectangle([x + 30, 300, x + 380, 490], outline=line, width=3)
    d.text((x + 400, 380), "BATT", font=font(BOLD, 30), fill=line)
    for i, (shape, label) in enumerate((("o", "SNS"), ("^", "AUD"), ("#", "MTR"))):
        cx, cy = x + 90 + i * 110, 360
        used = label in ports
        col = line if used else faint
        w = 5 if used else 2
        if shape == "o":
            d.ellipse([cx - 26, cy - 26, cx + 26, cy + 26], outline=col, width=w)
        elif shape == "^":
            d.polygon([(cx, cy - 30), (cx - 28, cy + 22), (cx + 28, cy + 22)], outline=col, width=w)
        else:
            d.rectangle([cx - 24, cy - 24, cx + 24, cy + 24], outline=col, width=w)
        d.text((cx - 30, cy + 40), label, font=font(BOLD, 28), fill=col)
    for sx, sy in ((x + 50, 320), (x + 360, 470)):
        d.ellipse([sx - 9, sy - 9, sx + 9, sy + 9], outline=line, width=3)
        d.line([(sx - 6, sy), (sx + 6, sy)], fill=line, width=3)
    y = 545
    for p in parts:
        d.text((x, y), "- " + p, font=font(REG, 28), fill=line)
        y += 36


recipe(60, "WHISPERBOX", "for the call center headset",
       ["salvaged casing", "controller board", "battery pack", "speaker module -> AUD (triangle)"], {"AUD"})
recipe(740, "STEADYHAND", "for carrying trays at the wedding",
       ["salvaged casing", "controller board", "battery pack", "tilt sensor -> SNS (round)", "vibration motor -> MTR (square)"], {"SNS", "MTR"})
d.text((60, 772), "1 casing on the mat    2 board in the casing    3 modules into their ports", font=font(BOLD, 30), fill=line)
d.text((60, 814), "4 two screws    5 battery    6 close the lid    7 TEST on the jig", font=font(BOLD, 30), fill=line)
im.save(os.path.join(OUT, "blueprint_sheet.png"))
print("blueprint_sheet", im.size)
