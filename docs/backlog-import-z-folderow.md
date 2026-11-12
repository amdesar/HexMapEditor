# Kolejka importu z folderów — pomysł odłożony

Nie jest funkcją 0.6.1. Nie dodajemy skanowania, okien startowych ani usuwania plików wejściowych.

Użytkownik umieszcza wiele obsługiwanych grafik JPG/PNG/innych w przeznaczonym miejscu danej biblioteki, np. 20 przygotowanych grafik terenu. Przy uruchomieniu program wykrywa oczekujące pliki wejściowe, odróżniając je od własnych metadanych JSON i magazynów Sources. Docelowo właściwe typy bibliotek używają odpowiednich edytorów, nie jednego uniwersalnego kadru.

Pierwszy komunikat daje wybór:

- **Nie teraz** — zwykła praca, bez obowiązku importowania.
- **Otwórz folder** — pokazanie plików w Eksploratorze.
- **Importuj** — właściwa biblioteka i jej kolejka.

Kolejka pokazuje kolejne pliki pojedynczo w odpowiednim edytorze. Użytkownik kadruje i ustawia parametry. Akcje: **Dodaj do biblioteki**, **Usuń plik bez dodawania**, **Pomiń**. Pomiń pozostawia plik i przechodzi dalej; nie blokuje programu i nie traktuje pliku jako uszkodzonego aktywnego wpisu biblioteki blokującego zapis.

Po udanym dodaniu źródło i parametry trafiają do formatu biblioteki. Oryginał z folderu oczekujących ma być usuwany dopiero po poprawnym zapisie i jego weryfikacji, aby nie utrzymywać obu kopii. Nie wolno usuwać go przed zapisem ani przy błędzie.

Przed wdrożeniem trzeba rozstrzygnąć z użytkownikiem:

- osobny podfolder wejściowy czy katalog biblioteki;
- zapamiętywanie pominięcia i kolejne przypomnienia;
- duplikaty źródeł/definicji oraz konflikty nazw;
- anulowanie i wznowienie kolejki;
- Kosz czy trwałe usuwanie;
- ochronę pliku zmienionego przez inny proces podczas importu;
- granice Undo/Redo usunięcia wejściowego oryginału.

Powyższe decyzje pozostają otwarte. Nie wolno zgadywać ich w ramach 0.6.1.
