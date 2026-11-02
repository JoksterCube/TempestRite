"""Prints a Markdown table of every TempestRite rite, read from the source code (not from memory).

Columns: #, rite, category, key items, amount, duration (minutes; for kinship rites the wait before it can be
received again), radius, on by default, effect.

Sources:
- Settings/Constants.cs: each `internal static readonly RiteSettings X = new(...)` (section, key items, amounts,
  minutes, radius, effect text).
- Settings/PluginConfig.cs: `Add(WeatherKind.Kind, X)` ties a settings block to a rite.
- Domain/WeatherKind.cs: the rite's group (category) and the order rites are listed in (WeatherKinds.All).
- Translations/English.json: the rite's display name (<key>_name).

Usage: python rite_table.py [repo root] [--why]   (--why adds the long "why" description column)
"""
import json
import os
import re
import sys

args = [a for a in sys.argv[1:] if not a.startswith('--')]
ROOT = os.path.abspath(args[0] if args else os.path.join(os.path.dirname(__file__), '..', '..', '..'))
WHY = '--why' in sys.argv


sys.stdout.reconfigure(encoding='utf-8')


def read(*parts):
    with open(os.path.join(ROOT, *parts), encoding='utf-8') as f:
        return f.read()


constants = read('Settings', 'Constants.cs')
plugin_config = read('Settings', 'PluginConfig.cs')
weather_kind = read('Domain', 'WeatherKind.cs')
english = json.loads(read('Translations', 'English.json'))


def split_args(text):
    """Splits a C# argument list at top-level commas (ignores commas inside strings and brackets)."""
    out, depth, cur, i, in_str = [], 0, '', 0, False
    while i < len(text):
        c = text[i]
        if in_str:
            cur += c
            if c == '\\':
                cur += text[i + 1]
                i += 1
            elif c == '"':
                in_str = False
        elif c == '"':
            in_str = True
            cur += c
        elif c in '([{':
            depth += 1
            cur += c
        elif c in ')]}':
            depth -= 1
            cur += c
        elif c == ',' and depth == 0:
            out.append(cur.strip())
            cur = ''
        else:
            cur += c
        i += 1
    if cur.strip():
        out.append(cur.strip())
    return out


def literal(expr):
    """Joins "a" + "b" string literals; returns other expressions as they are."""
    parts = re.findall(r'\$?"((?:[^"\\]|\\.)*)"', expr)
    if parts and re.fullmatch(r'\s*(\$?"(?:[^"\\]|\\.)*"\s*\+?\s*)+', expr):
        return ''.join(parts)
    return expr.strip()


settings = {}
for m in re.finditer(r'internal static readonly RiteSettings (\w+) = new\(', constants):
    start, depth, i = m.end(), 1, m.end()
    in_str = False
    while depth:
        c = constants[i]
        if in_str:
            if c == '\\':
                i += 1
            elif c == '"':
                in_str = False
        elif c == '"':
            in_str = True
        elif c == '(':
            depth += 1
        elif c == ')':
            depth -= 1
        i += 1
    raw = split_args(constants[start:i - 1])
    positional = [a for a in raw if not re.match(r'^\w+:', a)]
    named = dict((a.split(':', 1)[0].strip(), a.split(':', 1)[1].strip()) for a in raw if re.match(r'^\w+:', a))
    names = ['group', 'effect', 'keyItem', 'keyItemName', 'why', 'enabled', 'lasts', 'environments', 'namePrefix', 'amount',
             'minutes', 'radius', 'minutesDescription', 'distinctKeys']
    values = dict(zip(names, positional))
    values.update(named)
    settings[m.group(1)] = {k: literal(v) for k, v in values.items()}

kind_to_setting = dict((k, v.split('.')[-1]) for k, v in re.findall(r'Add\(WeatherKind\.(\w+), ([\w.]+)\)', plugin_config))
groups = dict(re.findall(r'\[WeatherKind\.(\w+)\] = new\([^;]*?RiteGroup\.(\w+)', weather_kind))
order_block = re.search(r'internal static readonly WeatherKind\[\] All =\s*\[(.*?)\];', weather_kind, re.S).group(1)
order = re.findall(r'WeatherKind\.(\w+)', order_block)
untimed = re.search(r'bool Untimed\(WeatherKind kind\) => (.*?);', weather_kind)
untimed = set(re.findall(r'WeatherKind\.(\w+)', untimed.group(1))) if untimed else set()
# [WeatherKind.Clear] = new(() => Messages.ClearName, ...) and ClearName => Text.Get("clear_name").
name_props = dict(re.findall(r'\[WeatherKind\.(\w+)\] = new\(\(\) => Messages\.(\w+)', weather_kind))
text_keys = dict(re.findall(r'internal static string (\w+) => Text\.Get\("(\w+)"\)', constants))
domain_radius = re.search(r'DomainRadius = new\(.*?\)\),\s*([\d.]+)f', constants, re.S)
domain_radius = domain_radius.group(1) if domain_radius else '?'


def display_name(kind, section):
    key = text_keys.get(name_props.get(kind, ''), '')
    if key in english:
        return english[key]
    # Section titles read "7 - Reckoning - Crimson": the name after the group.
    return re.sub(r'^\d+ - [^-]+? - ', '', section) or kind


def num(text, default):
    text = (text or '').rstrip('fF')
    return text if text else default


rows = []
for index, kind in enumerate(order, 1):
    s = settings.get(kind_to_setting.get(kind, ''), {})
    section = s.get('group', '')
    number = str(index)
    lasts = s.get('lasts', 'true') != 'false'
    minutes = num(s.get('minutes'), '30') if lasts else 'at once'
    radius = num(s.get('radius'), '')
    radius = '' if radius in ('', '0') else radius + ' m'
    if not radius and groups.get(kind) == 'Domain' and kind != 'Grounding':
        radius = domain_radius + ' m (Domain)'
    if kind in untimed:
        minutes = 'until won'
    amount = num(s.get('amount'), '1')
    if s.get('distinctKeys') == 'true':
        amount += ' different'
    effect = s.get('effect', '')
    paren = re.search(r'\((.*)\)\s*$', effect)
    effect = paren.group(1) if paren else effect
    row = [number, display_name(kind, section), groups.get(kind, '?'), s.get('keyItem', '?'), amount, minutes, radius,
           'on' if s.get('enabled', 'true') == 'true' else 'off', effect]
    if WHY:
        row.append(s.get('why', ''))
    rows.append(row)

head = ['#', 'Rite', 'Category', 'Key items (prefabs)', 'Amount', 'Minutes', 'Radius', 'Default', 'Effect']
if WHY:
    head.append('Details')
print('| ' + ' | '.join(head) + ' |')
print('|' + '|'.join('---' for _ in head) + '|')
for r in rows:
    print('| ' + ' | '.join(str(c).replace('|', '\\|') for c in r) + ' |')
# An item performs one rite only (the one listed first on the rites page keeps it), so a key item listed twice is a bug.
owners = {}
for r in rows:
    for item in re.split(r'\s*,\s*', r[3]):
        owners.setdefault(item, []).append(r[1])
for item, rites in owners.items():
    if len(rites) > 1:
        print(f'\nKey item {item} is listed by more than one rite: {", ".join(rites)}', file=sys.stderr)
missing = [k for k in order if k not in kind_to_setting]
if missing:
    print('\nRites with no settings block found: ' + ', '.join(missing), file=sys.stderr)
