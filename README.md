# 🏢 Firma Muhasebe ve Kasa Takip Sistemi

> **Windows kurulum paketi (.exe installer) için aşağıdaki "Build" bölümüne bakın.**

Modern PyQt5 masaüstü muhasebe uygulaması. SQLite veritabanı ile çalışır.

---

## 🚀 Kurulum ve Çalıştırma

### 1. Gereksinimler
Python 3.8+

### 2. Kütüphaneyi yükle
```bash
pip install PyQt5
```

### 3. Uygulamayı başlat
```bash
python main.py
```

İlk çalıştırmada `muhasebe.db` otomatik oluşturulur.

---

## ✨ Özellikler

| Sekme | Açıklama |
|---|---|
| 🏠 Ana Sayfa | Anlık KPI kartları + son 15 hareket |
| ➕ Gider Ekle | Kategori/alt kalem seçimi, ödeme türü, belge no |
| 💰 Gelir Ekle | Tahsilat kaydı, kimden bilgisi |
| 🏦 Kasa Girişi | Sermaye, ortak katkısı, dış kaynak |
| 📋 Kalem Tanımları | Kategori ve kalemleri ekle/sil |
| 🗂 Tüm Hareketler | Tarih/tür/metin filtresi + CSV export |
| 📊 Gelir/Gider Tablosu | Özet istatistikler + filtrelenmiş liste |
| 📈 Raporlar | Aylık özet, kategori dağılımı, top kalemler |

---

## 💾 Veri ve Yedek

- Veriler `muhasebe.db` (SQLite) dosyasında saklanır
- **Yedekle** → JSON dosyasına export eder
- **Yedek Yükle** → JSON yedekten tüm veriyi geri yükler
  *(Web sürümünün `.json` yedekleriyle de uyumludur)*

---

## 📁 Proje Yapısı

```
muhasebe/
├── main.py                  ← Giriş noktası — bunu çalıştır
├── requirements.txt
├── muhasebe.db              ← Otomatik oluşur, dokunma
├── app/
│   ├── db/
│   │   └── database.py      ← SQLite işlemleri
│   └── ui/
│       ├── ana_pencere.py   ← Ana pencere ve tüm sekmeler
│       ├── widgets.py       ← Yardımcı widget'lar
│       └── style.py         ← Karanlık tema / stylesheet
└── index.html               ← Web sürümü (tarayıcıda çalışır)
```

---

## 🪟 Windows Installer Oluşturma

### Yöntem 1 — Tek tıkla `.exe` (taşınabilir)

```bat
build_windows.bat
```
→ `dist/MuhasebeSistemi.exe` oluşur. Kurulum gerektirmez, direkt çalışır.

### Yöntem 2 — Kurulum sihirbazı (önerilen)

1. `build_windows.bat` çalıştır → `dist/MuhasebeSistemi.exe` oluşur
2. [Inno Setup](https://jrsoftware.org/isinfo.php) kur
3. `installer.iss` dosyasını Inno Setup ile aç → **Build > Compile**
4. `installer_output/MuhasebeSistemi_Kurulum_v1.0.exe` hazır!

Kullanıcı bu dosyayı çalıştırınca:
- Klasik Windows kurulum sihirbazı açılır
- Masaüstüne kısayol seçeneği çıkar
- Programı Ekle/Kaldır listesine eklenir

---

## 🖼️ Logo Ekleme

Logonuzu `.png` veya `.svg` olarak hazırlayıp `assets/` klasörüne koyun:

```
assets/
  logo.png   ← Logonuz buraya
```

Ardından `.ico`'ya dönüştürün:

```bash
python logo_convert.py assets/logo.png
```

`build_windows.bat` zaten bunu otomatik yapar.

---

## 🚀 Geliştirici Olarak Çalıştırma

```bash
pip install PyQt5
python main.py
```

---

## 📁 Proje Yapısı

```
muhasebe/
├── main.py                  ← Giriş noktası
├── build_windows.bat        ← Windows build (tek tıkla .exe)
├── muhasebe.spec            ← PyInstaller ayarları
├── installer.iss            ← Inno Setup kurulum scripti
├── logo_convert.py          ← PNG/SVG → ICO dönüştürücü
├── version_info.txt         ← .exe metadata (sürüm, yayıncı)
├── requirements.txt
├── assets/
│   └── logo.ico             ← Logo buraya gelir
├── app/
│   ├── db/database.py       ← SQLite işlemleri
│   └── ui/
│       ├── ana_pencere.py   ← Ana pencere ve sekmeler
│       ├── style.py         ← Karanlık tema
│       └── widgets.py       ← Ortak bileşenler
└── index.html               ← Web sürümü
```
