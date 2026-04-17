namespace MuhasebeSistemi.Models;

public class Hareket
{
    public int Id { get; set; }
    public string IslemNo { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string Tur { get; set; } = "";
    public string AnaKategori { get; set; } = "";
    public string AltKategori { get; set; } = "";
    public string KalemAdi { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public decimal Tutar { get; set; }
    public decimal Giris { get; set; }
    public decimal Cikis { get; set; }
    public string KimdenKime { get; set; } = "";
    public string OdemeTuru { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public decimal Bakiye { get; set; }
    public string Durum { get; set; } = "✅";
    public string KayitTarihi { get; set; } = "";

    // Görüntüleme için
    public string TarihGoster => TarihFormat(Tarih);
    public string TutarGoster => Tutar.ToString("N2") + " ₺";
    public string GirisGoster => Giris > 0 ? Giris.ToString("N2") + " ₺" : "-";
    public string CikisGoster => Cikis > 0 ? Cikis.ToString("N2") + " ₺" : "-";
    public string BakiyeGoster => Bakiye.ToString("N2") + " ₺";

    private static string TarihFormat(string t)
    {
        if (string.IsNullOrEmpty(t)) return "-";
        if (DateTime.TryParseExact(t, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d))
            return d.ToString("dd.MM.yyyy");
        return t;
    }
}

public class Kalem
{
    public int Id { get; set; }
    public string Tur { get; set; } = "";
    public string AnaKategori { get; set; } = "";
    public string AltKategori { get; set; } = "";
    public string KalemAdi { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public string EklenmeTarihi { get; set; } = "";
}

public class AylikOzet
{
    public string Ay { get; set; } = "";
    public decimal Gelir { get; set; }
    public decimal Gider { get; set; }
    public decimal Net => Gelir - Gider;
    public int Sayi { get; set; }
    public string AyGoster
    {
        get
        {
            if (DateTime.TryParseExact(Ay + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
                return d.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            return Ay;
        }
    }
}

public class GenelOzet
{
    public int ToplamHareket { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal KasaGiris { get; set; }
    public decimal Bakiye { get; set; }
    public int KalemSayisi { get; set; }
}
