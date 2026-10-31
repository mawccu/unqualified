"""Draw the call center's help-pages printout as a texture (system Python + Pillow).

  python Tools/make_printout_texture.py Assets/Art/Office/printout_print.png

The printer's sheets used the rent bill's model and its FINAL NOTICE face; this is
their own face: the four help pages (the same text as the reference binders, from
CaseLibrary.HelpPages), large enough to read when held up in first person.
"""
import re
import sys

from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
W, H = 840, 1188  # A4 proportions, as the bill
img = Image.new("RGB", (W, H), (246, 245, 240))
d = ImageDraw.Draw(img)


def font(size, bold=False):
    for name in (("arialbd.ttf" if bold else "arial.ttf"), "DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def help_pages():
    """The help pages, read from CaseLibrary.cs so the printout never disagrees with the binders."""
    src = open("Assets/Scripts/Calls/CaseLibrary.cs", encoding="utf-8").read()
    block = src[src.index("HelpPages ="):]
    block = block[: block.index("};")]
    return [(t, b.replace("\\n", "\n")) for t, b in re.findall(r'\("([^"]+)",\s*"((?:[^"\\]|\\.)*)"\)', block)]


def wrap(text, f, width):
    lines = []
    for para in text.split("\n"):
        if not para.strip():
            lines.append("")
            continue
        line = ""
        for word in para.split():
            trial = (line + " " + word).strip()
            if d.textlength(trial, font=f) <= width:
                line = trial
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


ink = (36, 40, 48)
teal = (32, 128, 112)
grey = (120, 122, 126)
d.rectangle([0, 0, W, 112], fill=teal)
d.text((48, 26), "AGENT QUICK REFERENCE", font=font(46, True), fill=(250, 250, 246))
d.text((50, 132), "BRIGHTLINE HOME SUPPORT  ·  PRINTED FROM THE HELP PAGES", font=font(22, True), fill=grey)
y = 178
body = font(25)
for title, text in help_pages():
    d.line([48, y, W - 48, y], fill=(206, 206, 200), width=2)
    y += 14
    d.text((48, y), title.upper(), font=font(30, True), fill=teal)
    y += 44
    for line in wrap(text, body, W - 96):
        if line:
            d.text((48, y), line, font=body, fill=ink)
        y += 33 if line else 12
    y += 16
d.text((48, H - 52), "Never promise a delivery date.  RESOLVED means fixed.", font=font(22, True), fill=grey)
if y > H - 60:
    raise SystemExit(f"text runs to {y}px, past the footer")
img.save(OUT)
print("wrote", OUT, "text to", y)
