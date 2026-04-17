using MuhasebeSistemi.Data;
using MuhasebeSistemi.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MuhasebeSistemi.Views;

public class HareketDuzenleWindow : Window
{
    private readonly Database _db;
    private readonly int _hid;
    private readonly Hareket _h;

    private DatePicker _tarih = new();
    private ComboBox _tur = new(), _ana = new(), _kalem = new(), _odeme = new(), _kisi = new();
    private TextBox _tutar = new(), _belge = new(), _aciklama = new();

    public HareketDuzenleWindow(Database db, int hid)
    {
        _db = db; _hid = hid;
        _h = _db.HareketGetir(hid) ?? throw new Exception("Hareket bulunamadı");

        Title = $"✏️  Düzenle — {_h.IslemNo}";
        Width = 580; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.Resources["BgBrush"];

        var sp = new StackPanel { Margin = new Thickness(24, 20, 24, 20), Spacing = 14 };

        sp.Children.Add(new TextBlock
        {
            Text = $"📝  {_h.IslemNo} — Hareketi Düzenle",
            FontSize = 16, FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["TextBrush"]
        });

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 5; i++) g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Tarih
        _tarih = new DatePicker { Margin = new Thickness(0,0,0,0) };
        if (DateTime.TryParseExact(_h.Tarih, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d))
            _tarih.SelectedDate = d;

        // Tür
        _tur = new ComboBox();
        foreach (var t in new[] { "Gider", "Gelir", "Kasa Giriş" }) _tur.Items.Add(t);
        _tur.SelectedItem = _h.Tur;
        _tur.SelectionChanged += (_, _) => TurDegisti();

        // Ana kategori
        _ana = new ComboBox { IsEditable = true };
        var kalemler = _db.KalemListesi();
        foreach (var k in kalemler.Select(k => k.AnaKategori).Distinct().OrderBy(x => x))
            _ana.Items.Add(k);
        _ana.Text = _h.AnaKategori;
        _ana.SelectionChanged += (_, _) => AnaKategoriDegisti();

        // Kalem
        _kalem = new ComboBox { IsEditable = true };
        foreach (var k in kalemler.Select(k => k.KalemAdi).Distinct().OrderBy(x => x))
            _kalem.Items.Add(k);
        _kalem.Text = _h.KalemAdi;

        // Tutar
        _tutar = new TextBox { Text = _h.Tutar.ToString("F2") };

        // Ödeme
        _odeme = new ComboBox();
        foreach (var o in new[] { "Nakit", "Havale/EFT", "Kredi Kartı", "Çek", "Senet" })
            _odeme.Items.Add(o);
        _odeme.SelectedItem = _h.OdemeTuru;
        if (_odeme.SelectedIndex < 0) _odeme.SelectedIndex = 0;

        // Kişi
        _kisi = new ComboBox { IsEditable = true };
        _kisi.Items.Add("");
        foreach (var k in _db.KisiListesi()) _kisi.Items.Add(k);
        _kisi.Text = _h.KimdenKime;

        // Belge
        _belge = new TextBox { Text = _h.BelgeNo };

        // Açıklama
        _aciklama = new TextBox { Text = _h.Aciklama, MinLines = 2, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };

        void Ekle(UIElement ctrl, string lbl, int row, int col, int span = 1)
        {
            var wrap = new StackPanel { Margin = new Thickness(col == 1 ? 8 : 0, 0, 0, 10) };
            wrap.Children.Add(new TextBlock
            {
                Text = lbl,
                FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["Text2Brush"],
                Margin = new Thickness(0, 0, 0, 4)
            });
            wrap.Children.Add(ctrl);
            Grid.SetRow(wrap, row); Grid.SetColumn(wrap, col);
            Grid.SetColumnSpan(wrap, span); g.Children.Add(wrap);
        }

        Ekle(_tarih, "TARİH *",      0, 0);
        Ekle(_tur,   "İŞLEM TÜRÜ *", 0, 1);
        Ekle(_ana,   "ANA KATEGORİ", 1, 0);
        Ekle(_kalem, "KALEM",        1, 1);
        Ekle(_tutar, "TUTAR *",      2, 0);
        Ekle(_odeme, "ÖDEME TÜRÜ",   2, 1);
        Ekle(_kisi,  "KİŞİ / FİRMA", 3, 0);
        Ekle(_belge, "BELGE NO",     3, 1);
        Ekle(_aciklama, "AÇIKLAMA",  4, 0, 2);

        sp.Children.Add(g);

        // Butonlar
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnKaydet = new Button
        {
            Content = "💾  Kaydet",
            Style = (Style)Application.Current.Resources["PrimaryBtn"],
            Margin = new Thickness(0, 0, 8, 0)
        };
        btnKaydet.Click += Kaydet;
        var btnIptal = new Button
        {
            Content = "İptal",
            Style = (Style)Application.Current.Resources["SecondaryBtn"]
        };
        btnIptal.Click += (_, _) => { DialogResult = false; Close(); };
        btnRow.Children.Add(btnKaydet); btnRow.Children.Add(btnIptal);
        sp.Children.Add(btnRow);

        Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        TurDegisti(); // Başlangıçta kategori/kalem listesini güncelle
    }

    private void TurDegisti()
    {
        var tur = _tur.SelectedItem?.ToString() ?? "";
        var oncekiAna = _ana.Text;
        _ana.Items.Clear();
        var kalemler = _db.KalemListesi();
        foreach (var k in kalemler.Where(k => k.Tur == tur || string.IsNullOrEmpty(tur))
                                  .Select(k => k.AnaKategori).Distinct().OrderBy(x => x))
            _ana.Items.Add(k);
        _ana.Text = oncekiAna;
        AnaKategoriDegisti();
    }

    private void AnaKategoriDegisti()
    {
        var tur = _tur.SelectedItem?.ToString() ?? "";
        var ana = _ana.Text;
        var oncekiKalem = _kalem.Text;
        _kalem.Items.Clear();
        var kalemler = _db.KalemListesi()
            .Where(k => (string.IsNullOrEmpty(tur) || k.Tur == tur) &&
                        (string.IsNullOrEmpty(ana) || k.AnaKategori == ana))
            .Select(k => k.KalemAdi).Distinct().OrderBy(x => x);
        foreach (var k in kalemler) _kalem.Items.Add(k);
        _kalem.Text = oncekiKalem;
    }

    private void Kaydet(object sender, RoutedEventArgs e)
    {
        if (_tarih.SelectedDate == null || !decimal.TryParse(_tutar.Text.Replace(",", "."), out var tutar) || tutar <= 0)
        {
            MessageBox.Show("Tarih ve tutar zorunludur!", "Eksik", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var kalemler = _db.KalemListesi();
        var seciliKalem = _kalem.Text;
        var alt = kalemler.FirstOrDefault(k => k.KalemAdi == seciliKalem)?.AltKategori ?? "";

        _db.HareketGuncelle(_hid, new HareketGirdisi
        {
            Tarih       = _tarih.SelectedDate!.Value.ToString("yyyy-MM-dd"),
            Tur         = _tur.SelectedItem?.ToString() ?? _h.Tur,
            AnaKategori = _ana.Text,
            AltKategori = alt,
            KalemAdi    = seciliKalem,
            Tutar       = tutar,
            OdemeTuru   = _odeme.SelectedItem?.ToString() ?? "",
            KimdenKime  = _kisi.Text.Trim(),
            BelgeNo     = _belge.Text.Trim(),
            Aciklama    = _aciklama.Text.Trim()
        });
        DialogResult = true;
        Close();
    }
}
