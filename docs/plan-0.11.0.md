# Plan 0.11.0

- Baza `0194ce2`, indeks i drzewo robocze czyste. Sprawdzono raport 0.10.2 oraz ścieżki zapisu, autozapisu, presetów i zamykania.
- Jedna sesja dokumentu na kartę. Okno zachowuje jeden zestaw kontrolek, a przy aktywacji odtwarza stan widoku. Zapisy i kolejka autozapisu wskazują sesję dokumentu.
- Robocze dane palety pozostają odrębne od heksów i obiektów. Współdzielenie po powiązaniu ze znormalizowaną ścieżką pliku presetu. Ochrona niezapisanych zmian przy odświeżaniu.
- Wykonano sesje dokumentów, wspólne płótno, pasek, przekazywanie ścieżek, zamykanie, współdzielone drafty po ścieżce i jeden autozapis z kolejką dokumentów. Powtórki zależne od korekty migawki zapisu i dodatkowych prób zaznaczenia oraz szybkich zestawów.
- `documents` i `autosave` PASS. Szerokość minimum 1180 px, pasek 36 px, przewijany obszar 705 px, płótno 828 px wysokości w próbie. Wstępny EXE ujawnił zamknięcie uchwytu oczekiwania przed `EndRead`. Korekta przeszła próbę przekazania nowej i już otwartej ścieżki.
- Sprawdzony zwykły EXE w `tests/_work/patch-0110-fixture-c971c9ac2905470da8545d8d42735760`. SHA-256 `9810671CEA7ADBC1F182A02272DFB5EA0464B6DBE96BDD5241BD823566C81084`. Zamknięto własne okna próbne.
- Główny plik podmieniono po kontroli bazy i blokady. `fc /b` potwierdził zgodność bajtową, obie wersje metadanych to 0.11.0.0. Punkt końcowy przed weryfikacją Git: zakres jednego commita sprawdzony według listy plików.
