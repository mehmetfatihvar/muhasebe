# -*- mode: python ; coding: utf-8 -*-
import os

datas_list = [
    ("app/db/*.py", "app/db"),
    ("app/ui/*.py", "app/ui"),
]
if os.path.exists("assets/logo.ico"):
    datas_list.append(("assets/logo.ico", "assets"))

icon_path = "assets/logo.ico" if os.path.exists("assets/logo.ico") else None
version_path = "version_info.txt" if os.path.exists("version_info.txt") else None

a = Analysis(
    ["main.py"],
    pathex=["."],
    binaries=[],
    datas=datas_list,
    hiddenimports=[
        "PyQt5.QtCore",
        "PyQt5.QtGui",
        "PyQt5.QtWidgets",
        "sqlite3",
        "json",
        "csv",
    ],
    hookspath=[],
    runtime_hooks=[],
    excludes=["matplotlib", "numpy", "pandas", "scipy", "tkinter"],
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name="MuhasebeSistemi",
    debug=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    icon=icon_path,
    version=version_path,
)
