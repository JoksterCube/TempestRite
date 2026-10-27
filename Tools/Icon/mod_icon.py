# Builds the TempestRite icon on top of the shared base (Thunderstore/icon_base.png): a corner badge with a storm
# cloud, a lightning bolt and the sun peeking out behind it.
# The badge frame matches FairCraftingLuck's clover badge so the mod icons read as one family.
# Usage (needs Pillow): python mod_icon.py <icon_base.png> <output icon.png> [preview.png]
import math
import sys
from PIL import Image, ImageDraw, ImageFilter

W, SS = 256, 4
DARK = (24, 20, 16, 255)
GOLD = (176, 132, 62, 255)
SUN = (250, 196, 72, 255)
SUN_RAY = (236, 142, 44, 255)
CLOUD = (126, 136, 156, 255)
CLOUD_HI = (168, 178, 196, 255)
BOLT = (252, 222, 92, 255)
DROP = (86, 156, 236, 255)


def badge_frame(d, X, Y, R):
    # dark rim, gold ring, dark line, cream face
    d.ellipse([X - R, Y - R, X + R, Y + R], fill=DARK)
    R2 = R - 3.5 * SS
    d.ellipse([X - R2, Y - R2, X + R2, Y + R2], fill=GOLD)
    R3 = R2 - 5 * SS
    d.ellipse([X - R3, Y - R3, X + R3, Y + R3], fill=DARK)
    R4 = R3 - 2.5 * SS
    d.ellipse([X - R4, Y - R4, X + R4, Y + R4], fill=(238, 226, 190, 255))


def circle(d, x, y, r, fill):
    d.ellipse([x - r, y - r, x + r, y + r], fill=fill)


def sun(d, X, Y):
    # rays, then a dark-rimmed disc
    sx, sy = X + 9 * SS, Y - 9 * SS
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        x1, y1 = sx + math.cos(a) * 11 * SS, sy + math.sin(a) * 11 * SS
        x2, y2 = sx + math.cos(a) * 16 * SS, sy + math.sin(a) * 16 * SS
        d.line([(x1, y1), (x2, y2)], fill=DARK, width=int(5.4 * SS))
        for px, py in ((x1, y1), (x2, y2)):
            circle(d, px, py, 2.7 * SS, DARK)
        d.line([(x1, y1), (x2, y2)], fill=SUN_RAY, width=int(2.6 * SS))
        for px, py in ((x1, y1), (x2, y2)):
            circle(d, px, py, 1.3 * SS, SUN_RAY)
    circle(d, sx, sy, 10.5 * SS, DARK)
    circle(d, sx, sy, 8.6 * SS, SUN)


def cloud_parts(X, Y, pad):
    # puffs (x, y, r) and a flat bottom bar, all in badge units
    puffs = [(-11, 3, 7.5), (-2, -3, 9.5), (9, 2, 7.5)]
    bar = (-17, 4, 16, 11)
    return [(X + px * SS, Y + py * SS, (r + pad) * SS) for px, py, r in puffs], \
        [X + bar[0] * SS - pad * SS, Y + bar[1] * SS - pad * SS, X + bar[2] * SS + pad * SS, Y + bar[3] * SS + pad * SS]


def cloud(d, X, Y):
    puffs, bar = cloud_parts(X, Y, 2.0)
    for x, y, r in puffs:
        circle(d, x, y, r, DARK)
    d.rounded_rectangle(bar, radius=(bar[3] - bar[1]) / 2, fill=DARK)
    puffs, bar = cloud_parts(X, Y, 0)
    for x, y, r in puffs:
        circle(d, x, y, r, CLOUD)
    d.rounded_rectangle(bar, radius=(bar[3] - bar[1]) / 2, fill=CLOUD)
    # light tops on the puffs
    for x, y, r in puffs:
        d.chord([x - r * 0.8, y - r * 0.85, x + r * 0.8, y + r * 0.5], 200, 330, fill=CLOUD_HI)


def bolt(d, X, Y):
    pts = [(2, 6), (-6, 19), (-1, 19), (-5, 31), (7, 14), (2, 14), (6, 6)]
    pts = [(X + x * SS, Y + y * SS) for x, y in pts]
    d.line(pts + [pts[0]], fill=DARK, width=int(4 * SS), joint="curve")
    d.polygon(pts, fill=BOLT)


def drops(d, X, Y):
    for dx in (-12, 12):
        x1, y1 = X + dx * SS, Y + 16 * SS
        x2, y2 = x1 - 2 * SS, y1 + 6 * SS
        d.line([(x1, y1), (x2, y2)], fill=DARK, width=int(5 * SS))
        for px, py in ((x1, y1), (x2, y2)):
            circle(d, px, py, 2.5 * SS, DARK)
        d.line([(x1, y1), (x2, y2)], fill=DROP, width=int(2.4 * SS))
        for px, py in ((x1, y1), (x2, y2)):
            circle(d, px, py, 1.2 * SS, DROP)


def weather_badge(img, cx=204, cy=204, r=40):
    layer = Image.new("RGBA", (W * SS, W * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    X, Y, R = cx * SS, cy * SS, r * SS
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([X - R, Y - R + 3 * SS, X + R, Y + R + 3 * SS], fill=(0, 0, 0, 120))
    sh = sh.filter(ImageFilter.GaussianBlur(4 * SS))
    badge_frame(d, X, Y, R)
    # keep everything inside the cream face
    art = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    a = ImageDraw.Draw(art)
    oy = -3 * SS
    sun(a, X, Y + oy)
    cloud(a, X, Y + oy)
    drops(a, X, Y + oy)
    bolt(a, X, Y + oy)
    face = Image.new("L", layer.size, 0)
    R4 = R - 11 * SS
    ImageDraw.Draw(face).ellipse([X - R4, Y - R4, X + R4, Y + R4], fill=255)
    art.putalpha(Image.composite(art.getchannel("A"), Image.new("L", layer.size, 0), face))
    layer.alpha_composite(art)
    combined = Image.alpha_composite(sh, layer).resize((W, W), Image.LANCZOS)
    return Image.alpha_composite(img, combined)


base = Image.open(sys.argv[1]).convert("RGBA")
icon = weather_badge(base)
icon.save(sys.argv[2], optimize=True)

if len(sys.argv) > 3:
    sheet = Image.new("RGBA", (W * 2, W * 2), (255, 255, 255, 255))
    sheet.paste((28, 30, 34, 255), (0, W, W * 2, W * 2))
    for row in range(2):
        for i, im in enumerate([base, icon]):
            sheet.alpha_composite(im, (i * W, row * W))
    sheet.convert("RGB").save(sys.argv[3])
