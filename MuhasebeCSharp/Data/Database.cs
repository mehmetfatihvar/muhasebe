using Microsoft.Data.Sqlite;
using MuhasebeSistemi.Models;
using System.IO;

namespace MuhasebeSistemi.Data;

public class Database
{
    private readonly string _dbPath;

    public Database()
    {
        // exe'nin yanındaki klasöre yaz (kalıcı)
        var exeDir = AppContext.BaseDirectory;
        _dbPath = Path.Combine(exeDir, "muhasebe.db");
        VeritabaniOlustur();
    }

    private SqliteConnection Baglanti() =>
        new SqliteConnection($"Data Source={_dbPath}");

    // ── VERİTABANI OLUŞTUR ─────────────────────────────────────────────
    private void VeritabaniOlustur()
    {
        using var conn = Baglanti();
        conn.Open();
        conn.ExecuteNonQuery(@"
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
        ");

        // Sayaçları başlat
        foreach (var tur in new[] { "GD", "GL", "KG" })
            conn.ExecuteNonQuery(
                "INSERT OR IGNORE INTO sayaclar VALUES (@t, 0)",
                ("@t", tur));

        // Örnek kalemler
        var sayi = (long)(conn.ExecuteScalar("SELECT COUNT(*) FROM kalemler") ?? 0L);
        if (sayi == 0)
        {
            var bugun = DateTime.Now.ToString("dd.MM.yyyy");
            var ornekler = new[]
            {
                ("Gider","Personel","Maaş","Personel Maaşı","Aylık maaş ödemeleri"),
                ("Gider","Kira","Ofis","Ofis Kirası","Aylık kira gideri"),
                ("Gider","Fatura","Elektrik","Elektrik Gideri","Elektrik faturası"),
                ("Gider","Fatura","Su","Su Gideri","Su faturası"),
                ("Gider","Fatura","İnternet","İnternet Gideri","İnternet aboneliği"),
                ("Gelir","Satış","Nakit Satış","Nakit Satış Geliri","Nakit tahsilat"),
                ("Gelir","Satış","Havale/EFT","Banka Transferi Geliri","Havale ile tahsilat"),
                ("Gelir","Hizmet","Danışmanlık","Danışmanlık Geliri","Proje/hizmet geliri"),
                ("Kasa Giriş","Sermaye","Ortak Girişi","Kasaya Ortak Para Girişi","Ortak sermaye katkısı"),
                ("Kasa Giriş","Tahsilat","Borç Tahsil","Alacak Tahsilatı","Alınan borç geri ödemesi"),
            };
            foreach (var (tur, ana, alt, ad, aciklama) in ornekler)
                conn.ExecuteNonQuery(@"
                    INSERT INTO kalemler (tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi)
                    VALUES (@t,@a,@al,@ad,@ac,@e)",
                    ("@t", tur), ("@a", ana), ("@al", alt),
                    ("@ad", ad), ("@ac", aciklama), ("@e", bugun));
        }
    }

    // ── SAYAÇ ───────────────────────────────────────────────────────────
    private string YeniIslemNo(string prefix)
    {
        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery(
            "UPDATE sayaclar SET deger = deger + 1 WHERE tur = @t", ("@t", prefix));
        var deger = (long)(conn.ExecuteScalar(
            "SELECT deger FROM sayaclar WHERE tur=@t", ("@t", prefix)) ?? 1L);
        return $"{prefix}-{deger:D5}";
    }

    // ── MEVCUT BAKİYE ───────────────────────────────────────────────────
    public decimal MevcutBakiye()
    {
        using var conn = Baglanti(); conn.Open();
        var val = conn.ExecuteScalar(
            "SELECT COALESCE(SUM(giris)-SUM(cikis),0) FROM hareketler");
        return Convert.ToDecimal(val ?? 0);
    }

    // ── KALEMLER ────────────────────────────────────────────────────────
    public List<Kalem> KalemListesi()
    {
        using var conn = Baglanti(); conn.Open();
        return conn.Query<Kalem>(
            "SELECT id,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,aktif,eklenme_tarihi FROM kalemler ORDER BY id");
    }

    public void KalemEkle(string tur, string ana, string alt, string ad, string aciklama)
    {
        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery(@"
            INSERT INTO kalemler (tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi)
            VALUES (@t,@a,@al,@ad,@ac,@e)",
            ("@t", tur), ("@a", ana), ("@al", alt), ("@ad", ad),
            ("@ac", aciklama), ("@e", DateTime.Now.ToString("dd.MM.yyyy")));
    }

    public void KalemSil(int id)
    {
        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery("DELETE FROM kalemler WHERE id=@id", ("@id", id));
    }

    public List<string> Kategoriler(string tur)
    {
        using var conn = Baglanti(); conn.Open();
        var result = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT ana_kategori FROM kalemler WHERE tur=@t AND aktif=1 ORDER BY ana_kategori";
        cmd.Parameters.AddWithValue("@t", tur);
        using var r = cmd.ExecuteReader();
        while (r.Read()) result.Add(r.GetString(0));
        return result;
    }

    public List<Kalem> AltKalemler(string tur, string ana)
    {
        using var conn = Baglanti(); conn.Open();
        return conn.Query<Kalem>(
            "SELECT * FROM kalemler WHERE tur=@t AND ana_kategori=@a AND aktif=1",
            ("@t", tur), ("@a", ana));
    }

    public List<string> KisiListesi()
    {
        using var conn = Baglanti(); conn.Open();
        var result = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT kimden_kime FROM hareketler WHERE kimden_kime IS NOT NULL AND kimden_kime != '' ORDER BY kimden_kime";
        using var r = cmd.ExecuteReader();
        while (r.Read()) result.Add(r.GetString(0));
        return result;
    }

    // ── HAREKETLER ──────────────────────────────────────────────────────
    public string HareketEkle(HareketGirdisi g)
    {
        var prefix = g.Tur == "Gider" ? "GD" : g.Tur == "Gelir" ? "GL" : "KG";
        var no = YeniIslemNo(prefix);
        var giris = g.Tur != "Gider" ? g.Tutar : 0;
        var cikis = g.Tur == "Gider" ? g.Tutar : 0;
        var bakiye = MevcutBakiye() + giris - cikis;
        var tarihDb = NormalizeTarih(g.Tarih);

        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery(@"
            INSERT INTO hareketler
            (islem_no,tarih,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,
             tutar,giris,cikis,kimden_kime,odeme_turu,belge_no,bakiye,kayit_tarihi)
            VALUES (@no,@t,@tur,@ana,@alt,@kal,@ac,@tu,@gi,@ci,@ki,@od,@be,@ba,@kt)",
            ("@no", no), ("@t", tarihDb), ("@tur", g.Tur),
            ("@ana", g.AnaKategori), ("@alt", g.AltKategori), ("@kal", g.KalemAdi),
            ("@ac", g.Aciklama), ("@tu", (double)g.Tutar),
            ("@gi", (double)giris), ("@ci", (double)cikis),
            ("@ki", g.KimdenKime), ("@od", g.OdemeTuru), ("@be", g.BelgeNo),
            ("@ba", (double)bakiye), ("@kt", DateTime.Now.ToString("dd.MM.yyyy")));
        return no;
    }

    public void HareketGuncelle(int id, HareketGirdisi g)
    {
        var giris = g.Tur != "Gider" ? g.Tutar : 0;
        var cikis = g.Tur == "Gider" ? g.Tutar : 0;
        var tarihDb = NormalizeTarih(g.Tarih);

        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery(@"
            UPDATE hareketler SET
                tarih=@t, tur=@tur, ana_kategori=@ana, alt_kategori=@alt, kalem_adi=@kal,
                aciklama=@ac, tutar=@tu, giris=@gi, cikis=@ci,
                kimden_kime=@ki, odeme_turu=@od, belge_no=@be
            WHERE id=@id",
            ("@t", tarihDb), ("@tur", g.Tur), ("@ana", g.AnaKategori),
            ("@alt", g.AltKategori), ("@kal", g.KalemAdi), ("@ac", g.Aciklama),
            ("@tu", (double)g.Tutar), ("@gi", (double)giris), ("@ci", (double)cikis),
            ("@ki", g.KimdenKime), ("@od", g.OdemeTuru), ("@be", g.BelgeNo),
            ("@id", id));
        BakiyeHesapla(conn);
    }

    public void HareketSil(int id)
    {
        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery("DELETE FROM hareketler WHERE id=@id", ("@id", id));
        BakiyeHesapla(conn);
    }

    private void BakiyeHesapla(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id,giris,cikis FROM hareketler ORDER BY tarih,id";
        var satirlar = new List<(long id, double g, double c)>();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                satirlar.Add((r.GetInt64(0), r.GetDouble(1), r.GetDouble(2)));
        double bak = 0;
        foreach (var (id, g, c) in satirlar)
        {
            bak += g - c;
            conn.ExecuteNonQuery("UPDATE hareketler SET bakiye=@b WHERE id=@id",
                ("@b", bak), ("@id", id));
        }
    }

    public Hareket? HareketGetir(int id)
    {
        using var conn = Baglanti(); conn.Open();
        return conn.QueryOne<Hareket>("SELECT * FROM hareketler WHERE id=@id", ("@id", id));
    }

    public List<Hareket> HareketListesi(
        string? tarihBas = null, string? tarihBit = null,
        string? tur = null, string? anaKategori = null,
        string? kalem = null, string? kimden = null,
        string? odemeTuru = null, decimal? tutarMin = null,
        decimal? tutarMax = null, string? ara = null)
    {
        var q = "SELECT * FROM hareketler WHERE 1=1";
        var prms = new List<(string, object)>();

        if (!string.IsNullOrEmpty(tarihBas)) { q += " AND tarih >= @bas"; prms.Add(("@bas", tarihBas!)); }
        if (!string.IsNullOrEmpty(tarihBit)) { q += " AND tarih <= @bit"; prms.Add(("@bit", tarihBit!)); }
        if (!string.IsNullOrEmpty(tur)) { q += " AND tur = @tur"; prms.Add(("@tur", tur!)); }
        if (!string.IsNullOrEmpty(anaKategori)) { q += " AND ana_kategori = @ana"; prms.Add(("@ana", anaKategori!)); }
        if (!string.IsNullOrEmpty(kalem)) { q += " AND kalem_adi LIKE @kal"; prms.Add(("@kal", $"%{kalem}%")); }
        if (!string.IsNullOrEmpty(kimden)) { q += " AND kimden_kime LIKE @ki"; prms.Add(("@ki", $"%{kimden}%")); }
        if (!string.IsNullOrEmpty(odemeTuru)) { q += " AND odeme_turu = @od"; prms.Add(("@od", odemeTuru!)); }
        if (tutarMin.HasValue) { q += " AND tutar >= @tmin"; prms.Add(("@tmin", (double)tutarMin.Value)); }
        if (tutarMax.HasValue) { q += " AND tutar <= @tmax"; prms.Add(("@tmax", (double)tutarMax.Value)); }
        if (!string.IsNullOrEmpty(ara))
        {
            q += " AND (kalem_adi LIKE @ara OR kimden_kime LIKE @ara OR belge_no LIKE @ara OR aciklama LIKE @ara OR islem_no LIKE @ara)";
            prms.Add(("@ara", $"%{ara}%"));
        }
        q += " ORDER BY tarih DESC, id DESC";

        using var conn = Baglanti(); conn.Open();
        return conn.Query<Hareket>(q, prms.ToArray());
    }

    // ── RAPORLAR ────────────────────────────────────────────────────────
    public List<AylikOzet> AylikOzet()
    {
        using var conn = Baglanti(); conn.Open();
        return conn.Query<AylikOzet>(@"
            SELECT strftime('%Y-%m', tarih) as Ay,
                   SUM(giris) as Gelir, SUM(cikis) as Gider, COUNT(*) as Sayi
            FROM hareketler GROUP BY Ay ORDER BY Ay DESC");
    }

    public List<(string Tur, string Ana, decimal Tutar, int Sayi)> KategoriDagilim()
    {
        using var conn = Baglanti(); conn.Open();
        var result = new List<(string, string, decimal, int)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tur, ana_kategori, SUM(tutar), COUNT(*) FROM hareketler GROUP BY tur, ana_kategori ORDER BY SUM(tutar) DESC";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            result.Add((r.GetString(0), r.GetString(1),
                Convert.ToDecimal(r.GetDouble(2)), r.GetInt32(3)));
        return result;
    }

