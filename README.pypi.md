# AobscanFast Python

Python bindings for the native AobscanFast memory scanner.

```bash
pip install aobscanfast
```

```python
from aobscanfast import AobScanner

with AobScanner(pid=1234) as scanner:
    matches = scanner.scan("48 8B ?? ?? ?? AA")
```

The package bundles the NativeAOT library for supported Windows and Linux x64 platforms.
