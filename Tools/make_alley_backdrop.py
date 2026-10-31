"""Paint the alley seen through the flat's window (original): the neighbouring
building's brick wall with a drainpipe and a barred window, and a strip of grey
morning sky. Usage: python Tools/make_alley_backdrop.py"""
import os
import random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Apartment")
random.seed(4)
W, H = 1024, 1152
im = Image.new("RGB", (W, H), (96, 70, 58))
d = ImageDraw.Draw(im)
bh, bw = 26, 68
for row in range(H // bh + 1):
    off = (row % 2) * bw // 2
    for col in range(-1, W // bw + 2):
        x0, y0 = col * bw + off, row * bh
        t = random.randint(-14, 14)
        c = (118 + t + random.randint(-6, 6), 74 + t // 2, 60 + t // 3)
        d.rectangle([x0 + 2, y0 + 2, x0 + bw - 2, y0 + bh - 2], fill=c)
grime = Image.new("L", (W, H), 0)
gd = ImageDraw.Draw(grime)
for y in range(H):
    gd.line([(0, y), (W, y)], fill=int(110 * (y / H) ** 2))
gd.rectangle([600, 0, 660, H], fill=70)
grime = grime.filter(ImageFilter.GaussianBlur(30))
im = Image.composite(Image.new("RGB", (W, H), (40, 34, 30)), im, grime)
d = ImageDraw.Draw(im)
d.rectangle([180, 300, 470, 640], fill=(58, 66, 70))
d.rectangle([180, 300, 470, 640], outline=(150, 145, 130), width=14)
for x in range(205, 470, 34):
    d.line([(x, 300), (x, 640)], fill=(30, 30, 32), width=6)
d.rectangle([780, 0, 812, H], fill=(70, 74, 72))
for y in range(60, H, 220):
    d.rectangle([770, y, 822, y + 18], fill=(52, 55, 54))
im.save(os.path.join(OUT, "alley_wall.png"))
sky = Image.new("RGB", (256, 256))
sd = ImageDraw.Draw(sky)
for y in range(256):
    v = int(205 - y * 0.25)
    sd.line([(0, y), (256, y)], fill=(v - 10, v - 2, v + 8))
sky.save(os.path.join(OUT, "alley_sky.png"))
print("alley_wall.png, alley_sky.png")
