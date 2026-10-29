"""Renders every rite's status icon into Docs/Rites/<Kind>.png, for the README.

The icons are drawn by the mod's own code (WeatherIcons) from the game's sprites, so this:
1. finds the sprites the icon code asks for (GameArt.Get / ItemIconPixels names in WeatherIcons.cs) in the game's
   files and saves them as .rgba in a temporary folder;
2. runs the IconPreview test's Render_rite_icons, which draws the icons with that art;
3. turns its output into PNGs.

Needs UnityPy, Pillow and numpy (use a scratch venv), and a Release build of the mod (the tests use it).
Usage: python rite_previews.py ["C:/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data"]
"""
import os
import re
import struct
import subprocess
import sys
import tempfile

import UnityPy
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
GAME_DATA = sys.argv[1] if len(sys.argv) > 1 else r'C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data'
OUT = os.path.join(ROOT, 'Docs', 'Rites')
# Items whose icon isn't named after the item.
ITEM_SPRITES = {'MeadTasty': 'potion_tasty'}


def wanted():
    source = open(os.path.join(ROOT, 'Domain', 'WeatherIcons.cs'), encoding='utf-8').read()
    sprites = set(re.findall(r'GameArt\.Get\("([^"]+)"\)', source))
    items = set(re.findall(r'GameArt\.ItemIconPixels\("([^"]+)"\)', source))
    return sprites, items


def write_rgba(path, image):
    image = image.convert('RGBA')
    with open(path, 'wb') as f:
        f.write(struct.pack('<ii', image.width, image.height))
        f.write(image.tobytes())


def extract(sprites, items, art):
    # Item icons are found by the item's name in lower case (wisp, queendrop...).
    targets = {name: name for name in sprites}
    for item in items:
        if item in ITEM_SPRITES:
            targets.setdefault(ITEM_SPRITES[item], 'item_' + item)
        targets.setdefault(item.lower(), 'item_' + item)
        targets.setdefault(item, 'item_' + item)
    found = set()
    for folder, _, files in os.walk(GAME_DATA):
        for file in files:
            if file.endswith(('.resS', '.resource', '.json', '.hash', '.manifest')):
                continue
            try:
                env = UnityPy.load(os.path.join(folder, file))
            except Exception:
                continue
            for obj in env.objects:
                if obj.type.name != 'Sprite':
                    continue
                try:
                    name = obj.peek_name()
                except Exception:
                    continue
                key = targets.get(name)
                if key is None or key in found:
                    continue
                try:
                    write_rgba(os.path.join(art, key + '.rgba'), obj.read().image)
                    found.add(key)
                except Exception as error:
                    print('could not read', name, error)
            if len(found) == len(set(targets.values())):
                return found
    return found


def main():
    sprites, items = wanted()
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as art, tempfile.TemporaryDirectory() as rendered:
        found = extract(sprites, items, art)
        missing = sorted((set(sprites) | {'item_' + i for i in items}) - found)
        if missing:
            print('not found (stand-ins are drawn):', ', '.join(missing))
        env = dict(os.environ, TEMPESTRITE_ICON_ART=art, TEMPESTRITE_ICON_OUT=rendered)
        subprocess.run(['dotnet', 'test', os.path.join(ROOT, 'Tests', 'TempestRite.Tests'), '--filter', 'Render_rite_icons'],
                       check=True, env=env)
        for file in sorted(os.listdir(rendered)):
            with open(os.path.join(rendered, file), 'rb') as f:
                width, height = struct.unpack('<ii', f.read(8))
                image = Image.frombytes('RGBA', (width, height), f.read(width * height * 4))
            image.save(os.path.join(OUT, file[:-5] + '.png'))
            print('wrote', file[:-5] + '.png')


if __name__ == '__main__':
    main()
