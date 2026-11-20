# Plan 0.9.3

Baza d8a7a70 potwierdzona. Git początkowo czysty. Dane prób wyłącznie tests/_work. Odtworzyć oba importy i sprawdzić stan, zamykanie i odczyt zapisu sekcji. Następnie skorygować roboczy stan szybkiego zestawu, zweryfikować wyjątki i zwykły EXE. Sprawdzić źródło etykiety MRU. Po testach podmienić EXE i wykonać jeden lokalny commit.

- Baza odtworzona metodami. Ikona i obiekt znalazły się w quick po wczytaniu osobnych obrazów i zatwierdzeniu kadru. Brak sygnału we wszystkich trzech miejscach, prawidłowy zapis i odczyt sekcji. Etykieta odłożona po przeglądzie modelu, zapisu, odczytu i MRU.
- Poprawka w kodzie. Migawki sesyjne per sekcja, wspólne porównanie dla statusu, przycisku i zamknięcia. Rozszerzono Case o wyjątki i Undo. Pierwsza próba rozszerzona ujawniła brak odświeżenia przy Undo. Po poprawce i usunięciu trybu odtwarzania bazy finalny Case preset PASS.
- Zwykły EXE sprawdzony na mapie Próba 093. Nowa ikona z PNG trafiła do quick, status i przycisk wskazały niezapisany preset. Zamknięcie zapytało o preset i pozwoliło go zapisać. Ponowny odczyt JSON potwierdził ikonę, własny proces zamknięty.
- FileVersion i ProductVersion 0.9.3.0. Główny EXE podmieniony po próbie, SHA-256 zgodny. Pozostała operacja to jeden lokalny commit wraz z raportem, potem kontrola statusu. Brak własnych otwartych okien i niepewnych operacji.
