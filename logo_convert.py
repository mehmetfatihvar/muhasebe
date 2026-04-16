"""
logo_convert.py — Tek komutla tüm platform ve kullanim icin logo uretici

Kullanim:
    python logo_convert.py logo.png
    python logo_convert.py logo.svg

Uretilen dosyalar (assets/ klasorune):

  Windows
    logo.ico              Uygulama simgesi (16-256px, 7 boyut)
    logo_256.png          Windows Store / yuksek DPI

  macOS
    logo.icns             Mac uygulama simgesi  (pip install icnsutil)

  Web / PWA
    favicon.ico           Tarayici sekmesi (16/32/48px)
    favicon-16.png
    favicon-32.png
    favicon-48.png
    apple-touch-icon.png  iOS ana ekran (180x180)
    icon-192.png          Android / PWA manifest
    icon-512.png          PWA splash screen

  Android
    icon-android-48.png   ldpi
    icon-android-72.png   mdpi
    icon-android-96.png   hdpi
    icon-android-144.png  xhdpi
    icon-android-192.png  xxhdpi

  iOS
    icon-ios-60.png
    icon-ios-76.png
    icon-ios-120.png
    icon-ios-152.png
    icon-ios-180.png

  Masaustu paketleme
    icon-128.png          Linux uygulama menusu
    icon-256.png          Windows installer kenar cubugu
    icon-512.png          Flatpak / AppImage

  Sosyal Medya
    og-image.png          Open Graph - Facebook/LinkedIn (1200x630)
    twitter-card.png      Twitter karti (1200x600)
    icon-512-square.png   Instagram profil (512x512)
"""

import sys, os
from PIL import Image

def kaynak_yukle(dosya):
    ext = os.path.splitext(dosya)[1].lower()
    if ext == '.svg':
        try:
            import cairosvg, io
            png = cairosvg.svg2png(url=os.path.abspath(dosya),
                                   output_width=1024, output_height=1024)
            return Image.open(io.BytesIO(png)).convert('RGBA')
        except ImportError:
            print('[!] SVG destegi icin: pip install cairosvg')
            sys.exit(1)
    return Image.open(dosya).convert('RGBA')

def kaydet(img, yol, boyut):
    os.makedirs(os.path.dirname(yol) or '.', exist_ok=True)
    img.resize(boyut, Image.LANCZOS).save(yol, 'PNG')
    print(f'  OK  {yol:50s}  {boyut[0]}x{boyut[1]}')

