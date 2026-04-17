from PyQt5.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QTabWidget,
    QLabel, QTableWidget, QTableWidgetItem, QHeaderView,
    QPushButton, QComboBox, QLineEdit, QDateEdit, QTextEdit,
    QMessageBox, QFileDialog, QFrame, QGridLayout, QSizePolicy,
    QScrollArea, QStatusBar, QDoubleSpinBox, QSplitter, QApplication,
    QDialog, QDialogButtonBox
)
from PyQt5.QtCore import Qt, QDate, QTimer
from PyQt5.QtGui import QFont, QColor
from datetime import datetime, date
import sys, os

sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))
from app.ui.widgets import (para_format, tarih_format, kpi_kart, form_kart,
                             etiket_input, combo, tarih_input,
                             tutar_input, btn, ayrac)
from app.ui.style import STYLESHEET
import app.db.database as db


class AnaPencere(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle('🏢 Firma Muhasebe ve Kasa Takip Sistemi')
        self.setStyleSheet(STYLESHEET)

        # Ekran boyutuna göre başlangıç boyutu
        ekran = QApplication.primaryScreen().availableGeometry()
        self._ekran_w = ekran.width()
        self._ekran_h = ekran.height()

        # Minimum boyut ekrana göre
        min_w = min(900,  int(self._ekran_w * 0.6))
        min_h = min(600,  int(self._ekran_h * 0.6))
        self.setMinimumSize(min_w, min_h)

        # Başlangıç: ekranın %85'i, ortada
        baslangic_w = int(self._ekran_w * 0.85)
        baslangic_h = int(self._ekran_h * 0.85)
        self.resize(baslangic_w, baslangic_h)
        self.move(
            (self._ekran_w - baslangic_w) // 2,
            (self._ekran_h - baslangic_h) // 2
        )

        # Durum çubuğu
        self.status = QStatusBar()
        self.setStatusBar(self.status)
        self.status.showMessage('Sistem hazır.')

        # Merkez widget
        merkez = QWidget(); self.setCentralWidget(merkez)
        ana = QVBoxLayout(merkez); ana.setContentsMargins(0,0,0,0); ana.setSpacing(0)

        # Üst bar
        ana.addWidget(self._ust_bar())

        # KPI bar
        self.kpi_bar = self._kpi_bar()
        ana.addWidget(self.kpi_bar)

        # Tab widget
        self.tabs = QTabWidget(); self.tabs.setDocumentMode(True)
        ana.addWidget(self.tabs)

        # Tab'ları ekle
        self.tabs.addTab(self._tab_anasayfa(),    '🏠  Ana Sayfa')
        self.tabs.addTab(self._tab_gider(),       '➕  Gider Ekle')
        self.tabs.addTab(self._tab_gelir(),       '💰  Gelir Ekle')
        self.tabs.addTab(self._tab_kasa(),        '🏦  Kasa Girişi')
        self.tabs.addTab(self._tab_kalemler(),    '📋  Kalem Tanımları')
        self.tabs.addTab(self._tab_hareketler(),  '🗂  Tüm Hareketler')
        self.tabs.addTab(self._tab_tablo(),       '📊  Gelir/Gider Tablosu')
        self.tabs.addTab(self._tab_raporlar(),    '📈  Raporlar')

        self.tabs.currentChanged.connect(self._tab_degisti)
        self._h_liste = []   # sayfalama için liste cache
        self._h_sayfa = 0    # mevcut sayfa
        self.kpi_guncelle()
        self.son_hareketler_yukle()

    def resizeEvent(self, event):
        """Pencere boyutu değişince font ve padding'i adapte et"""
        super().resizeEvent(event)
        w = self.width()
        self._kpi_yeniden_duz(w)
        if w < 1100:
            self._adaptif_stil('kompakt')
        elif w < 1500:
            self._adaptif_stil('normal')
        else:
            self._adaptif_stil('genis')

    def _adaptif_stil(self, mod):
        if getattr(self, '_son_mod', None) == mod:
            return  # değişmediyse uygulama
        self._son_mod = mod

        if mod == 'kompakt':
            font_beden   = '12px'
            kpi_beden    = '16px'
            tab_padding  = '7px 12px'
            kenar        = '12px'
        elif mod == 'normal':
            font_beden   = '13px'
            kpi_beden    = '20px'
            tab_padding  = '10px 18px'
            kenar        = '20px'
        else:  # genis
            font_beden   = '14px'
            kpi_beden    = '24px'
            tab_padding  = '12px 24px'
            kenar        = '28px'

        ek_stil = f"""
        QMainWindow, QWidget {{ font-size: {font_beden}; }}
        QLabel#kpiDeger {{ font-size: {kpi_beden}; }}
        QTabBar::tab {{ padding: {tab_padding}; }}
        """
        self.setStyleSheet(STYLESHEET + ek_stil)

    # ─────────────────────────── ÜST BAR ───────────────────────────────
    def _ust_bar(self):
        bar = QFrame(); bar.setFixedHeight(62)
        bar.setStyleSheet('background:#181c27; border-bottom:1px solid #2a3050;')
        lay = QHBoxLayout(bar); lay.setContentsMargins(24, 0, 24, 0); lay.setSpacing(14)

        marka = QLabel('🏢  MUHASEBE SİSTEMİ')
        marka.setStyleSheet('font-size:18px; font-weight:700; color:#5b9aff; letter-spacing:1px;')
        lay.addWidget(marka)
        lay.addStretch()

        for txt, slot, oid in [
            ('💾  Yedekle',    self.yedekle,    'btnTehlike'),
            ('📂  Yedek Yükle', self.yedek_yukle, 'btnTehlike'),
        ]:
            b = QPushButton(txt); b.setObjectName(oid)
            b.setStyleSheet('font-size:14px; padding:8px 18px;')
            b.clicked.connect(slot)
            lay.addWidget(b)
        return bar

    # ─────────────────────────── KPI BAR ───────────────────────────────
    def _kpi_bar(self):
        frame = QFrame()
        frame.setStyleSheet('background:#0f1117; padding:12px 20px 0px 20px;')
        grid = QGridLayout(frame); grid.setSpacing(10); grid.setContentsMargins(0,0,0,10)

        # Her kart eşit genişlik alsın
        for col in range(5):
            grid.setColumnStretch(col, 1)

        self._kpi_kasa,  self._kpi_kasa_v  = kpi_kart('Güncel Kasa',  '₺0',     '#5b9aff')
        self._kpi_gelir, self._kpi_gelir_v = kpi_kart('Bu Ay Gelir',  '₺0',     '#2eca8b')
        self._kpi_gider, self._kpi_gider_v = kpi_kart('Bu Ay Gider',  '₺0',     '#ff4d6d')
        self._kpi_net,   self._kpi_net_v   = kpi_kart('Net Durum',    '₺0',     '#e8ecf4')
        self._kpi_7gun,  self._kpi_7gun_v  = kpi_kart('Son 7 Gün',    '0 işlem','#e8ecf4')

        for i, w in enumerate([self._kpi_kasa, self._kpi_gelir,
                                self._kpi_gider, self._kpi_net, self._kpi_7gun]):
            grid.addWidget(w, 0, i)

        # Pencere küçüldüğünde KPI'ları 2 satıra al
        self._kpi_grid = grid
        return frame

    def _kpi_yeniden_duz(self, genislik):
        """Dar ekranda KPI'ları 2 satıra, geniş ekranda 1 satıra diz"""
        kartlar = [self._kpi_kasa, self._kpi_gelir, self._kpi_gider,
                   self._kpi_net, self._kpi_7gun]
        g = self._kpi_grid
        for k in kartlar:
            g.removeWidget(k)

        if genislik < 1000:
            # 3 + 2 düzeni
            pozlar = [(0,0),(0,1),(0,2),(1,0),(1,1)]
            for col in range(3): g.setColumnStretch(col, 1)
        else:
            # 5'li tek satır
            pozlar = [(0,0),(0,1),(0,2),(0,3),(0,4)]
            for col in range(5): g.setColumnStretch(col, 1)

        for k, (r, c) in zip(kartlar, pozlar):
            g.addWidget(k, r, c)

    def kpi_guncelle(self):
        ozet = db.genel_ozet()
        buAy = datetime.now().strftime('%Y-%m')
        hareketler = db.hareket_listesi()

        buAyGelir = sum(h['giris'] for h in hareketler if h['tarih'][:7]==buAy)
        buAyGider = sum(h['cikis'] for h in hareketler if h['tarih'][:7]==buAy)
        simdi = datetime.now()
        son7 = sum(1 for h in hareketler
                   if (simdi - datetime.strptime(h['tarih'], '%Y-%m-%d')).days <= 7)

        self._kpi_kasa_v.setText(para_format(ozet['bakiye']))
        self._kpi_gelir_v.setText(para_format(buAyGelir))
        self._kpi_gider_v.setText(para_format(buAyGider))
        net = buAyGelir - buAyGider
        self._kpi_net_v.setText(para_format(net))
        self._kpi_net_v.setStyleSheet(f"color: {'#2eca8b' if net>=0 else '#ff4d6d'}; font-size:22px; font-weight:700;")
        self._kpi_7gun_v.setText(f"{son7} işlem")

    # ─────────────────────────── ANA SAYFA ─────────────────────────────
    def _tab_anasayfa(self):
        w = QWidget(); lay = QVBoxLayout(w); lay.setContentsMargins(20,16,20,20); lay.setSpacing(14)

        # Hızlı erişim butonları
        hizli_baslik = QLabel('Hızlı Erişim'); hizli_baslik.setObjectName('baslik')
        lay.addWidget(hizli_baslik)

        btn_grid = QGridLayout(); btn_grid.setSpacing(10)
        kisayollar = [
            ('➕', 'Gider Ekle',           'Yeni gider kaydı',       1),
            ('💰', 'Gelir Ekle',           'Yeni gelir kaydı',       2),
            ('🏦', 'Kasaya Para Girişi',   'Sermaye / tahsilat',     3),
            ('📋', 'Kalem Tanımları',      'Kategori yönetimi',      4),
            ('🗂', 'Tüm Hareketler',       'Kayıtları görüntüle',    5),
            ('📊', 'Gelir/Gider Tablosu', 'Filtreli özet',          6),
            ('📈', 'Raporlar',             'Aylık & kategori özeti', 7),
        ]
        for i, (ikon, ad, aciklama, tab_idx) in enumerate(kisayollar):
            kart = QFrame(); kart.setObjectName('formKart')
            kart.setCursor(Qt.PointingHandCursor)
            kart.setFixedHeight(80)
            klay = QHBoxLayout(kart); klay.setContentsMargins(16, 10, 16, 10); klay.setSpacing(12)

            ikon_lbl = QLabel(ikon)
            ikon_lbl.setStyleSheet('font-size:24px;')
            ikon_lbl.setFixedWidth(36)

            metin = QWidget(); mlay = QVBoxLayout(metin); mlay.setContentsMargins(0,0,0,0); mlay.setSpacing(2)
            ad_lbl = QLabel(ad); ad_lbl.setStyleSheet('font-size:13px; font-weight:700; color:#e8ecf4;')
            ac_lbl = QLabel(aciklama); ac_lbl.setStyleSheet('font-size:11px; color:#5a6480;')
            mlay.addWidget(ad_lbl); mlay.addWidget(ac_lbl)

            klay.addWidget(ikon_lbl); klay.addWidget(metin); klay.addStretch()

            # Hover efekti
            kart.setStyleSheet("""
                QFrame#formKart { background:#181c27; border:1px solid #2a3050; border-radius:10px; }
                QFrame#formKart:hover { border-color:#3d7fff; background:#1e2333; }
            """)
            # Tıklama için eventFilter
            kart.mousePressEvent = lambda e, idx=tab_idx: self.tabs.setCurrentIndex(idx)
            btn_grid.addWidget(kart, i // 4, i % 4)

        lay.addLayout(btn_grid)

        # Son hareketler
        son_baslik = QLabel('Son Hareketler'); son_baslik.setObjectName('baslik')
        lay.addWidget(son_baslik)
        self.tbl_son = self._tablo(['İşlem No','Tarih','Tür','Kategori','Kalem','Tutar','Bakiye','Durum'])
        lay.addWidget(self.tbl_son)
        return w

    def son_hareketler_yukle(self):
        rows = db.hareket_listesi()[:15]
        self._tablo_doldur(self.tbl_son, rows,
            ['islem_no','tarih','tur','ana_kategori','kalem_adi','tutar','bakiye','durum'])

    # ─────────────────────────── GİDER ─────────────────────────────────
    def _tab_gider(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20)
        kart, lay = form_kart('Yeni Gider Kaydı  —  (*) zorunlu alanlar')
        kart.setMaximumWidth(int(self._ekran_w * 0.6))

        self.g_tarih = tarih_input()
        self.g_kat   = combo(); self._kategori_doldur(self.g_kat, 'Gider')
        self.g_kat.currentTextChanged.connect(lambda t: self._alt_doldur(self.g_kat, self.g_alt, 'Gider'))
        self.g_alt   = combo()
        self.g_tutar = tutar_input()
        self.g_odeme = combo()
        for o in ['Nakit','Havale/EFT','Kredi Kartı','Çek','Senet']: self.g_odeme.addItem(o)
        self.g_kime = QComboBox(); self.g_kime.setEditable(True)
        self.g_kime.setInsertPolicy(QComboBox.NoInsert)
        self.g_kime.lineEdit().setPlaceholderText('Ad / Şirket')
        self.g_belge  = QLineEdit(); self.g_belge.setPlaceholderText('FIS-001')
        self.g_aciklama = QTextEdit(); self.g_aciklama.setMaximumHeight(70)
        self.g_aciklama.setPlaceholderText('İsteğe bağlı not...')

        grid = QGridLayout(); grid.setSpacing(14)
        grid.addWidget(etiket_input('Tarih *',          self.g_tarih),  0, 0)
        grid.addWidget(etiket_input('Gider Kategorisi *', self.g_kat),  0, 1)
        grid.addWidget(etiket_input('Alt Kalem *',       self.g_alt),   1, 0)
        grid.addWidget(etiket_input('Tutar *',           self.g_tutar), 1, 1)
        grid.addWidget(etiket_input('Ödeme Türü *',      self.g_odeme), 2, 0)
        grid.addWidget(etiket_input('Kime Ödendi',       self.g_kime),  2, 1)
        grid.addWidget(etiket_input('Belge / Fiş No',    self.g_belge), 3, 0)
        grid.addWidget(etiket_input('Açıklama',      self.g_aciklama),  3, 1)
        lay.addLayout(grid)

        btn_row = QHBoxLayout()
        b_kaydet = btn('✅  Kaydet'); b_kaydet.clicked.connect(self.gider_kaydet)
        b_temiz  = btn('🗑️  Temizle', 'btnTehlike'); b_temiz.clicked.connect(lambda: self._gider_temizle())
        btn_row.addWidget(b_kaydet); btn_row.addWidget(b_temiz); btn_row.addStretch()
        lay.addLayout(btn_row)

        ana.addWidget(kart); ana.addStretch()
        return w

    def gider_kaydet(self):
        tarih = self.g_tarih.date().toString('yyyy-MM-dd')
        kat   = self.g_kat.currentText()
        alt   = self.g_alt.currentText()
        tutar = self.g_tutar.value()
        odeme = self.g_odeme.currentText()
        if kat == '-- Seçiniz --' or alt == '-- Seçiniz --' or tutar == 0 or odeme == '-- Seçiniz --':
            QMessageBox.warning(self, 'Eksik Alan', '(*) işaretli alanları doldurun!'); return
        kalemler = db.alt_kalemler('Gider', kat)
        k = next((x for x in kalemler if x['kalem_adi']==alt), {})
        no = db.hareket_ekle({
            'tarih': tarih, 'tur': 'Gider',
            'ana': kat, 'alt': k.get('alt_kategori',''), 'kalem': alt,
            'aciklama': self.g_aciklama.toPlainText(),
            'tutar': tutar, 'kimden': self.g_kime.currentText().strip(),
            'odeme': odeme, 'belge': self.g_belge.text()
        })
        self.kpi_guncelle(); self.son_hareketler_yukle()
        QTimer.singleShot(300, self._filtre_kisi_guncelle)
        QTimer.singleShot(300, self._form_kisi_guncelle)
        self.status.showMessage(f'✅ Gider kaydedildi: {no}', 4000)
        self._gider_temizle()

    def _gider_temizle(self):
        self.g_tarih.setDate(QDate.currentDate())
        self.g_kat.setCurrentIndex(0); self.g_alt.clear(); self.g_alt.addItem('-- Seçiniz --')
        self.g_tutar.setValue(0); self.g_odeme.setCurrentIndex(0)
        self.g_kime.setCurrentIndex(0); self.g_kime.lineEdit().clear()
        self.g_belge.clear(); self.g_aciklama.clear()

    # ─────────────────────────── GELİR ─────────────────────────────────
    def _tab_gelir(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20)
        kart, lay = form_kart('Yeni Gelir Kaydı  —  (*) zorunlu alanlar')
        kart.setMaximumWidth(int(self._ekran_w * 0.6))

        self.gl_tarih   = tarih_input()
        self.gl_kat     = combo(); self._kategori_doldur(self.gl_kat, 'Gelir')
        self.gl_kat.currentTextChanged.connect(lambda t: self._alt_doldur(self.gl_kat, self.gl_alt, 'Gelir'))
        self.gl_alt     = combo()
        self.gl_tutar   = tutar_input()
        self.gl_tahsilat = combo()
        for o in ['Nakit','Havale/EFT','Kredi Kartı','Çek','Senet']: self.gl_tahsilat.addItem(o)
        self.gl_kimden = QComboBox(); self.gl_kimden.setEditable(True)
        self.gl_kimden.setInsertPolicy(QComboBox.NoInsert)
        self.gl_kimden.lineEdit().setPlaceholderText('Ad / Şirket')
        self.gl_belge   = QLineEdit(); self.gl_belge.setPlaceholderText('FAT-001')
        self.gl_aciklama = QTextEdit(); self.gl_aciklama.setMaximumHeight(70)

        grid = QGridLayout(); grid.setSpacing(14)
        grid.addWidget(etiket_input('Tarih *',             self.gl_tarih),    0, 0)
        grid.addWidget(etiket_input('Gelir Kategorisi *',  self.gl_kat),      0, 1)
        grid.addWidget(etiket_input('Alt Kalem *',         self.gl_alt),      1, 0)
        grid.addWidget(etiket_input('Tutar *',             self.gl_tutar),    1, 1)
        grid.addWidget(etiket_input('Tahsilat Türü *',     self.gl_tahsilat), 2, 0)
        grid.addWidget(etiket_input('Kimden Geldi',        self.gl_kimden),   2, 1)
        grid.addWidget(etiket_input('Belge No',            self.gl_belge),    3, 0)
        grid.addWidget(etiket_input('Açıklama',            self.gl_aciklama), 3, 1)
        lay.addLayout(grid)

        btn_row = QHBoxLayout()
        b_kaydet = btn('✅  Kaydet'); b_kaydet.clicked.connect(self.gelir_kaydet)
        b_temiz  = btn('🗑️  Temizle', 'btnTehlike'); b_temiz.clicked.connect(self._gelir_temizle)
        btn_row.addWidget(b_kaydet); btn_row.addWidget(b_temiz); btn_row.addStretch()
        lay.addLayout(btn_row)

        ana.addWidget(kart); ana.addStretch()
        return w

    def gelir_kaydet(self):
        tarih = self.gl_tarih.date().toString('yyyy-MM-dd')
        kat   = self.gl_kat.currentText()
        alt   = self.gl_alt.currentText()
        tutar = self.gl_tutar.value()
        tahsilat = self.gl_tahsilat.currentText()
        if kat == '-- Seçiniz --' or alt == '-- Seçiniz --' or tutar == 0 or tahsilat == '-- Seçiniz --':
            QMessageBox.warning(self, 'Eksik Alan', '(*) işaretli alanları doldurun!'); return
        kalemler = db.alt_kalemler('Gelir', kat)
        k = next((x for x in kalemler if x['kalem_adi']==alt), {})
        no = db.hareket_ekle({
            'tarih': tarih, 'tur': 'Gelir',
            'ana': kat, 'alt': k.get('alt_kategori',''), 'kalem': alt,
            'aciklama': self.gl_aciklama.toPlainText(),
            'tutar': tutar, 'kimden': self.gl_kimden.currentText().strip(),
            'odeme': tahsilat, 'belge': self.gl_belge.text()
        })
        self.kpi_guncelle(); self.son_hareketler_yukle()
        QTimer.singleShot(300, self._filtre_kisi_guncelle)
        QTimer.singleShot(300, self._form_kisi_guncelle)
        self.status.showMessage(f'✅ Gelir kaydedildi: {no}', 4000)
        self._gelir_temizle()

    def _gelir_temizle(self):
        self.gl_tarih.setDate(QDate.currentDate())
        self.gl_kat.setCurrentIndex(0); self.gl_alt.clear(); self.gl_alt.addItem('-- Seçiniz --')
        self.gl_tutar.setValue(0); self.gl_tahsilat.setCurrentIndex(0)
        self.gl_kimden.setCurrentIndex(0); self.gl_kimden.lineEdit().clear()
        self.gl_belge.clear(); self.gl_aciklama.clear()

    # ─────────────────────────── KASA ──────────────────────────────────
    def _tab_kasa(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20)
        kart, lay = form_kart('Kasaya Para Girişi  —  Sermaye, tahsilat, dış kaynak')
        kart.setMaximumWidth(int(self._ekran_w * 0.6))

        self.k_tarih  = tarih_input()
        self.k_tur    = combo(); self._kategori_doldur(self.k_tur, 'Kasa Giriş')
        self.k_tur.currentTextChanged.connect(lambda t: self._alt_doldur(self.k_tur, self.k_alt, 'Kasa Giriş'))
        self.k_alt    = combo()
        self.k_tutar  = tutar_input()
        self.k_kimden = QComboBox(); self.k_kimden.setEditable(True)
        self.k_kimden.setInsertPolicy(QComboBox.NoInsert)
        self.k_kimden.lineEdit().setPlaceholderText('Ad / Şirket *')
        self.k_belge  = QLineEdit(); self.k_belge.setPlaceholderText('SER-001')
        self.k_aciklama = QTextEdit(); self.k_aciklama.setMaximumHeight(70)

        grid = QGridLayout(); grid.setSpacing(14)
        grid.addWidget(etiket_input('Tarih *',          self.k_tarih),   0, 0)
        grid.addWidget(etiket_input('Kasa Giriş Türü *', self.k_tur),    0, 1)
        grid.addWidget(etiket_input('Kalem *',          self.k_alt),     1, 0)
        grid.addWidget(etiket_input('Tutar *',          self.k_tutar),   1, 1)
        grid.addWidget(etiket_input('Kimden Geldi *',   self.k_kimden),  2, 0)
        grid.addWidget(etiket_input('Belge / Ref No',   self.k_belge),   2, 1)
        grid.addWidget(etiket_input('Açıklama',         self.k_aciklama),3, 0, 1, 2)
        lay.addLayout(grid)

        btn_row = QHBoxLayout()
        b_kaydet = btn('✅  Kaydet'); b_kaydet.clicked.connect(self.kasa_kaydet)
        b_temiz  = btn('🗑️  Temizle', 'btnTehlike'); b_temiz.clicked.connect(self._kasa_temizle)
        btn_row.addWidget(b_kaydet); btn_row.addWidget(b_temiz); btn_row.addStretch()
        lay.addLayout(btn_row)

        ana.addWidget(kart); ana.addStretch()
        return w

    def kasa_kaydet(self):
        tarih  = self.k_tarih.date().toString('yyyy-MM-dd')
        tur    = self.k_tur.currentText()
        alt    = self.k_alt.currentText()
        tutar  = self.k_tutar.value()
        kimden = self.k_kimden.currentText().strip()
        if tur == '-- Seçiniz --' or alt == '-- Seçiniz --' or tutar == 0 or not kimden:
            QMessageBox.warning(self, 'Eksik Alan', '(*) işaretli alanları doldurun!'); return
        kalemler = db.alt_kalemler('Kasa Giriş', tur)
        k = next((x for x in kalemler if x['kalem_adi']==alt), {})
        no = db.hareket_ekle({
            'tarih': tarih, 'tur': 'Kasa Giriş',
            'ana': tur, 'alt': k.get('alt_kategori',''), 'kalem': alt,
            'aciklama': self.k_aciklama.toPlainText(),
            'tutar': tutar, 'kimden': kimden,
            'odeme': 'Nakit', 'belge': self.k_belge.text()
        })
        self.kpi_guncelle(); self.son_hareketler_yukle()
        QTimer.singleShot(300, self._filtre_kisi_guncelle)
        QTimer.singleShot(300, self._form_kisi_guncelle)
        self.status.showMessage(f'✅ Kasa girişi kaydedildi: {no}', 4000)
        self._kasa_temizle()

    def _kasa_temizle(self):
        self.k_tarih.setDate(QDate.currentDate())
        self.k_tur.setCurrentIndex(0); self.k_alt.clear(); self.k_alt.addItem('-- Seçiniz --')
        self.k_tutar.setValue(0)
        self.k_kimden.setCurrentIndex(0); self.k_kimden.lineEdit().clear()
        self.k_belge.clear(); self.k_aciklama.clear()

    # ─────────────────────────── KALEM TANIMLARI ───────────────────────
    def _tab_kalemler(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20); ana.setSpacing(14)
        kart, lay = form_kart('Yeni Kalem Ekle')
        kart.setMaximumWidth(int(self._ekran_w * 0.6))

        self.kt_tur = combo()
        for t in ['Gider','Gelir','Kasa Giriş']: self.kt_tur.addItem(t)
        self.kt_ana = QLineEdit(); self.kt_ana.setPlaceholderText('Örn: Personel, Satış...')
        self.kt_alt = QLineEdit(); self.kt_alt.setPlaceholderText('Örn: Maaş, Nakit Satış...')
        self.kt_ad  = QLineEdit(); self.kt_ad.setPlaceholderText('Örn: Personel Maaşı')
        self.kt_aciklama = QLineEdit(); self.kt_aciklama.setPlaceholderText('Kısa açıklama (isteğe bağlı)')

        grid = QGridLayout(); grid.setSpacing(14)
        grid.addWidget(etiket_input('Kalem Türü *',   self.kt_tur),      0, 0)
        grid.addWidget(etiket_input('Ana Kategori *', self.kt_ana),      0, 1)
        grid.addWidget(etiket_input('Alt Kategori *', self.kt_alt),      1, 0)
        grid.addWidget(etiket_input('Kalem Adı *',    self.kt_ad),       1, 1)
        grid.addWidget(etiket_input('Açıklama',       self.kt_aciklama), 2, 0, 1, 2)
        lay.addLayout(grid)

        btn_row = QHBoxLayout()
        b_kaydet = btn('✅  Kaydet'); b_kaydet.clicked.connect(self.kalem_kaydet)
        b_temiz  = btn('🗑️  Temizle', 'btnTehlike'); b_temiz.clicked.connect(self._kalem_temizle)
        btn_row.addWidget(b_kaydet); btn_row.addWidget(b_temiz); btn_row.addStretch()
        lay.addLayout(btn_row)

        ana.addWidget(kart)

        baslik = QLabel('Tanımlı Kalemler'); baslik.setObjectName('baslik')
        ana.addWidget(baslik)
        self.tbl_kalemler = self._tablo(['ID','Tür','Ana Kategori','Alt Kategori','Kalem Adı','Açıklama','Eklenme',''])
        self.tbl_kalemler.horizontalHeader().setSectionResizeMode(4, QHeaderView.Stretch)
        ana.addWidget(self.tbl_kalemler)
        return w

    def kalem_kaydet(self):
        tur = self.kt_tur.currentText()
        ana = self.kt_ana.text().strip()
        alt = self.kt_alt.text().strip()
        ad  = self.kt_ad.text().strip()
        if tur == '-- Seçiniz --' or not ana or not alt or not ad:
            QMessageBox.warning(self, 'Eksik Alan', '(*) işaretli alanları doldurun!'); return
        db.kalem_ekle(tur, ana, alt, ad, self.kt_aciklama.text().strip())
        self._h_filtre_hazir = False
        self._kategori_yenile()
        self.filtre_dropdownlari_yenile()
        self.kalemler_yukle()
        self._kalem_temizle()
        self.status.showMessage(f'✅ Kalem eklendi: {ad}', 3000)

    def _kalem_temizle(self):
        self.kt_tur.setCurrentIndex(0); self.kt_ana.clear()
        self.kt_alt.clear(); self.kt_ad.clear(); self.kt_aciklama.clear()

    def kalemler_yukle(self):
        rows = db.kalem_listesi()
        tbl = self.tbl_kalemler
        tbl.setRowCount(0)
        renk_map = {'Gider':'#ff4d6d','Gelir':'#2eca8b','Kasa Giriş':'#5b9aff'}
        for r in rows:
            row = tbl.rowCount(); tbl.insertRow(row)
            vals = [str(r['id']), r['tur'], r['ana_kategori'], r['alt_kategori'],
                    r['kalem_adi'], r.get('aciklama',''), r.get('eklenme_tarihi','')]
            for c, v in enumerate(vals):
                item = QTableWidgetItem(v); item.setFlags(Qt.ItemIsEnabled)
                if c == 1:
                    item.setForeground(QColor(renk_map.get(v,'#e8ecf4')))
                tbl.setItem(row, c, item)
            b_sil = QPushButton('🗑️'); b_sil.setObjectName('btnSil')
            kid = r['id']
            b_sil.clicked.connect(lambda _, i=kid: self._kalem_sil(i))
            tbl.setCellWidget(row, 7, b_sil)

    def _kalem_sil(self, kid):
        cevap = QMessageBox.question(self, 'Onay', 'Bu kalemi silmek istediğinize emin misiniz?')
        if cevap == QMessageBox.Yes:
            db.kalem_sil(kid)
            self.kalemler_yukle()
            self._kategori_yenile()
            self.filtre_dropdownlari_yenile()
            self.status.showMessage('Kalem silindi.', 3000)

    # ─────────────────────────── TÜM HAREKETLER ────────────────────────
    def _tab_hareketler(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20); ana.setSpacing(12)

        # Filtre kartı — 2 satır grid
        filtre = QFrame(); filtre.setObjectName('formKart')
        fl = QVBoxLayout(filtre); fl.setContentsMargins(16,14,16,14); fl.setSpacing(10)

        # 1. satır: Tarih + Tür + Ana Kategori
        satir1 = QHBoxLayout(); satir1.setSpacing(12)
        self.f_bas = tarih_input(); self.f_bas.setDate(QDate(QDate.currentDate().year(),1,1))
        self.f_bit = tarih_input()
        self.f_tur = QComboBox()
        for t in ['Tümü','Gelir','Gider','Kasa Giriş']: self.f_tur.addItem(t)
        self.f_tur.currentTextChanged.connect(self._filtre_kategori_guncelle)

        self.f_ana_kat = QComboBox(); self.f_ana_kat.addItem('Tüm Kategoriler')
        self.f_ana_kat.currentTextChanged.connect(self._filtre_kalem_guncelle)

        self.f_kalem = QComboBox(); self.f_kalem.addItem('Tüm Kalemler')

        satir1.addWidget(etiket_input('Başlangıç Tarihi', self.f_bas))
        satir1.addWidget(etiket_input('Bitiş Tarihi',     self.f_bit))
        satir1.addWidget(etiket_input('İşlem Türü',       self.f_tur))
        satir1.addWidget(etiket_input('Ana Kategori',     self.f_ana_kat))
        satir1.addWidget(etiket_input('Kalem',            self.f_kalem))

        # 2. satır: Kişi + Ödeme türü + Tutar aralığı + Genel arama
        satir2 = QHBoxLayout(); satir2.setSpacing(12)

        # Kişi/şirket — yazarak da arama yapılabilsin, mevcut değerler listede çıksın
        self.f_kisi = QComboBox()
        self.f_kisi.setEditable(True)
        self.f_kisi.setInsertPolicy(QComboBox.NoInsert)
        self.f_kisi.lineEdit().setPlaceholderText('Kimden / Kime...')
        self.f_kisi.lineEdit().returnPressed.connect(self.hareketler_filtrele)

        self.f_odeme  = QComboBox()
        for o in ['Tüm Ödeme Türleri','Nakit','Havale/EFT','Kredi Kartı','Çek','Senet']:
            self.f_odeme.addItem(o)
        self.f_tutar_min = QDoubleSpinBox(); self.f_tutar_min.setRange(0,999999999)
        self.f_tutar_min.setPrefix('₺ '); self.f_tutar_min.setDecimals(0)
        self.f_tutar_max = QDoubleSpinBox(); self.f_tutar_max.setRange(0,999999999)
        self.f_tutar_max.setPrefix('₺ '); self.f_tutar_max.setDecimals(0)
        self.f_ara = QLineEdit(); self.f_ara.setPlaceholderText('Belge no, açıklama, işlem no...')
        self.f_ara.returnPressed.connect(self.hareketler_filtrele)

        satir2.addWidget(etiket_input('Kişi / Firma',    self.f_kisi))
        satir2.addWidget(etiket_input('Ödeme Türü',      self.f_odeme))
        satir2.addWidget(etiket_input('Min Tutar',        self.f_tutar_min))
        satir2.addWidget(etiket_input('Max Tutar',        self.f_tutar_max))
        satir2.addWidget(etiket_input('Genel Arama',     self.f_ara))

        # Butonlar
        btn_row = QHBoxLayout(); btn_row.setSpacing(8)
        b_filtre = btn('🔍  Filtrele');         b_filtre.clicked.connect(self.hareketler_filtrele)
        b_temiz  = btn('🔄  Temizle','btnTehlike'); b_temiz.clicked.connect(self._hareketler_temizle)
        b_excel  = btn('📥  CSV Aktar','btnYesil'); b_excel.clicked.connect(self.csv_aktar)
        btn_row.addWidget(b_filtre); btn_row.addWidget(b_temiz); btn_row.addWidget(b_excel)
        btn_row.addStretch()

        fl.addLayout(satir1)
        fl.addLayout(satir2)
        fl.addLayout(btn_row)
        ana.addWidget(filtre)

        self.h_sayisi = QLabel('Tüm Kayıtlar'); self.h_sayisi.setObjectName('baslik')
        ana.addWidget(self.h_sayisi)

        sutunlar = ['İşlem No','Tarih','Tür','Kategori','Kalem','Açıklama',
                    'Tutar','Giriş','Çıkış','Kimden/Kime','Ödeme','Belge','Bakiye','','']
        self.tbl_hareketler = self._tablo(sutunlar)
        self.tbl_hareketler.horizontalHeader().setSectionResizeMode(5, QHeaderView.Stretch)
        ana.addWidget(self.tbl_hareketler)

        # Sayfalama çubuğu
        self._h_sayfa = 0          # mevcut sayfa (0-indexed)
        self._h_liste = []         # tüm filtrelenmiş liste
        self.SAYFA_BOYUTU = 100    # sayfa başı kayıt

        sayfa_bar = QFrame(); sayfa_bar.setObjectName('formKart')
        sayfa_bar.setFixedHeight(48)
        sb_lay = QHBoxLayout(sayfa_bar); sb_lay.setContentsMargins(12,0,12,0); sb_lay.setSpacing(8)

        self.btn_ilk   = QPushButton('⏮'); self.btn_ilk.setFixedWidth(36)
        self.btn_geri  = QPushButton('◀'); self.btn_geri.setFixedWidth(36)
        self.lbl_sayfa = QLabel('—'); self.lbl_sayfa.setAlignment(Qt.AlignCenter)
        self.lbl_sayfa.setStyleSheet('color:#8b96b0; font-size:13px; min-width:160px;')
        self.btn_ileri = QPushButton('▶'); self.btn_ileri.setFixedWidth(36)
        self.btn_son   = QPushButton('⏭'); self.btn_son.setFixedWidth(36)

        for b in [self.btn_ilk, self.btn_geri, self.btn_ileri, self.btn_son]:
            b.setObjectName('btnTehlike')
            b.setStyleSheet('font-size:14px; padding:4px;')

        self.btn_ilk.clicked.connect(lambda: self._sayfa_git(0))
        self.btn_geri.clicked.connect(lambda: self._sayfa_git(self._h_sayfa - 1))
        self.btn_ileri.clicked.connect(lambda: self._sayfa_git(self._h_sayfa + 1))
        self.btn_son.clicked.connect(lambda: self._sayfa_git(self._h_sayfa_toplam() - 1))

        sb_lay.addStretch()
        sb_lay.addWidget(self.btn_ilk)
        sb_lay.addWidget(self.btn_geri)
        sb_lay.addWidget(self.lbl_sayfa)
        sb_lay.addWidget(self.btn_ileri)
        sb_lay.addWidget(self.btn_son)
        sb_lay.addStretch()
        ana.addWidget(sayfa_bar)
        return w

    def _filtre_kategori_guncelle(self, tur_text):
        """İşlem türü değişince ana kategori dropdown'ını kalem tanımlarından doldur"""
        onceki = self.f_ana_kat.currentText()
        self.f_ana_kat.blockSignals(True)
        self.f_ana_kat.clear()
        self.f_ana_kat.addItem('Tüm Kategoriler')

        tur = None if tur_text == 'Tümü' else tur_text
        kalemler = db.kalem_listesi()
        kategoriler = sorted(set(
            k['ana_kategori'] for k in kalemler
            if (tur is None or k['tur'] == tur)
        ))
        for k in kategoriler:
            self.f_ana_kat.addItem(k)

        # Önceki seçimi koru (hâlâ listede varsa)
        idx = self.f_ana_kat.findText(onceki)
        self.f_ana_kat.setCurrentIndex(idx if idx >= 0 else 0)
        self.f_ana_kat.blockSignals(False)
        self._filtre_kalem_guncelle(self.f_ana_kat.currentText())

    def _filtre_kalem_guncelle(self, ana_text):
        """Ana kategori değişince kalem dropdown'ını kalem tanımlarından doldur"""
        onceki = self.f_kalem.currentText()
        self.f_kalem.blockSignals(True)
        self.f_kalem.clear()
        self.f_kalem.addItem('Tüm Kalemler')

        tur_text = self.f_tur.currentText()
        tur = None if tur_text == 'Tümü' else tur_text
        ana = None if ana_text == 'Tüm Kategoriler' else ana_text

        kalemler = db.kalem_listesi()
        filtreli = sorted(set(
            k['kalem_adi'] for k in kalemler
            if (tur is None or k['tur'] == tur)
            and (ana is None or k['ana_kategori'] == ana)
        ))
        for k in filtreli:
            self.f_kalem.addItem(k)

        # Önceki seçimi koru
        idx = self.f_kalem.findText(onceki)
        self.f_kalem.setCurrentIndex(idx if idx >= 0 else 0)
        self.f_kalem.blockSignals(False)

    def filtre_dropdownlari_yenile(self):
        """Kalem ekleme/silme ve sekme geçişinde tüm filtre dropdown'larını yenile"""
        self._filtre_kategori_guncelle(self.f_tur.currentText())
        self._filtre_kisi_guncelle()

    def _filtre_kisi_guncelle(self):
        """Hareketlerdeki mevcut kişi/şirket listesini güncelle, seçimi koru"""
        onceki = self.f_kisi.currentText().strip()
        self.f_kisi.blockSignals(True)
        self.f_kisi.clear()
        self.f_kisi.addItem('')   # boş = tümü
        for kisi in db.kisi_listesi():
            self.f_kisi.addItem(kisi)
        # Önceki yazıyı koru
        if onceki:
            idx = self.f_kisi.findText(onceki)
            if idx >= 0:
                self.f_kisi.setCurrentIndex(idx)
            else:
                self.f_kisi.setCurrentText(onceki)
        self.f_kisi.blockSignals(False)

    def _h_sayfa_toplam(self):
        if not self._h_liste: return 1
        import math
        return math.ceil(len(self._h_liste) / self.SAYFA_BOYUTU)

    def _sayfa_goster(self, liste, sayfa=0):
        """Listeyi sayfalar halinde tabloya yükler"""
        self._h_liste = liste
        self._h_sayfa = max(0, min(sayfa, self._h_sayfa_toplam() - 1))
        bas = self._h_sayfa * self.SAYFA_BOYUTU
        bit = bas + self.SAYFA_BOYUTU
        self.hareketler_yukle(liste[bas:bit])

        toplam = len(liste)
        toplam_sayfa = self._h_sayfa_toplam()
        gosterilen_bas = bas + 1 if toplam else 0
        gosterilen_bit = min(bit, toplam)

        self.h_sayisi.setText(
            f'{toplam} kayıt  —  {gosterilen_bas}-{gosterilen_bit} arası gösteriliyor'
        )
        self.lbl_sayfa.setText(f'Sayfa  {self._h_sayfa + 1}  /  {toplam_sayfa}')
        self.btn_ilk.setEnabled(self._h_sayfa > 0)
        self.btn_geri.setEnabled(self._h_sayfa > 0)
        self.btn_ileri.setEnabled(self._h_sayfa < toplam_sayfa - 1)
        self.btn_son.setEnabled(self._h_sayfa < toplam_sayfa - 1)

    def _sayfa_git(self, sayfa):
        self._sayfa_goster(self._h_liste, sayfa)

    def hareketler_yukle(self, liste=None):
        if liste is None:
            liste = db.hareket_listesi()
        tbl = self.tbl_hareketler
        self.h_sayisi.setText(f'{len(liste)} Kayıt')

        tbl.setUpdatesEnabled(False)
        tbl.setSortingEnabled(False)
        tbl.clearContents()
        tbl.setRowCount(len(liste))

        renk = {'Gelir':QColor('#2eca8b'),'Gider':QColor('#ff4d6d'),'Kasa Giriş':QColor('#5b9aff')}
        sag  = Qt.AlignRight | Qt.AlignVCenter

        for row, h in enumerate(liste):
            vals = [
                h['islem_no'], tarih_format(h['tarih']), h['tur'], h['ana_kategori'],
                h['kalem_adi'], h.get('aciklama',''),
                para_format(h['tutar']),
                para_format(h['giris']) if h['giris'] else '-',
                para_format(h['cikis']) if h['cikis'] else '-',
                h.get('kimden_kime',''), h.get('odeme_turu',''),
                h.get('belge_no',''), para_format(h['bakiye']), h.get('durum','✅')
            ]
            for c, v in enumerate(vals):
                item = QTableWidgetItem(str(v))
                item.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)
                if c == 2 and v in renk:
                    item.setForeground(renk[v])
                if c in (6, 7, 8, 12):
                    item.setTextAlignment(sag)
                # ID'yi gizli veri olarak sakla
                if c == 0:
                    item.setData(Qt.UserRole, h['id'])
                tbl.setItem(row, c, item)

            # Düzenle butonu
            b_duzenle = QPushButton('✏️'); b_duzenle.setObjectName('btnSil')
            b_duzenle.setToolTip('Düzenle')
            hid = h['id']
            b_duzenle.clicked.connect(lambda _, i=hid: self._hareket_duzenle(i))
            tbl.setCellWidget(row, 13, b_duzenle)

            b_sil = QPushButton('🗑️'); b_sil.setObjectName('btnSil')
            b_sil.setToolTip('Sil')
            b_sil.clicked.connect(lambda _, i=hid: self._hareket_sil(i))
            tbl.setCellWidget(row, 14, b_sil)

        tbl.setUpdatesEnabled(True)

        # Çift tıkla düzenle
        try: tbl.doubleClicked.disconnect()
        except: pass
        tbl.doubleClicked.connect(self._tablo_cift_tiklandi)

    def _tablo_cift_tiklandi(self, index):
        item = self.tbl_hareketler.item(index.row(), 0)
        if item:
            hid = item.data(Qt.UserRole)
            if hid:
                self._hareket_duzenle(hid)

    def _hareket_duzenle(self, hid):
        h = db.hareket_getir(hid)
        if not h:
            return

        dialog = QDialog(self)
        dialog.setWindowTitle(f'Hareketi Düzenle — {h["islem_no"]}')
        dialog.setMinimumWidth(560)
        dialog.setStyleSheet(STYLESHEET)

        ana = QVBoxLayout(dialog)
        ana.setContentsMargins(20, 16, 20, 16)
        ana.setSpacing(14)

        baslik = QLabel(f'📝  {h["islem_no"]} — Düzenle')
        baslik.setObjectName('baslik')
        ana.addWidget(baslik)

        grid = QGridLayout(); grid.setSpacing(12)

        # Tarih
        d_tarih = tarih_input()
        try:
            d_tarih.setDate(QDate.fromString(h['tarih'], 'yyyy-MM-dd'))
        except:
            d_tarih.setDate(QDate.currentDate())

        # Tür
        d_tur = QComboBox()
        for t in ['Gider','Gelir','Kasa Giriş']: d_tur.addItem(t)
        d_tur.setCurrentText(h.get('tur','Gider'))

        # Ana kategori
        d_ana = QComboBox(); d_ana.setEditable(True)
        kalemler = db.kalem_listesi()
        analar = sorted(set(k['ana_kategori'] for k in kalemler))
        d_ana.addItems(analar)
        d_ana.setCurrentText(h.get('ana_kategori',''))

        # Kalem
        d_kalem = QComboBox(); d_kalem.setEditable(True)
        kalem_adlari = sorted(set(k['kalem_adi'] for k in kalemler))
        d_kalem.addItems(kalem_adlari)
        d_kalem.setCurrentText(h.get('kalem_adi',''))

        # Tür değişince kategori listesini güncelle
        def tur_degisti(tur_text):
            d_ana.blockSignals(True)
            onceki_ana = d_ana.currentText()
            d_ana.clear()
            filtreli = sorted(set(k['ana_kategori'] for k in kalemler if k['tur']==tur_text))
            d_ana.addItems(filtreli if filtreli else analar)
            d_ana.setCurrentText(onceki_ana)
            d_ana.blockSignals(False)
            ana_degisti(d_ana.currentText())

        def ana_degisti(ana_text):
            tur_text = d_tur.currentText()
            onceki = d_kalem.currentText()
            d_kalem.clear()
            filtreli = sorted(set(
                k['kalem_adi'] for k in kalemler
                if k['tur']==tur_text and k['ana_kategori']==ana_text
            ))
            d_kalem.addItems(filtreli if filtreli else kalem_adlari)
            idx = d_kalem.findText(onceki)
            if idx >= 0: d_kalem.setCurrentIndex(idx)

        d_tur.currentTextChanged.connect(tur_degisti)
        d_ana.currentTextChanged.connect(ana_degisti)
        tur_degisti(d_tur.currentText())

        # Tutar
        d_tutar = tutar_input()
        d_tutar.setValue(float(h.get('tutar', 0)))

        # Ödeme türü
        d_odeme = QComboBox()
        for o in ['Nakit','Havale/EFT','Kredi Kartı','Çek','Senet']:
            d_odeme.addItem(o)
        d_odeme.setCurrentText(h.get('odeme_turu','Nakit'))

        # Kişi
        d_kisi = QComboBox(); d_kisi.setEditable(True)
        d_kisi.addItem('')
        for k in db.kisi_listesi(): d_kisi.addItem(k)
        d_kisi.setCurrentText(h.get('kimden_kime',''))

        # Belge
        d_belge = QLineEdit(h.get('belge_no',''))

        # Açıklama
        d_aciklama = QTextEdit(h.get('aciklama',''))
        d_aciklama.setMaximumHeight(70)

        grid.addWidget(etiket_input('Tarih *',       d_tarih),    0, 0)
        grid.addWidget(etiket_input('İşlem Türü *',  d_tur),      0, 1)
        grid.addWidget(etiket_input('Ana Kategori',  d_ana),      1, 0)
        grid.addWidget(etiket_input('Kalem',         d_kalem),    1, 1)
        grid.addWidget(etiket_input('Tutar *',       d_tutar),    2, 0)
        grid.addWidget(etiket_input('Ödeme Türü',    d_odeme),    2, 1)
        grid.addWidget(etiket_input('Kişi / Firma',  d_kisi),     3, 0)
        grid.addWidget(etiket_input('Belge No',      d_belge),    3, 1)
        grid.addWidget(etiket_input('Açıklama',      d_aciklama), 4, 0, 1, 2)
        ana.addLayout(grid)

        # Butonlar
        bb = QDialogButtonBox()
        b_kaydet = bb.addButton('💾  Kaydet', QDialogButtonBox.AcceptRole)
        b_kaydet.setStyleSheet('background:#3d7fff; color:#fff; font-weight:700; padding:8px 20px;')
        b_iptal  = bb.addButton('İptal',      QDialogButtonBox.RejectRole)
        b_iptal.setObjectName('btnTehlike')
        bb.accepted.connect(dialog.accept)
        bb.rejected.connect(dialog.reject)
        ana.addWidget(bb)

        if dialog.exec_() != QDialog.Accepted:
            return

        kalemler_db = db.kalem_listesi()
        secili_kalem = d_kalem.currentText()
        alt = next((k['alt_kategori'] for k in kalemler_db if k['kalem_adi']==secili_kalem), '')

        db.hareket_guncelle(hid, {
            'tarih':  d_tarih.date().toString('yyyy-MM-dd'),
            'tur':    d_tur.currentText(),
            'ana':    d_ana.currentText(),
            'alt':    alt,
            'kalem':  secili_kalem,
            'tutar':  d_tutar.value(),
            'odeme':  d_odeme.currentText(),
            'kimden': d_kisi.currentText().strip(),
            'belge':  d_belge.text().strip(),
            'aciklama': d_aciklama.toPlainText().strip(),
        })

        self.kpi_guncelle()
        self.son_hareketler_yukle()
        self._hareketler_ilk_yukle()
        self.status.showMessage(f'✅ Hareket güncellendi: {h["islem_no"]}', 4000)

    def hareketler_filtrele(self):
        bas      = self.f_bas.date().toString('yyyy-MM-dd')
        bit      = self.f_bit.date().toString('yyyy-MM-dd')
        tur      = self.f_tur.currentText();      tur      = None if tur      == 'Tümü'              else tur
        ana_kat  = self.f_ana_kat.currentText();  ana_kat  = None if ana_kat  == 'Tüm Kategoriler'   else ana_kat
        kalem    = self.f_kalem.currentText();    kalem    = None if kalem    == 'Tüm Kalemler'       else kalem
        kisi     = self.f_kisi.currentText().strip() or None
        odeme    = self.f_odeme.currentText();    odeme    = None if odeme    == 'Tüm Ödeme Türleri'  else odeme
        ara      = self.f_ara.text().strip()      or None
        t_min    = self.f_tutar_min.value()       or None
        t_max    = self.f_tutar_max.value()       or None

        liste = db.hareket_listesi(
            tarih_bas=bas, tarih_bit=bit, tur=tur,
            ana_kategori=ana_kat, kalem=kalem,
            kimden=kisi, odeme_turu=odeme,
            tutar_min=t_min, tutar_max=t_max, ara=ara
        )
        self._sayfa_goster(liste)

    def _hareketler_temizle(self):
        self.f_bas.setDate(QDate(QDate.currentDate().year(),1,1))
        self.f_bit.setDate(QDate.currentDate())
        self.f_tur.setCurrentIndex(0)
        self.f_ana_kat.setCurrentIndex(0)
        self.f_kalem.setCurrentIndex(0)
        self.f_kisi.setCurrentIndex(0)
        self.f_odeme.setCurrentIndex(0)
        self.f_tutar_min.setValue(0)
        self.f_tutar_max.setValue(0)
        self.f_ara.clear()
        self.hareketler_yukle()

    def _hareket_sil(self, hid):
        cevap = QMessageBox.question(self, 'Onay', 'Bu hareketi silmek istediğinize emin misiniz?')
        if cevap == QMessageBox.Yes:
            db.hareket_sil(hid)
            self.kpi_guncelle(); self.son_hareketler_yukle()
            self._hareketler_ilk_yukle()
            self.status.showMessage('Hareket silindi.', 3000)

    # ─────────────────────────── GELİR/GİDER TABLOSU ───────────────────
    def _tab_tablo(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20); ana.setSpacing(12)

        filtre = QFrame(); filtre.setObjectName('formKart')
        fl = QHBoxLayout(filtre); fl.setContentsMargins(16,12,16,12); fl.setSpacing(12)
        self.t_bas = tarih_input(); self.t_bas.setDate(QDate(QDate.currentDate().year(),1,1))
        self.t_bit = tarih_input()
        self.t_tur = QComboBox()
        for t in ['Tümü','Gelir','Gider','Kasa Giriş']: self.t_tur.addItem(t)
        fl.addWidget(etiket_input('Başlangıç', self.t_bas))
        fl.addWidget(etiket_input('Bitiş',     self.t_bit))
        fl.addWidget(etiket_input('Tür',       self.t_tur))
        b_f = btn('🔍  Filtrele'); b_f.clicked.connect(self.tablo_filtrele)
        b_t = btn('🔄  Temizle', 'btnTehlike'); b_t.clicked.connect(self._tablo_temizle)
        fl.addWidget(b_f); fl.addWidget(b_t); fl.addStretch()
        ana.addWidget(filtre)

        # Özet KPI'lar
        ozet_frame = QFrame()
        ozet_lay = QHBoxLayout(ozet_frame); ozet_lay.setSpacing(10); ozet_lay.setContentsMargins(0,0,0,0)
        _, self.t_gelir_v = kpi_kart('Toplam Gelir', '₺0', '#2eca8b')
        _, self.t_gider_v = kpi_kart('Toplam Gider', '₺0', '#ff4d6d')
        _, self.t_net_v   = kpi_kart('Net Fark',     '₺0', '#e8ecf4')
        _, self.t_sayi_v  = kpi_kart('Hareket',       '0',  '#e8ecf4')
        _, self.t_ort_v   = kpi_kart('Ort. İşlem',   '₺0', '#e8ecf4')
        k1,_ = kpi_kart('Toplam Gelir','₺0','#2eca8b'); k1.layout().itemAt(1).widget().hide()
        for kart, val in [
            (kpi_kart('Toplam Gelir','₺0','#2eca8b')),
        ]: pass

        # Kısayol: direk layout'a ekle
        for label, attr, renk in [
            ('Toplam Gelir','t_gelir_v','#2eca8b'),
            ('Toplam Gider','t_gider_v','#ff4d6d'),
            ('Net Fark',    't_net_v',  '#e8ecf4'),
            ('Hareket',     't_sayi_v', '#e8ecf4'),
            ('Ort. İşlem',  't_ort_v',  '#e8ecf4'),
        ]:
            krt = QFrame(); krt.setObjectName('kpiKart')
            kv = QVBoxLayout(krt); kv.setContentsMargins(14,12,14,12); kv.setSpacing(6)
            lbl = QLabel(label); lbl.setObjectName('kpiLabel')
            val_lbl = QLabel('₺0'); val_lbl.setObjectName('kpiDeger')
            val_lbl.setStyleSheet(f'color:{renk}; font-size:20px; font-weight:700;')
            setattr(self, attr, val_lbl)
            kv.addWidget(lbl); kv.addWidget(val_lbl)
            ozet_lay.addWidget(krt)
        ana.addWidget(ozet_frame)

        sutunlar = ['İşlem No','Tarih','Tür','Kategori','Kalem','Açıklama',
                    'Tutar','Giriş','Çıkış','Kimden/Kime','Ödeme','Belge','Bakiye','Durum']
        self.tbl_tablo = self._tablo(sutunlar)
        self.tbl_tablo.horizontalHeader().setSectionResizeMode(5, QHeaderView.Stretch)
        ana.addWidget(self.tbl_tablo)
        return w

    def tablo_filtrele(self):
        bas = self.t_bas.date().toString('yyyy-MM-dd')
        bit = self.t_bit.date().toString('yyyy-MM-dd')
        tur = self.t_tur.currentText(); tur = None if tur=='Tümü' else tur
        liste = db.hareket_listesi(bas, bit, tur)

        top_gelir = sum(h['giris'] for h in liste)
        top_gider = sum(h['cikis'] for h in liste)
        net = top_gelir - top_gider

        self.t_gelir_v.setText(para_format(top_gelir))
        self.t_gider_v.setText(para_format(top_gider))
        self.t_net_v.setText(para_format(net))
        self.t_net_v.setStyleSheet(f"color:{'#2eca8b' if net>=0 else '#ff4d6d'}; font-size:20px; font-weight:700;")
        self.t_sayi_v.setText(str(len(liste)))
        ort = (top_gelir + top_gider) / len(liste) if liste else 0
        self.t_ort_v.setText(para_format(ort))

        tbl = self.tbl_tablo
        tbl.setUpdatesEnabled(False)
        tbl.clearContents()
        tbl.setRowCount(len(liste))
        renk = {'Gelir':QColor('#2eca8b'),'Gider':QColor('#ff4d6d'),'Kasa Giriş':QColor('#5b9aff')}
        sag = Qt.AlignRight | Qt.AlignVCenter
        for row, h in enumerate(liste):
            vals = [h['islem_no'], tarih_format(h['tarih']), h['tur'], h['ana_kategori'],
                    h['kalem_adi'], h.get('aciklama',''),
                    para_format(h['tutar']),
                    para_format(h['giris']) if h['giris'] else '-',
                    para_format(h['cikis']) if h['cikis'] else '-',
                    h.get('kimden_kime',''), h.get('odeme_turu',''),
                    h.get('belge_no',''), para_format(h['bakiye']), h.get('durum','✅')]
            for c, v in enumerate(vals):
                item = QTableWidgetItem(str(v))
                item.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)
                if c == 2 and v in renk: item.setForeground(renk[v])
                if c in (6,7,8,12): item.setTextAlignment(sag)
                tbl.setItem(row, c, item)
        tbl.setUpdatesEnabled(True)

    def _tablo_temizle(self):
        self.t_bas.setDate(QDate(QDate.currentDate().year(),1,1))
        self.t_bit.setDate(QDate.currentDate())
        self.t_tur.setCurrentIndex(0); self.tablo_filtrele()

    # ─────────────────────────── RAPORLAR ──────────────────────────────
    def _tab_raporlar(self):
        w = QWidget(); ana = QVBoxLayout(w); ana.setContentsMargins(20,16,20,20); ana.setSpacing(14)

        grid = QGridLayout(); grid.setSpacing(14)
        self.rapor_aylik    = self._rapor_kart("📅  Aylık Gelir - Gider Özeti")
        self.rapor_kategori = self._rapor_kart("📂  Kategori Bazlı Dağılım")
        self.rapor_top      = self._rapor_kart("⭐  En Çok Kullanılan Kalemler (Top 5)")
        self.rapor_genel    = self._rapor_kart("📊  Genel Özet")

        grid.addWidget(self.rapor_aylik[0],    0, 0)
        grid.addWidget(self.rapor_kategori[0], 0, 1)
        grid.addWidget(self.rapor_top[0],      1, 0)
        grid.addWidget(self.rapor_genel[0],    1, 1)
        ana.addLayout(grid)
        return w

    def _rapor_kart(self, baslik):
        frame = QFrame(); frame.setObjectName("formKart")
        lay = QVBoxLayout(frame); lay.setContentsMargins(18,16,18,16); lay.setSpacing(4)
        lbl = QLabel(baslik); lbl.setObjectName("formBaslik")
        sep = QFrame(); sep.setFrameShape(QFrame.HLine)
        sep.setStyleSheet("color:#2a3050; margin:4px 0;")
        icerik = QVBoxLayout()
        lay.addWidget(lbl); lay.addWidget(sep); lay.addLayout(icerik)
        return frame, icerik

    def raporlar_yukle(self):
        # Aylık
        _, lay = self.rapor_aylik
        self._lay_temizle(lay)
        for a in db.aylik_ozet():
            net = a['gelir'] - a['gider']
            satir = QHBoxLayout()
            satir.addWidget(QLabel(f"{a['ay']}  ({a['sayi']} işlem)"))
            v = QLabel(para_format(net))
            renk_net = '#2eca8b' if net >= 0 else '#ff4d6d'
            v.setStyleSheet(f'color:{renk_net}; font-weight:700;')
            v.setAlignment(Qt.AlignRight)
            satir.addWidget(v); lay.addLayout(satir)
            alt = QLabel(f"   Gelir: {para_format(a['gelir'])}   |   Gider: {para_format(a['gider'])}")
            alt.setStyleSheet('color:#5a6480; font-size:11px;')
            lay.addWidget(alt)
        if lay.count() == 0: lay.addWidget(QLabel('Veri yok.'))

        # Kategori
        _, lay = self.rapor_kategori
        self._lay_temizle(lay)
        renk = {'Gelir':'#2eca8b','Gider':'#ff4d6d','Kasa Giriş':'#5b9aff'}
        for k in db.kategori_dagilim():
            satir = QHBoxLayout()
            tur_renk = renk.get(k['tur'], '#e8ecf4')
            lbl = QLabel(f"[{k['tur']}]  {k['ana_kategori']}")
            lbl.setStyleSheet(f'color:{tur_renk};')
            v = QLabel(para_format(k['tutar'])); v.setAlignment(Qt.AlignRight)
            v.setStyleSheet('font-weight:700;')
            satir.addWidget(lbl); satir.addWidget(v); lay.addLayout(satir)
        if lay.count() == 0: lay.addWidget(QLabel('Veri yok.'))

        # Top kalemler
        _, lay = self.rapor_top
        self._lay_temizle(lay)
        for i, k in enumerate(db.top_kalemler(5), 1):
            satir = QHBoxLayout()
            satir.addWidget(QLabel(f"{i}. {k['kalem_adi']}  ({k['sayi']}x)"))
            v = QLabel(para_format(k['tutar'])); v.setAlignment(Qt.AlignRight)
            v.setStyleSheet('font-weight:700;')
            satir.addWidget(v); lay.addLayout(satir)
        if lay.count() == 0: lay.addWidget(QLabel('Veri yok.'))

        # Genel özet
        _, lay = self.rapor_genel
        self._lay_temizle(lay)
        ozet = db.genel_ozet()
        for label, val, renk_str in [
            ('Toplam Hareket',  str(ozet['toplam_hareket']),       '#e8ecf4'),
            ('Toplam Gelir',    para_format(ozet['toplam_gelir']),  '#2eca8b'),
            ('Toplam Gider',    para_format(ozet['toplam_gider']),  '#ff4d6d'),
            ('Kasa Girişleri',  para_format(ozet['kasa_giris']),    '#5b9aff'),
            ('Güncel Bakiye',   para_format(ozet['bakiye']),        '#e8ecf4'),
            ('Tanımlı Kalem',   str(ozet['kalem_sayisi']),          '#e8ecf4'),
        ]:
            satir = QHBoxLayout()
            satir.addWidget(QLabel(label))
            v = QLabel(val); v.setAlignment(Qt.AlignRight)
            v.setStyleSheet(f'color:{renk_str}; font-weight:700;')
            satir.addWidget(v); lay.addLayout(satir)

    def _lay_temizle(self, layout):
        while layout.count():
            item = layout.takeAt(0)
            if item.widget(): item.widget().deleteLater()
            elif item.layout(): self._lay_temizle(item.layout())

    # ─────────────────────────── YARDIMCI ──────────────────────────────
    def _tablo(self, sutunlar):
        tbl = QTableWidget(0, len(sutunlar))
        tbl.setHorizontalHeaderLabels(sutunlar)
        tbl.setEditTriggers(QTableWidget.NoEditTriggers)
        tbl.setSelectionBehavior(QTableWidget.SelectRows)
        tbl.setAlternatingRowColors(False)
        tbl.verticalHeader().hide()
        tbl.horizontalHeader().setStretchLastSection(False)
        tbl.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeToContents)
        return tbl

    def _tablo_doldur(self, tbl, rows, alanlar):
        tbl.setUpdatesEnabled(False)
        tbl.clearContents()
        tbl.setRowCount(len(rows))
        renk_map = {'Gelir':QColor('#2eca8b'),'Gider':QColor('#ff4d6d'),'Kasa Giriş':QColor('#5b9aff')}
        para_alanlar = {'tutar','giris','cikis','bakiye'}
        sag = Qt.AlignRight | Qt.AlignVCenter
        for row, h in enumerate(rows):
            for c, alan in enumerate(alanlar):
                v = h.get(alan, '')
                if alan in para_alanlar:
                    v = para_format(v) if v else '-'
                elif alan == 'tarih':
                    v = tarih_format(str(v))
                item = QTableWidgetItem(str(v))
                item.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)
                if alan == 'tur': item.setForeground(renk_map.get(str(v), QColor('#e8ecf4')))
                if alan in para_alanlar: item.setTextAlignment(sag)
                tbl.setItem(row, c, item)
        tbl.setUpdatesEnabled(True)

    def _kategori_doldur(self, combo_widget, tur):
        combo_widget.clear(); combo_widget.addItem('-- Seçiniz --')
        for kat in db.kategoriler(tur): combo_widget.addItem(kat)

    def _alt_doldur(self, kat_combo, alt_combo, tur):
        kat = kat_combo.currentText()
        alt_combo.clear(); alt_combo.addItem('-- Seçiniz --')
        if kat and kat != '-- Seçiniz --':
            for k in db.alt_kalemler(tur, kat): alt_combo.addItem(k['kalem_adi'])

    def _kategori_yenile(self):
        self._kategori_doldur(self.g_kat, 'Gider')
        self._kategori_doldur(self.gl_kat, 'Gelir')
        self._kategori_doldur(self.k_tur, 'Kasa Giriş')

    def _tab_degisti(self, idx):
        self.kpi_guncelle()
        if idx == 0:
            self.son_hareketler_yukle()
        elif idx in (1, 2, 3):
            QTimer.singleShot(0, self._form_kisi_guncelle)
        elif idx == 4:
            self.kalemler_yukle()
        elif idx == 5:
            self.filtre_dropdownlari_yenile()
            QTimer.singleShot(0, self._hareketler_ilk_yukle)
        elif idx == 6:
            QTimer.singleShot(0, self.tablo_filtrele)
        elif idx == 7:
            QTimer.singleShot(0, self.raporlar_yukle)

    def _hareketler_ilk_yukle(self):
        liste = db.hareket_listesi()
        self._sayfa_goster(liste)

    def _form_kisi_guncelle(self):
        """Gider/Gelir/Kasa formlarındaki kişi combo'larını mevcut hareket listesinden besle"""
        kisiler = db.kisi_listesi()
        for combo_w in (self.g_kime, self.gl_kimden, self.k_kimden):
            onceki = combo_w.currentText().strip()
            combo_w.blockSignals(True)
            combo_w.clear()
            combo_w.addItem('')
            for k in kisiler:
                combo_w.addItem(k)
            if onceki:
                idx = combo_w.findText(onceki)
                if idx >= 0:
                    combo_w.setCurrentIndex(idx)
                else:
                    combo_w.setCurrentText(onceki)
            combo_w.blockSignals(False)

    # ─────────────────────────── YEDEK ─────────────────────────────────
    def yedekle(self):
        dosya, _ = QFileDialog.getSaveFileName(
            self, 'Yedek Kaydet', f'muhasebe_yedek_{date.today()}.json',
            'JSON Dosyası (*.json)'
        )
        if dosya:
            db.json_yedek_al(dosya)
            self.status.showMessage(f'✅ Yedek alındı: {dosya}', 5000)
            QMessageBox.information(self, 'Yedek Alındı', f'Yedek başarıyla kaydedildi.\n{dosya}')

    def yedek_yukle(self):
        dosya, _ = QFileDialog.getOpenFileName(self, 'Yedek Seç', '', 'JSON (*.json)')
        if not dosya: return
        try:
            import json
            with open(dosya, encoding='utf-8') as f:
                veri = json.load(f)
            h_sayi = len(veri.get('hareketler', []))
            k_sayi = len(veri.get('kalemler', []))
            cevap = QMessageBox.question(self, 'Yedek Yükle',
                f'Dosyada {h_sayi} hareket ve {k_sayi} kalem var.\n'
                'Mevcut verinin üzerine yazılacak. Devam edilsin mi?')
            if cevap == QMessageBox.Yes:
                db.json_yedek_yukle(dosya)
                self.kpi_guncelle(); self.son_hareketler_yukle()
                self._kategori_yenile()
                self.status.showMessage(f'✅ Yedek yüklendi: {h_sayi} hareket geri getirildi.', 5000)
                QMessageBox.information(self, 'Başarılı', f'{h_sayi} hareket ve {k_sayi} kalem yüklendi!')
        except Exception as e:
            QMessageBox.critical(self, 'Hata', f'Yedek yüklenemedi:\n{e}')

    # ─────────────────────────── CSV AKTAR ─────────────────────────────
    def csv_aktar(self):
        dosya, _ = QFileDialog.getSaveFileName(
            self, 'CSV Kaydet', f'hareketler_{date.today()}.csv', 'CSV (*.csv)'
        )
        if not dosya: return
        bas = self.f_bas.date().toString('yyyy-MM-dd')
        bit = self.f_bit.date().toString('yyyy-MM-dd')
        tur = self.f_tur.currentText(); tur = None if tur=='Tümü' else tur
        ara = self.f_ara.text().strip() or None
        liste = db.hareket_listesi(bas, bit, tur, ara)
        baslik = ['İşlem No','Tarih','Tür','Ana Kategori','Alt Kategori','Kalem',
                  'Açıklama','Tutar','Giriş','Çıkış','Kimden/Kime','Ödeme','Belge','Bakiye','Durum']
        with open(dosya, 'w', encoding='utf-8-sig', newline='') as f:
            import csv
            w = csv.writer(f); w.writerow(baslik)
            for h in liste:
                w.writerow([h.get('islem_no',''), h.get('tarih',''), h.get('tur',''),
                            h.get('ana_kategori',''), h.get('alt_kategori',''), h.get('kalem_adi',''),
                            h.get('aciklama',''), h.get('tutar',0), h.get('giris',0),
                            h.get('cikis',0), h.get('kimden_kime',''), h.get('odeme_turu',''),
                            h.get('belge_no',''), h.get('bakiye',0), h.get('durum','')])
        self.status.showMessage(f'✅ CSV aktarıldı: {dosya}', 5000)
