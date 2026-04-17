"""
rapor_pdf.py — Dönem Kapanış Raporu PDF Üreticisi
==================================================
reportlab kütüphanesi ile profesyonel PDF raporu üretir.
"""

import os
from datetime import datetime
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import cm
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_RIGHT, TA_LEFT
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table,
                                 TableStyle, HRFlowable, PageBreak)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus.flowables import KeepTogether

import app.db.database as db

# ── RENKLER ────────────────────────────────────────────────────────────
KOYU   = colors.HexColor('#0f1117')
YUZ    = colors.HexColor('#181c27')
SINIR  = colors.HexColor('#2a3050')
ACCENT = colors.HexColor('#3d7fff')
YESIL  = colors.HexColor('#2eca8b')
KIRMIZI= colors.HexColor('#ff4d6d')
SARI   = colors.HexColor('#ffc107')
METIN  = colors.HexColor('#1a1a2e')
METIN2 = colors.HexColor('#4a4a6a')
BEYAZ  = colors.white
GRI    = colors.HexColor('#f0f2f8')
GRI2   = colors.HexColor('#e0e4f0')


def para_format(tutar):
    return f"₺{abs(tutar):,.2f}".replace(',', 'X').replace('.', ',').replace('X', '.')


def tarih_format(t):
    if not t: return '-'
    try:
        return datetime.strptime(t, '%Y-%m-%d').strftime('%d.%m.%Y')
    except:
        return t