def og_gorsel(img, yol, w, h):
    os.makedirs(os.path.dirname(yol) or '.', exist_ok=True)
    zemin = Image.new('RGBA', (w, h), (15, 17, 23, 255))
    logo_h = int(h * 0.6)
    logo_w = int(img.width * logo_h / img.height)
    kucuk  = img.resize((logo_w, logo_h), Image.LANCZOS)
    zemin.paste(kucuk, ((w - logo_w) // 2, (h - logo_h) // 2), kucuk)
    zemin.convert('RGB').save(yol, 'PNG')
    print(f'  OK  {yol:50s}  {w}x{h}')

def tum_formatlara_cevir(kaynak_dosya):
    print(f'\nKaynak: {kaynak_dosya}')
    img = kaynak_yukle(kaynak_dosya)
    d   = 'assets'
    os.makedirs(d, exist_ok=True)

    # Windows ICO
    print('\n[Windows ICO]')
    ico_b = [(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)]
    kat   = [img.resize(b, Image.LANCZOS) for b in ico_b]
    ico_p = os.path.join(d, 'logo.ico')
    kat[0].save(ico_p, format='ICO', sizes=ico_b, append_images=kat[1:])
    print(f'  OK  {ico_p:50s}  16-256px (7 boyut)')
    kaydet(img, os.path.join(d, 'logo_256.png'), (256, 256))

    # Web / Favicon
    print('\n[Web / Favicon]')
    fav_b = [(16,16),(32,32),(48,48)]
    fav_k = [img.resize(b, Image.LANCZOS) for b in fav_b]
    fav_p = os.path.join(d, 'favicon.ico')
    fav_k[0].save(fav_p, format='ICO', sizes=fav_b, append_images=fav_k[1:])
    print(f'  OK  {fav_p:50s}  16/32/48px')
    kaydet(img, os.path.join(d, 'favicon-16.png'),       (16,  16))
    kaydet(img, os.path.join(d, 'favicon-32.png'),       (32,  32))
    kaydet(img, os.path.join(d, 'favicon-48.png'),       (48,  48))
    kaydet(img, os.path.join(d, 'apple-touch-icon.png'), (180,180))
    kaydet(img, os.path.join(d, 'icon-192.png'),         (192,192))
    kaydet(img, os.path.join(d, 'icon-512.png'),         (512,512))

    # Masaustu paketleme
    print('\n[Masaustu Paketleme]')
    kaydet(img, os.path.join(d, 'icon-128.png'),  (128,128))
    kaydet(img, os.path.join(d, 'icon-256.png'),  (256,256))

    # Android
    print('\n[Android]')
    for boyut, etiket in [(48,'ldpi'),(72,'mdpi'),(96,'hdpi'),
                          (144,'xhdpi'),(192,'xxhdpi')]:
        kaydet(img, os.path.join(d, f'icon-android-{boyut}.png'), (boyut, boyut))

    # iOS
    print('\n[iOS]')
    for boyut in [60, 76, 120, 152, 180]:
        kaydet(img, os.path.join(d, f'icon-ios-{boyut}.png'), (boyut, boyut))

    # Sosyal Medya
    print('\n[Sosyal Medya]')
    og_gorsel(img, os.path.join(d, 'og-image.png'),       1200, 630)
    og_gorsel(img, os.path.join(d, 'twitter-card.png'),   1200, 600)
    kaydet(img,    os.path.join(d, 'icon-512-square.png'), (512, 512))

    # macOS ICNS (opsiyonel)
    print('\n[macOS ICNS]')
    try:
        import icnsutil, io
        icns_b = {'ic04':(16,16),'ic05':(32,32),'ic07':(128,128),
                  'ic08':(256,256),'ic09':(512,512),'ic10':(1024,1024)}
        icns = icnsutil.IcnsFile()
        for tip, b in icns_b.items():
            buf = io.BytesIO()
            img.resize(b, Image.LANCZOS).save(buf, 'PNG')
            icns.add_media(tip, data=buf.getvalue())
        icns_p = os.path.join(d, 'logo.icns')
        icns.write(icns_p)
        print(f'  OK  {icns_p:50s}  16-1024px')
    except ImportError:
        print('  Atlandi -- macOS icns icin: pip install icnsutil')

    sayi = len([f for f in os.listdir(d) if os.path.isfile(os.path.join(d,f))])
    print(f'\nTamamlandi! {sayi} dosya --> {os.path.abspath(d)}\n')

if __name__ == '__main__':
    if len(sys.argv) < 2:
        for isim in ['logo.png','logo.jpg','logo.svg','logo.webp']:
            aday = os.path.join('assets', isim)
            if os.path.exists(aday):
                print(f'[i] Otomatik bulundu: {aday}')
                tum_formatlara_cevir(aday)
                sys.exit(0)
        print('Kullanim: python logo_convert.py <logo.png|jpg|svg|webp>')
        sys.exit(1)

    dosya = sys.argv[1]
    if not os.path.exists(dosya):
        print(f'[!] Dosya bulunamadi: {dosya}')
        sys.exit(1)

    if os.path.splitext(dosya)[1].lower() not in ('.png','.jpg','.jpeg','.bmp','.webp','.svg'):
        print(f'[!] Desteklenmeyen format')
        sys.exit(1)

    tum_formatlara_cevir(dosya)
