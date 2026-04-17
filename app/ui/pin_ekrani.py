from PyQt5.QtWidgets import (QDialog, QVBoxLayout, QHBoxLayout, QLabel,
                             QLineEdit, QPushButton, QFrame, QMessageBox)
from PyQt5.QtCore import Qt, QTimer
from PyQt5.QtGui import QFont
import app.db.database as db


class PinGirisEkrani(QDialog):
    """
    Uygulama açılışında gösterilen PIN giriş ekranı.
    PIN tanımlı değilse ilk kullanımda yeni PIN oluşturulur.
    3 yanlış denemede 30 saniye kilitlenir.
    """
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle('Muhasebe Sistemi — Giriş')
        self.setFixedSize(360, 440)
        self.setWindowFlags(Qt.Window | Qt.WindowTitleHint | Qt.CustomizeWindowHint)
        self.setStyleSheet("""
            QDialog { background:#0f1117; }
            QLabel  { color:#e8ecf4; font-family:'Segoe UI'; }
            QLineEdit {
                background:#181c27; border:2px solid #2a3050; border-radius:10px;
                color:#e8ecf4; font-size:28px; letter-spacing:8px;
                padding:12px; text-align:center;
                font-family:'Courier New';
            }
            QLineEdit:focus { border-color:#3d7fff; }
            QPushButton {
                background:#3d7fff; color:white; border:none;
                border-radius:8px; font-size:14px; font-weight:bold;
                padding:12px; font-family:'Segoe UI';
            }
            QPushButton:hover  { background:#5b9aff; }
            QPushButton:pressed{ background:#2a5fd6; }
            QPushButton#btnTemizle {
                background:transparent; border:1px solid #2a3050; color:#8b96b0;
            }
            QPushButton#btnTemizle:hover { border-color:#ff4d6d; color:#ff4d6d; }
        """)

        self._yanlis_deneme = 0
        self._kilitli = False
        self._ilk_kurulum = not db.pin_var_mi()

        lay = QVBoxLayout(self)
        lay.setContentsMargins(32, 32, 32, 32)
        lay.setSpacing(20)

        # Logo / başlık
        baslik = QLabel('🏢')
        baslik.setAlignment(Qt.AlignCenter)
        baslik.setStyleSheet('font-size:48px;')
        lay.addWidget(baslik)

        alt_baslik = QLabel('Firma Muhasebe Sistemi')
        alt_baslik.setAlignment(Qt.AlignCenter)
        alt_baslik.setStyleSheet('font-size:15px; font-weight:bold; color:#5b9aff;')
        lay.addWidget(alt_baslik)

        # Mesaj
        self.mesaj = QLabel('PIN kodunuzu girin' if not self._ilk_kurulum else 'Yeni PIN belirleyin (4-6 rakam)')
        self.mesaj.setAlignment(Qt.AlignCenter)
        self.mesaj.setStyleSheet('font-size:13px; color:#8b96b0;')
        lay.addWidget(self.mesaj)

        # PIN girişi
        self.pin_input = QLineEdit()
        self.pin_input.setEchoMode(QLineEdit.Password)
        self.pin_input.setAlignment(Qt.AlignCenter)
        self.pin_input.setMaxLength(6)
        self.pin_input.setPlaceholderText('● ● ● ●')
        self.pin_input.returnPressed.connect(self.gir)
        lay.addWidget(self.pin_input)

        # İkinci PIN (ilk kurulum için)
        self.pin_tekrar = QLineEdit()
        self.pin_tekrar.setEchoMode(QLineEdit.Password)
        self.pin_tekrar.setAlignment(Qt.AlignCenter)
        self.pin_tekrar.setMaxLength(6)
        self.pin_tekrar.setPlaceholderText('● ● ● ● (tekrar)')
        self.pin_tekrar.returnPressed.connect(self.gir)
        self.pin_tekrar.setVisible(self._ilk_kurulum)
        lay.addWidget(self.pin_tekrar)

        # Butonlar
        btn_row = QHBoxLayout()
        self.btn_gir = QPushButton('Giriş')
        self.btn_gir.clicked.connect(self.gir)
        btn_temizle = QPushButton('Temizle')
        btn_temizle.setObjectName('btnTemizle')
        btn_temizle.clicked.connect(lambda: (self.pin_input.clear(), self.pin_tekrar.clear()))
        btn_row.addWidget(btn_temizle)
        btn_row.addWidget(self.btn_gir)
        lay.addLayout(btn_row)

        lay.addStretch()

        # Kilitleme sayacı
        self.kilit_label = QLabel('')
        self.kilit_label.setAlignment(Qt.AlignCenter)
        self.kilit_label.setStyleSheet('color:#ff4d6d; font-size:12px;')
        lay.addWidget(self.kilit_label)

        self.pin_input.setFocus()

    def gir(self):
        if self._kilitli:
            return

        pin = self.pin_input.text().strip()

        if len(pin) < 4:
            self._hata_goster('En az 4 rakam girmelisiniz.')
            return

        if self._ilk_kurulum:
            # İlk kurulum — PIN oluştur
            tekrar = self.pin_tekrar.text().strip()
            if pin != tekrar:
                self._hata_goster('PIN kodları eşleşmiyor. Tekrar deneyin.')
                self.pin_input.clear(); self.pin_tekrar.clear()
                return
            db.pin_kaydet(pin)
            self.mesaj.setText('PIN kaydedildi. ✅')
            self.accept()
        else:
            # Giriş kontrolü
            if db.pin_kontrol(pin):
                self._yanlis_deneme = 0
                self.accept()
            else:
                self._yanlis_deneme += 1
                kalan = 3 - self._yanlis_deneme
                if self._yanlis_deneme >= 3:
                    self._kilitle()
                else:
                    self._hata_goster(f'Hatalı PIN. {kalan} deneme hakkınız kaldı.')
                    self.pin_input.clear()

    def _hata_goster(self, mesaj):
        self.mesaj.setText(mesaj)
        self.mesaj.setStyleSheet('font-size:13px; color:#ff4d6d;')
        QTimer.singleShot(3000, lambda: self.mesaj.setStyleSheet('font-size:13px; color:#8b96b0;'))

    def _kilitle(self):
        self._kilitli = True
        self.btn_gir.setEnabled(False)
        self.pin_input.setEnabled(False)
        self._sure = 30

        def geri_say():
            self._sure -= 1
            self.kilit_label.setText(f'🔒 Çok fazla hatalı deneme. {self._sure} saniye bekleyin.')
            if self._sure <= 0:
                self._kilitli = False
                self._yanlis_deneme = 0
                self.btn_gir.setEnabled(True)
                self.pin_input.setEnabled(True)
                self.pin_input.clear()
                self.kilit_label.setText('')
                self.mesaj.setText('PIN kodunuzu girin')
                self.mesaj.setStyleSheet('font-size:13px; color:#8b96b0;')
            else:
                QTimer.singleShot(1000, geri_say)

        self._hata_goster('Çok fazla hatalı deneme!')
        QTimer.singleShot(1000, geri_say)


