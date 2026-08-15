"""Compatibility import for the pre-package Python binding."""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from aobscanfast import AobError, AobScanner

__all__ = ["AobError", "AobScanner"]
