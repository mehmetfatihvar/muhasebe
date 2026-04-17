using Microsoft.Win32;
using MuhasebeSistemi.Data;
using MuhasebeSistemi.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MuhasebeSistemi.Views;

public partial class MainWindow : Window
{
    private readonly Database _db = new();
    private List<Hareket> _hTumListe = new();
    private int _hSayfa = 0;
    private const int SayfaBoyutu = 100;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            KpiGuncelle();
            AnaSayfaYukle();
        };
    }

    // ── KPI ─────────────────────────────────────────────────────────────
    private void KpiGuncelle()
    {
        var ozet = _db.GenelOzet();
        var buAy = DateTime.Now.ToString("yyyy-MM");
        var hareketler = _db.HareketListesi();
        var buAyGelir = hareketler.Where(h => h.Tarih.StartsWith(buAy)).Sum(h => h.Giris);
        var buAyGider = hareketler.Where(h => h.Tarih.StartsWith(buAy)).Sum(h => h.Cikis);
        var son7 = hareketler.Count(h =>
        {
            if (DateTime.TryParseExact(h.Tarih, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
                return (DateTime.Now - d).Days <= 7;
            return false;
        });
        var net = buAyGelir - buAyGider;

        KpiKasa.Text = Para(ozet.Bakiye);
        KpiGelir.Text = Para(buAyGelir);
        KpiGider.Text = Para(buAyGider);
        KpiNet.Text = Para(net);
        KpiNet.Foreground = net >= 0
            ? (Brush)FindResource("GreenBrush")
            : (Brush)FindResource("RedBrush");
        KpiSon7.Text = $"{son7} işlem";
    }

    private static string Para(decimal v) =>
        v.ToString("N2", new System.Globalization.CultureInfo("tr-TR")) + " ₺";

    private void SetStatus(string msg) => StatusText.Text = msg;

    // ── TAB DEĞİŞTİ ─────────────────────────────────────────────────────
    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl) return;
        KpiGuncelle();

        var tab = MainTabs.SelectedItem as TabItem;
        if (tab == null) return;

        if (tab == TabAnaSayfa)      AnaSayfaYukle();
        else if (tab == TabGider)    GiderTabYukle();
        else if (tab == TabGelir)    GelirTabYukle();
        else if (tab == TabKasa)     KasaTabYukle();
        else if (tab == TabKalemler) KalemlerYukle();
        else if (tab == TabHareketler) HareketlerIlkYukle();
        else if (tab == TabTablo)    TabloFiltrele();
        else if (tab == TabRaporlar) RaporlarYukle();
    }

    // ── ANA SAYFA ───────────────────────────────────────────────────────
    private void AnaSayfaYukle()
    {
        if (TabAnaSayfa.Content != null) { HareketGridGuncelle(TabAnaSayfa); return; }

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Hızlı erişim
        var baslik1 = new TextBlock { Text = "Hızlı Erişim", Style = (Style)FindResource("PageTitle") };
        Grid.SetRow(baslik1, 0);
        grid.Children.Add(baslik1);

        var kisayollar = new (string ikon, string ad, string acik, TabItem tab)[]
        {
            ("➕", "Gider Ekle",          "Yeni gider kaydı",      TabGider),
            ("💰", "Gelir Ekle",          "Yeni gelir kaydı",      TabGelir),
            ("🏦", "Kasaya Para Girişi",  "Sermaye / tahsilat",    TabKasa),
            ("📋", "Kalem Tanımları",     "Kategori yönetimi",     TabKalemler),
            ("🗂", "Tüm Hareketler",      "Kayıtları görüntüle",   TabHareketler),
            ("📊", "Gelir/Gider Tablosu","Filtrelenmiş özet",      TabTablo),
            ("📈", "Raporlar",            "Aylık & kategori özeti", TabRaporlar),
        };

        var wp = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
        foreach (var (ikon, ad, acik, hedef) in kisayollar)
        {
            var btn = new Button
            {
                Width = 200, Height = 80, Margin = new Thickness(0, 0, 10, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = hedef,
            };
            btn.Click += (_, _) => MainTabs.SelectedItem = hedef;
            var border = new Border
            {
                Background = (Brush)FindResource("SurfaceBrush"),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var ikonTb = new TextBlock { Text = ikon, FontSize = 24, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            var textSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textSp.Children.Add(new TextBlock { Text = ad, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush") });
            textSp.Children.Add(new TextBlock { Text = acik, FontSize = 11, Foreground = (Brush)FindResource("Text3Brush") });
            sp.Children.Add(ikonTb); sp.Children.Add(textSp);
            border.Child = sp;

            var btnContent = new Border { Child = border };
            btn.Content = btnContent;
            btn.Template = new ControlTemplate(typeof(Button))
            {
                VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
            };
            // Hover efekti
            btn.MouseEnter += (_, _) => border.BorderBrush = (Brush)FindResource("AccentBrush");
            btn.MouseLeave += (_, _) => border.BorderBrush = (Brush)FindResource("BorderBrush");
            wp.Children.Add(btn);
        }
        Grid.SetRow(wp, 1); grid.Children.Add(wp);

        // Son hareketler tablosu
        var baslik2 = new TextBlock { Text = "Son Hareketler", Style = (Style)FindResource("PageTitle") };
        Grid.SetRow(baslik2, 2);

        var sp2 = new StackPanel();
        sp2.Children.Add(baslik2);
        var dg = MakeDataGrid(false);
        dg.Tag = "sonHareketler";
        sp2.Children.Add(dg);
        Grid.SetRow(sp2, 2);
        grid.Children.Add(sp2);

        var scroll = new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20, 16, 20, 20) };
        TabAnaSayfa.Content = scroll;
        HareketGridGuncelle(TabAnaSayfa);
    }

    private void HareketGridGuncelle(TabItem tab)
    {
        var scroll = tab.Content as ScrollViewer;
        var grid = scroll?.Content as Grid;
        var dg = FindName<DataGrid>(grid, "sonHareketler");
        if (dg == null) return;
        var rows = _db.HareketListesi().Take(15).ToList();
        dg.ItemsSource = rows;
    }

    // ── GİDER FORMU ─────────────────────────────────────────────────────
    private DatePicker? _gTarih; private ComboBox? _gKat, _gAlt, _gOdeme, _gKime;
    private TextBox? _gBelge, _gAciklama; private TextBox? _gTutar;

    private void GiderTabYukle()
    {
        if (TabGider.Content == null)
        {
            TabGider.Content = FormTabOlustur("Gider",
                ref _gTarih, ref _gKat, ref _gAlt, ref _gOdeme, ref _gKime,
                ref _gBelge, ref _gAciklama, ref _gTutar,
                GiderKaydet);
        }
        KisiComboGuncelle(_gKime);
        KategoriDoldur(_gKat!, "Gider");
    }

    private void GiderKaydet(object sender, RoutedEventArgs e)
    {
        if (!FormDogrula(_gTarih, _gKat, _gAlt, _gTutar, _gOdeme)) return;
        var kalem = _db.AltKalemler("Gider", _gKat!.SelectedItem?.ToString() ?? "")
                       .FirstOrDefault(k => k.KalemAdi == _gAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi
        {
            Tarih = _gTarih!.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
            Tur = "Gider", AnaKategori = _gKat!.SelectedItem?.ToString() ?? "",
            AltKategori = kalem?.AltKategori ?? "", KalemAdi = _gAlt!.SelectedItem?.ToString() ?? "",
            Tutar = decimal.TryParse(_gTutar!.Text.Replace(",","."), out var t) ? t : 0,
            OdemeTuru = _gOdeme!.SelectedItem?.ToString() ?? "",
            KimdenKime = _gKime!.Text, BelgeNo = _gBelge!.Text, Aciklama = _gAciklama!.Text
        });
        KpiGuncelle(); FormTemizle(_gTarih, _gTutar, _gBelge, _gAciklama);
        SetStatus($"✅ Gider kaydedildi: {no}");
    }

    // ── GELİR FORMU ─────────────────────────────────────────────────────
    private DatePicker? _glTarih; private ComboBox? _glKat, _glAlt, _glOdeme, _glKimden;
    private TextBox? _glBelge, _glAciklama, _glTutar;

    private void GelirTabYukle()
    {
        if (TabGelir.Content == null)
        {
            TabGelir.Content = FormTabOlustur("Gelir",
                ref _glTarih, ref _glKat, ref _glAlt, ref _glOdeme, ref _glKimden,
                ref _glBelge, ref _glAciklama, ref _glTutar,
                GelirKaydet);
        }
        KisiComboGuncelle(_glKimden);
        KategoriDoldur(_glKat!, "Gelir");
    }

    private void GelirKaydet(object sender, RoutedEventArgs e)
    {
        if (!FormDogrula(_glTarih, _glKat, _glAlt, _glTutar, _glOdeme)) return;
        var kalem = _db.AltKalemler("Gelir", _glKat!.SelectedItem?.ToString() ?? "")
                       .FirstOrDefault(k => k.KalemAdi == _glAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi
        {
            Tarih = _glTarih!.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
            Tur = "Gelir", AnaKategori = _glKat!.SelectedItem?.ToString() ?? "",
            AltKategori = kalem?.AltKategori ?? "", KalemAdi = _glAlt!.SelectedItem?.ToString() ?? "",
            Tutar = decimal.TryParse(_glTutar!.Text.Replace(",","."), out var t) ? t : 0,
            OdemeTuru = _glOdeme!.SelectedItem?.ToString() ?? "",
            KimdenKime = _glKimden!.Text, BelgeNo = _glBelge!.Text, Aciklama = _glAciklama!.Text
        });
        KpiGuncelle(); FormTemizle(_glTarih, _glTutar, _glBelge, _glAciklama);
        SetStatus($"✅ Gelir kaydedildi: {no}");
    }

    // ── KASA FORMU ──────────────────────────────────────────────────────
    private DatePicker? _kTarih; private ComboBox? _kTur, _kAlt, _kKimden;
    private TextBox? _kBelge, _kAciklama, _kTutar;

    private void KasaTabYukle()
    {
        if (TabKasa.Content == null)
        {
            TabKasa.Content = FormTabOlustur("Kasa Giriş",
                ref _kTarih, ref _kTur, ref _kAlt, ref _kKimden, ref _kKimden,
                ref _kBelge, ref _kAciklama, ref _kTutar,
                KasaKaydet);
        }
        KisiComboGuncelle(_kKimden);
        KategoriDoldur(_kTur!, "Kasa Giriş");
    }

    private void KasaKaydet(object sender, RoutedEventArgs e)
    {
        if (!FormDogrula(_kTarih, _kTur, _kAlt, _kTutar, null)) return;
        if (string.IsNullOrWhiteSpace(_kKimden?.Text))
        { MessageBox.Show("Kimden Geldi alanı zorunludur!","Eksik Alan",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        var kalem = _db.AltKalemler("Kasa Giriş", _kTur!.SelectedItem?.ToString() ?? "")
                       .FirstOrDefault(k => k.KalemAdi == _kAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi
        {
            Tarih = _kTarih!.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
            Tur = "Kasa Giriş", AnaKategori = _kTur!.SelectedItem?.ToString() ?? "",
            AltKategori = kalem?.AltKategori ?? "", KalemAdi = _kAlt!.SelectedItem?.ToString() ?? "",
            Tutar = decimal.TryParse(_kTutar!.Text.Replace(",","."), out var t) ? t : 0,
            OdemeTuru = "Nakit", KimdenKime = _kKimden!.Text,
            BelgeNo = _kBelge!.Text, Aciklama = _kAciklama!.Text
        });
        KpiGuncelle(); FormTemizle(_kTarih, _kTutar, _kBelge, _kAciklama);
        SetStatus($"✅ Kasa girişi kaydedildi: {no}");
    }

    // ── FORM OLUŞTURUCU (Gider/Gelir/Kasa için ortak) ───────────────────
    private UIElement FormTabOlustur(string tur,
        ref DatePicker? tarih, ref ComboBox? kat, ref ComboBox? alt,
        ref ComboBox? odeme, ref ComboBox? kisi,
        ref TextBox? belge, ref TextBox? aciklama, ref TextBox? tutarBox,
        RoutedEventHandler kaydetHandler)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };

        var baslik = new TextBlock
        {
            Text = tur == "Gider" ? "➕  Gider Girişi" : tur == "Gelir" ? "💰  Gelir Girişi" : "🏦  Kasaya Para Girişi",
            Style = (Style)FindResource("PageTitle")
        };
        outer.Children.Add(baslik);

        var kart = new Border { Style = (Style)FindResource("FormCard"), MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Left };
        var sp = new StackPanel { Spacing = 14 };

        var altBaslik = new TextBlock
        {
            Text = $"Yeni {(tur == "Kasa Giriş" ? "Kasa" : tur)} Kaydı  —  (*) zorunlu",
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("Text2Brush"), Margin = new Thickness(0, 0, 0, 8)
        };
        sp.Children.Add(altBaslik);

        // Tarih
        tarih = new DatePicker { SelectedDate = DateTime.Today };
        // Kategori
        kat = new ComboBox();
        kat.SelectionChanged += (_, _) => AltKalemDoldur(kat, alt!, tur);
        // Alt kalem
        alt = new ComboBox();
        // Tutar
        tutarBox = new TextBox { Text = "0" };
        // Ödeme/Tahsilat
        odeme = new ComboBox();
        foreach (var o in new[] { "Nakit", "Havale/EFT", "Kredi Kartı", "Çek", "Senet" })
            odeme.Items.Add(o);
        odeme.SelectedIndex = 0;
        // Kişi
        kisi = new ComboBox { IsEditable = true };
        // Belge
        belge = new TextBox { Text = "" };
        // Açıklama
        aciklama = new TextBox { Text = "", MinLines = 2, MaxLines = 3, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };

        // Grid 2 sütun
        var g = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.RowDefinitions.Add(new RowDefinition()); g.RowDefinitions.Add(new RowDefinition());
        g.RowDefinitions.Add(new RowDefinition()); g.RowDefinitions.Add(new RowDefinition());

        void Ekle(UIElement ctrl, string label, int row, int col, int colSpan = 1)
        {
            var wrap = new StackPanel { Margin = new Thickness(col == 0 ? 0 : 8, 0, col == 1 || colSpan > 1 ? 0 : 0, 8) };
            wrap.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
            wrap.Children.Add(ctrl);
            Grid.SetRow(wrap, row); Grid.SetColumn(wrap, col); Grid.SetColumnSpan(wrap, colSpan);
            g.Children.Add(wrap);
        }

        Ekle(tarih, "TARİH *", 0, 0);
        Ekle(kat, tur == "Kasa Giriş" ? "KASA GİRİŞ TÜRÜ *" : $"{tur.ToUpper()} KATEGORİSİ *", 0, 1);
        Ekle(alt, "KALEM *", 1, 0);
        Ekle(tutarBox, "TUTAR (₺) *", 1, 1);
        Ekle(odeme, tur == "Gelir" ? "TAHSİLAT TÜRÜ *" : "ÖDEME TÜRÜ *", 2, 0);
        Ekle(kisi, tur == "Gider" ? "KİME ÖDENDİ" : "KİMDEN GELDİ" + (tur == "Kasa Giriş" ? " *" : ""), 2, 1);
        Ekle(belge, "BELGE / FİŞ NO", 3, 0);
        Ekle(aciklama, "AÇIKLAMA", 3, 1);

        sp.Children.Add(g);

        // Butonlar
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        var btnKaydet = new Button { Content = "✅  Kaydet", Style = (Style)FindResource("PrimaryBtn"), Margin = new Thickness(0, 0, 8, 0) };
        btnKaydet.Click += kaydetHandler;
        var btnTemizle = new Button { Content = "🗑️  Temizle", Style = (Style)FindResource("SecondaryBtn") };
        btnTemizle.Click += (_, _) => FormTemizle(tarih, tutarBox, belge, aciklama);
        btnRow.Children.Add(btnKaydet); btnRow.Children.Add(btnTemizle);
        sp.Children.Add(btnRow);

        kart.Child = sp; outer.Children.Add(kart);
        scroll.Content = outer;
        return scroll;
    }

    // ── KALEM TANIMLARI ─────────────────────────────────────────────────
    private ComboBox? _ktTur; private TextBox? _ktAna, _ktAlt, _ktAd, _ktAciklama;
    private DataGrid? _kalemGrid;

    private void KalemlerYukle()
    {
        if (TabKalemler.Content == null) KalemTabOlustur();
        KalemGridDoldur();
    }

    private void KalemTabOlustur()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new StackPanel { Margin = new Thickness(20, 16, 20, 20), Spacing = 16 };

        outer.Children.Add(new TextBlock { Text = "📋  Kalem Tanımları", Style = (Style)FindResource("PageTitle") });

        var kart = new Border { Style = (Style)FindResource("FormCard"), MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Left };
        var sp = new StackPanel { Spacing = 12 };
        sp.Children.Add(new TextBlock { Text = "Yeni Kalem Ekle", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Text2Brush") });

        _ktTur = new ComboBox();
        foreach (var t in new[] { "Gider", "Gelir", "Kasa Giriş" }) _ktTur.Items.Add(t);
        _ktTur.SelectedIndex = 0;
        _ktAna = new TextBox(); _ktAlt = new TextBox(); _ktAd = new TextBox(); _ktAciklama = new TextBox();

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 3; i++) g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        void Ekle(UIElement ctrl, string lbl, int row, int col)
        {
            var wrap = new StackPanel { Margin = new Thickness(col == 1 ? 8 : 0, 0, 0, 10) };
            wrap.Children.Add(new TextBlock { Text = lbl, Style = (Style)FindResource("FieldLabel") });
            wrap.Children.Add(ctrl);
            Grid.SetRow(wrap, row); Grid.SetColumn(wrap, col); g.Children.Add(wrap);
        }
        Ekle(_ktTur, "KALEM TÜRÜ *", 0, 0); Ekle(_ktAna!, "ANA KATEGORİ *", 0, 1);
        Ekle(_ktAlt!, "ALT KATEGORİ *", 1, 0); Ekle(_ktAd!, "KALEM ADI *", 1, 1);

        var aciklamaWrap = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        aciklamaWrap.Children.Add(new TextBlock { Text = "AÇIKLAMA", Style = (Style)FindResource("FieldLabel") });
        aciklamaWrap.Children.Add(_ktAciklama);
        Grid.SetRow(aciklamaWrap, 2); Grid.SetColumnSpan(aciklamaWrap, 2); g.Children.Add(aciklamaWrap);
        sp.Children.Add(g);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        var btnKaydet = new Button { Content = "✅  Kaydet", Style = (Style)FindResource("PrimaryBtn"), Margin = new Thickness(0, 0, 8, 0) };
        btnKaydet.Click += KalemKaydet;
        var btnTemizle = new Button { Content = "🗑️  Temizle", Style = (Style)FindResource("SecondaryBtn") };
        btnTemizle.Click += (_, _) => { _ktAna!.Text=""; _ktAlt!.Text=""; _ktAd!.Text=""; _ktAciklama!.Text=""; _ktTur!.SelectedIndex=0; };
        btnRow.Children.Add(btnKaydet); btnRow.Children.Add(btnTemizle);
        sp.Children.Add(btnRow);
        kart.Child = sp; outer.Children.Add(kart);

        outer.Children.Add(new TextBlock { Text = "Tanımlı Kalemler", Style = (Style)FindResource("PageTitle") });
        _kalemGrid = new DataGrid { MaxHeight = 400 };
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 50 });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Tür", Binding = new System.Windows.Data.Binding("Tur"), Width = 100 });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Ana Kategori", Binding = new System.Windows.Data.Binding("AnaKategori"), Width = 130 });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Alt Kategori", Binding = new System.Windows.Data.Binding("AltKategori"), Width = 130 });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Kalem Adı", Binding = new System.Windows.Data.Binding("KalemAdi"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Açıklama", Binding = new System.Windows.Data.Binding("Aciklama"), Width = 180 });
        _kalemGrid.Columns.Add(new DataGridTextColumn { Header = "Eklenme", Binding = new System.Windows.Data.Binding("EklenmeTarihi"), Width = 100 });

        var silCol = new DataGridTemplateColumn { Header = "", Width = 50 };
        var silFactory = new FrameworkElementFactory(typeof(Button));
        silFactory.SetValue(Button.ContentProperty, "🗑️");
        silFactory.SetValue(Button.StyleProperty, FindResource("IconBtn"));
        silFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(KalemSil_Click));
        silCol.CellTemplate = new DataTemplate { VisualTree = silFactory };
        _kalemGrid.Columns.Add(silCol);

        outer.Children.Add(_kalemGrid);
        scroll.Content = outer;
        TabKalemler.Content = scroll;
    }

    private void KalemGridDoldur()
    {
        if (_kalemGrid != null) _kalemGrid.ItemsSource = _db.KalemListesi();
    }

    private void KalemKaydet(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ktAna?.Text) || string.IsNullOrWhiteSpace(_ktAlt?.Text) || string.IsNullOrWhiteSpace(_ktAd?.Text))
        { MessageBox.Show("(*) alanları doldurun!","Eksik",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        _db.KalemEkle(_ktTur!.SelectedItem?.ToString()!, _ktAna!.Text.Trim(),
            _ktAlt!.Text.Trim(), _ktAd!.Text.Trim(), _ktAciklama?.Text.Trim() ?? "");
        KalemGridDoldur();
        SetStatus($"✅ Kalem eklendi: {_ktAd!.Text}");
        _ktAna.Text=""; _ktAlt!.Text=""; _ktAd!.Text=""; _ktAciklama!.Text="";
    }

    private void KalemSil_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && _kalemGrid?.CurrentItem is Kalem k)
        {
            if (MessageBox.Show($"'{k.KalemAdi}' silinsin mi?","Onay",MessageBoxButton.YesNo,MessageBoxImage.Question) == MessageBoxResult.Yes)
            { _db.KalemSil(k.Id); KalemGridDoldur(); SetStatus("Kalem silindi."); }
        }
    }

    // ── TÜM HAREKETLER ──────────────────────────────────────────────────
    private DataGrid? _hGrid; private TextBlock? _hSayisiTb, _hSayfaTb;
    private Button? _hIlk, _hGeri, _hIleri, _hSon;
    private DatePicker? _fBas, _fBit; private ComboBox? _fTur, _fAnaKat, _fKalem, _fKisi, _fOdeme;
    private TextBox? _fTutarMin, _fTutarMax, _fAra;

    private void HareketlerIlkYukle()
    {
        if (TabHareketler.Content == null) HareketlerTabOlustur();
        _hTumListe = _db.HareketListesi();
        SayfaGoster(_hTumListe, 0);
    }

    private void HareketlerTabOlustur()
    {
        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.Margin = new Thickness(20, 16, 20, 20);

        // Filtre
        var filtreBorder = new Border { Style = (Style)FindResource("FormCard"), Padding = new Thickness(16,12,16,12), Margin = new Thickness(0,0,0,12) };
        var filtreSpMain = new StackPanel { Spacing = 10 };
        var satir1 = new WrapPanel { Orientation = Orientation.Horizontal };
        var satir2 = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,8,0,0) };

        _fBas = new DatePicker { SelectedDate = new DateTime(DateTime.Today.Year, 1, 1), Width = 130, Margin = new Thickness(0,0,8,0) };
        _fBit = new DatePicker { SelectedDate = DateTime.Today, Width = 130, Margin = new Thickness(0,0,8,0) };
        _fTur = new ComboBox { Width = 120, Margin = new Thickness(0,0,8,0) };
        foreach (var t in new[] { "Tümü", "Gelir", "Gider", "Kasa Giriş" }) _fTur.Items.Add(t);
        _fTur.SelectedIndex = 0;
        _fTur.SelectionChanged += (_, _) => FiltreMasterKatGuncelle();
        _fAnaKat = new ComboBox { Width = 150, Margin = new Thickness(0,0,8,0), IsEditable = false };
        _fAnaKat.SelectionChanged += (_, _) => FiltreKalemGuncelle();
        _fKalem = new ComboBox { Width = 180, Margin = new Thickness(0,0,8,0), IsEditable = false };

        foreach (var w in new UIElement[] { FiltreSarma("BAŞLANGIÇ", _fBas), FiltreSarma("BİTİŞ", _fBit), FiltreSarma("TÜR", _fTur), FiltreSarma("KATEGORİ", _fAnaKat), FiltreSarma("KALEM", _fKalem) })
            satir1.Children.Add(w);

        _fKisi = new ComboBox { Width = 150, IsEditable = true, Margin = new Thickness(0,0,8,0) };
        _fOdeme = new ComboBox { Width = 140, Margin = new Thickness(0,0,8,0) };
        foreach (var o in new[] { "Tüm Ödemeler", "Nakit", "Havale/EFT", "Kredi Kartı", "Çek", "Senet" }) _fOdeme.Items.Add(o);
        _fOdeme.SelectedIndex = 0;
        _fTutarMin = new TextBox { Width = 100, Margin = new Thickness(0,0,8,0) };
        _fTutarMax = new TextBox { Width = 100, Margin = new Thickness(0,0,8,0) };
        _fAra = new TextBox { Width = 180, Margin = new Thickness(0,0,8,0) };
        _fAra.KeyDown += (_, k) => { if (k.Key == System.Windows.Input.Key.Return) HareketlerFiltrele(); };

        foreach (var w in new UIElement[] { FiltreSarma("KİŞİ/FİRMA", _fKisi), FiltreSarma("ÖDEME", _fOdeme), FiltreSarma("MİN TUTAR", _fTutarMin), FiltreSarma("MAX TUTAR", _fTutarMax), FiltreSarma("GENEL ARAMA", _fAra) })
            satir2.Children.Add(w);

        var btnRow = new WrapPanel { Margin = new Thickness(0,10,0,0) };
        var bFiltre = new Button { Content = "🔍  Filtrele", Style = (Style)FindResource("PrimaryBtn"), Margin = new Thickness(0,0,6,0) };
        bFiltre.Click += (_, _) => HareketlerFiltrele();
        var bTemiz = new Button { Content = "🔄  Temizle", Style = (Style)FindResource("SecondaryBtn"), Margin = new Thickness(0,0,6,0) };
        bTemiz.Click += (_, _) => HareketFiltreTemizle();
        var bCsv = new Button { Content = "📥  CSV Aktar", Style = (Style)FindResource("GreenBtn") };
        bCsv.Click += (_, _) => CsvAktar();
        foreach (var b in new[] {bFiltre, bTemiz, bCsv}) btnRow.Children.Add(b);

        filtreSpMain.Children.Add(satir1); filtreSpMain.Children.Add(satir2); filtreSpMain.Children.Add(btnRow);
        filtreBorder.Child = filtreSpMain;
        Grid.SetRow(filtreBorder, 0); outer.Children.Add(filtreBorder);

        // Sayı bilgisi
        _hSayisiTb = new TextBlock { Text = "Tüm Kayıtlar", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("TextBrush"), Margin = new Thickness(0,0,0,8) };
        Grid.SetRow(_hSayisiTb, 1); outer.Children.Add(_hSayisiTb);

        // Tablo
        _hGrid = MakeDataGrid(true);
        Grid.SetRow(_hGrid, 2); outer.Children.Add(_hGrid);

        // Sayfalama
        var sayfaBar = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("BorderBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Height = 44, Margin = new Thickness(0,8,0,0) };
        var sayfaSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _hIlk  = SayfaBtn("⏮"); _hIlk.Click  += (_, _) => SayfaGoster(_hTumListe, 0);
        _hGeri = SayfaBtn("◀"); _hGeri.Click  += (_, _) => SayfaGoster(_hTumListe, _hSayfa - 1);
        _hSayfaTb = new TextBlock { Text = "Sayfa 1 / 1", Width = 140, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("Text2Brush"), FontSize = 13 };
        _hIleri = SayfaBtn("▶"); _hIleri.Click += (_, _) => SayfaGoster(_hTumListe, _hSayfa + 1);
        _hSon  = SayfaBtn("⏭"); _hSon.Click   += (_, _) => SayfaGoster(_hTumListe, (int)Math.Ceiling((double)_hTumListe.Count / SayfaBoyutu) - 1);
        foreach (var c in new UIElement[] { _hIlk, _hGeri, _hSayfaTb, _hIleri, _hSon }) sayfaSp.Children.Add(c);
        sayfaBar.Child = sayfaSp;
        Grid.SetRow(sayfaBar, 3); outer.Children.Add(sayfaBar);

        TabHareketler.Content = outer;
        FiltreMasterKatGuncelle();
    }

    private void SayfaGoster(List<Hareket> liste, int sayfa)
    {
        _hTumListe = liste;
        var toplam = liste.Count;
        var toplamSayfa = (int)Math.Ceiling((double)toplam / SayfaBoyutu);
        _hSayfa = Math.Max(0, Math.Min(sayfa, toplamSayfa - 1));
        var bas = _hSayfa * SayfaBoyutu;
        var bit = Math.Min(bas + SayfaBoyutu, toplam);
        if (_hGrid != null) _hGrid.ItemsSource = liste.Skip(bas).Take(SayfaBoyutu).ToList();
        if (_hSayisiTb != null)
            _hSayisiTb.Text = toplam == 0
                ? "Kayıt bulunamadı"
                : $"{toplam} kayıt  —  {bas+1}-{bit} arası gösteriliyor";
        if (_hSayfaTb != null) _hSayfaTb.Text = $"Sayfa  {_hSayfa+1}  /  {Math.Max(1,toplamSayfa)}";
        if (_hIlk  != null) _hIlk.IsEnabled  = _hSayfa > 0;
        if (_hGeri != null) _hGeri.IsEnabled  = _hSayfa > 0;
        if (_hIleri!= null) _hIleri.IsEnabled = _hSayfa < toplamSayfa - 1;
        if (_hSon  != null) _hSon.IsEnabled   = _hSayfa < toplamSayfa - 1;
    }

    private void HareketlerFiltrele()
    {
        var bas = _fBas?.SelectedDate?.ToString("yyyy-MM-dd");
        var bit = _fBit?.SelectedDate?.ToString("yyyy-MM-dd");
        var tur = _fTur?.SelectedItem?.ToString(); if (tur == "Tümü") tur = null;
        var ana = _fAnaKat?.SelectedItem?.ToString(); if (ana == "Tüm Kategoriler") ana = null;
        var kal = _fKalem?.SelectedItem?.ToString(); if (kal == "Tüm Kalemler") kal = null;
        var kisi = _fKisi?.Text?.Trim(); if (string.IsNullOrEmpty(kisi)) kisi = null;
        var odeme = _fOdeme?.SelectedItem?.ToString(); if (odeme == "Tüm Ödemeler") odeme = null;
        var ara = _fAra?.Text?.Trim(); if (string.IsNullOrEmpty(ara)) ara = null;
        decimal? tmin = decimal.TryParse(_fTutarMin?.Text, out var mn) && mn > 0 ? mn : null;
        decimal? tmax = decimal.TryParse(_fTutarMax?.Text, out var mx) && mx > 0 ? mx : null;

        var liste = _db.HareketListesi(bas, bit, tur, ana, kal, kisi, odeme, tmin, tmax, ara);
        SayfaGoster(liste, 0);
    }

    private void HareketFiltreTemizle()
    {
        if (_fBas != null) _fBas.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        if (_fBit != null) _fBit.SelectedDate = DateTime.Today;
        if (_fTur != null) _fTur.SelectedIndex = 0;
        if (_fAnaKat != null) { _fAnaKat.Items.Clear(); _fAnaKat.Items.Add("Tüm Kategoriler"); _fAnaKat.SelectedIndex = 0; }
        if (_fKalem != null) { _fKalem.Items.Clear(); _fKalem.Items.Add("Tüm Kalemler"); _fKalem.SelectedIndex = 0; }
        if (_fKisi != null) _fKisi.Text = "";
        if (_fOdeme != null) _fOdeme.SelectedIndex = 0;
        if (_fTutarMin != null) _fTutarMin.Text = "";
        if (_fTutarMax != null) _fTutarMax.Text = "";
        if (_fAra != null) _fAra.Text = "";
        _hTumListe = _db.HareketListesi();
        SayfaGoster(_hTumListe, 0);
    }

    private void FiltreMasterKatGuncelle()
    {
        if (_fAnaKat == null) return;
        var tur = _fTur?.SelectedItem?.ToString(); if (tur == "Tümü") tur = null;
        _fAnaKat.Items.Clear(); _fAnaKat.Items.Add("Tüm Kategoriler");
        var katlar = tur != null ? _db.Kategoriler(tur) : _db.KalemListesi().Select(k => k.AnaKategori).Distinct().OrderBy(x => x).ToList();
        foreach (var k in katlar) _fAnaKat.Items.Add(k);
        _fAnaKat.SelectedIndex = 0;
    }

    private void FiltreKalemGuncelle()
    {
        if (_fKalem == null) return;
        var tur = _fTur?.SelectedItem?.ToString(); if (tur == "Tümü") tur = null;
        var ana = _fAnaKat?.SelectedItem?.ToString(); if (ana == "Tüm Kategoriler") ana = null;
        _fKalem.Items.Clear(); _fKalem.Items.Add("Tüm Kalemler");
        var kalemler = _db.KalemListesi()
            .Where(k => (tur == null || k.Tur == tur) && (ana == null || k.AnaKategori == ana))
            .Select(k => k.KalemAdi).Distinct().OrderBy(x => x);
        foreach (var k in kalemler) _fKalem.Items.Add(k);
        _fKalem.SelectedIndex = 0;
    }

    // ── GELİR/GİDER TABLOSU ─────────────────────────────────────────────
    private DatePicker? _tBas, _tBit; private ComboBox? _tTur;
    private TextBlock? _tGelir, _tGider, _tNet, _tSayi, _tOrt;
    private DataGrid? _tGrid;

    private void TabloFiltrele()
    {
        if (TabTablo.Content == null) TabloTabOlustur();
        var bas = _tBas?.SelectedDate?.ToString("yyyy-MM-dd");
        var bit = _tBit?.SelectedDate?.ToString("yyyy-MM-dd");
        var tur = _tTur?.SelectedItem?.ToString(); if (tur == "Tümü") tur = null;
        var liste = _db.HareketListesi(bas, bit, tur);
        var tGelir = liste.Sum(h => h.Giris); var tGider = liste.Sum(h => h.Cikis);
        var net = tGelir - tGider; var ort = liste.Count > 0 ? (tGelir + tGider) / liste.Count : 0;
        if (_tGelir != null) _tGelir.Text = Para(tGelir);
        if (_tGider != null) _tGider.Text = Para(tGider);
        if (_tNet   != null) { _tNet.Text = Para(net); _tNet.Foreground = net >= 0 ? (Brush)FindResource("GreenBrush") : (Brush)FindResource("RedBrush"); }
        if (_tSayi  != null) _tSayi.Text = liste.Count.ToString();
        if (_tOrt   != null) _tOrt.Text = Para(ort);
        if (_tGrid  != null) _tGrid.ItemsSource = liste;
    }

    private void TabloTabOlustur()
    {
        var outer = new Grid { Margin = new Thickness(20,16,20,20) };
        for (int i = 0; i < 4; i++) outer.RowDefinitions.Add(new RowDefinition { Height = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var filtreBorder = new Border { Style = (Style)FindResource("FormCard"), Padding = new Thickness(16,12,16,12), Margin = new Thickness(0,0,0,12) };
        var filtreRow = new WrapPanel { Orientation = Orientation.Horizontal };
        _tBas = new DatePicker { SelectedDate = new DateTime(DateTime.Today.Year,1,1), Width = 130, Margin = new Thickness(0,0,8,0) };
        _tBit = new DatePicker { SelectedDate = DateTime.Today, Width = 130, Margin = new Thickness(0,0,8,0) };
        _tTur = new ComboBox { Width = 120, Margin = new Thickness(0,0,8,0) };
        foreach (var t in new[] { "Tümü","Gelir","Gider","Kasa Giriş" }) _tTur.Items.Add(t);
        _tTur.SelectedIndex = 0;
        var bF = new Button { Content="🔍  Filtrele", Style=(Style)FindResource("PrimaryBtn"), Margin=new Thickness(0,0,6,0) }; bF.Click += (_,_)=>TabloFiltrele();
        var bT = new Button { Content="🔄  Temizle", Style=(Style)FindResource("SecondaryBtn") }; bT.Click += (_,_)=>{ _tBas.SelectedDate=new DateTime(DateTime.Today.Year,1,1); _tBit.SelectedDate=DateTime.Today; _tTur.SelectedIndex=0; TabloFiltrele(); };
        foreach (var w in new UIElement[]{FiltreSarma("BAŞLANGIÇ",_tBas),FiltreSarma("BİTİŞ",_tBit),FiltreSarma("TÜR",_tTur),bF,bT}) filtreRow.Children.Add(w);
        filtreBorder.Child = filtreRow; Grid.SetRow(filtreBorder,0); outer.Children.Add(filtreBorder);

        // Mini KPI
        var kpiRow = new UniformGrid { Columns=5, Rows=1, Margin=new Thickness(0,0,0,12) };
        T MiniKpi(string lbl, string renk, out TextBlock tb) {
            var b = new Border { Style=(Style)FindResource("KpiCard"), Margin=new Thickness(0,0,8,0) };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text=lbl, FontSize=10, FontWeight=FontWeights.SemiBold, Foreground=(Brush)FindResource("Text3Brush"), Margin=new Thickness(0,0,0,4) });
            tb = new TextBlock { Text="₺0,00", FontSize=16, FontWeight=FontWeights.Bold, FontFamily=new FontFamily("Courier New"), Foreground=(Brush)FindResource(renk) };
            sp.Children.Add(tb); b.Child=sp; return (T)(object)b; }
        kpiRow.Children.Add(MiniKpi("TOPLAM GELİR","GreenBrush",out _tGelir!));
        kpiRow.Children.Add(MiniKpi("TOPLAM GİDER","RedBrush",out _tGider!));
        kpiRow.Children.Add(MiniKpi("NET FARK","TextBrush",out _tNet!));
        kpiRow.Children.Add(MiniKpi("HAREKET","TextBrush",out _tSayi!));
        kpiRow.Children.Add(MiniKpi("ORT. İŞLEM","TextBrush",out _tOrt!));
        Grid.SetRow(kpiRow,1); outer.Children.Add(kpiRow);

        _tGrid = MakeDataGrid(false); Grid.SetRow(_tGrid,2); outer.Children.Add(_tGrid);
        TabTablo.Content = outer;
    }

    // ── RAPORLAR ────────────────────────────────────────────────────────
    private void RaporlarYukle()
    {
        var ozet = _db.GenelOzet();
        var aylik = _db.AylikOzet();
        var kategori = _db.KategoriDagilim();
        var top = _db.TopKalemler(5);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new StackPanel { Margin = new Thickness(20,16,20,20), Spacing = 14 };
        outer.Children.Add(new TextBlock { Text = "📈  Raporlar", Style = (Style)FindResource("PageTitle") });

        var grid2 = new UniformGrid { Columns = 2, Rows = 2 };

        // Aylık
        grid2.Children.Add(RaporKart("📅  Aylık Gelir - Gider Özeti", aylik.Select(a =>
            ($"{a.AyGoster}  ({a.Sayi} işlem)", Para(a.Net), a.Net >= 0 ? "GreenBrush" : "RedBrush")).ToList()));
        // Kategori
        grid2.Children.Add(RaporKart("📂  Kategori Bazlı Dağılım", kategori.Select(k =>
            ($"[{k.Tur}]  {k.Ana}", Para(k.Tutar), k.Tur == "Gelir" ? "GreenBrush" : k.Tur == "Gider" ? "RedBrush" : "Accent2Brush")).ToList()));
        // Top kalemler
        grid2.Children.Add(RaporKart("⭐  En Çok Kullanılan Kalemler", top.Select((k,i) =>
            ($"{i+1}. {k.Kalem}  ({k.Sayi}x)", Para(k.Tutar), "TextBrush")).ToList()));
        // Genel özet
        grid2.Children.Add(RaporKart("📊  Genel Özet", new List<(string,string,string)>
        {
            ("Toplam Hareket",   ozet.ToplamHareket.ToString(),   "TextBrush"),
            ("Toplam Gelir",     Para(ozet.ToplamGelir),           "GreenBrush"),
            ("Toplam Gider",     Para(ozet.ToplamGider),           "RedBrush"),
            ("Kasa Girişleri",   Para(ozet.KasaGiris),             "Accent2Brush"),
            ("Güncel Bakiye",    Para(ozet.Bakiye),                "TextBrush"),
            ("Tanımlı Kalem",    ozet.KalemSayisi.ToString(),      "TextBrush"),
        }));

        outer.Children.Add(grid2);
        scroll.Content = outer;
        TabRaporlar.Content = scroll;
    }

    private Border RaporKart(string baslik, List<(string lbl, string val, string renk)> satirlar)
    {
        var b = new Border { Style=(Style)FindResource("FormCard"), Margin=new Thickness(0,0,8,8) };
        var sp = new StackPanel { Spacing=4 };
        sp.Children.Add(new TextBlock { Text=baslik, FontSize=13, FontWeight=FontWeights.SemiBold, Foreground=(Brush)FindResource("Text2Brush"), Margin=new Thickness(0,0,0,8) });
        sp.Children.Add(new Separator());
        foreach (var (lbl, val, renk) in satirlar)
        {
            var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var lTb = new TextBlock { Text=lbl, Foreground=(Brush)FindResource("Text2Brush"), FontSize=12 };
            var vTb = new TextBlock { Text=val, Foreground=(Brush)FindResource(renk), FontWeight=FontWeights.SemiBold, FontFamily=new FontFamily("Courier New"), HorizontalAlignment=HorizontalAlignment.Right };
            Grid.SetColumn(vTb,1); row.Children.Add(lTb); row.Children.Add(vTb);
            sp.Children.Add(row);
        }
        if (satirlar.Count == 0) sp.Children.Add(new TextBlock { Text="Veri yok.", Foreground=(Brush)FindResource("Text3Brush") });
        b.Child = sp; return b;
    }

    // ── YEDEK ───────────────────────────────────────────────────────────
    private void Yedekle_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON Dosyası (*.json)|*.json",
            FileName = $"muhasebe_yedek_{DateTime.Today:yyyy-MM-dd}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            _db.JsonYedekAl(dlg.FileName);
            MessageBox.Show($"Yedek alındı:\n{dlg.FileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus($"✅ Yedek alındı: {dlg.FileName}");
        }
    }

    private void YedekYukle_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON Dosyası (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var json = System.IO.File.ReadAllText(dlg.FileName, System.Text.Encoding.UTF8);
            var veri = System.Text.Json.JsonDocument.Parse(json);
            var hSayi = veri.RootElement.TryGetProperty("hareketler", out var h) ? h.GetArrayLength() : 0;
            var kSayi = veri.RootElement.TryGetProperty("kalemler", out var k) ? k.GetArrayLength() : 0;
            var sonuc = MessageBox.Show(
                $"Dosyada {hSayi} hareket ve {kSayi} kalem var.\nMevcut verilerin üzerine yazılacak. Devam edilsin mi?",
                "Yedek Yükle", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (sonuc != MessageBoxResult.Yes) return;
            // Python yedekten yükleme için basit JSON import
            YedekJsonYukle(dlg.FileName);
            KpiGuncelle(); AnaSayfaYukle();
            MessageBox.Show($"{hSayi} hareket ve {kSayi} kalem yüklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus($"✅ Yedek yüklendi: {hSayi} hareket");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yedek yüklenemedi:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void YedekJsonYukle(string dosya) => _db.JsonYedekYukle(dosya);

    // ── CSV AKTAR ───────────────────────────────────────────────────────
    private void CsvAktar()
    {
        var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"hareketler_{DateTime.Today:yyyy-MM-dd}.csv" };
        if (dlg.ShowDialog() != true) return;
        var liste = _hTumListe.Count > 0 ? _hTumListe : _db.HareketListesi();
        var baslik = "İşlem No,Tarih,Tür,Ana Kategori,Kalem,Açıklama,Tutar,Giriş,Çıkış,Kimden/Kime,Ödeme,Belge,Bakiye";
        var satirlar = liste.Select(h =>
            $"{h.IslemNo},{h.TarihGoster},{h.Tur},{h.AnaKategori},{h.KalemAdi},{h.Aciklama},{h.Tutar},{h.Giris},{h.Cikis},{h.KimdenKime},{h.OdemeTuru},{h.BelgeNo},{h.Bakiye}");
        System.IO.File.WriteAllLines(dlg.FileName, new[] { baslik }.Concat(satirlar), System.Text.Encoding.UTF8);
        SetStatus($"✅ CSV aktarıldı: {dlg.FileName}");
    }

    // ── YARDIMCI ────────────────────────────────────────────────────────
    private DataGrid MakeDataGrid(bool editButon)
    {
        var dg = new DataGrid();
        dg.Columns.Add(Col("İşlem No",   "IslemNo",      80));
        dg.Columns.Add(Col("Tarih",       "TarihGoster",  95));
        dg.Columns.Add(Col("Tür",         "Tur",          80));
        dg.Columns.Add(Col("Kategori",    "AnaKategori",  110));
        dg.Columns.Add(Col("Kalem",       "KalemAdi",     140));
        dg.Columns.Add(Col("Açıklama",    "Aciklama",     new DataGridLength(1,DataGridLengthUnitType.Star)));
        dg.Columns.Add(Col("Tutar",       "TutarGoster",  100, true));
        dg.Columns.Add(Col("Giriş",       "GirisGoster",  90,  true));
        dg.Columns.Add(Col("Çıkış",       "CikisGoster",  90,  true));
        dg.Columns.Add(Col("Kimden/Kime", "KimdenKime",   120));
        dg.Columns.Add(Col("Ödeme",       "OdemeTuru",    90));
        dg.Columns.Add(Col("Belge",       "BelgeNo",      90));
        dg.Columns.Add(Col("Bakiye",      "BakiyeGoster", 110, true));

        if (editButon)
        {
            var editCol = new DataGridTemplateColumn { Header = "", Width = 36 };
            var ef = new FrameworkElementFactory(typeof(Button));
            ef.SetValue(Button.ContentProperty, "✏️");
            ef.SetValue(Button.StyleProperty, FindResource("IconBtn"));
            ef.AddHandler(Button.ClickEvent, new RoutedEventHandler(HareketDuzenle_Click));
            editCol.CellTemplate = new DataTemplate { VisualTree = ef };
            dg.Columns.Add(editCol);

            var silCol = new DataGridTemplateColumn { Header = "", Width = 36 };
            var sf = new FrameworkElementFactory(typeof(Button));
            sf.SetValue(Button.ContentProperty, "🗑️");
            sf.SetValue(Button.StyleProperty, FindResource("IconBtn"));
            sf.AddHandler(Button.ClickEvent, new RoutedEventHandler(HareketSil_Click));
            silCol.CellTemplate = new DataTemplate { VisualTree = sf };
            dg.Columns.Add(silCol);
        }
        return dg;
    }

    private static DataGridTextColumn Col(string header, string binding, DataGridLength width, bool sag = false) =>
        new() { Header = header, Binding = new System.Windows.Data.Binding(binding), Width = width,
                ElementStyle = sag ? new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) } } : null };

    private static DataGridTextColumn Col(string header, string binding, double width, bool sag = false) =>
        Col(header, binding, new DataGridLength(width), sag);

    private void HareketSil_Click(object sender, RoutedEventArgs e)
    {
        if (_hGrid?.CurrentItem is not Hareket h) return;
        if (MessageBox.Show($"{h.IslemNo} silinsin mi?","Onay",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
        { _db.HareketSil(h.Id); KpiGuncelle(); HareketlerIlkYukle(); SetStatus("Hareket silindi."); }
    }

    private void HareketDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (_hGrid?.CurrentItem is not Hareket h) return;
        var dlg = new HareketDuzenleWindow(_db, h.Id) { Owner = this };
        if (dlg.ShowDialog() == true) { KpiGuncelle(); HareketlerIlkYukle(); SetStatus($"✅ Hareket güncellendi: {h.IslemNo}"); }
    }

    private void KategoriDoldur(ComboBox cb, string tur)
    {
        var sec = cb.SelectedItem?.ToString();
        cb.Items.Clear(); cb.Items.Add("-- Seçiniz --");
        foreach (var k in _db.Kategoriler(tur)) cb.Items.Add(k);
        var idx = sec != null ? cb.Items.IndexOf(sec) : 0;
        cb.SelectedIndex = idx > 0 ? idx : 0;
    }

    private void AltKalemDoldur(ComboBox kat, ComboBox alt, string tur)
    {
        var ana = kat.SelectedItem?.ToString() ?? ""; alt.Items.Clear(); alt.Items.Add("-- Seçiniz --");
        if (!string.IsNullOrEmpty(ana) && ana != "-- Seçiniz --")
            foreach (var k in _db.AltKalemler(tur, ana)) alt.Items.Add(k.KalemAdi);
        alt.SelectedIndex = 0;
    }

    private void KisiComboGuncelle(ComboBox? cb)
    {
        if (cb == null) return;
        var sec = cb.Text;
        cb.Items.Clear(); cb.Items.Add("");
        foreach (var k in _db.KisiListesi()) cb.Items.Add(k);
        cb.Text = sec;
    }

    private bool FormDogrula(DatePicker? tarih, ComboBox? kat, ComboBox? alt, TextBox? tutar, ComboBox? odeme)
    {
        if (tarih?.SelectedDate == null || kat?.SelectedItem?.ToString() == "-- Seçiniz --" ||
            alt?.SelectedItem?.ToString() == "-- Seçiniz --" ||
            !decimal.TryParse(tutar?.Text?.Replace(",","."), out var t) || t <= 0 ||
            (odeme != null && odeme.SelectedItem == null))
        { MessageBox.Show("(*) işaretli alanları doldurun!","Eksik Alan",MessageBoxButton.OK,MessageBoxImage.Warning); return false; }
        return true;
    }

    private void FormTemizle(DatePicker? tarih, TextBox? tutar, TextBox? belge, TextBox? aciklama)
    {
        if (tarih != null) tarih.SelectedDate = DateTime.Today;
        if (tutar != null) tutar.Text = "0";
        if (belge != null) belge.Text = "";
        if (aciklama != null) aciklama.Text = "";
    }

    private static StackPanel FiltreSarma(string lbl, UIElement ctrl)
    {
        var sp = new StackPanel { Margin = new Thickness(0,0,8,0), VerticalAlignment = VerticalAlignment.Bottom };
        sp.Children.Add(new TextBlock { Text=lbl, FontSize=10, FontWeight=FontWeights.SemiBold,
            Foreground=new SolidColorBrush(Color.FromRgb(0x5A,0x64,0x80)), Margin=new Thickness(0,0,0,3) });
        sp.Children.Add(ctrl);
        return sp;
    }

    private static Button SayfaBtn(string icerik) =>
        new() { Content=icerik, Width=32, Height=32, FontSize=13,
                Background=new SolidColorBrush(Color.FromRgb(0x18,0x1C,0x27)),
                Foreground=new SolidColorBrush(Color.FromRgb(0x8B,0x96,0xB0)),
                BorderThickness=new Thickness(0), Margin=new Thickness(2,0,2,0), Cursor=System.Windows.Input.Cursors.Hand };

    private static T? FindName<T>(DependencyObject? parent, string tag) where T : FrameworkElement
    {
        if (parent == null) return null;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T el && el.Tag?.ToString() == tag) return el;
            var found = FindName<T>(child, tag);
            if (found != null) return found;
        }
        return null;
    }
}

// Generic local helper to avoid CS0246
file static class LocalHelper
{
    public static Border MiniKpiHelper(string lbl, string renk, out System.Windows.Controls.TextBlock tb, System.Windows.ResourceDictionary res)
    {
        var b = new Border { Style = (Style)res["KpiCard"], Margin = new Thickness(0, 0, 8, 0) };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = lbl, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = (SolidColorBrush)res["Text3Brush"], Margin = new Thickness(0, 0, 0, 4) });
        tb = new TextBlock { Text = "₺0,00", FontSize = 16, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New"), Foreground = (SolidColorBrush)res[renk] };
        sp.Children.Add(tb); b.Child = sp; return b;
    }
}
