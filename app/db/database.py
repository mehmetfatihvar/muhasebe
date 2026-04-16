import sqlite3
import json
import os
from datetime import datetime

DB_PATH = os.path.join(os.path.dirname(__file__), '..', '..', 'muhasebe.db')

def baglanti():
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn

def veritabani_olustur():
    conn = baglanti()
    c = conn.cursor()
    c.executescript("""
        CREATE TABLE IF NOT EXISTS kalemler (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            tur TEXT NOT NULL,
            ana_kategori TEXT NOT NULL,
            alt_kategori TEXT NOT NULL,
            kalem_adi TEXT NOT NULL,
            aciklama TEXT,
            aktif INTEGER DEFAULT 1,
            eklenme_tarihi TEXT
        );

        CREATE TABLE IF NOT EXISTS hareketler (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            islem_no TEXT UNIQUE,
            tarih TEXT NOT NULL,
            tur TEXT NOT NULL,
            ana_kategori TEXT,
            alt_kategori TEXT,
            kalem_adi TEXT,
            aciklama TEXT,
            tutar REAL NOT NULL,
            giris REAL DEFAULT 0,
            cikis REAL DEFAULT 0,
            kimden_kime TEXT,
            odeme_turu TEXT,
            belge_no TEXT,
            bakiye REAL,
            durum TEXT DEFAULT '✅',
            kayit_tarihi TEXT
        );

        CREATE TABLE IF NOT EXISTS sayaclar (
            tur TEXT PRIMARY KEY,
            deger INTEGER DEFAULT 0
        );
    """)
    # Sayaçları başlat
    for tur in ['GD', 'GL', 'KG']:
        c.execute("INSERT OR IGNORE INTO sayaclar VALUES (?, 0)", (tur,))
    conn.commit()

    # Örnek kalemler yoksa ekle
    if c.execute("SELECT COUNT(*) FROM kalemler").fetchone()[0] == 0:
        bugun = datetime.now().strftime('%d.%m.%Y')
        ornek = [
            ('Gider','Personel','Maaş','Personel Maaşı','Aylık maaş ödemeleri'),
            ('Gider','Kira','Ofis','Ofis Kirası','Aylık kira gideri'),
            ('Gider','Fatura','Elektrik','Elektrik Gideri','Elektrik faturası'),
            ('Gider','Fatura','Su','Su Gideri','Su faturası'),
            ('Gider','Fatura','İnternet','İnternet Gideri','İnternet aboneliği'),
            ('Gelir','Satış','Nakit Satış','Nakit Satış Geliri','Nakit tahsilat'),
            ('Gelir','Satış','Havale/EFT','Banka Transferi Geliri','Havale ile tahsilat'),
            ('Gelir','Hizmet','Danışmanlık','Danışmanlık Geliri','Proje/hizmet geliri'),
            ('Kasa Giriş','Sermaye','Ortak Girişi','Kasaya Ortak Para Girişi','Ortak sermaye katkısı'),
            ('Kasa Giriş','Tahsilat','Borç Tahsil','Alacak Tahsilatı','Alınan borç geri ödemesi'),
        ]
        c.executemany(
            "INSERT INTO kalemler (tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi) VALUES (?,?,?,?,?,?)",
            [(*o, bugun) for o in ornek]
        )
        conn.commit()
    conn.close()

# ── SAYAÇ ──────────────────────────────────────────────
def yeni_islem_no(tur_kisa):
    conn = baglanti()
    c = conn.cursor()
    c.execute("UPDATE sayaclar SET deger = deger + 1 WHERE tur = ?", (tur_kisa,))
    deger = c.execute("SELECT deger FROM sayaclar WHERE tur=?", (tur_kisa,)).fetchone()[0]
    conn.commit(); conn.close()
    return f"{tur_kisa}-{str(deger).zfill(5)}"

# ── KALEMLERs ──────────────────────────────────────────
def kalem_listesi():
    conn = baglanti()
    rows = conn.execute("SELECT * FROM kalemler ORDER BY id").fetchall()
    conn.close()
    return [dict(r) for r in rows]

def kalem_ekle(tur, ana, alt, ad, aciklama=''):
    conn = baglanti()
    conn.execute(
        "INSERT INTO kalemler (tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi) VALUES (?,?,?,?,?,?)",
        (tur, ana, alt, ad, aciklama, datetime.now().strftime('%d.%m.%Y'))
    )
    conn.commit(); conn.close()

def kalem_sil(kid):
    conn = baglanti()
    conn.execute("DELETE FROM kalemler WHERE id=?", (kid,))
    conn.commit(); conn.close()

