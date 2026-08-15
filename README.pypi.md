# AobscanFast Python

Python bindings for the native AobscanFast memory scanner.

```bash
pip install aobscanfast
```

```python
from aobscanfast import AobScanner

scanner = AobScanner(pid=1234)
```

The package bundles the NativeAOT library for supported Windows and Linux x64 platforms.
