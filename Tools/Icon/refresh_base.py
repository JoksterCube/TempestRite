# Refreshes the original JoksterCube icon into the shared base: transparent background, larger coin, livelier colors.
# Usage (needs Pillow + numpy): python refresh_base.py <original icon.png> <output icon_base.png>
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageChops
import numpy as np

src, out = sys.argv[1], sys.argv[2]
S = 4  # supersample for smooth edges
base = Image.open(src).convert("RGB")
W = base.width
cx, cy = 128.0, 128.8
R = 106.6
TARGET_R = 117.0

# 1) Color refresh: richer, crisper, slightly warmer.
im = ImageEnhance.Contrast(base).enhance(1.12)
im = ImageEnhance.Color(im).enhance(1.22)
im = im.filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
a = np.asarray(im).astype(np.float32)
a[..., 0] *= 1.03; a[..., 2] *= 0.97
im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))

# 2) Soft warm glow behind the cube (screen blend, radial falloff).
yy, xx = np.mgrid[0:W, 0:W]
dist = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
glow = np.clip(1 - dist / 62, 0, 1) ** 2 * 0.28
glow_rgb = np.stack([glow * 255, glow * 225, glow * 150], -1)
arr = np.asarray(im).astype(np.float32)
arr = 255 - (255 - arr) * (255 - glow_rgb) / 255
im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))

# 3) Cut the coin out with an anti-aliased circle.
mask = Image.new("L", (W * S, W * S), 0)
ImageDraw.Draw(mask).ellipse([(cx - R) * S, (cy - R) * S, (cx + R) * S, (cy + R) * S], fill=255)
mask = mask.resize((W, W), Image.LANCZOS)
coin = im.convert("RGBA"); coin.putalpha(mask)
# Recenter and enlarge so the coin fills the frame.
k = TARGET_R / R
box = (cx - R - 1, cy - R - 1, cx + R + 1, cy + R + 1)
size = round((2 * R + 2) * k)
big = coin.resize((size, size), Image.LANCZOS, box=box)
coin = Image.new("RGBA", (W, W), (0, 0, 0, 0))
off = (W - size) // 2
coin.paste(big, (off, off - 2), big)
# Fine dark outline over the cut edge, so no light fringe shows on dark pages.
ring = Image.new("L", (W * S, W * S), 0)
c2, r2 = W / 2 * S, size / 2 * S
cy2 = (off - 2 + size / 2) * S
ImageDraw.Draw(ring).ellipse([c2 - r2 + 1, cy2 - r2 + 1, c2 + r2 - 1, cy2 + r2 - 1], outline=255, width=int(2.2 * S))
ring = ring.resize((W, W), Image.LANCZOS)
dark = Image.new("RGBA", (W, W), (22, 20, 18, 255)); dark.putalpha(ImageChops.multiply(ring, coin.getchannel("A")))
coin = Image.alpha_composite(coin, dark)
mask = coin.getchannel("A")

# 4) Subtle drop shadow so it reads on light and dark pages.
shadow = Image.new("RGBA", (W, W), (0, 0, 0, 0))
sh_mask = ImageChops.offset(mask, 0, 3).point(lambda v: int(v * 0.45)).filter(ImageFilter.GaussianBlur(4))
shadow.putalpha(sh_mask)
canvas = Image.alpha_composite(shadow, coin)
canvas.save(out, optimize=True)
