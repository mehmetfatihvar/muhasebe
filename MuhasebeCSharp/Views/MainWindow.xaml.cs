using Microsoft.Win32;
using MuhasebeSistemi.Data;
using MuhasebeSistemi.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MuhasebeSistemi.Views;

public partial class MainWindow : Window
{
    private readonly Database _db = new();
    private List<Hareket> _hTumListe = new();
    private int _hSayfa = 0;
    private const int SayfaBoyutu = 100;

    // Form alanları field olarak (lambda içinde ref param kullanılamaz)
    private DatePicker? _gTarih; private ComboBox? _gKat, _gAlt, _gOdeme, _gKime;
    private TextBox? _gBelge, _gAciklama, _gTutar;
    private DatePicker? _glTarih; private ComboBox? _glKat, _glAlt, _glOdeme, _glKimden;
    private TextBox? _glBelge, _glAciklama, _glTutar;
    private DatePicker? _kTarih; private ComboBox? _kTur, _kAlt, _kKimden;
    private TextBox? _kBelge, _kAciklama, _kTutar;
    private ComboBox? _ktTur; private TextBox? _ktAna, _ktAlt, _ktAd, _ktAciklama;
    private DataGrid? _kalemGrid, _sonHareketGrid;
    private DataGrid? _hGrid; private TextBlock? _hSayisiTb, _hSayfaTb;
    private Button? _hIlk, _hGeri, _hIleri, _hSon;
    private DatePicker? _fBas, _fBit; private ComboBox? _fTur, _fAnaKat, _fKalem, _fKisi, _fOdeme;
    private TextBox? _fTutarMin, _fTutarMax, _fAra;
    private DatePicker? _tBas, _tBit; private ComboBox? _tTur;
    private TextBlock? _tGelir, _tGider, _tNet, _tSayi, _tOrt;
    private DataGrid? _tGrid;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { KpiGuncelle(); AnaSayfaYukle(); };
    }

    private void KpiGuncelle()
    {
        var ozet = _db.GenelOzet();
        var buAy = DateTime.Now.ToString("yyyy-MM");
        var hareketler = _db.HareketListesi();
        var buAyGelir = hareketler.Where(h => h.Tarih.StartsWith(buAy)).Sum(h => h.Giris);
        var buAyGider = hareketler.Where(h => h.Tarih.StartsWith(buAy)).Sum(h => h.Cikis);
        var son7 = hareketler.Count(h => {
            if (DateTime.TryParseExact(h.Tarih, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                return (DateTime.Now - d).Days <= 7;
            return false;
        });
        var net = buAyGelir - buAyGider;
        KpiKasa.Text = Para(ozet.Bakiye); KpiGelir.Text = Para(buAyGelir); KpiGider.Text = Para(buAyGider);
        KpiNet.Text = Para(net); KpiNet.Foreground = net >= 0 ? Br("GreenBrush") : Br("RedBrush");
        KpiSon7.Text = $"{son7} işlem";
    }

    private static string Para(decimal v) => v.ToString("N2", new System.Globalization.CultureInfo("tr-TR")) + " ₺";
    private Brush Br(string key) => (Brush)FindResource(key);
    private void SetStatus(string msg) => StatusText.Text = msg;

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl) return;
        KpiGuncelle();
        var tab = MainTabs.SelectedItem as TabItem;
        if (tab == TabAnaSayfa)       AnaSayfaYukle();
        else if (tab == TabGider)     GiderTabYukle();
        else if (tab == TabGelir)     GelirTabYukle();
        else if (tab == TabKasa)      KasaTabYukle();
        else if (tab == TabKalemler)  KalemlerYukle();
        else if (tab == TabHareketler) HareketlerIlkYukle();
        else if (tab == TabTablo)     TabloFiltrele();
        else if (tab == TabRaporlar)  RaporlarYukle();
    }

    // ── ANA SAYFA ───────────────────────────────────────────────────────
    private void AnaSayfaYukle()
    {
        if (TabAnaSayfa.Content == null) AnaSayfaOlustur();
        if (_sonHareketGrid != null) _sonHareketGrid.ItemsSource = _db.HareketListesi().Take(15).ToList();
    }

    private void AnaSayfaOlustur()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20, 16, 20, 20) };
        var outer = new StackPanel();
        outer.Children.Add(new TextBlock { Text = "Hızlı Erişim", Style = (Style)FindResource("PageTitle") });

        var wp = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
        foreach (var (ikon, ad, acik, hedef) in new (string, string, string, TabItem)[] {
            ("➕","Gider Ekle","Yeni gider kaydı",TabGider),
            ("💰","Gelir Ekle","Yeni gelir kaydı",TabGelir),
            ("🏦","Kasa Para Girişi","Sermaye/tahsilat",TabKasa),
            ("📋","Kalem Tanımları","Kategori yönetimi",TabKalemler),
            ("🗂","Tüm Hareketler","Kayıtları görüntüle",TabHareketler),
            ("📊","Gelir/Gider","Filtrelenmiş özet",TabTablo),
            ("📈","Raporlar","Aylık & kategori özeti",TabRaporlar) })
        {
            var border = new Border { Width=195, Height=78, Margin=new Thickness(0,0,10,10),
                Background=Br("SurfaceBrush"), BorderBrush=Br("BorderBrush"), BorderThickness=new Thickness(1),
                CornerRadius=new CornerRadius(10), Cursor=System.Windows.Input.Cursors.Hand };
            var sp = new StackPanel { Orientation=Orientation.Horizontal, Margin=new Thickness(14,0,0,0), VerticalAlignment=VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text=ikon, FontSize=22, Margin=new Thickness(0,0,10,0), VerticalAlignment=VerticalAlignment.Center });
            var txt = new StackPanel { VerticalAlignment=VerticalAlignment.Center };
            txt.Children.Add(new TextBlock { Text=ad, FontSize=13, FontWeight=FontWeights.SemiBold, Foreground=Br("TextBrush") });
            txt.Children.Add(new TextBlock { Text=acik, FontSize=11, Foreground=Br("Text3Brush") });
            sp.Children.Add(txt); border.Child = sp;
            var tab = hedef;
            border.MouseLeftButtonUp += (_, _) => MainTabs.SelectedItem = tab;
            border.MouseEnter += (_, _) => border.BorderBrush = Br("AccentBrush");
            border.MouseLeave += (_, _) => border.BorderBrush = Br("BorderBrush");
            wp.Children.Add(border);
        }
        outer.Children.Add(wp);
        outer.Children.Add(new TextBlock { Text = "Son Hareketler", Style = (Style)FindResource("PageTitle") });
        _sonHareketGrid = MakeDataGrid(false);
        outer.Children.Add(_sonHareketGrid);
        scroll.Content = outer; TabAnaSayfa.Content = scroll;
    }

    // ── GİDER ───────────────────────────────────────────────────────────
    private void GiderTabYukle()
    {
        if (TabGider.Content == null) TabGider.Content = FormTabOlustur("Gider");
        KisiComboGuncelle(_gKime); KategoriDoldur(_gKat!, "Gider");
    }
    private void GiderKaydet(object s, RoutedEventArgs e)
    {
        if (!FormDogrula(_gTarih, _gKat, _gAlt, _gTutar, _gOdeme)) return;
        var k = _db.AltKalemler("Gider", _gKat!.SelectedItem?.ToString()??string.Empty).FirstOrDefault(x=>x.KalemAdi==_gAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi { Tarih=_gTarih!.SelectedDate?.ToString("yyyy-MM-dd")??"", Tur="Gider",
            AnaKategori=_gKat!.SelectedItem?.ToString()??string.Empty, AltKategori=k?.AltKategori??string.Empty,
            KalemAdi=_gAlt!.SelectedItem?.ToString()??string.Empty,
            Tutar=decimal.TryParse(_gTutar!.Text.Replace(",","."),out var t)?t:0,
            OdemeTuru=_gOdeme!.SelectedItem?.ToString()??string.Empty, KimdenKime=_gKime!.Text, BelgeNo=_gBelge!.Text, Aciklama=_gAciklama!.Text });
        KpiGuncelle(); FormTemizle(_gTarih,_gTutar,_gBelge,_gAciklama); SetStatus($"✅ Gider: {no}");
    }

    // ── GELİR ───────────────────────────────────────────────────────────
    private void GelirTabYukle()
    {
        if (TabGelir.Content == null) TabGelir.Content = FormTabOlustur("Gelir");
        KisiComboGuncelle(_glKimden); KategoriDoldur(_glKat!, "Gelir");
    }
    private void GelirKaydet(object s, RoutedEventArgs e)
    {
        if (!FormDogrula(_glTarih, _glKat, _glAlt, _glTutar, _glOdeme)) return;
        var k = _db.AltKalemler("Gelir", _glKat!.SelectedItem?.ToString()??string.Empty).FirstOrDefault(x=>x.KalemAdi==_glAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi { Tarih=_glTarih!.SelectedDate?.ToString("yyyy-MM-dd")??"", Tur="Gelir",
            AnaKategori=_glKat!.SelectedItem?.ToString()??string.Empty, AltKategori=k?.AltKategori??string.Empty,
            KalemAdi=_glAlt!.SelectedItem?.ToString()??string.Empty,
            Tutar=decimal.TryParse(_glTutar!.Text.Replace(",","."),out var t)?t:0,
            OdemeTuru=_glOdeme!.SelectedItem?.ToString()??string.Empty, KimdenKime=_glKimden!.Text, BelgeNo=_glBelge!.Text, Aciklama=_glAciklama!.Text });
        KpiGuncelle(); FormTemizle(_glTarih,_glTutar,_glBelge,_glAciklama); SetStatus($"✅ Gelir: {no}");
    }

    // ── KASA ────────────────────────────────────────────────────────────
    private void KasaTabYukle()
    {
        if (TabKasa.Content == null) TabKasa.Content = FormTabOlustur("Kasa Giriş");
        KisiComboGuncelle(_kKimden); KategoriDoldur(_kTur!, "Kasa Giriş");
    }
    private void KasaKaydet(object s, RoutedEventArgs e)
    {
        if (!FormDogrula(_kTarih, _kTur, _kAlt, _kTutar, null)) return;
        if (string.IsNullOrWhiteSpace(_kKimden?.Text)) { MessageBox.Show("Kimden Geldi zorunlu!","Eksik",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        var k = _db.AltKalemler("Kasa Giriş", _kTur!.SelectedItem?.ToString()??string.Empty).FirstOrDefault(x=>x.KalemAdi==_kAlt!.SelectedItem?.ToString());
        var no = _db.HareketEkle(new HareketGirdisi { Tarih=_kTarih!.SelectedDate?.ToString("yyyy-MM-dd")??"", Tur="Kasa Giriş",
            AnaKategori=_kTur!.SelectedItem?.ToString()??string.Empty, AltKategori=k?.AltKategori??string.Empty,
            KalemAdi=_kAlt!.SelectedItem?.ToString()??string.Empty,
            Tutar=decimal.TryParse(_kTutar!.Text.Replace(",","."),out var t)?t:0,
            OdemeTuru="Nakit", KimdenKime=_kKimden!.Text, BelgeNo=_kBelge!.Text, Aciklama=_kAciklama!.Text });
        KpiGuncelle(); FormTemizle(_kTarih,_kTutar,_kBelge,_kAciklama); SetStatus($"✅ Kasa: {no}");
    }

    // ── FORM OLUŞTURUCU ─────────────────────────────────────────────────
    private UIElement FormTabOlustur(string tur)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new StackPanel { Margin = new Thickness(20,16,20,20) };
        outer.Children.Add(new TextBlock { Text=tur=="Gider"?"➕  Gider Girişi":tur=="Gelir"?"💰  Gelir Girişi":"🏦  Kasa Para Girişi", Style=(Style)FindResource("PageTitle") });

        var kart = new Border { Style=(Style)FindResource("FormCard"), MaxWidth=720, HorizontalAlignment=HorizontalAlignment.Left };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text=$"Yeni {(tur=="Kasa Giriş"?"Kasa":tur)} Kaydı — (*) zorunlu", FontSize=13, FontWeight=FontWeights.SemiBold, Foreground=Br("Text2Brush"), Margin=new Thickness(0,0,0,14) });

        var tarih = new DatePicker { SelectedDate=DateTime.Today };
        var kat = new ComboBox(); var alt = new ComboBox();
        var tutar = new TextBox { Text="0" };
        var odeme = new ComboBox();
        foreach (var o in new[]{"Nakit","Havale/EFT","Kredi Kartı","Çek","Senet"}) odeme.Items.Add(o);
        odeme.SelectedIndex = 0;
        var kisi = new ComboBox { IsEditable=true };
        var belge = new TextBox(); var aciklama = new TextBox { MinLines=2, AcceptsReturn=true, TextWrapping=TextWrapping.Wrap };

        if (tur=="Gider")      { _gTarih=tarih;_gKat=kat;_gAlt=alt;_gTutar=tutar;_gOdeme=odeme;_gKime=kisi;_gBelge=belge;_gAciklama=aciklama; }
        else if (tur=="Gelir") { _glTarih=tarih;_glKat=kat;_glAlt=alt;_glTutar=tutar;_glOdeme=odeme;_glKimden=kisi;_glBelge=belge;_glAciklama=aciklama; }
        else                   { _kTarih=tarih;_kTur=kat;_kAlt=alt;_kTutar=tutar;_kKimden=kisi;_kBelge=belge;_kAciklama=aciklama; }

        kat.SelectionChanged += (_, _) => AltKalemDoldur(kat, alt, tur);

        var g = new Grid { Margin=new Thickness(0,0,0,12) };
        g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i=0;i<4;i++) g.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});

        void Ekle(UIElement ctrl, string lbl, int row, int col, int span=1) {
            var w = new StackPanel{Margin=new Thickness(col==1?8:0,0,0,10)};
            w.Children.Add(new TextBlock{Text=lbl,Style=(Style)FindResource("FieldLabel")});
            w.Children.Add(ctrl);
            Grid.SetRow(w,row);Grid.SetColumn(w,col);Grid.SetColumnSpan(w,span);g.Children.Add(w); }

        Ekle(tarih,"TARİH *",0,0);
        Ekle(kat,tur=="Kasa Giriş"?"KASA GİRİŞ TÜRÜ *":$"{tur.ToUpper()} KATEGORİSİ *",0,1);
        Ekle(alt,"KALEM *",1,0); Ekle(tutar,"TUTAR (₺) *",1,1);
        if (tur!="Kasa Giriş") Ekle(odeme,tur=="Gelir"?"TAHSİLAT TÜRÜ *":"ÖDEME TÜRÜ *",2,0);
        Ekle(kisi,tur=="Gider"?"KİME ÖDENDİ":"KİMDEN GELDİ"+(tur=="Kasa Giriş"?" *":""),2,tur=="Kasa Giriş"?0:1);
        Ekle(belge,"BELGE / FİŞ NO",3,0); Ekle(aciklama,"AÇIKLAMA",3,1);
        sp.Children.Add(g);

        var btnRow = new StackPanel{Orientation=Orientation.Horizontal};
        RoutedEventHandler kaydetH = tur=="Gider"?GiderKaydet:tur=="Gelir"?(RoutedEventHandler)GelirKaydet:KasaKaydet;
        var bK = new Button{Content="✅  Kaydet",Style=(Style)FindResource("PrimaryBtn"),Margin=new Thickness(0,0,8,0)};
        bK.Click += kaydetH;
        var bT = new Button{Content="🗑️  Temizle",Style=(Style)FindResource("SecondaryBtn")};
        bT.Click += (_, _) => FormTemizle(tarih,tutar,belge,aciklama);
        btnRow.Children.Add(bK);btnRow.Children.Add(bT);sp.Children.Add(btnRow);
        kart.Child=sp;outer.Children.Add(kart);scroll.Content=outer;return scroll;
    }

    // ── KALEM TANIMLARI ─────────────────────────────────────────────────
    private void KalemlerYukle() { if(TabKalemler.Content==null) KalemTabOlustur(); KalemGridDoldur(); }

    private void KalemTabOlustur()
    {
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        var outer=new StackPanel{Margin=new Thickness(20,16,20,20)};
        outer.Children.Add(new TextBlock{Text="📋  Kalem Tanımları",Style=(Style)FindResource("PageTitle")});

        var kart=new Border{Style=(Style)FindResource("FormCard"),MaxWidth=720,HorizontalAlignment=HorizontalAlignment.Left};
        var sp=new StackPanel();
        sp.Children.Add(new TextBlock{Text="Yeni Kalem Ekle",FontSize=13,FontWeight=FontWeights.SemiBold,Foreground=Br("Text2Brush"),Margin=new Thickness(0,0,0,12)});

        _ktTur=new ComboBox(); foreach(var t in new[]{"Gider","Gelir","Kasa Giriş"}) _ktTur.Items.Add(t); _ktTur.SelectedIndex=0;
        _ktAna=new TextBox();_ktAlt=new TextBox();_ktAd=new TextBox();_ktAciklama=new TextBox();

        var g=new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition());g.ColumnDefinitions.Add(new ColumnDefinition());
        for(int i=0;i<3;i++) g.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        void E(UIElement c,string l,int r,int col){var w=new StackPanel{Margin=new Thickness(col==1?8:0,0,0,10)};w.Children.Add(new TextBlock{Text=l,Style=(Style)FindResource("FieldLabel")});w.Children.Add(c);Grid.SetRow(w,r);Grid.SetColumn(w,col);g.Children.Add(w);}
        E(_ktTur!,"KALEM TÜRÜ *",0,0);E(_ktAna!,"ANA KATEGORİ *",0,1);E(_ktAlt!,"ALT KATEGORİ *",1,0);E(_ktAd!,"KALEM ADI *",1,1);
        var aw=new StackPanel{Margin=new Thickness(0,0,0,10)};aw.Children.Add(new TextBlock{Text="AÇIKLAMA",Style=(Style)FindResource("FieldLabel")});aw.Children.Add(_ktAciklama!);
        Grid.SetRow(aw,2);Grid.SetColumnSpan(aw,2);g.Children.Add(aw);sp.Children.Add(g);

        var br=new StackPanel{Orientation=Orientation.Horizontal};
        var bK=new Button{Content="✅  Kaydet",Style=(Style)FindResource("PrimaryBtn"),Margin=new Thickness(0,0,8,0)};bK.Click+=KalemKaydet;
        var bT=new Button{Content="🗑️  Temizle",Style=(Style)FindResource("SecondaryBtn")};
        bT.Click+=(_, _)=>{_ktAna!.Text="";_ktAlt!.Text="";_ktAd!.Text="";_ktAciklama!.Text="";_ktTur!.SelectedIndex=0;};
        br.Children.Add(bK);br.Children.Add(bT);sp.Children.Add(br);kart.Child=sp;outer.Children.Add(kart);

        outer.Children.Add(new TextBlock{Text="Tanımlı Kalemler",Style=(Style)FindResource("PageTitle"),Margin=new Thickness(0,16,0,8)});
        _kalemGrid=new DataGrid{MaxHeight=380};
        _kalemGrid.Columns.Add(Col("ID","Id",50));_kalemGrid.Columns.Add(Col("Tür","Tur",90));
        _kalemGrid.Columns.Add(Col("Ana Kategori","AnaKategori",130));_kalemGrid.Columns.Add(Col("Alt Kategori","AltKategori",130));
        _kalemGrid.Columns.Add(Col("Kalem Adı","KalemAdi",new DataGridLength(1,DataGridLengthUnitType.Star)));
        _kalemGrid.Columns.Add(Col("Açıklama","Aciklama",180));_kalemGrid.Columns.Add(Col("Eklenme","EklenmeTarihi",100));
        var sc2=new DataGridTemplateColumn{Header="",Width=50};
        var sf=new FrameworkElementFactory(typeof(Button));sf.SetValue(Button.ContentProperty,"🗑️");sf.SetValue(Button.StyleProperty,FindResource("IconBtn"));
        sf.AddHandler(Button.ClickEvent,new RoutedEventHandler(KalemSil_Click));
        sc2.CellTemplate=new DataTemplate{VisualTree=sf};_kalemGrid.Columns.Add(sc2);
        outer.Children.Add(_kalemGrid);scroll.Content=outer;TabKalemler.Content=scroll;
    }

    private void KalemGridDoldur(){if(_kalemGrid!=null)_kalemGrid.ItemsSource=_db.KalemListesi();}
    private void KalemKaydet(object s,RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(_ktAna?.Text)||string.IsNullOrWhiteSpace(_ktAlt?.Text)||string.IsNullOrWhiteSpace(_ktAd?.Text))
        {MessageBox.Show("(*) alanları doldurun!","Eksik",MessageBoxButton.OK,MessageBoxImage.Warning);return;}
        _db.KalemEkle(_ktTur!.SelectedItem?.ToString()!,_ktAna!.Text.Trim(),_ktAlt!.Text.Trim(),_ktAd!.Text.Trim(),_ktAciklama?.Text.Trim()??string.Empty);
        KalemGridDoldur();SetStatus($"✅ Kalem: {_ktAd!.Text}");
        _ktAna.Text="";_ktAlt!.Text="";_ktAd!.Text="";if(_ktAciklama!=null)_ktAciklama.Text="";
    }
    private void KalemSil_Click(object s,RoutedEventArgs e)
    {
        if(_kalemGrid?.CurrentItem is Kalem k&&MessageBox.Show($"'{k.KalemAdi}' silinsin mi?","Onay",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
        {_db.KalemSil(k.Id);KalemGridDoldur();SetStatus("Kalem silindi.");}
    }

    // ── TÜM HAREKETLER ──────────────────────────────────────────────────
    private void HareketlerIlkYukle(){if(TabHareketler.Content==null)HareketlerTabOlustur();_hTumListe=_db.HareketListesi();SayfaGoster(_hTumListe,0);}

    private void HareketlerTabOlustur()
    {
        var outer=new Grid{Margin=new Thickness(20,16,20,20)};
        outer.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        outer.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        outer.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
        outer.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});

        var fb=new Border{Style=(Style)FindResource("FormCard"),Padding=new Thickness(16,12,16,12),Margin=new Thickness(0,0,0,12)};
        var fm=new StackPanel();var s1=new WrapPanel();var s2=new WrapPanel{Margin=new Thickness(0,8,0,0)};
        _fBas=new DatePicker{SelectedDate=new DateTime(DateTime.Today.Year,1,1),Width=130,Margin=new Thickness(0,0,8,0)};
        _fBit=new DatePicker{SelectedDate=DateTime.Today,Width=130,Margin=new Thickness(0,0,8,0)};
        _fTur=new ComboBox{Width=120,Margin=new Thickness(0,0,8,0)};
        foreach(var t in new[]{"Tümü","Gelir","Gider","Kasa Giriş"}) _fTur.Items.Add(t);
        _fTur.SelectedIndex=0;_fTur.SelectionChanged+=(_, _)=>FiltreMasterKatGuncelle();
        _fAnaKat=new ComboBox{Width=150,Margin=new Thickness(0,0,8,0)};_fAnaKat.SelectionChanged+=(_, _)=>FiltreKalemGuncelle();
        _fKalem=new ComboBox{Width=180,Margin=new Thickness(0,0,8,0)};
        foreach(var w in new UIElement[]{FS("BAŞLANGIÇ",_fBas),FS("BİTİŞ",_fBit),FS("TÜR",_fTur),FS("KATEGORİ",_fAnaKat),FS("KALEM",_fKalem)}) s1.Children.Add(w);

        _fKisi=new ComboBox{Width=150,IsEditable=true,Margin=new Thickness(0,0,8,0)};
        _fOdeme=new ComboBox{Width=140,Margin=new Thickness(0,0,8,0)};
        foreach(var o in new[]{"Tüm Ödemeler","Nakit","Havale/EFT","Kredi Kartı","Çek","Senet"}) _fOdeme.Items.Add(o);_fOdeme.SelectedIndex=0;
        _fTutarMin=new TextBox{Width=100,Margin=new Thickness(0,0,8,0)};_fTutarMax=new TextBox{Width=100,Margin=new Thickness(0,0,8,0)};
        _fAra=new TextBox{Width=180,Margin=new Thickness(0,0,8,0)};_fAra.KeyDown+=(_, k)=>{if(k.Key==System.Windows.Input.Key.Return)HareketlerFiltrele();};
        foreach(var w in new UIElement[]{FS("KİŞİ/FİRMA",_fKisi),FS("ÖDEME",_fOdeme),FS("MİN TUTAR",_fTutarMin),FS("MAX TUTAR",_fTutarMax),FS("GENEL ARAMA",_fAra)}) s2.Children.Add(w);

        var br2=new WrapPanel{Margin=new Thickness(0,10,0,0)};
        var bF=new Button{Content="🔍  Filtrele",Style=(Style)FindResource("PrimaryBtn"),Margin=new Thickness(0,0,6,0)};bF.Click+=(_, _)=>HareketlerFiltrele();
        var bT2=new Button{Content="🔄  Temizle",Style=(Style)FindResource("SecondaryBtn"),Margin=new Thickness(0,0,6,0)};bT2.Click+=(_, _)=>HareketFiltreTemizle();
        var bC=new Button{Content="📥  CSV",Style=(Style)FindResource("GreenBtn")};bC.Click+=(_, _)=>CsvAktar();
        br2.Children.Add(bF);br2.Children.Add(bT2);br2.Children.Add(bC);
        fm.Children.Add(s1);fm.Children.Add(s2);fm.Children.Add(br2);fb.Child=fm;Grid.SetRow(fb,0);outer.Children.Add(fb);

        _hSayisiTb=new TextBlock{Text="Tüm Kayıtlar",FontSize=15,FontWeight=FontWeights.Bold,Foreground=Br("TextBrush"),Margin=new Thickness(0,0,0,8)};
        Grid.SetRow(_hSayisiTb,1);outer.Children.Add(_hSayisiTb);
        _hGrid=MakeDataGrid(true);Grid.SetRow(_hGrid,2);outer.Children.Add(_hGrid);

        var spBar=new Border{Background=Br("SurfaceBrush"),BorderBrush=Br("BorderBrush"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Height=44,Margin=new Thickness(0,8,0,0)};
        var spSp=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        _hIlk=SBtn("⏮");_hIlk.Click+=(_, _)=>SayfaGoster(_hTumListe,0);
        _hGeri=SBtn("◀");_hGeri.Click+=(_, _)=>SayfaGoster(_hTumListe,_hSayfa-1);
        _hSayfaTb=new TextBlock{Text="Sayfa 1/1",Width=140,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=Br("Text2Brush"),FontSize=13};
        _hIleri=SBtn("▶");_hIleri.Click+=(_, _)=>SayfaGoster(_hTumListe,_hSayfa+1);
        _hSon=SBtn("⏭");_hSon.Click+=(_, _)=>SayfaGoster(_hTumListe,(int)Math.Ceiling((double)_hTumListe.Count/SayfaBoyutu)-1);
        foreach(var c in new UIElement[]{_hIlk,_hGeri,_hSayfaTb,_hIleri,_hSon}) spSp.Children.Add(c);
        spBar.Child=spSp;Grid.SetRow(spBar,3);outer.Children.Add(spBar);
        TabHareketler.Content=outer;FiltreMasterKatGuncelle();
    }

    private void SayfaGoster(List<Hareket> liste,int sayfa)
    {
        _hTumListe=liste;var toplam=liste.Count;
        var toplamS=(int)Math.Ceiling((double)toplam/SayfaBoyutu);
        _hSayfa=Math.Max(0,Math.Min(sayfa,toplamS-1));
        var bas=_hSayfa*SayfaBoyutu;var bit=Math.Min(bas+SayfaBoyutu,toplam);
        if(_hGrid!=null)_hGrid.ItemsSource=liste.Skip(bas).Take(SayfaBoyutu).ToList();
        if(_hSayisiTb!=null)_hSayisiTb.Text=toplam==0?"Kayıt bulunamadı":$"{toplam} kayıt — {bas+1}-{bit} arası";
        if(_hSayfaTb!=null)_hSayfaTb.Text=$"Sayfa {_hSayfa+1}/{Math.Max(1,toplamS)}";
        if(_hIlk!=null)_hIlk.IsEnabled=_hSayfa>0;if(_hGeri!=null)_hGeri.IsEnabled=_hSayfa>0;
        if(_hIleri!=null)_hIleri.IsEnabled=_hSayfa<toplamS-1;if(_hSon!=null)_hSon.IsEnabled=_hSayfa<toplamS-1;
    }

    private void HareketlerFiltrele()
    {
        var bas=_fBas?.SelectedDate?.ToString("yyyy-MM-dd");var bit=_fBit?.SelectedDate?.ToString("yyyy-MM-dd");
        var tur=_fTur?.SelectedItem?.ToString();if(tur=="Tümü")tur=null;
        var ana=_fAnaKat?.SelectedItem?.ToString();if(ana=="Tüm Kategoriler")ana=null;
        var kal=_fKalem?.SelectedItem?.ToString();if(kal=="Tüm Kalemler")kal=null;
        var kisi=_fKisi?.Text?.Trim();if(string.IsNullOrEmpty(kisi))kisi=null;
        var odeme=_fOdeme?.SelectedItem?.ToString();if(odeme=="Tüm Ödemeler")odeme=null;
        var ara=_fAra?.Text?.Trim();if(string.IsNullOrEmpty(ara))ara=null;
        decimal? tmin=decimal.TryParse(_fTutarMin?.Text,out var mn)&&mn>0?mn:null;
        decimal? tmax=decimal.TryParse(_fTutarMax?.Text,out var mx)&&mx>0?mx:null;
        SayfaGoster(_db.HareketListesi(bas,bit,tur,ana,kal,kisi,odeme,tmin,tmax,ara),0);
    }

    private void HareketFiltreTemizle()
    {
        if(_fBas!=null)_fBas.SelectedDate=new DateTime(DateTime.Today.Year,1,1);
        if(_fBit!=null)_fBit.SelectedDate=DateTime.Today;if(_fTur!=null)_fTur.SelectedIndex=0;
        if(_fAnaKat!=null){_fAnaKat.Items.Clear();_fAnaKat.Items.Add("Tüm Kategoriler");_fAnaKat.SelectedIndex=0;}
        if(_fKalem!=null){_fKalem.Items.Clear();_fKalem.Items.Add("Tüm Kalemler");_fKalem.SelectedIndex=0;}
        if(_fKisi!=null)_fKisi.Text="";if(_fOdeme!=null)_fOdeme.SelectedIndex=0;
        if(_fTutarMin!=null)_fTutarMin.Text="";if(_fTutarMax!=null)_fTutarMax.Text="";if(_fAra!=null)_fAra.Text="";
        SayfaGoster(_db.HareketListesi(),0);
    }

    private void FiltreMasterKatGuncelle()
    {
        if(_fAnaKat==null)return;var tur=_fTur?.SelectedItem?.ToString();if(tur=="Tümü")tur=null;
        _fAnaKat.Items.Clear();_fAnaKat.Items.Add("Tüm Kategoriler");
        var katlar=tur!=null?_db.Kategoriler(tur):_db.KalemListesi().Select(k=>k.AnaKategori).Distinct().OrderBy(x=>x).ToList();
        foreach(var k in katlar)_fAnaKat.Items.Add(k);_fAnaKat.SelectedIndex=0;
    }

    private void FiltreKalemGuncelle()
    {
        if(_fKalem==null)return;var tur=_fTur?.SelectedItem?.ToString();if(tur=="Tümü")tur=null;
        var ana=_fAnaKat?.SelectedItem?.ToString();if(ana=="Tüm Kategoriler")ana=null;
        _fKalem.Items.Clear();_fKalem.Items.Add("Tüm Kalemler");
        foreach(var k in _db.KalemListesi().Where(k=>(tur==null||k.Tur==tur)&&(ana==null||k.AnaKategori==ana)).Select(k=>k.KalemAdi).Distinct().OrderBy(x=>x)) _fKalem.Items.Add(k);
        _fKalem.SelectedIndex=0;
    }

    // ── GELİR/GİDER TABLOSU ─────────────────────────────────────────────
    private void TabloFiltrele()
    {
        if(TabTablo.Content==null)TabloTabOlustur();
        var bas=_tBas?.SelectedDate?.ToString("yyyy-MM-dd");var bit=_tBit?.SelectedDate?.ToString("yyyy-MM-dd");
        var tur=_tTur?.SelectedItem?.ToString();if(tur=="Tümü")tur=null;
        var liste=_db.HareketListesi(bas,bit,tur);
        var tG=liste.Sum(h=>h.Giris);var tGi=liste.Sum(h=>h.Cikis);var net=tG-tGi;var ort=liste.Count>0?(tG+tGi)/liste.Count:0;
        if(_tGelir!=null)_tGelir.Text=Para(tG);if(_tGider!=null)_tGider.Text=Para(tGi);
        if(_tNet!=null){_tNet.Text=Para(net);_tNet.Foreground=net>=0?Br("GreenBrush"):Br("RedBrush");}
        if(_tSayi!=null)_tSayi.Text=liste.Count.ToString();if(_tOrt!=null)_tOrt.Text=Para(ort);
        if(_tGrid!=null)_tGrid.ItemsSource=liste;
    }

    private void TabloTabOlustur()
    {
        var outer=new Grid{Margin=new Thickness(20,16,20,20)};
        for(int i=0;i<3;i++) outer.RowDefinitions.Add(new RowDefinition{Height=i==2?new GridLength(1,GridUnitType.Star):GridLength.Auto});
        var fb=new Border{Style=(Style)FindResource("FormCard"),Padding=new Thickness(16,12,16,12),Margin=new Thickness(0,0,0,12)};
        var fRow=new WrapPanel();
        _tBas=new DatePicker{SelectedDate=new DateTime(DateTime.Today.Year,1,1),Width=130,Margin=new Thickness(0,0,8,0)};
        _tBit=new DatePicker{SelectedDate=DateTime.Today,Width=130,Margin=new Thickness(0,0,8,0)};
        _tTur=new ComboBox{Width=120,Margin=new Thickness(0,0,8,0)};
        foreach(var t in new[]{"Tümü","Gelir","Gider","Kasa Giriş"})_tTur.Items.Add(t);_tTur.SelectedIndex=0;
        var bF=new Button{Content="🔍  Filtrele",Style=(Style)FindResource("PrimaryBtn"),Margin=new Thickness(0,0,6,0)};bF.Click+=(_, _)=>TabloFiltrele();
        var bT=new Button{Content="🔄  Temizle",Style=(Style)FindResource("SecondaryBtn")};bT.Click+=(_, _)=>{_tBas.SelectedDate=new DateTime(DateTime.Today.Year,1,1);_tBit.SelectedDate=DateTime.Today;_tTur.SelectedIndex=0;TabloFiltrele();};
        foreach(var w in new UIElement[]{FS("BAŞLANGIÇ",_tBas),FS("BİTİŞ",_tBit),FS("TÜR",_tTur),bF,bT})fRow.Children.Add(w);
        fb.Child=fRow;Grid.SetRow(fb,0);outer.Children.Add(fb);

        var kpiRow=new WrapPanel{Margin=new Thickness(0,0,0,12)};
        Border MK(string lbl,string renk,out TextBlock tb){
            var b=new Border{Style=(Style)FindResource("KpiCard"),Margin=new Thickness(0,0,8,0),MinWidth=130};
            var sp2=new StackPanel();sp2.Children.Add(new TextBlock{Text=lbl,FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=Br("Text3Brush"),Margin=new Thickness(0,0,0,4)});
            tb=new TextBlock{Text="₺0,00",FontSize=16,FontWeight=FontWeights.Bold,FontFamily=new FontFamily("Courier New"),Foreground=Br(renk)};
            sp2.Children.Add(tb);b.Child=sp2;return b;}
        kpiRow.Children.Add(MK("TOPLAM GELİR","GreenBrush",out _tGelir!));
        kpiRow.Children.Add(MK("TOPLAM GİDER","RedBrush",out _tGider!));
        kpiRow.Children.Add(MK("NET FARK","TextBrush",out _tNet!));
        kpiRow.Children.Add(MK("HAREKET","TextBrush",out _tSayi!));
        kpiRow.Children.Add(MK("ORT. İŞLEM","TextBrush",out _tOrt!));
        Grid.SetRow(kpiRow,1);outer.Children.Add(kpiRow);
        _tGrid=MakeDataGrid(false);Grid.SetRow(_tGrid,2);outer.Children.Add(_tGrid);TabTablo.Content=outer;
    }

    // ── RAPORLAR ────────────────────────────────────────────────────────
    private void RaporlarYukle()
    {
        var ozet=_db.GenelOzet();var aylik=_db.AylikOzet();var kategori=_db.KategoriDagilim();var top=_db.TopKalemler(5);
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        var outer=new StackPanel{Margin=new Thickness(20,16,20,20)};
        outer.Children.Add(new TextBlock{Text="📈  Raporlar",Style=(Style)FindResource("PageTitle")});
        var g2=new UniformGrid{Columns=2};
        g2.Children.Add(RK("📅  Aylık Özeti",aylik.Select(a=>($"{a.AyGoster}  ({a.Sayi} işlem)",Para(a.Net),a.Net>=0?"GreenBrush":"RedBrush")).ToList()));
        g2.Children.Add(RK("📂  Kategori Dağılım",kategori.Select(k=>($"[{k.Tur}]  {k.Ana}",Para(k.Tutar),k.Tur=="Gelir"?"GreenBrush":k.Tur=="Gider"?"RedBrush":"Accent2Brush")).ToList()));
        g2.Children.Add(RK("⭐  En Çok Kullanılan",top.Select((k,i)=>($"{i+1}. {k.Kalem}  ({k.Sayi}x)",Para(k.Tutar),"TextBrush")).ToList()));
        g2.Children.Add(RK("📊  Genel Özet",new List<(string,string,string)>{("Toplam Hareket",ozet.ToplamHareket.ToString(),"TextBrush"),("Toplam Gelir",Para(ozet.ToplamGelir),"GreenBrush"),("Toplam Gider",Para(ozet.ToplamGider),"RedBrush"),("Kasa Girişleri",Para(ozet.KasaGiris),"Accent2Brush"),("Güncel Bakiye",Para(ozet.Bakiye),"TextBrush"),("Tanımlı Kalem",ozet.KalemSayisi.ToString(),"TextBrush")}));
        outer.Children.Add(g2);scroll.Content=outer;TabRaporlar.Content=scroll;
    }

    private Border RK(string baslik,List<(string lbl,string val,string renk)> satirlar)
    {
        var b=new Border{Style=(Style)FindResource("FormCard"),Margin=new Thickness(0,0,8,8)};
        var sp=new StackPanel();sp.Children.Add(new TextBlock{Text=baslik,FontSize=13,FontWeight=FontWeights.SemiBold,Foreground=Br("Text2Brush"),Margin=new Thickness(0,0,0,8)});sp.Children.Add(new Separator());
        foreach(var (lbl,val,renk) in satirlar){var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            var lt=new TextBlock{Text=lbl,Foreground=Br("Text2Brush"),FontSize=12};var vt=new TextBlock{Text=val,Foreground=Br(renk),FontWeight=FontWeights.SemiBold,FontFamily=new FontFamily("Courier New"),HorizontalAlignment=HorizontalAlignment.Right};
            Grid.SetColumn(vt,1);row.Children.Add(lt);row.Children.Add(vt);sp.Children.Add(row);}
        if(satirlar.Count==0)sp.Children.Add(new TextBlock{Text="Veri yok.",Foreground=Br("Text3Brush")});
        b.Child=sp;return b;
    }

    // ── YEDEK ───────────────────────────────────────────────────────────
    private void Yedekle_Click(object s,RoutedEventArgs e)
    {
        var dlg=new SaveFileDialog{Filter="JSON (*.json)|*.json",FileName=$"muhasebe_yedek_{DateTime.Today:yyyy-MM-dd}.json"};
        if(dlg.ShowDialog()==true){_db.JsonYedekAl(dlg.FileName);MessageBox.Show($"Yedek alındı:\n{dlg.FileName}","Başarılı",MessageBoxButton.OK,MessageBoxImage.Information);SetStatus($"✅ Yedek: {dlg.FileName}");}
    }

    private void YedekYukle_Click(object s,RoutedEventArgs e)
    {
        var dlg=new OpenFileDialog{Filter="JSON (*.json)|*.json"};
        if(dlg.ShowDialog()!=true)return;
        try{
            var json=System.IO.File.ReadAllText(dlg.FileName,System.Text.Encoding.UTF8);
            var doc=System.Text.Json.JsonDocument.Parse(json);
            var hS=doc.RootElement.TryGetProperty("hareketler",out var h)?h.GetArrayLength():0;
            var kS=doc.RootElement.TryGetProperty("kalemler",out var k)?k.GetArrayLength():0;
            if(MessageBox.Show($"Dosyada {hS} hareket ve {kS} kalem var.\nDevam?","Yedek Yükle",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
            _db.JsonYedekYukle(dlg.FileName);KpiGuncelle();AnaSayfaYukle();
            MessageBox.Show($"{hS} hareket yüklendi!","Başarılı",MessageBoxButton.OK,MessageBoxImage.Information);SetStatus($"✅ Yedek yüklendi.");
        }catch(Exception ex){MessageBox.Show($"Hata:\n{ex.Message}","Hata",MessageBoxButton.OK,MessageBoxImage.Error);}
    }

    private void CsvAktar()
    {
        var dlg=new SaveFileDialog{Filter="CSV (*.csv)|*.csv",FileName=$"hareketler_{DateTime.Today:yyyy-MM-dd}.csv"};
        if(dlg.ShowDialog()!=true)return;
        var liste=_hTumListe.Count>0?_hTumListe:_db.HareketListesi();
        var lines=new List<string>{"İşlem No,Tarih,Tür,Ana Kategori,Kalem,Açıklama,Tutar,Giriş,Çıkış,Kimden/Kime,Ödeme,Belge,Bakiye"};
        lines.AddRange(liste.Select(h=>$"{h.IslemNo},{h.TarihGoster},{h.Tur},{h.AnaKategori},{h.KalemAdi},{h.Aciklama},{h.Tutar},{h.Giris},{h.Cikis},{h.KimdenKime},{h.OdemeTuru},{h.BelgeNo},{h.Bakiye}"));
        System.IO.File.WriteAllLines(dlg.FileName,lines,System.Text.Encoding.UTF8);SetStatus($"✅ CSV: {dlg.FileName}");
    }

    private void HareketSil_Click(object s,RoutedEventArgs e)
    {
        if(_hGrid?.CurrentItem is not Hareket h)return;
        if(MessageBox.Show($"{h.IslemNo} silinsin mi?","Onay",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
        {_db.HareketSil(h.Id);KpiGuncelle();HareketlerIlkYukle();SetStatus("Hareket silindi.");}
    }

    private void HareketDuzenle_Click(object s,RoutedEventArgs e)
    {
        if(_hGrid?.CurrentItem is not Hareket h)return;
        var dlg=new HareketDuzenleWindow(_db,h.Id){Owner=this};
        if(dlg.ShowDialog()==true){KpiGuncelle();HareketlerIlkYukle();SetStatus($"✅ Güncellendi: {h.IslemNo}");}
    }

    // ── YARDIMCI ────────────────────────────────────────────────────────
    private DataGrid MakeDataGrid(bool editButon)
    {
        var dg=new DataGrid();
        dg.Columns.Add(Col("İşlem No","IslemNo",80));dg.Columns.Add(Col("Tarih","TarihGoster",95));
        dg.Columns.Add(Col("Tür","Tur",80));dg.Columns.Add(Col("Kategori","AnaKategori",110));
        dg.Columns.Add(Col("Kalem","KalemAdi",140));
        dg.Columns.Add(Col("Açıklama","Aciklama",new DataGridLength(1,DataGridLengthUnitType.Star)));
        dg.Columns.Add(Col("Tutar","TutarGoster",100,true));dg.Columns.Add(Col("Giriş","GirisGoster",90,true));
        dg.Columns.Add(Col("Çıkış","CikisGoster",90,true));dg.Columns.Add(Col("Kimden/Kime","KimdenKime",120));
        dg.Columns.Add(Col("Ödeme","OdemeTuru",90));dg.Columns.Add(Col("Belge","BelgeNo",90));
        dg.Columns.Add(Col("Bakiye","BakiyeGoster",110,true));
        if(editButon){
            void AddBtn(string ikon,RoutedEventHandler handler){
                var c=new DataGridTemplateColumn{Header="",Width=36};
                var f=new FrameworkElementFactory(typeof(Button));f.SetValue(Button.ContentProperty,ikon);f.SetValue(Button.StyleProperty,FindResource("IconBtn"));f.AddHandler(Button.ClickEvent,handler);
                c.CellTemplate=new DataTemplate{VisualTree=f};dg.Columns.Add(c);}
            AddBtn("✏️",HareketDuzenle_Click);AddBtn("🗑️",HareketSil_Click);}
        return dg;
    }

    private static DataGridTextColumn Col(string h,string b,DataGridLength w,bool sag=false)=>
        new(){Header=h,Binding=new System.Windows.Data.Binding(b),Width=w,
              ElementStyle=sag?new Style(typeof(TextBlock)){Setters={new Setter(TextBlock.TextAlignmentProperty,TextAlignment.Right)}}:null};
    private static DataGridTextColumn Col(string h,string b,double w,bool sag=false)=>Col(h,b,new DataGridLength(w),sag);

    private void KategoriDoldur(ComboBox cb,string tur){var sec=cb.SelectedItem?.ToString();cb.Items.Clear();cb.Items.Add("-- Seçiniz --");foreach(var k in _db.Kategoriler(tur))cb.Items.Add(k);var idx=sec!=null?cb.Items.IndexOf(sec):0;cb.SelectedIndex=idx>0?idx:0;}
    private void AltKalemDoldur(ComboBox kat,ComboBox alt,string tur){var ana=kat.SelectedItem?.ToString()??string.Empty;alt.Items.Clear();alt.Items.Add("-- Seçiniz --");if(!string.IsNullOrEmpty(ana)&&ana!="-- Seçiniz --")foreach(var k in _db.AltKalemler(tur,ana))alt.Items.Add(k.KalemAdi);alt.SelectedIndex=0;}
    private void KisiComboGuncelle(ComboBox? cb){if(cb==null)return;var sec=cb.Text;cb.Items.Clear();cb.Items.Add(string.Empty);foreach(var k in _db.KisiListesi())cb.Items.Add(k);cb.Text=sec;}
    private bool FormDogrula(DatePicker? tarih,ComboBox? kat,ComboBox? alt,TextBox? tutar,ComboBox? odeme){if(tarih?.SelectedDate==null||kat?.SelectedItem?.ToString()=="-- Seçiniz --"||alt?.SelectedItem?.ToString()=="-- Seçiniz --"||!decimal.TryParse(tutar?.Text?.Replace(",","."),out var t)||t<=0||(odeme!=null&&odeme.SelectedItem==null)){MessageBox.Show("(*) alanları doldurun!","Eksik",MessageBoxButton.OK,MessageBoxImage.Warning);return false;}return true;}
    private void FormTemizle(DatePicker? tarih,TextBox? tutar,TextBox? belge,TextBox? aciklama){if(tarih!=null)tarih.SelectedDate=DateTime.Today;if(tutar!=null)tutar.Text="0";if(belge!=null)belge.Text=string.Empty;if(aciklama!=null)aciklama.Text=string.Empty;}
    private static StackPanel FS(string lbl,UIElement ctrl){var sp=new StackPanel{Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Bottom};sp.Children.Add(new TextBlock{Text=lbl,FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=new SolidColorBrush(Color.FromRgb(0x5A,0x64,0x80)),Margin=new Thickness(0,0,0,3)});sp.Children.Add(ctrl);return sp;}
    private static Button SBtn(string t)=>new(){Content=t,Width=32,Height=32,FontSize=13,Background=new SolidColorBrush(Color.FromRgb(0x18,0x1C,0x27)),Foreground=new SolidColorBrush(Color.FromRgb(0x8B,0x96,0xB0)),BorderThickness=new Thickness(0),Margin=new Thickness(2,0,2,0),Cursor=System.Windows.Input.Cursors.Hand};
}
