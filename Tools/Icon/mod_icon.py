# Builds the TempestRite icon on top of the shared base (Thunderstore/icon_base.png): a corner badge with the mod's
# symbol. The badge frame matches FairCraftingLuck's clover badge so the mod icons read as one family.
# Symbols: "runebolt" (the default: a rune stone on a mossy mound, a lightning bolt carved in it and glowing, sparks
# crackling off it, as on the in-game rites button), "flame" (a flame with a lightning bolt in it, on an altar stone), "othala" (a rune stone with the glowing Othala rune, as on the rites button),
# "weather" (the old storm cloud with sun and bolt).
# Usage (needs Pillow): python mod_icon.py <icon_base.png> <output icon.png> [preview.png] [symbol]
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
STONE = (150, 147, 140, 255)
STONE_HI = (182, 178, 168, 255)
STONE_LO = (116, 113, 108, 255)
STORM_GLOW = (110, 200, 255, 255)
STORM_CORE = (225, 248, 255, 255)
FIRE_GLOW = (255, 150, 40, 255)
FIRE_CORE = (255, 228, 140, 255)
DEFAULT_SYMBOL = "runebolt"


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


def pts(X, Y, points):
    return [(X + x * SS, Y + y * SS) for x, y in points]


def glow(art, draw, color, blur, passes=2):
    # a soft halo: the shape drawn on its own layer and blurred, laid down under the sharp shape
    layer = Image.new("RGBA", art.size, (0, 0, 0, 0))
    draw(ImageDraw.Draw(layer), color)
    halo = layer.filter(ImageFilter.GaussianBlur(blur * SS))
    for _ in range(passes):
        art.alpha_composite(halo)


# ---- the old weather symbol ----

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
    p = pts(X, Y, [(2, 6), (-6, 19), (-1, 19), (-5, 31), (7, 14), (2, 14), (6, 6)])
    d.line(p + [p[0]], fill=DARK, width=int(4 * SS), joint="curve")
    d.polygon(p, fill=BOLT)


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


def weather(art, X, Y):
    a = ImageDraw.Draw(art)
    Y -= 3 * SS
    sun(a, X, Y)
    cloud(a, X, Y)
    drops(a, X, Y)
    bolt(a, X, Y)


# ---- rune stone (the same stone, rune and sparks as the in-game rites button, WeatherIcons.FlapPicture) ----

def flap(points):
    # the button's 64-pixel coordinates in badge units
    return [((x - 32) * 0.86, (y - 31) * 0.86) for x, y in points]


STONE_SHAPE = flap([(19, 57), (18, 29), (20, 15), (24, 7), (29, 4), (33, 6), (38, 9), (43, 15), (46, 26), (46.5, 44),
                    (45, 57)])
STONE_HI_SHAPE = flap([(19, 52), (18, 29), (20, 15), (24, 7), (29, 4), (31, 5), (26, 11), (23, 20), (22, 34), (22, 50)])
STONE_LO_SHAPE = flap([(38, 9), (43, 15), (46, 26), (46.5, 44), (45, 57), (40, 57), (41, 40), (40, 22), (36, 11)])
BOLT_RUNE = flap([(36, 15), (27, 33), (36, 33), (28, 51)])
SPARKS = [flap([(44.5, 15), (49, 12), (48, 8.5), (52.5, 5)]), flap([(19.5, 25), (15, 22.5), (16.5, 19), (11.5, 16)])]
GLINTS = [flap([(51, 22)])[0] + (1.6,), flap([(13, 33)])[0] + (1.3,)]


def zigzag(d, X, Y, points, color, width):
    p = pts(X, Y, points)
    d.line(p, fill=color, width=int(width * SS), joint="curve")
    for x, y in p:
        circle(d, x, y, width * SS / 2, color)


def rune_stone(art, X, Y):
    d = ImageDraw.Draw(art)
    # a soft storm glow behind, a mossy mound, then the stone with a dark rim, lit from the upper left
    glow(art, lambda g, c: circle(g, X, Y - 2 * SS, 17 * SS, c), STORM_GLOW, 7, passes=1)
    d.ellipse([X - 22 * SS, Y + 18 * SS, X + 22 * SS, Y + 30 * SS], fill=(76, 98, 48, 255))
    d.ellipse([X - 16 * SS, Y + 19 * SS, X + 12 * SS, Y + 24 * SS], fill=(98, 124, 62, 255))
    p = pts(X, Y, STONE_SHAPE)
    d.line(p + [p[0]], fill=DARK, width=int(4.5 * SS), joint="curve")
    d.polygon(p, fill=STONE)
    d.polygon(pts(X, Y, STONE_LO_SHAPE), fill=STONE_LO)
    d.polygon(pts(X, Y, STONE_HI_SHAPE), fill=STONE_HI)


def carved(art, X, Y, draw_rune, glow_color, core_color):
    # cut dark into the stone, then glowing from inside
    d = ImageDraw.Draw(art)
    draw_rune(d, DARK, 4.6)
    glow(art, lambda g, c: draw_rune(g, c, 5.0), glow_color, 3.0, passes=3)
    draw_rune(d, glow_color, 3.0)
    draw_rune(d, core_color, 1.3)


def bolt_rune(X, Y):
    return lambda d, color, width: zigzag(d, X, Y, BOLT_RUNE, color, width)


