"""
logo_convert.py — Logo dosyasını Windows .ico formatına çevirir

Kullanım:
    python logo_convert.py logo.png
    python logo_convert.py logo.svg   (SVG için 'cairosvg' gerekir: pip install cairosvg)

Çıktı: assets/logo.ico
"""

import sys
import os
from PIL import Image

def png_to_ico(kaynak, hedef='assets/logo.ico'):
    os.makedirs('assets', exist_ok=True)
    img = Image.open(kaynak).convert('RGBA')

    # Windows için gerekli tüm boyutlar
    boyutlar = [(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)]
    katmanlar = []
    for b in boyutlar:
        katman = img.resize(b, Image.LANCZOS)
        katmanlar.append(katman)

    katmanlar[0].save(
        hedef,
        format='ICO',
        sizes=boyutlar,
        append_images=katmanlar[1:]
    )
    print(f'✅ ICO oluşturuldu: {hedef}')
    print(f'   Boyutlar: {", ".join(str(b[0])+"px" for b in boyutlar)}')

def svg_to_ico(kaynak, hedef='assets/logo.ico'):
    try:
        import cairosvg
    except ImportError:
        print('SVG için cairosvg gerekli: pip install cairosvg')
        sys.exit(1)
    import io
    os.makedirs('assets', exist_ok=True)
    png_data = cairosvg.svg2png(url=kaynak, output_width=256, output_height=256)
    img = Image.open(io.BytesIO(png_data)).convert('RGBA')
    img.save('_temp_logo.png')
    png_to_ico('_temp_logo.png', hedef)
    os.remove('_temp_logo.png')

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print('Kullanım: python logo_convert.py <logo.png veya logo.svg>')
        sys.exit(1)

    dosya = sys.argv[1]
    if not os.path.exists(dosya):
        print(f'Dosya bulunamadı: {dosya}')
        sys.exit(1)

    ext = os.path.splitext(dosya)[1].lower()
    if ext == '.svg':
        svg_to_ico(dosya)
    elif ext in ('.png', '.jpg', '.jpeg', '.bmp', '.webp'):
        png_to_ico(dosya)
    else:
        print(f'Desteklenmeyen format: {ext}')
        sys.exit(1)