def kategoriler(tur):
    conn = baglanti()
    rows = conn.execute(
        "SELECT DISTINCT ana_kategori FROM kalemler WHERE tur=? AND aktif=1", (tur,)
    ).fetchall()
    conn.close()
    return [r[0] for r in rows]

def alt_kalemler(tur, ana):
    conn = baglanti()
    rows = conn.execute(
        "SELECT * FROM kalemler WHERE tur=? AND ana_kategori=? AND aktif=1", (tur, ana)
    ).fetchall()
    conn.close()
    return [dict(r) for r in rows]

# ── HAREKETLER ─────────────────────────────────────────
def mevcut_bakiye():
    conn = baglanti()
    row = conn.execute("SELECT COALESCE(SUM(giris)-SUM(cikis),0) FROM hareketler").fetchone()
    conn.close()
    return row[0]

def hareket_ekle(veri: dict):
    tur = veri['tur']
    prefix = 'GD' if tur=='Gider' else ('GL' if tur=='Gelir' else 'KG')
    no = yeni_islem_no(prefix)
    tutar = float(veri['tutar'])
    giris = tutar if tur in ('Gelir','Kasa Giriş') else 0
    cikis = tutar if tur == 'Gider' else 0
    bakiye = mevcut_bakiye() + giris - cikis
    conn = baglanti()
    conn.execute("""
        INSERT INTO hareketler
        (islem_no,tarih,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,
         tutar,giris,cikis,kimden_kime,odeme_turu,belge_no,bakiye,kayit_tarihi)
        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
    """, (
        no, veri['tarih'], tur,
        veri.get('ana',''), veri.get('alt',''), veri.get('kalem',''),
        veri.get('aciklama',''), tutar, giris, cikis,
        veri.get('kimden',''), veri.get('odeme',''), veri.get('belge',''),
        bakiye, datetime.now().strftime('%d.%m.%Y')
    ))
    conn.commit(); conn.close()
    return no

def hareket_sil(hid):
    conn = baglanti()
    conn.execute("DELETE FROM hareketler WHERE id=?", (hid,))
    # Bakiyeleri yeniden hesapla
    rows = conn.execute("SELECT id,giris,cikis FROM hareketler ORDER BY id").fetchall()
    bak = 0
    for r in rows:
        bak += r['giris'] - r['cikis']
        conn.execute("UPDATE hareketler SET bakiye=? WHERE id=?", (bak, r['id']))
    conn.commit(); conn.close()

def kisi_listesi():
    """Hareketlerde daha önce girilmiş benzersiz kişi/şirket isimlerini döndürür"""
    conn = baglanti()
    rows = conn.execute("""
        SELECT DISTINCT kimden_kime FROM hareketler
        WHERE kimden_kime IS NOT NULL AND kimden_kime != ''
        ORDER BY kimden_kime
    """).fetchall()
    conn.close()
    return [r[0] for r in rows]

def hareket_listesi(tarih_bas=None, tarih_bit=None, tur=None, ara=None,
                    kalem=None, ana_kategori=None, kimden=None,
                    tutar_min=None, tutar_max=None, odeme_turu=None):
    conn = baglanti()
    q = "SELECT * FROM hareketler WHERE 1=1"
    params = []
    if tarih_bas:     q += " AND tarih >= ?";              params.append(tarih_bas)
    if tarih_bit:     q += " AND tarih <= ?";              params.append(tarih_bit)
    if tur:           q += " AND tur = ?";                 params.append(tur)
    if ana_kategori:  q += " AND ana_kategori = ?";        params.append(ana_kategori)
    if kalem:         q += " AND kalem_adi LIKE ?";        params.append(f'%{kalem}%')
    if kimden:        q += " AND kimden_kime LIKE ?";      params.append(f'%{kimden}%')
    if odeme_turu:    q += " AND odeme_turu = ?";          params.append(odeme_turu)
    if tutar_min is not None: q += " AND tutar >= ?";      params.append(tutar_min)
    if tutar_max is not None: q += " AND tutar <= ?";      params.append(tutar_max)
    if ara:
        q += """ AND (kalem_adi LIKE ? OR kimden_kime LIKE ?
                   OR belge_no LIKE ? OR aciklama LIKE ?
                   OR ana_kategori LIKE ? OR islem_no LIKE ?)"""
        params += [f'%{ara}%']*6
    q += " ORDER BY id DESC"
    rows = conn.execute(q, params).fetchall()
    conn.close()
    return [dict(r) for r in rows]