def othala_rune(X, Y):
    top, left, right = (0, -15), (-9, -3), (9, -3)
    lines = [[top, left], [top, right], [left, (8, 15)], [right, (-8, 15)]]

    def draw(d, color, width):
        for line in lines:
            zigzag(d, X, Y, line, color, width)
    return draw


def sparks(art, X, Y):
    d = ImageDraw.Draw(art)
    for spark in SPARKS:
        glow(art, lambda g, c, s=spark: zigzag(g, X, Y, s, c, 2.6), STORM_GLOW, 1.8, passes=2)
        zigzag(d, X, Y, spark, STORM_CORE, 1.1)
    for gx, gy, size in GLINTS:
        glow(art, lambda g, c: circle(g, X + gx * SS, Y + gy * SS, size * 1.4 * SS, c), STORM_GLOW, 1.5, passes=2)
        for dx, dy in ((size, 0), (0, size)):
            d.line(pts(X, Y, [(gx - dx, gy - dy), (gx + dx, gy + dy)]), fill=STORM_CORE, width=int(0.9 * SS))


def runebolt(art, X, Y):
    rune_stone(art, X, Y)
    carved(art, X, Y, bolt_rune(X, Y), STORM_GLOW, STORM_CORE)
    sparks(art, X, Y)

def othala(art, X, Y):
    rune_stone(art, X, Y)
    carved(art, X, Y + 1 * SS, othala_rune(X, Y + 1 * SS), FIRE_GLOW, FIRE_CORE)


# ---- flame on an altar stone ----

FLAME = [(0, -24), (3, -15), (7, -11), (7, -18), (12, -8), (14, 2), (11, 10), (5, 14), (-5, 14), (-11, 10), (-14, 2),
         (-12, -6), (-8, -13), (-7, -6), (-3, -14)]


def flame_shape(X, Y, scale):
    # the flame's outline, shrunk toward the middle of its base for the hotter inner layers
    return pts(X, Y, [(x * scale, 14 - (14 - y) * scale) for x, y in FLAME])

def flame(art, X, Y):
    d = ImageDraw.Draw(art)
    # altar slab
    slab = pts(X, Y, [(-19, 17), (-17, 12), (17, 12), (19, 17), (18, 24), (-18, 24)])
    d.line(slab + [slab[0]], fill=DARK, width=int(4 * SS), joint="curve")
    d.polygon(slab, fill=STONE)
    d.polygon(pts(X, Y, [(-17, 12), (17, 12), (19, 17), (-19, 17)]), fill=STONE_HI)
    fy = Y - 2 * SS
    outer = flame_shape(X, fy, 1.0)
    glow(art, lambda g, c: g.polygon(outer, fill=c), FIRE_GLOW, 4, passes=2)
    d.line(outer + [outer[0]], fill=DARK, width=int(3.5 * SS), joint="curve")
    d.polygon(outer, fill=(222, 92, 32, 255))
    d.polygon(flame_shape(X, fy, 0.72), fill=(250, 150, 44, 255))
    d.polygon(flame_shape(X, fy, 0.45), fill=FIRE_CORE)
    # the bolt in its heart
    p = pts(X, fy, [(2, -9), (-4, 3), (1, 3), (-2, 13), (5, 0), (0, 0), (4, -9)])
    d.line(p + [p[0]], fill=DARK, width=int(2.5 * SS), joint="curve")
    d.polygon(p, fill=STORM_CORE)


SYMBOLS = {"runebolt": runebolt, "flame": flame, "othala": othala, "weather": weather}


def badge(img, symbol, cx=204, cy=204, r=40):
    layer = Image.new("RGBA", (W * SS, W * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    X, Y, R = cx * SS, cy * SS, r * SS
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([X - R, Y - R + 3 * SS, X + R, Y + R + 3 * SS], fill=(0, 0, 0, 120))
    sh = sh.filter(ImageFilter.GaussianBlur(4 * SS))
    badge_frame(d, X, Y, R)
    # keep everything inside the cream face
    art = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    SYMBOLS[symbol](art, X, Y)
    face = Image.new("L", layer.size, 0)
    R4 = R - 11 * SS
    ImageDraw.Draw(face).ellipse([X - R4, Y - R4, X + R4, Y + R4], fill=255)
    art.putalpha(Image.composite(art.getchannel("A"), Image.new("L", layer.size, 0), face))
    layer.alpha_composite(art)
    combined = Image.alpha_composite(sh, layer).resize((W, W), Image.LANCZOS)
    return Image.alpha_composite(img, combined)


if __name__ == "__main__":
    base = Image.open(sys.argv[1]).convert("RGBA")
    symbol = sys.argv[4] if len(sys.argv) > 4 else DEFAULT_SYMBOL
    icon = badge(base, symbol)
    icon.save(sys.argv[2], optimize=True)

    if len(sys.argv) > 3 and sys.argv[3]:
        sheet = Image.new("RGBA", (W * 2, W * 2), (255, 255, 255, 255))
        sheet.paste((28, 30, 34, 255), (0, W, W * 2, W * 2))
        for row in range(2):
            for i, im in enumerate([base, icon]):
                sheet.alpha_composite(im, (i * W, row * W))
        sheet.convert("RGB").save(sys.argv[3])
