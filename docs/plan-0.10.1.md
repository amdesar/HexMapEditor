# Plan HexMapEditor 0.10.1

Baza lokalna `383dd55`, czysty indeks i drzewo robocze. Przeczytano raporty 0.9.3 i 0.10.0. Zakres obejmuje zapis i listę ostatnich projektów, prawy panel, opis, wybory przy rozwiązaniu grupy oraz import obrazu ze schowka.

1. Odtworzyć zgłoszenia na własnych danych, ustalić formaty schowka i sprawdzić ścieżkę startu z mapą.
2. Naprawić geometrię i odrysowanie panelu oraz stan edytora opisu. Zachować trzy widoki.
3. Sprawdzić przypadki celowane, uruchomić zwykły kandydat i dopiero potem podmienić główny EXE.
4. Zapisać rzeczywiste wyniki, odstępstwa i jeden lokalny commit.

- Zwykły 0.10.0 otworzył mapę z argumentu. Próba Worda odtworzyła pozostawienie samego tekstu. Próbka RTF zawiera PNG z zagnieżdżonymi właściwościami, a HTML wskazuje do pliku tymczasowego Worda. Osobno skopiowany obraz ma dodatkowo PNG, JFIF i Bitmap. Zachowano lokalny odczyt danych RTF, bez korzystania ze ścieżek tymczasowych Worda.
- Writer utrwala opcjonalne pole w Map12. Case metadata przeszedł zapis ręczny, Zapisz jako, kontrolowany autozapis, MRU i dwa komunikaty przy niezgodności.
- Wprowadzono pomiar układu i poprawkę ponownego użycia pikseli przy ruchu panelu. Pierwszy Case panel wykrył brak podpisu ikony pod obiektem. Powód to użycie `Visible` odziedziczonego po ukrytym rodzicu jako warunku ustawienia tekstu. Poprawiono. Do sprawdzenia przejścia, geometria i masowe akcje.
- Case notes przeszedł wstępnie lokalny RTF, rzeczywiste próbki Worda i import pliku 2800×5400. Ponowienie po dodaniu wcześniejszego ograniczenia łącznego rozmiaru wklejanych obrazów. Następnie zwykły EXE na własnej mapie.
- Końcowe przypadki metadata, panel i notes przeszły. Case panel sprawdza 7/8/19 stanów i pozycje podczas aktywnego gestu. Zwykła kopia potwierdziła nową etykietę, przejścia widoków, ostrzeżenie o duplikacie, zbiorczy wybór rozwiązania, wklejenie z Worda, trwałość opisu i trzy kadry ze schowka. Własne okna testowe zamknięto.
- Sprawdzony kandydat `3AD288B4…` zastąpił główny EXE. Potwierdzono FileVersion/ProductVersion 0.10.1.0 i zgodność bajtów. Raport zawiera wyniki metod, zwykłego EXE i ograniczenie obserwacji klatek w ruchu. Ostatnia operacja to commit i sprawdzenie statusu Git.
