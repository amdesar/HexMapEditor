# Plan wydań

| Wersja | Uzgodniony zakres |
|---|---|
| 0.7.0 | Zakończenie gumki, kamera mapy, grube gradienty, ręczny High/Low, kolory sesji, start i ostatnie projekty |
| 0.7.1 | Poprawki startu/Nowy z plikiem, no-op i przekazanie wyniku gumki, stan cache naprzemiennego zoomu |
| 0.8.0 | Górny pasek i wygląd |
| 0.9.0 | Lewy panel, presety i ikony |
| 0.10.0 | Prawy panel i opisy z obrazami |
| 0.11.0 | Niezależne mapy w zakładkach **na dole** |
| 0.12.0 | Lokalne ustawienia i [kolejka importu](backlog-import-z-folderow.md) |
| 0.13.0 | Aktualizator |
| 0.13.x | Końcowa stabilizacja i uzgodnione testy |
| 1.0.0 | Wyłącznie oznaczenia wersji, bez nowych zmian funkcjonalnych/stylistycznych |

Wszystkie funkcje, poprawki i audyty kończą się przed1.0. Od1.0 nowsze wydania mają otwierać mapy/presety/biblioteki1.0 i kolejnych publicznych wersji. Nie wdrażamy teraz systemu przyszłych migracji ani konwersji0.x.

Merge heksów i import osadzonych zasobów mapy do Library dopiero po1.0. Pozostałe backlogi paneli, UX zapisów presetów, lokalnych ustawień/górnego paska i kolejki pozostają odrębne. High/Low **nigdy nie będzie utrwalany** także przez przyszłe ustawienia lokalne.

## Aktualizator 0.13.0 — uzgodniony backlog

- Automatyczna kontrola aktualizacji w tle przy zwykłym starcie na ekranie powitalnym; brak internetu nie blokuje pracy.
- Bez automatycznej instalacji: tylko informacja o dostępnej wersji i możliwość pominięcia.
- Ręczne „Sprawdź aktualizacje” na starcie.
- Uruchomienie z mapą nie wymusza ekranu powitalnego.
- Zachowanie Library, map, lokalnych ustawień i `recent-projects.json` podczas przyszłej aktualizacji.

W 0.7.1 nie ma połączenia z GitHubem ani implementacji aktualizatora.
