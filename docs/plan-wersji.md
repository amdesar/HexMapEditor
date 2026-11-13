# Plan wydań

| Wersja | Uzgodniony zakres |
|---|---|
| 0.7.0 | Zakończenie gumki, kamera mapy, grube gradienty, ręczny High/Low, kolory sesji, start i ostatnie projekty |
| 0.7.1 | Poprawki startu/Nowy z plikiem, no-op i przekazanie wyniku gumki, stan cache naprzemiennego zoomu |
| 0.7.2 | Rozdzielone Nowa start/edytor, autozapis istniejącej mapy co5min, autor i jawna data wydania |
| 0.8.0 | Menu i szybkie polecenia, wspólne pola liczbowe, ciemny motyw własnych kontrolek, rename pliku |
| 0.8.1 | Poprawki odbiorcze pól/historii, switchy, stempla i góry; hybryda wycofana po regresji RestoreBounds |
| 0.9.0 | Lewy panel, presety i ikony; niebieska ramka aktywnego narzędzia |
| 0.10.0 | Prawy panel i opisy z obrazami |
| 0.11.0 | Niezależne mapy w zakładkach; lokalizacja do ponownej oceny na tym etapie |
| 0.12.0 | Lokalne ustawienia i [kolejka importu](backlog-import-z-folderow.md) |
| 0.13.0 | Aktualizator |
| 0.13.x | Końcowa stabilizacja i uzgodnione testy |
| 1.0.0 | Wyłącznie oznaczenia wersji, bez nowych zmian funkcjonalnych/stylistycznych |

Wszystkie funkcje, poprawki i audyty kończą się przed1.0. Od1.0 nowsze wydania mają otwierać mapy/presety/biblioteki1.0 i kolejnych publicznych wersji. Nie wdrażamy teraz systemu przyszłych migracji ani konwersji0.x.

Merge heksów i import osadzonych zasobów mapy do Library dopiero po1.0. Pozostałe backlogi paneli, UX zapisów presetów, lokalnych ustawień/górnego paska i kolejki pozostają odrębne. High/Low **nigdy nie będzie utrwalany** także przez przyszłe ustawienia lokalne.

[Uzupełnienia UI po 0.8](backlog-ui-po-0.8.md): toasty po zakładkach (patch nieustalony), kopiowanie/wklejanie po1.0 po rozmowie z GM, widoczne źródła z metadanymi obok/kolejką w0.12 oraz ewentualne menu zamykania kart w0.11. Wyłącznie zapis pomysłów, bez wdrożenia w0.8.

## Aktualizator 0.13.0 — uzgodniony backlog

- Automatyczna kontrola aktualizacji w tle przy zwykłym starcie na ekranie powitalnym; brak internetu nie blokuje pracy.
- Bez automatycznej instalacji: tylko informacja o dostępnej wersji i możliwość pominięcia.
- Ręczne „Sprawdź aktualizacje” na starcie.
- Uruchomienie z mapą nie wymusza ekranu powitalnego.
- Zachowanie Library, map, lokalnych ustawień i `recent-projects.json` podczas przyszłej aktualizacji.

W 0.7.1 nie ma połączenia z GitHubem ani implementacji aktualizatora.

Do zaprojektowania w 0.13.0 po wykryciu dostępnej aktualizacji: A — link do konkretnego GitHub Release albo B — osobne okno z pobranym opisem wydania. Wybór A/B nie został zatwierdzony. Brak stałego przycisku GitHub w 0.7.2 nie blokuje przyszłego aktualizatora; w 0.7.2 nie implementujemy żadnego wariantu ani połączeń aktualizacyjnych.

## Po 1.0 — szersze preferencje użytkownika

Oddzielny panel: interwał/włączenie autozapisu, preferowane wartości i kolory, użytkowy domyślny preset, konfigurowalne skróty z alternatywnymi kombinacjami i kontrolą konfliktów oraz preferencja uruchamiania w maksymalizacji. To inny zakres niż zapis układu/kolumn/paneli z resetem i fallback w 0.12.0. Przy reworku góry0.8 można przewidzieć przyszłe wejście; teraz bez aktywnego pustego okna i bez zmiany chronionego Domyślnego. Szczegóły: [backlog ustawień](backlog-ustawienia-lokalne.md). High/Low nadal wyłącznie sesyjny.