class PinDegistirEkrani(QDialog):
    """Ayarlar menüsünden PIN değiştirme ekranı."""
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle('PIN Değiştir')
        self.setFixedSize(340, 320)
        self.setStyleSheet("""
            QDialog { background:#0f1117; }
            QLabel  { color:#e8ecf4; font-family:'Segoe UI'; }
            QLineEdit {
                background:#181c27; border:1px solid #2a3050; border-radius:8px;
                color:#e8ecf4; font-size:20px; letter-spacing:6px;
                padding:10px; text-align:center;
            }
            QLineEdit:focus { border-color:#3d7fff; }
            QPushButton {
                background:#3d7fff; color:white; border:none;
                border-radius:7px; font-size:13px; font-weight:bold; padding:10px;
            }
            QPushButton:hover { background:#5b9aff; }
            QPushButton#iptal { background:transparent; border:1px solid #2a3050; color:#8b96b0; }
        """)

        lay = QVBoxLayout(self)
        lay.setContentsMargins(28, 24, 28, 24); lay.setSpacing(14)

        lay.addWidget(QLabel('🔐 PIN Değiştir'))

        self.eski = QLineEdit(); self.eski.setEchoMode(QLineEdit.Password); self.eski.setPlaceholderText('Mevcut PIN')
        self.yeni = QLineEdit(); self.yeni.setEchoMode(QLineEdit.Password); self.yeni.setPlaceholderText('Yeni PIN (4-6 rakam)')
        self.tekrar = QLineEdit(); self.tekrar.setEchoMode(QLineEdit.Password); self.tekrar.setPlaceholderText('Yeni PIN (tekrar)')
        self.mesaj = QLabel(''); self.mesaj.setStyleSheet('color:#ff4d6d; font-size:12px;'); self.mesaj.setAlignment(Qt.AlignCenter)

        for w in [self.eski, self.yeni, self.tekrar, self.mesaj]: lay.addWidget(w)

        btn_row = QHBoxLayout()
        btn_iptal = QPushButton('İptal'); btn_iptal.setObjectName('iptal'); btn_iptal.clicked.connect(self.reject)
        btn_kaydet = QPushButton('Kaydet'); btn_kaydet.clicked.connect(self.kaydet)
        btn_row.addWidget(btn_iptal); btn_row.addWidget(btn_kaydet)
        lay.addLayout(btn_row)

    def kaydet(self):
        if not db.pin_kontrol(self.eski.text()):
            self.mesaj.setText('Mevcut PIN hatalı!'); return
        if len(self.yeni.text()) < 4:
            self.mesaj.setText('PIN en az 4 rakam olmalı!'); return
        if self.yeni.text() != self.tekrar.text():
            self.mesaj.setText('Yeni PIN\'ler eşleşmiyor!'); return
        db.pin_kaydet(self.yeni.text())
        QMessageBox.information(self, 'Başarılı', 'PIN başarıyla değiştirildi.')
        self.accept()
