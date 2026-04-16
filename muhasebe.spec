# -*- mode: python ; coding: utf-8 -*-
# PyInstaller spec dosyası — Windows .exe üretir
# Kullanım: pyinstaller muhasebe.spec

import os
datas_list = [
    ('app/db/*.py', 'app/db'),
    ('app/ui/*.py', 'app/ui'),
]
if os.path.exists('assets/logo.ico'):
    datas_list.append(('assets/logo.ico', 'assets'))

icon_path = 'assets/logo.ico' if os.path.exists('assets/logo.ico') else None

block_cipher = None

a = Analysis(
    ['main.py'],
    pathex=['.'],
    binaries=[],
    datas=datas_list,
    hiddenimports=[
        'PyQt5.QtCore',
        'PyQt5.QtGui',
        'PyQt5.QtWidgets',
        'sqlite3',
        'json',
        'csv',
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=['matplotlib', 'numpy', 'pandas', 'scipy', 'tkinter'],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name='MuhasebeSistemi',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,           # Konsol penceresi çıkmaz
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=icon_path,
    version='version_info.txt',
)
