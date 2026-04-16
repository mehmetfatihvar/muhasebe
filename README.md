# 🏢 Firma Muhasebe ve Kasa Takip Sistemi

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
