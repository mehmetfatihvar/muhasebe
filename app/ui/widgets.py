from PyQt5.QtWidgets import (QFrame, QVBoxLayout, QHBoxLayout, QLabel,
                             QLineEdit, QComboBox, QDateEdit, QPushButton,
                             QTextEdit, QDoubleSpinBox, QWidget, QSizePolicy)
from PyQt5.QtCore import Qt, QDate
from PyQt5.QtGui import QFont


def para_format(tutar):
    return f"₺{tutar:,.2f}".replace(',', 'X').replace('.', ',').replace('X', '.')


def tarih_format(tarih_str):
    """yyyy-MM-dd → dd.MM.yyyy — DB'deki formatı gösterim formatına çevirir"""
    if not tarih_str:
        return '-'
    try:
        from datetime import datetime
        return datetime.strptime(tarih_str, '%Y-%m-%d').strftime('%d.%m.%Y')
    except ValueError:
        return tarih_str  # zaten başka formattaysa olduğu gibi döndür


def kpi_kart(label, deger, renk='#e8ecf4'):
    kart = QFrame(); kart.setObjectName('kpiKart')
    kart.setMinimumHeight(90)
    layout = QVBoxLayout(kart); layout.setContentsMargins(16, 14, 16, 14); layout.setSpacing(6)
    lbl = QLabel(label); lbl.setObjectName('kpiLabel')
    val = QLabel(deger); val.setObjectName('kpiDeger')
    val.setStyleSheet(f'color: {renk};')
    layout.addWidget(lbl); layout.addWidget(val)
    return kart, val


def form_kart(baslik_text=''):
    kart = QFrame(); kart.setObjectName('formKart')
    layout = QVBoxLayout(kart); layout.setContentsMargins(24, 20, 24, 20); layout.setSpacing(16)
    if baslik_text:
        baslik = QLabel(baslik_text); baslik.setObjectName('formBaslik')
        sep = QFrame(); sep.setFrameShape(QFrame.HLine)
        sep.setStyleSheet('color: #2a3050; margin-bottom: 4px;')
        layout.addWidget(baslik); layout.addWidget(sep)
    return kart, layout


def etiket_input(label_text, widget):
    grp = QWidget()
    vbox = QVBoxLayout(grp); vbox.setContentsMargins(0,0,0,0); vbox.setSpacing(5)
    lbl = QLabel(label_text.upper()); lbl.setObjectName('etiket')
    vbox.addWidget(lbl); vbox.addWidget(widget)
    return grp


def combo(placeholder='-- Seçiniz --'):
    cb = QComboBox(); cb.addItem(placeholder)
    return cb


def tarih_input():
    de = QDateEdit(); de.setCalendarPopup(True)
    de.setDate(QDate.currentDate())
    de.setDisplayFormat('dd.MM.yyyy')
    return de


def tutar_input():
    sp = QDoubleSpinBox()
    sp.setRange(0, 999_999_999); sp.setDecimals(2)
    sp.setPrefix('₺ '); sp.setSingleStep(100)
    return sp


def btn(text, obj_name=None, min_w=120):
    b = QPushButton(text)
    if obj_name: b.setObjectName(obj_name)
    b.setMinimumWidth(min_w)
    return b


def ayrac():
    line = QFrame(); line.setFrameShape(QFrame.HLine)
    line.setStyleSheet('color: #2a3050;')
    return line
