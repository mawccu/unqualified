"""Draw the rent bill's printed face as a texture (run with the system Python + Pillow).

  python Tools/make_bill_texture.py Assets/Art/Apartment/bill_print.png

Only facts that stay true on paper (rent 840, due by day 7); the live countdown
is on the HUD. The text is
large enough to read when the bill is held up in first person.
"""
import sys

from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
W, H = 840, 1188  # A4 proportions
img = Image.new("RGB", (W, H), (244, 241, 230))
d = ImageDraw.Draw(img)


def font(size, bold=False):
    for name in (("arialbd.ttf" if bold else "arial.ttf"), "DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


ink = (40, 40, 46)
red = (178, 34, 30)
grey = (120, 118, 112)
d.rectangle([0, 0, W, 150], fill=red)
d.text((56, 38), "FINAL NOTICE", font=font(76, True), fill=(250, 246, 236))
d.text((60, 190), "FLAT 4B  ·  FROM YOUR LANDLORD", font=font(30, True), fill=grey)
d.line([60, 240, W - 60, 240], fill=(200, 196, 186), width=3)
# Only what stays true on paper: the live countdown and savings are on the HUD.
rows = [("Rent owed", "$840"), ("Due by", "Day 7"), ("Late fee", "Your stuff")]
y = 290
for label, value in rows:
    d.text((60, y), label, font=font(48), fill=ink)
    d.text((W - 60, y), value, font=font(56, True), fill=red if label == "Rent owed" else ink, anchor="ra")
    y += 110
d.line([60, y + 10, W - 60, y + 10], fill=(200, 196, 186), width=3)
small = font(30)
lines = ['The payment reference must not say', '"entrepreneur".', '', 'Pay or leave. Your choice.', '(It is not your choice.)']
y += 50
for line in lines:
    d.text((60, y), line, font=small, fill=ink)
    y += 44

# A crooked OVERDUE stamp.
stamp = Image.new("RGBA", (520, 150), (0, 0, 0, 0))
s = ImageDraw.Draw(stamp)
s.rounded_rectangle([6, 6, 514, 144], radius=18, outline=red + (200,), width=10)
s.text((260, 75), "OVERDUE", font=font(92, True), fill=red + (200,), anchor="mm")
stamp = stamp.rotate(-12, expand=True)
img.paste(stamp, (W - stamp.width - 20, H - stamp.height - 60), stamp)
d.text((60, H - 70), "Signed: The Management (me)", font=font(26), fill=grey)
img.save(OUT)
print("wrote", OUT)
