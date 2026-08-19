#!/usr/bin/env python3
"""
Sprint 39 (DEC-125) — Second pass: update remaining red-500 (icons/asterisks) to danger-500.
The semantic token gives the same color but is more meaningful in the design system.
"""
import os
from pathlib import Path

ROOT = Path(r"C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\src\frontend\app\(authenticated)")

# Only update the asterisk / icon color usages (not the text-red-800/700/600 which we already did)
REPLACEMENTS = [
    ("text-red-500", "text-danger-500"),
    ("hover:text-red-500", "hover:text-danger-600"),
    ("hover:text-red-700", "hover:text-danger-700"),
    ("hover:text-red-600", "hover:text-danger-600"),
    # bg-red-500 for danger buttons (rare)
    ("bg-red-500 ", "bg-danger-500 "),
    ("bg-red-500\"", "bg-danger-500\""),
    ("hover:bg-red-500", "hover:bg-danger-600"),
    ("hover:bg-red-600", "hover:bg-danger-700"),
    ("hover:bg-red-700", "hover:bg-danger-700"),
]

CHANGED = 0
FILES = 0
for path in ROOT.rglob("*.tsx"):
    text = path.read_text(encoding="utf-8")
    new = text
    for old, repl in REPLACEMENTS:
        if old in new:
            new = new.replace(old, repl)
    if new != text:
        path.write_text(new, encoding="utf-8")
        FILES += 1
        for old, repl in REPLACEMENTS:
            CHANGED += text.count(old)
        print(f"  ✓ {path.relative_to(ROOT)}")

print(f"\nUpdated {FILES} files, {CHANGED} substitutions.")
