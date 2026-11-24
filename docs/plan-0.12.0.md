# Plan HexMapEditor 0.12.0

- Potwierdzono bazę `9b9efe9` i czysty Git. Przeczytano raport 0.11.1. Wszystkie dane robocze będą w `tests/_work`.
- Wydzielić jawny tryb instalacji i ścieżki danych, zachowując fabryczną inicjalizację i dotychczasowe zachowanie kopii portable.
- Zbudować Inno Setup i Portable ZIP z tej samej kompilacji. Zarejestrować własny ProgID opcjonalnym zadaniem per użytkownik. Wybór domyślnej aplikacji pozostaje w Windows.
- Dodać kontrolę publicznych stabilnych wydań, sprawdzanie SHA-256 i przekazanie zweryfikowanej instalacji po zwykłym zamknięciu. Konfiguracja kanału będzie pusta.
- Podpisany kompilator Inno Setup 6.7.4 pobrano i rozpakowano wyłącznie w `tests/_work`. Windows Sandbox nie jest lokalnie dostępny. Próby instalatora wymagają izolacji instalacji i danych od bieżącego profilu aplikacji.
- Zaimplementowano ścieżki danych, paczki i aktualizator z pustą konfiguracją kanału. Zaliczone `paths`, `close`, `channel` oraz próbny upgrade 0.12.0→0.12.1 z osobnymi kluczami. `channel` powtórzono po naprawie zapisu oznaczenia pochodzenia pliku.
- Skompilowano obie finalne paczki z tego samego EXE. Zwykła kopia Portable zapisała i ponownie otworzyła próbny Map12. Sprawdzono wersje, listę plików ZIP, skróty SHA-256 oraz zgodność głównego EXE i kopii przez `fc.exe /b`.
- Powtórzony upgrade po zmianie blokady pliku i kodowania Inno przeszedł. Zainstalowany EXE fixture otworzył mapę po upgrade. Próba Setup przy działającej kopii potwierdziła odmowę. Ponowny Setup naprawczy i deinstalacja fixture zachowały dane. Zamknięto własne okna i usunięto tymczasowy alias Portable.
- Raport zawiera ograniczenia próby bez VM i instrukcję przyszłego publicznego testu. Końcowa operacja Git obejmuje jeden lokalny commit i odczyt statusu.
