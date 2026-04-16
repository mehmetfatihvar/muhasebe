import sys
import os

# Proje kök dizinini path'e ekle
sys.path.insert(0, os.path.dirname(__file__))

from PyQt5.QtWidgets import QApplication
from PyQt5.QtGui import QIcon
from app.db.database import veritabani_olustur
from app.ui.ana_pencere import AnaPencere


def main():
    veritabani_olustur()

    app = QApplication(sys.argv)
    app.setApplicationName('Firma Muhasebe Sistemi')
    app.setOrganizationName('MFV')

    pencere = AnaPencere()
    pencere.show()

    sys.exit(app.exec_())


if __name__ == '__main__':
    main()