def rapor_uret(dosya_yolu: str, tarih_bas: str, tarih_bit: str,
               firma_adi: str = 'Firma Muhasebe Sistemi') -> str:
    """
    Dönem kapanış raporunu PDF olarak üretir.
    Returns: dosya_yolu
    """
    doc = SimpleDocTemplate(
        dosya_yolu, pagesize=A4,
        leftMargin=2*cm, rightMargin=2*cm,
        topMargin=2.5*cm, bottomMargin=2*cm,
        title=f'Dönem Kapanış Raporu — {firma_adi}',
        author='Muhasebe Sistemi',
    )

    stiller = _stiller()
    hikaye = []

    # Veri çek
    hareketler = db.hareket_listesi(tarih_bas=tarih_bas, tarih_bit=tarih_bit)
    top_gelir   = sum(h['giris'] for h in hareketler)
    top_gider   = sum(h['cikis'] for h in hareketler)
    net         = top_gelir - top_gider
    baslangic_bakiye = db.hareket_listesi(tarih_bit=tarih_bas)
    onceki_bakiye = sum(h['giris'] - h['cikis'] for h in baslangic_bakiye) if baslangic_bakiye else 0
    son_bakiye    = onceki_bakiye + net

    # ── BAŞLIK SAYFASI ──────────────────────────────────────────────────
    hikaye.append(Spacer(1, 1*cm))
    hikaye.append(Paragraph(firma_adi, stiller['firma']))
    hikaye.append(Spacer(1, 0.4*cm))
    hikaye.append(Paragraph('DÖNEM KAPANIŞ RAPORU', stiller['rapor_baslik']))
    hikaye.append(Spacer(1, 0.3*cm))
    hikaye.append(Paragraph(
        f"{tarih_format(tarih_bas)}  —  {tarih_format(tarih_bit)}",
        stiller['donem']
    ))
    hikaye.append(Spacer(1, 0.2*cm))
    hikaye.append(Paragraph(
        f"Rapor tarihi: {datetime.now().strftime('%d.%m.%Y %H:%M')}",
        stiller['kucuk_metin']
    ))
    hikaye.append(HRFlowable(width='100%', thickness=2, color=ACCENT, spaceAfter=20))

    # ── ÖZET KPI KUTULARI ───────────────────────────────────────────────
    hikaye.append(Paragraph('📊  DÖNEM ÖZETİ', stiller['bolum_baslik']))
    hikaye.append(Spacer(1, 0.3*cm))

    ozet_data = [
        ['AÇILIŞ BAKİYESİ', 'TOPLAM GELİR', 'TOPLAM GİDER', 'NET', 'KAPANIŞ BAKİYESİ'],
        [
            para_format(onceki_bakiye),
            para_format(top_gelir),
            para_format(top_gider),
            para_format(net),
            para_format(son_bakiye),
        ]
    ]
    ozet_tablo = Table(ozet_data, colWidths=[3.2*cm]*5)
    ozet_tablo.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,0), ACCENT),
        ('TEXTCOLOR',  (0,0), (-1,0), BEYAZ),
        ('FONTNAME',   (0,0), (-1,0), 'Helvetica-Bold'),
        ('FONTSIZE',   (0,0), (-1,0), 9),
        ('ALIGN',      (0,0), (-1,-1), 'CENTER'),
        ('VALIGN',     (0,0), (-1,-1), 'MIDDLE'),
        ('ROWHEIGHT',  (0,0), (-1,0), 22),

        ('BACKGROUND', (0,1), (0,1),  GRI),
        ('BACKGROUND', (1,1), (1,1),  colors.HexColor('#e8f8f2')),  # yeşilimsi
        ('BACKGROUND', (2,1), (2,1),  colors.HexColor('#fff0f3')),  # kırmızımsı
        ('TEXTCOLOR',  (1,1), (1,1),  YESIL),
        ('TEXTCOLOR',  (2,1), (2,1),  KIRMIZI),
        ('TEXTCOLOR',  (3,1), (3,1),  YESIL if net >= 0 else KIRMIZI),
        ('FONTNAME',   (0,1), (-1,1), 'Helvetica-Bold'),
        ('FONTSIZE',   (0,1), (-1,1), 11),
        ('ROWHEIGHT',  (0,1), (-1,1), 28),

        ('GRID',       (0,0), (-1,-1), 0.5, SINIR),
        ('ROUNDEDCORNERS', [3]),
    ]))
    hikaye.append(ozet_tablo)
    hikaye.append(Spacer(1, 0.5*cm))

    # İşlem sayısı
    hikaye.append(Paragraph(
        f"Toplam <b>{len(hareketler)}</b> hareket &nbsp;|&nbsp; "
        f"Gelir: <b>{sum(1 for h in hareketler if h['tur']=='Gelir')}</b> &nbsp;|&nbsp; "
        f"Gider: <b>{sum(1 for h in hareketler if h['tur']=='Gider')}</b> &nbsp;|&nbsp; "
        f"Kasa Giriş: <b>{sum(1 for h in hareketler if h['tur']=='Kasa Giriş')}</b>",
        stiller['kucuk_metin']
    ))
    hikaye.append(Spacer(1, 0.6*cm))

    # ── KATEGORİ BAZLI DAĞILIM ──────────────────────────────────────────
    hikaye.append(HRFlowable(width='100%', thickness=1, color=SINIR, spaceAfter=10))
    hikaye.append(Paragraph('📂  KATEGORİ BAZLI DAĞILIM', stiller['bolum_baslik']))
    hikaye.append(Spacer(1, 0.3*cm))

    # Kategorileri hesapla
    kategori_ozet = {}
    for h in hareketler:
        key = (h['tur'], h['ana_kategori'])
        if key not in kategori_ozet:
            kategori_ozet[key] = {'tutar': 0, 'sayi': 0}
        kategori_ozet[key]['tutar'] += h['tutar']
        kategori_ozet[key]['sayi']  += 1

    kat_data = [['Tür', 'Kategori', 'İşlem Sayısı', 'Toplam Tutar', 'Pay %']]
    toplam_tum = top_gelir + top_gider
    for (tur, ana), v in sorted(kategori_ozet.items(), key=lambda x: -x[1]['tutar']):
        pay = (v['tutar'] / toplam_tum * 100) if toplam_tum else 0
        kat_data.append([tur, ana, str(v['sayi']), para_format(v['tutar']), f"%{pay:.1f}"])

    if len(kat_data) > 1:
        kat_tablo = Table(kat_data, colWidths=[2.5*cm, 4.5*cm, 2.5*cm, 3.5*cm, 2*cm])
        tur_renkler = {'Gelir': colors.HexColor('#e8f8f2'), 'Gider': colors.HexColor('#fff0f3'), 'Kasa Giriş': colors.HexColor('#e8f0ff')}
        tur_metin   = {'Gelir': YESIL, 'Gider': KIRMIZI, 'Kasa Giriş': ACCENT}

        stil = [
            ('BACKGROUND', (0,0), (-1,0), YUZ),
            ('TEXTCOLOR',  (0,0), (-1,0), BEYAZ),
            ('FONTNAME',   (0,0), (-1,0), 'Helvetica-Bold'),
            ('FONTSIZE',   (0,0), (-1,0), 9),
            ('ALIGN',      (0,0), (-1,-1), 'CENTER'),
            ('ALIGN',      (1,1), (1,-1), 'LEFT'),
            ('GRID',       (0,0), (-1,-1), 0.3, SINIR),
            ('ROWHEIGHT',  (0,0), (-1,-1), 20),
            ('FONTSIZE',   (0,1), (-1,-1), 9),
        ]
        for i, (tur, ana), in enumerate(sorted(kategori_ozet.keys(), key=lambda x: -kategori_ozet[x]['tutar']), 1):
            bg = tur_renkler.get(tur, GRI)
            stil.append(('BACKGROUND', (0,i), (0,i), bg))
            stil.append(('TEXTCOLOR',  (0,i), (0,i), tur_metin.get(tur, METIN)))
            if i % 2 == 0:
                stil.append(('BACKGROUND', (1,i), (-1,i), GRI))

        kat_tablo.setStyle(TableStyle(stil))
        hikaye.append(kat_tablo)
    else:
        hikaye.append(Paragraph('Bu dönemde kayıt bulunamadı.', stiller['kucuk_metin']))
    hikaye.append(Spacer(1, 0.6*cm))

    # ── AYLIK ÖZET ──────────────────────────────────────────────────────
    aylik = db.aylik_ozet()
    # Sadece seçili dönemdeki ayları filtrele
    aylik_donem = [a for a in aylik if tarih_bas[:7] <= a['ay'] <= tarih_bit[:7]]

    if aylik_donem:
        hikaye.append(HRFlowable(width='100%', thickness=1, color=SINIR, spaceAfter=10))
        hikaye.append(Paragraph('📅  AYLIK GELİR / GİDER ÖZETİ', stiller['bolum_baslik']))
        hikaye.append(Spacer(1, 0.3*cm))

        ay_data = [['Ay', 'Gelir', 'Gider', 'Net', 'İşlem']]
        for a in aylik_donem:
            net_ay = a['gelir'] - a['gider']
            ay_data.append([
                a['ay'],
                para_format(a['gelir']),
                para_format(a['gider']),
                para_format(net_ay),
                str(a['sayi'])
            ])

        ay_tablo = Table(ay_data, colWidths=[3*cm, 3.5*cm, 3.5*cm, 3.5*cm, 2*cm])
        ay_stil = [
            ('BACKGROUND', (0,0), (-1,0), YUZ),
            ('TEXTCOLOR',  (0,0), (-1,0), BEYAZ),
            ('FONTNAME',   (0,0), (-1,0), 'Helvetica-Bold'),
            ('FONTSIZE',   (0,0), (-1,-1), 9),
            ('ALIGN',      (1,0), (-1,-1), 'RIGHT'),
            ('ALIGN',      (0,0), (0,-1), 'CENTER'),
            ('GRID',       (0,0), (-1,-1), 0.3, SINIR),
            ('ROWHEIGHT',  (0,0), (-1,-1), 20),
        ]
        for i, a in enumerate(aylik_donem, 1):
            net_ay = a['gelir'] - a['gider']
            if i % 2 == 0:
                ay_stil.append(('BACKGROUND', (0,i), (-1,i), GRI))
            renk = YESIL if net_ay >= 0 else KIRMIZI
            ay_stil.append(('TEXTCOLOR', (3,i), (3,i), renk))
            ay_stil.append(('FONTNAME',  (3,i), (3,i), 'Helvetica-Bold'))

        ay_tablo.setStyle(TableStyle(ay_stil))
        hikaye.append(ay_tablo)
        hikaye.append(Spacer(1, 0.6*cm))

    # ── TÜM HAREKETLER LİSTESİ ──────────────────────────────────────────
    hikaye.append(PageBreak())
    hikaye.append(Paragraph('🗂  TÜM HAREKETLER', stiller['bolum_baslik']))
    hikaye.append(Spacer(1, 0.3*cm))

    h_data = [['İşlem No', 'Tarih', 'Tür', 'Kategori', 'Kalem', 'Tutar', 'Bakiye']]
    for h in hareketler:
        h_data.append([
            h['islem_no'],
            tarih_format(h['tarih']),
            h['tur'],
            h['ana_kategori'],
            h['kalem_adi'],
            para_format(h['tutar']),
            para_format(h['bakiye']),
        ])

    h_tablo = Table(h_data, colWidths=[2.2*cm, 2*cm, 2*cm, 2.8*cm, 4*cm, 2.5*cm, 2.5*cm])
    tur_bg = {'Gelir': colors.HexColor('#e8f8f2'), 'Gider': colors.HexColor('#fff0f3'), 'Kasa Giriş': colors.HexColor('#e8f0ff')}
    tur_fg = {'Gelir': YESIL, 'Gider': KIRMIZI, 'Kasa Giriş': ACCENT}

    h_stil = [
        ('BACKGROUND', (0,0), (-1,0), YUZ),
        ('TEXTCOLOR',  (0,0), (-1,0), BEYAZ),
        ('FONTNAME',   (0,0), (-1,0), 'Helvetica-Bold'),
        ('FONTSIZE',   (0,0), (-1,-1), 8),
        ('ALIGN',      (5,0), (-1,-1), 'RIGHT'),
        ('ALIGN',      (0,0), (4,-1), 'LEFT'),
        ('GRID',       (0,0), (-1,-1), 0.2, SINIR),
        ('ROWHEIGHT',  (0,0), (-1,-1), 17),
        ('VALIGN',     (0,0), (-1,-1), 'MIDDLE'),
    ]
    for i, h in enumerate(hareketler, 1):
        if i % 2 == 0:
            h_stil.append(('BACKGROUND', (0,i), (-1,i), GRI))
        h_stil.append(('TEXTCOLOR', (2,i), (2,i), tur_fg.get(h['tur'], METIN)))

    h_tablo.setStyle(TableStyle(h_stil))
    hikaye.append(h_tablo)

    # ── KAPANIŞ İMZA ALANI ──────────────────────────────────────────────
    hikaye.append(Spacer(1, 1.5*cm))
    hikaye.append(HRFlowable(width='100%', thickness=1, color=SINIR, spaceAfter=15))

    imza_data = [
        ['Hazırlayan', '', 'Onaylayan'],
        ['', '', ''],
        ['', '', ''],
        ['Ad Soyad:', '________________________', 'Ad Soyad: ________________________'],
        ['Tarih:',    '________________________', 'Tarih:    ________________________'],
    ]
    imza_tablo = Table(imza_data, colWidths=[5.5*cm, 5.5*cm, 6*cm])
    imza_tablo.setStyle(TableStyle([
        ('FONTSIZE', (0,0), (-1,-1), 9),
        ('FONTNAME', (0,0), (-1,0), 'Helvetica-Bold'),
        ('ALIGN',    (0,0), (-1,-1), 'CENTER'),
        ('TOPPADDING', (0,0), (-1,-1), 4),
    ]))
    hikaye.append(imza_tablo)

    # ── PDF ÜRETİM ──────────────────────────────────────────────────────
    def _sayfa_alt_bilgi(canvas, document):
        canvas.saveState()
        canvas.setFont('Helvetica', 8)
        canvas.setFillColor(METIN2)
        canvas.drawString(2*cm, 1.2*cm, f'{firma_adi}  —  Dönem Raporu  —  {tarih_format(tarih_bas)} / {tarih_format(tarih_bit)}')
        canvas.drawRightString(A4[0] - 2*cm, 1.2*cm, f'Sayfa {document.page}')
        canvas.restoreState()

    doc.build(hikaye, onFirstPage=_sayfa_alt_bilgi, onLaterPages=_sayfa_alt_bilgi)
    return dosya_yolu


def _stiller():
    s = getSampleStyleSheet()
    return {
        'firma': ParagraphStyle('firma', fontName='Helvetica-Bold', fontSize=16,
                                 textColor=METIN, alignment=TA_CENTER, spaceAfter=4),
        'rapor_baslik': ParagraphStyle('rapor_baslik', fontName='Helvetica-Bold', fontSize=22,
                                        textColor=ACCENT, alignment=TA_CENTER, spaceAfter=6),
        'donem': ParagraphStyle('donem', fontName='Helvetica', fontSize=13,
                                 textColor=METIN2, alignment=TA_CENTER, spaceAfter=4),
        'kucuk_metin': ParagraphStyle('kucuk', fontName='Helvetica', fontSize=9,
                                       textColor=METIN2, alignment=TA_CENTER),
        'bolum_baslik': ParagraphStyle('bolum', fontName='Helvetica-Bold', fontSize=11,
                                        textColor=ACCENT, spaceBefore=6, spaceAfter=4),
        'normal': s['Normal'],
    }
