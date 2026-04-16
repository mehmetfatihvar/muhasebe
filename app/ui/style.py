KOYU = {
    'bg':       '#0f1117',
    'surface':  '#181c27',
    'surface2': '#1e2333',
    'border':   '#2a3050',
    'accent':   '#3d7fff',
    'accent2':  '#5b9aff',
    'green':    '#2eca8b',
    'red':      '#ff4d6d',
    'yellow':   '#ffc107',
    'text':     '#e8ecf4',
    'text2':    '#8b96b0',
    'text3':    '#5a6480',
}

STYLESHEET = """
QMainWindow, QWidget {
    background-color: #0f1117;
    color: #e8ecf4;
    font-family: 'Segoe UI', Arial, sans-serif;
    font-size: 13px;
}
QTabWidget::pane {
    border: 1px solid #2a3050;
    background: #181c27;
    border-radius: 8px;
}
QTabBar::tab {
    background: #181c27;
    color: #8b96b0;
    padding: 10px 20px;
    border: none;
    font-weight: 600;
    font-size: 12px;
}
QTabBar::tab:selected {
    background: #3d7fff;
    color: white;
    border-radius: 6px 6px 0 0;
}
QTabBar::tab:hover:!selected {
    background: #1e2333;
    color: #e8ecf4;
}
QPushButton {
    background-color: #3d7fff;
    color: white;
    border: none;
    padding: 9px 18px;
    border-radius: 7px;
    font-weight: 600;
    font-size: 13px;
}
QPushButton:hover { background-color: #5b9aff; }
QPushButton:pressed { background-color: #2a5fd6; }
QPushButton#btnTehlike {
    background-color: transparent;
    border: 1px solid #3a4570;
    color: #8b96b0;
}
QPushButton#btnTehlike:hover { border-color: #ff4d6d; color: #ff4d6d; }
QPushButton#btnYesil { background-color: #2eca8b; color: #000; }
QPushButton#btnYesil:hover { background-color: #25b57d; }
QPushButton#btnSil {
    background: transparent;
    color: #5a6480;
    border: none;
    padding: 4px 8px;
    font-size: 14px;
}
QPushButton#btnSil:hover { color: #ff4d6d; background: rgba(255,77,109,30); border-radius:4px; }
QLineEdit, QComboBox, QDateEdit, QTextEdit, QSpinBox, QDoubleSpinBox {
    background-color: #0f1117;
    border: 1px solid #3a4570;
    border-radius: 7px;
    color: #e8ecf4;
    padding: 8px 12px;
    font-size: 13px;
}
QLineEdit:focus, QComboBox:focus, QDateEdit:focus, QTextEdit:focus {
    border-color: #3d7fff;
}
QComboBox::drop-down { border: none; width: 24px; }
QComboBox::down-arrow { image: none; border: none; }
QComboBox QAbstractItemView {
    background: #1e2333;
    border: 1px solid #2a3050;
    color: #e8ecf4;
    selection-background-color: #3d7fff;
}
QTableWidget {
    background-color: #181c27;
    border: none;
    gridline-color: #2a3050;
    color: #e8ecf4;
}
QTableWidget::item { padding: 8px 12px; border-bottom: 1px solid #2a3050; }
QTableWidget::item:selected { background-color: #1e2333; color: #e8ecf4; }
QHeaderView::section {
    background-color: #1e2333;
    color: #5a6480;
    padding: 8px 12px;
    border: none;
    border-bottom: 1px solid #2a3050;
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
}
QScrollBar:vertical {
    background: #0f1117; width: 6px; border: none;
}
QScrollBar::handle:vertical { background: #3a4570; border-radius: 3px; min-height: 20px; }
QScrollBar:horizontal {
    background: #0f1117; height: 6px; border: none;
}
QScrollBar::handle:horizontal { background: #3a4570; border-radius: 3px; }
QLabel#baslik {
    font-size: 17px; font-weight: 700; color: #e8ecf4;
}
QLabel#kpiLabel {
    font-size: 11px; color: #5a6480;
    text-transform: uppercase; letter-spacing: 1px;
}
QLabel#kpiDeger { font-size: 22px; font-weight: 700; font-family: 'Courier New'; }
QFrame#kpiKart {
    background: #181c27;
    border: 1px solid #2a3050;
    border-radius: 10px;
}
QFrame#formKart {
    background: #181c27;
    border: 1px solid #2a3050;
    border-radius: 12px;
}
QLabel#formBaslik {
    font-size: 13px; font-weight: 600; color: #8b96b0;
}
QLabel { color: #e8ecf4; }
QLabel#etiket {
    font-size: 11px; font-weight: 700;
    color: #8b96b0; letter-spacing: 1px;
}
QMessageBox { background: #181c27; color: #e8ecf4; }
QMessageBox QPushButton { min-width: 80px; }
QStatusBar { background: #181c27; color: #8b96b0; border-top: 1px solid #2a3050; }
QSplitter::handle { background: #2a3050; }
"""
