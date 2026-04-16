# 🏢 Firma Muhasebe ve Kasa Takip Sistemi

Tarayıcıda çalışan, sıfır kurulum gerektiren muhasebe ve kasa takip uygulaması.

## 🚀 Kullanım

`index.html` dosyasını herhangi bir tarayıcıda açın — kurulum gerekmez.

## ✨ Özellikler

- **Ana Sayfa** — Anlık kasa, bu ay gelir/gider, net durum, son hareketler
- **Gider Girişi** — Kategori/alt kategori seçimi, ödeme türü, belge no
- **Gelir Girişi** — Tahsilat kaydı, kimden geldiği bilgisi
- **Kasaya Para Girişi** — Sermaye, ortak katkısı, tahsilat girişleri
- **Kalem Tanımları** — Kendi kategori ve kalemlerinizi tanımlayın/silin
- **Tüm Hareketler** — Tarih, tür, arama bazlı filtreleme; CSV export
- **Gelir/Gider Tablosu** — Filtrelenmiş özet ve hareket listesi
- **Raporlar** — Aylık özet, kategori dağılımı, top kalemler, genel özet
- **Yedekleme** — Tüm veriyi JSON olarak indirin

## 💾 Veri Saklama

Veriler tarayıcının `localStorage` alanında saklanır. Yedek almak için **"Kaydet & Yedekle"** butonunu kullanın.

## 📁 Dosyalar

```
index.html   ← Uygulamanın tamamı tek dosyada
README.md    ← Bu dosya
```

## 🔧 Teknik Detaylar

- Saf HTML / CSS / JavaScript (framework yok)
- IBM Plex Sans & Mono fontları (Google Fonts)
- localStorage ile kalıcı veri
- CSV export (Excel uyumlu, UTF-8 BOM)
- JSON yedekleme/geri yükleme