    public List<(string Kalem, int Sayi, decimal Tutar)> TopKalemler(int limit = 5)
    {
        using var conn = Baglanti(); conn.Open();
        var result = new List<(string, int, decimal)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT kalem_adi, COUNT(*), SUM(tutar) FROM hareketler GROUP BY kalem_adi ORDER BY COUNT(*) DESC LIMIT {limit}";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            result.Add((r.GetString(0), r.GetInt32(1), Convert.ToDecimal(r.GetDouble(2))));
        return result;
    }

    public GenelOzet GenelOzet()
    {
        using var conn = Baglanti(); conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*),
                   COALESCE(SUM(CASE WHEN tur='Gelir' THEN tutar ELSE 0 END),0),
                   COALESCE(SUM(CASE WHEN tur='Gider' THEN tutar ELSE 0 END),0),
                   COALESCE(SUM(CASE WHEN tur='Kasa Giriş' THEN tutar ELSE 0 END),0),
                   COALESCE(SUM(giris)-SUM(cikis),0)
            FROM hareketler";
        using var r = cmd.ExecuteReader();
        r.Read();
        var kalemSayisi = (long)(conn.ExecuteScalar("SELECT COUNT(*) FROM kalemler") ?? 0L);
        return new GenelOzet
        {
            ToplamHareket = r.GetInt32(0),
            ToplamGelir   = Convert.ToDecimal(r.GetDouble(1)),
            ToplamGider   = Convert.ToDecimal(r.GetDouble(2)),
            KasaGiris     = Convert.ToDecimal(r.GetDouble(3)),
            Bakiye        = Convert.ToDecimal(r.GetDouble(4)),
            KalemSayisi   = (int)kalemSayisi
        };
    }

    // ── YEDEKLEME ───────────────────────────────────────────────────────
    public void JsonYedekAl(string dosyaYolu)
    {
        var veri = new
        {
            kalemler = KalemListesi(),
            hareketler = HareketListesi(),
            tarih = DateTime.Now.ToString("o")
        };
        var json = System.Text.Json.JsonSerializer.Serialize(veri,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        File.WriteAllText(dosyaYolu, json, System.Text.Encoding.UTF8);
    }

    // ── YARDIMCI ────────────────────────────────────────────────────────
    private static string NormalizeTarih(string t)
    {
        if (string.IsNullOrEmpty(t)) return DateTime.Now.ToString("yyyy-MM-dd");
        if (DateTime.TryParseExact(t, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d1))
            return d1.ToString("yyyy-MM-dd");
        if (DateTime.TryParseExact(t, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d2))
            return d2.ToString("yyyy-MM-dd");
        return t;
    }
}

