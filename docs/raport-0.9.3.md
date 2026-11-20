# HexMapEditor 0.9.3

Baza d8a7a70 potwierdzona, początkowy Git czysty. Data wydania 20.11.2026.

## Odtworzenie i przyczyna

Na bazie 0.9.2, na czystym zapisanym presecie, użyto dwóch wygenerowanych PNG. Test metod zatwierdził każdy obraz w produkcyjnym dialogu kadrowania i wywołał produkcyjną operację zapisu bibliotecznego dla ikony i obiektu. Oba ID pojawiły się w szybkim zestawie. Status i przycisk pozostały neutralne, a procedura zamykania nie zapytała o preset. Ręczny zapis Ikon, potem Obiektów, przeszedł z ponownym odczytem obu sekcji. Przyczyną było porównywanie osadzonej kopii sekcji, mimo że ręczny zapis pobierał widoczne zestawy.

Jawny import do zestawu oraz jawne usunięcie z zestawu utrwalają zmianę zawartości widocznej sekcji w sesyjnym drafcie. Wspólne porównanie z biblioteką zasila status, przycisk i pytanie przy zamykaniu. Zapis i przywrócenie rozliczają wybrane sekcje. Istniejąca historia importu przywraca również odpowiednią sekcję i odświeża jej status. Zmiany jednego draftu nie podświetlają czystego bieżącego presetu. Stan mapy jest niezależny. Format i dotychczasowe zachowanie kolejności szybkiego dostępu pozostają bez zmian. Zapis wyłącznie do biblioteki i automatyczne czyszczenie list po usunięciu pliku bibliotecznego nie tworzą niezapisanej zmiany presetu.

## Ostatnie projekty

Etykieta odłożona. Writer serializuje MapData po walidacji. Reader sprawdza FormatVersion, które oznacza numer schematu mapy. LoadedFileVersion/ProjectFileVersion to długość i SHA-256 bajtów do wykrywania konfliktu, a MRU zapisuje tablicę ścieżek. Żadne z tych danych nie identyfikuje programu wykonującego ostatni zapis. Map11, preset5 i formaty bibliotek zachowane.

## Celowana weryfikacja

- Metody na bazie 0.9.2 odtworzyły zgłoszenie i potwierdziły poprawny odczyt zapisu sekcji. Log baseline w tests/_work.
- Case preset na 0.9.3 przeszło. Obejmuje ikonę i obiekt, cofnięcie importu, stan przy zamknięciu, częściowy zapis z odczytem, osobny zapis biblioteczny, usunięcie biblioteczne i cofnięcie, konwersję między bibliotekami, zmianę draftu, przywrócenie i niezależny stan zapisu mapy. Rozszerzona próba ujawniła brak odświeżenia statusu po Undo. Powtórzono zależny Case po poprawce. Ostatnie uruchomienie zweryfikowało także uporządkowaną wersję runnera. Brak pełnej regresji i Run-AllTests.
- Zwykły kandydat. Ekran startowy pokazał 0.9.3. Otwarto odizolowaną mapę z MRU, zaimportowano własny PNG i zatwierdzono kadr. Ikona pojawiła się w quick, przycisk presetu podświetlił się, a mapa pozostała bez gwiazdki. Przy zamknięciu pojawiło się pytanie o preset. Zatwierdzono zapis, proces się zamknął, a odczyt testowego JSON potwierdził ikonę. Drugi typ zasobu i zapis częściowy sprawdzono metodami. Próba uruchomienia przez Start-Process z argumentem nie wystawiła okna. Sesję przeprowadzono przez ekran startowy, a pierwszej ścieżki nie zaliczono jako zweryfikowanej.

- Import do szybkiego zestawu → kafelek, wyróżniony zapis presetu i pytanie przy zamykaniu.
- Zapis samych ikon przy zmienionych obiektach → ikony wracają z pliku, obiekty nadal sygnalizują niezapisane zmiany.
- Przejście na czysty preset → neutralny przycisk.
- Usunięcie biblioteczne → automatyczne uporządkowanie list, czysty preset pozostaje czysty.

## Artefakt i Git

Aplikacja 0.9.3, FileVersion i ProductVersion 0.9.3.0. Główny EXE podmieniono sprawdzonym kandydatem. Identyczny SHA-256 obu kopii: `6101F0EF8FD041035BA0FA5D5F28CA319C2928B3917F7FB56A7524F4316826E5`. Poprzedni EXE zachowano w tests/_work. Jeden lokalny commit kodu, EXE, testów i dokumentacji. Identyfikator oraz odczyt końcowego statusu podano w odpowiedzi. Dane robocze poza commitem.
