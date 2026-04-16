"""
logo_convert.py — Profesyonel ICO dönüştürücü
===============================================
Desteklenen girdi: PNG, JPG, JPEG, BMP, WEBP, SVG, TIFF, ICO
Çıktı: assets/logo.ico (Windows için tüm boyutlar)

Kullanım:
    python logo_convert.py logo.png
    python logo_convert.py logo.svg
    python logo_convert.py logo.png --cikti assets/ozel.ico
    python logo_convert.py logo.png --arkaplan beyaz
    python logo_convert.py logo.png --dolgu
"""

import sys
import os
import argparse
from PIL import Image, ImageOps, ImageFilter

ICO_BOYUTLARI = [16, 24, 32, 48, 64, 128, 256]


def log(msg, seviye='INFO'):
    renk = {'INFO': '\033[94m', 'OK': '\033[92m', 'WARN': '\033[93m', 'ERR': '\033[91m'}
    print(f"{renk.get(seviye,'')}{msg}\033[0m")


def svg_to_pil(dosya, boyut=512):
    try:
        import cairosvg, io
        png = cairosvg.svg2png(url=dosya, output_width=boyut, output_height=boyut)
        return Image.open(io.BytesIO(png)).convert('RGBA')
    except ImportError:
        log("SVG icin 'cairosvg' gerekli: pip install cairosvg", 'ERR'); sys.exit(1)
    except Exception as e:
        log(f"SVG okunamadi: {e}", 'ERR'); sys.exit(1)


def gorsel_yukle(dosya):
    ext = os.path.splitext(dosya)[1].lower()
    if ext == '.svg':
        log("SVG render ediliyor (512px)...")
        return svg_to_pil(dosya, 512)
    try:
        img = Image.open(dosya)
        if ext == '.ico':
            # ICO icin en buyuk boyutu sec
            boyutlar = getattr(img, 'ico', {}).get('sizes', []) or [(img.width, img.height)]
            en_buyuk = max(boyutlar, key=lambda x: x[0])
            img = img.resize(en_buyuk, Image.LANCZOS)
        img = img.convert('RGBA')
        log(f"Gorsel yuklendi: {img.width}x{img.height}px")
        return img
    except Exception as e:
        log(f"Dosya acilamadi: {e}", 'ERR'); sys.exit(1)


def arkaplan_uygula(img, arkaplan='seffaf'):
    if arkaplan == 'seffaf':
        return img
    if arkaplan == 'beyaz':
        renk = (255, 255, 255, 255)
    elif arkaplan == 'siyah':
        renk = (0, 0, 0, 255)
    elif arkaplan.startswith('#'):
        h = arkaplan.lstrip('#')
        renk = tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)
    else:
        renk = (255, 255, 255, 255)
    zemin = Image.new('RGBA', img.size, renk)
    zemin.paste(img, mask=img.split()[3])
    return zemin


def boyutlandir(img, hedef, dolgu=False):
    w, h = img.size
    if dolgu:
        oran = max(hedef / w, hedef / h)
        nw, nh = int(w * oran), int(h * oran)
        tmp = img.resize((nw, nh), Image.LANCZOS)
        sol, ust = (nw - hedef) // 2, (nh - hedef) // 2
        return tmp.crop((sol, ust, sol + hedef, ust + hedef))
    else:
        # %80 ic alan, %10 padding her taraftan
        ic = int(hedef * 0.80)
        oran = min(ic / w, ic / h)
        nw, nh = max(1, int(w * oran)), max(1, int(h * oran))
        kucuk = img.resize((nw, nh), Image.LANCZOS)
        zemin = Image.new('RGBA', (hedef, hedef), (0, 0, 0, 0))
        x, y = (hedef - nw) // 2, (hedef - nh) // 2
        zemin.paste(kucuk, (x, y), kucuk.split()[3])
        return zemin


def ico_olustur(img, cikti, dolgu=False):
    os.makedirs(os.path.dirname(os.path.abspath(cikti)), exist_ok=True)
    katmanlar = []
    log("\nBoyutlar olusturuluyor:")
    for b in ICO_BOYUTLARI:
        katman = boyutlandir(img, b, dolgu)
        if b <= 32:
            katman = katman.filter(ImageFilter.SHARPEN)
        katmanlar.append(katman)
        log(f"  + {b}x{b}px", 'OK')

    katmanlar[0].save(
        cikti, format='ICO',
        sizes=[(b, b) for b in ICO_BOYUTLARI],
        append_images=katmanlar[1:]
    )
    kb = os.path.getsize(cikti) / 1024
    log(f"\n ICO olusturuldu : {cikti}", 'OK')
    log(f"   Dosya boyutu  : {kb:.1f} KB")
    log(f"   Boyutlar      : {', '.join(str(b)+'px' for b in ICO_BOYUTLARI)}")


def main():
    parser = argparse.ArgumentParser(
        description='Profesyonel Logo -> ICO donusturucu',
        epilog="""
Ornekler:
  python logo_convert.py logo.png
  python logo_convert.py logo.svg --cikti assets/uygulama.ico
  python logo_convert.py logo.png --arkaplan beyaz
  python logo_convert.py logo.png --arkaplan '#1a1a2e'
  python logo_convert.py logo.png --dolgu
        """
    )
    parser.add_argument('girdi', help='Logo dosyasi (PNG, SVG, JPG, BMP, WEBP)')
    parser.add_argument('--cikti', default='assets/logo.ico', help='Cikti ICO yolu')
    parser.add_argument('--arkaplan', default='seffaf',
                        help='Arka plan: seffaf (varsayilan), beyaz, siyah, #RRGGBB')
    parser.add_argument('--dolgu', action='store_true', help='Padding olmadan tam doldur')
    args = parser.parse_args()

    if not os.path.exists(args.girdi):
        log(f"Dosya bulunamadi: {args.girdi}", 'ERR'); sys.exit(1)

    log("=" * 48)
    log("  Logo -> ICO Profesyonel Donusturucu")
    log("=" * 48)
    log(f"Girdi    : {args.girdi}")
    log(f"Cikti    : {args.cikti}")
    log(f"Arkaplan : {args.arkaplan}")
    log(f"Dolgu    : {'Tam doldur' if args.dolgu else 'Padding var (%10)'}")

    img = gorsel_yukle(args.girdi)
    img = arkaplan_uygula(img, args.arkaplan)
    ico_olustur(img, args.cikti, dolgu=args.dolgu)
    log("\nTamamlandi!", 'OK')


if __name__ == '__main__':
    main()
