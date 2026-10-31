"""Draw the reference binders' spine labels (system Python + Pillow).

  python Tools/make_binder_spines.py

One label per binder, named for its help page (CaseLibrary.HelpPages): the text
runs bottom to top, like a real binder's spine. Written to Assets/Art/Office/.
"""
import os

from PIL import Image, ImageDraw, ImageFont

OUT = "Assets/Art/Office"
bold = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 54)
small = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 30)
LABELS = (
    ("spine_deliveries", "DELIVERIES", "late, held, lost"),
    ("spine_plug", "SMART PLUG", "lights & setup"),
    ("spine_dustbot", "DUSTBOT", "lights & fixes"),
    ("spine_refunds", "REFUNDS", "returns, warranty"),
)
for name, title, sub in LABELS:
    w, h = 620, 150
    im = Image.new("RGBA", (w, h), (246, 244, 236, 255))
    d = ImageDraw.Draw(im)
    d.rectangle([3, 3, w - 4, h - 4], outline=(60, 60, 66, 255), width=4)
    tw = d.textlength(title, font=bold)
    d.text(((w - tw) / 2, 18), title, font=bold, fill=(30, 30, 36, 255))
    sw = d.textlength(sub, font=small)
    d.text(((w - sw) / 2, 92), sub, font=small, fill=(90, 90, 96, 255))
    im.rotate(90, expand=True).save(os.path.join(OUT, name + ".png"))
print("wrote", len(LABELS), "spine labels")
