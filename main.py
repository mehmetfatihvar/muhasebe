import sys
import os

sys.path.insert(0, os.path.dirname(__file__))

from PyQt5.QtWidgets import QApplication
from app.db.database import veritabani_olustur, gunluk_otomatik_yedek, pin_var_mi
from app.ui.ana_pencere import AnaPencere
from app.ui.pin_ekrani import PinGirisEkrani


def main():
    veritabani_olustur()

    app = QApplication(sys.argv)
    app.setApplicationName('Firma Muhasebe Sistemi')

    # Otomatik günlük yedek (sessizce)
    try:
        yedek = gunluk_otomatik_yedek()
        if yedek:
            print(f'Otomatik yedek alındı: {yedek}')
    except Exception as e:
        print(f'Otomatik yedek hatası: {e}')

    # PIN ekranı
    pin_dlg = PinGirisEkrani()
    if pin_dlg.exec_() != pin_dlg.Accepted:
        sys.exit(0)  # PIN yanlış veya kapatıldı

    # Ana pencere
    pencere = AnaPencere()
    pencere.show()
    sys.exit(app.exec_())


if __name__ == '__main__':
    main()