// Hareket girdi modeli
public class HareketGirdisi
{
    public string Tarih { get; set; } = "";
    public string Tur { get; set; } = "";
    public string AnaKategori { get; set; } = "";
    public string AltKategori { get; set; } = "";
    public string KalemAdi { get; set; } = "";
    public decimal Tutar { get; set; }
    public string OdemeTuru { get; set; } = "";
    public string KimdenKime { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public string Aciklama { get; set; } = "";
}

    public void JsonYedekYukle(string dosyaYolu)
    {
        var json = File.ReadAllText(dosyaYolu, System.Text.Encoding.UTF8);
        var doc = System.Text.Json.JsonDocument.Parse(json);

        using var conn = Baglanti(); conn.Open();
        conn.ExecuteNonQuery("DELETE FROM hareketler");
        conn.ExecuteNonQuery("DELETE FROM kalemler");
        conn.ExecuteNonQuery("DELETE FROM sayaclar");
        foreach (var t in new[] { "GD", "GL", "KG" })
            conn.ExecuteNonQuery("INSERT INTO sayaclar VALUES (@t,0)", ("@t", t));

        if (doc.RootElement.TryGetProperty("kalemler", out var kalemler))
            foreach (var k in kalemler.EnumerateArray())
            {
                conn.ExecuteNonQuery(@"INSERT OR IGNORE INTO kalemler
                    (tur,ana_kategori,alt_kategori,kalem_adi,aciklama,eklenme_tarihi)
                    VALUES (@t,@a,@al,@ad,@ac,@e)",
                    ("@t",  k.TryGetProp("tur")),
                    ("@a",  k.TryGetProp("ana_kategori") ?? k.TryGetProp("ana")),
                    ("@al", k.TryGetProp("alt_kategori") ?? k.TryGetProp("alt")),
                    ("@ad", k.TryGetProp("kalem_adi") ?? k.TryGetProp("ad")),
                    ("@ac", k.TryGetProp("aciklama")),
                    ("@e",  k.TryGetProp("eklenme_tarihi") ?? k.TryGetProp("tarih")));
            }

        if (doc.RootElement.TryGetProperty("hareketler", out var hareketler))
            foreach (var h in hareketler.EnumerateArray())
            {
                var no = h.TryGetProp("islem_no") ?? h.TryGetProp("no") ?? "";
                var prefix = no.Length >= 2 ? no[..2] : "GD";
                if (int.TryParse(no.Split('-').LastOrDefault(), out var sayi))
                    conn.ExecuteNonQuery("UPDATE sayaclar SET deger=MAX(deger,@s) WHERE tur=@t",
                        ("@s", sayi), ("@t", prefix));

                conn.ExecuteNonQuery(@"INSERT OR IGNORE INTO hareketler
                    (islem_no,tarih,tur,ana_kategori,alt_kategori,kalem_adi,aciklama,
                     tutar,giris,cikis,kimden_kime,odeme_turu,belge_no,bakiye,kayit_tarihi)
                    VALUES (@no,@t,@tur,@ana,@alt,@kal,@ac,@tu,@gi,@ci,@ki,@od,@be,@ba,@kt)",
                    ("@no",  no),
                    ("@t",   h.TryGetProp("tarih")),
                    ("@tur", h.TryGetProp("tur")),
                    ("@ana", h.TryGetProp("ana_kategori") ?? h.TryGetProp("ana")),
                    ("@alt", h.TryGetProp("alt_kategori") ?? h.TryGetProp("alt")),
                    ("@kal", h.TryGetProp("kalem_adi") ?? h.TryGetProp("kalem")),
                    ("@ac",  h.TryGetProp("aciklama")),
                    ("@tu",  double.TryParse(h.TryGetProp("tutar"), out var tu) ? tu : 0),
                    ("@gi",  double.TryParse(h.TryGetProp("giris"), out var gi) ? gi : 0),
                    ("@ci",  double.TryParse(h.TryGetProp("cikis"), out var ci) ? ci : 0),
                    ("@ki",  h.TryGetProp("kimden_kime") ?? h.TryGetProp("kimden")),
                    ("@od",  h.TryGetProp("odeme_turu") ?? h.TryGetProp("odeme")),
                    ("@be",  h.TryGetProp("belge_no") ?? h.TryGetProp("belge")),
                    ("@ba",  double.TryParse(h.TryGetProp("bakiye"), out var ba) ? ba : 0),
                    ("@kt",  h.TryGetProp("kayit_tarihi")));
            }
        conn.CommitIfTransaction();
    }
