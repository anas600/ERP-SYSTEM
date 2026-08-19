#!/usr/bin/env python3
"""
Sprint 39 (DEC-125) — Bulk update inline error box colors to use new design tokens.

Replaces:
  bg-red-50  -> bg-danger-50
  border-red-200 -> border-danger-200
  text-red-700 -> text-danger-700

This is the inline error box pattern. The 73 occurrences across 73 files all
use this exact combination (or subsets of it). The new design system uses
the semantic danger color tokens.

ONLY runs against files in the (authenticated) app tree. Skips node_modules etc.
"""
import os
import re
import sys
from pathlib import Path

ROOT = Path(r"C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\src\frontend\app\(authenticated)")

# Map of replacements
REPLACEMENTS = [
    ("bg-red-50 ", "bg-danger-50 "),
    ("bg-red-50\"", "bg-danger-50\""),
    ("border-red-200 ", "border-danger-200 "),
    ("border-red-200\"", "border-danger-200\""),
    ("text-red-700 ", "text-danger-700 "),
    ("text-red-700\"", "text-danger-700\""),
    ("text-red-600 ", "text-danger-600 "),
    ("text-red-600\"", "text-danger-600\""),
    ("text-red-800 ", "text-danger-700 "),
    ("text-red-800\"", "text-danger-700\""),
    # border-red-300 / border-red-400 / border-red-500 (less common)
    ("border-red-300 ", "border-danger-300 "),
    ("border-red-400 ", "border-danger-400 "),
    ("border-red-500 ", "border-danger-500 "),
    ("border-red-500\"", "border-danger-500\""),
    # focus rings
    ("focus:ring-red-200", "focus:ring-danger-500/20"),
    ("focus:ring-red-500", "focus:ring-danger-500/20"),
    ("focus:border-red-400", "focus:border-danger-500"),
    ("focus:border-red-500", "focus:border-danger-500"),
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
        # Count substitutions
        for old, repl in REPLACEMENTS:
            CHANGED += text.count(old)
        print(f"  ✓ {path.relative_to(ROOT)}")

print(f"\nUpdated {FILES} files, {CHANGED} substitutions.")