# ── RAPORLAR ───────────────────────────────────────────
def aylik_ozet():
    conn = baglanti()
    rows = conn.execute("""
        SELECT strftime('%Y-%m', tarih) as ay,
               SUM(giris) as gelir, SUM(cikis) as gider, COUNT(*) as sayi
        FROM hareketler GROUP BY ay ORDER BY ay DESC
    """).fetchall()
    conn.close()
    return [dict(r) for r in rows]

def kategori_dagilim():
    conn = baglanti()
    rows = conn.execute("""
        SELECT tur, ana_kategori, SUM(tutar) as tutar, COUNT(*) as sayi
        FROM hareketler GROUP BY tur, ana_kategori ORDER BY tutar DESC
    """).fetchall()
    conn.close()
    return [dict(r) for r in rows]

def top_kalemler(limit=5):
    conn = baglanti()
    rows = conn.execute("""
        SELECT kalem_adi, COUNT(*) as sayi, SUM(tutar) as tutar
        FROM hareketler GROUP BY kalem_adi ORDER BY sayi DESC LIMIT ?
    """, (limit,)).fetchall()
    conn.close()
    return [dict(r) for r in rows]

def genel_ozet():
    conn = baglanti()
    r = conn.execute("""
        SELECT
            COUNT(*) as toplam_hareket,
            COALESCE(SUM(CASE WHEN tur='Gelir' THEN tutar ELSE 0 END),0) as toplam_gelir,
            COALESCE(SUM(CASE WHEN tur='Gider' THEN tutar ELSE 0 END),0) as toplam_gider,
            COALESCE(SUM(CASE WHEN tur='Kasa Giriş' THEN tutar ELSE 0 END),0) as kasa_giris,
            COALESCE(SUM(giris)-SUM(cikis),0) as bakiye
        FROM hareketler
    """).fetchone()
    kalem_sayisi = conn.execute("SELECT COUNT(*) FROM kalemler").fetchone()[0]
    conn.close()
    d = dict(r)
    d['kalem_sayisi'] = kalem_sayisi
    return d

# ── YEDEKLEME ──────────────────────────────────────────
def json_yedek_al(dosya_yolu):
    veri = {
        'kalemler': kalem_listesi(),
        'hareketler': hareket_listesi(),
        'tarih': datetime.now().isoformat()
    }
    with open(dosya_yolu, 'w', encoding='utf-8') as f:
        json.dump(veri, f, ensure_ascii=False, indent=2)

def json_yedek_yukle(dosya_yolu):
    with open(dosya_yolu, encoding='utf-8') as f:
        veri = json.load(f)
    conn = baglanti()
    conn.execute("DELETE FROM hareketler")
    conn.execute("DELETE FROM kalemler")
    conn.execute("DELETE FROM sayaclar")
    for tur in ['GD','GL','KG']:
        conn.execute("INSERT INTO sayaclar VALUES (?,0)", (tur,))
    for k in veri.get('kalemler', []):
        conn.execute("""INSERT INTO kalemler
            (id,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi)
            VALUES (?,?,?,?,?,?,?)""",
            (k.get('id'), k.get('tur',''), k.get('ana_kategori', k.get('ana','')),
             k.get('alt_kategori', k.get('alt','')), k.get('kalem_adi', k.get('ad','')),
             k.get('aciklama',''), k.get('eklenme_tarihi', k.get('tarih','')))
        )
    for h in veri.get('hareketler', []):
        no = h.get('islem_no', h.get('no',''))
        prefix = no[:2] if no else 'GD'
        try:
            sayi = int(no.split('-')[-1]) if no else 0
        except: sayi = 0
        conn.execute("UPDATE sayaclar SET deger=MAX(deger,?) WHERE tur=?", (sayi, prefix))
        conn.execute("""INSERT OR IGNORE INTO hareketler
            (islem_no,tarih,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,
             tutar,giris,cikis,kimden_kime,odeme_turu,belge_no,bakiye,kayit_tarihi)
            VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (no, h.get('tarih',''), h.get('tur',''),
             h.get('ana_kategori', h.get('ana','')),
             h.get('alt_kategori', h.get('alt','')),
             h.get('kalem_adi', h.get('kalem','')),
             h.get('aciklama',''), h.get('tutar',0),
             h.get('giris',0), h.get('cikis',0),
             h.get('kimden_kime', h.get('kimden','')),
             h.get('odeme_turu', h.get('odeme','')),
             h.get('belge_no', h.get('belge','')),
             h.get('bakiye',0), h.get('kayit_tarihi',''))
        )
    conn.commit(); conn.close()
