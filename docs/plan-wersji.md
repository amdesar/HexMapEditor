# Plan wydań

| Wersja | Uzgodniony zakres |
|---|---|
| 0.7.0 | Zakończenie gumki, kamera mapy, grube gradienty, ręczny High/Low, kolory sesji, start i ostatnie projekty |
| 0.7.1 | Poprawki startu/Nowy z plikiem, no-op i przekazanie wyniku gumki, stan cache naprzemiennego zoomu |
| 0.7.2 | Rozdzielone Nowa start/edytor, autozapis istniejącej mapy co5min, autor i jawna data wydania |
| 0.8.0 | Menu i szybkie polecenia, wspólne pola liczbowe, ciemny motyw własnych kontrolek, rename pliku |
| 0.8.1 | Poprawki odbiorcze pól/historii, switchy, stempla i góry; hybryda wycofana po regresji RestoreBounds |
| 0.8.2 | Hybrydowy tytuł głównego edytora: istniejące menu i natywne przyciski Windows, stabilna geometria przywracania |
| 0.8.3 | Split zoomu, wyrównanie pól nazw, warunkowe przełączanie zasobów na pędzel i jasne strzałki podmenu |
| 0.9.0 | Lewy panel 4×3 / 2×6, obracane figury, presety trzech sekcji, robocze wersje i szybki dostęp 24/12; niebieskie oznaczenia wyboru |
| 0.9.1 | Poprawki kompaktowego lewego panelu, panel z aktualnego presetu biblioteki, częściowe zamykanie, kosz quick/biblioteka i kwadratowe kadrowanie importu ikon/obiektów |
| 0.9.2 | Pojedyncze Importuj/Edytuj/Usuń w bibliotekach ikon i obiektów, trwały odpowiednik konwersji, wiadro ikon, sygnał zapisu presetu i kadrowanie; bez zbiorczego usuwania |
| 0.9.3 | Stan niezapisanych zmian sekcji szybkiego zestawu presetu po imporcie i jawnej edycji |
| 0.10.0 | Trzy widoki prawego panelu, obrazy w opisie heksa i indywidualne nakładki ustawień grupy |
| 0.10.1 | Wersja ostatniego zapisu, poprawki prawego panelu, macierz rozwiązania grupy oraz obrazy i import ze schowka |
| 0.10.2 | Odrysowanie prawego panelu podczas przeciągania, obrazy między akapitami, formatowanie słowa, jeden wiersz projektów i korekty kontrolek |
| 0.11.0 | Niezależne mapy w dolnych kartach, wspólne robocze presety według ścieżki, autozapis zapisanych kart i przekazywanie projektu do działającego EXE |
| 0.11.1 | Jednolite kafelki kart, wspólny termin autozapisu, podgląd mapy i presetu przy zamykaniu oraz dopasowania pod nakładką prawego panelu |
| 0.12.0 | Instalator dla bieżącego użytkownika, Portable ZIP, opcjonalna rejestracja skojarzenia, aktualizator z decyzją użytkownika i zachowaniem danych |
| 0.12.1 | Instalacja w Program Files, osobny wybór Library, opcjonalny skrót pulpitu, rozpoznanie instalacji i danych 0.12.0, link do Runtime oraz korekta powitania |
| 0.12.2 | Kanał testowy GitHub, wybór lokalizacji map według aktywnej Library, skrót pulpitu, cztery korekty edytora i przygotowanie wydania testowego |
| 0.12.3 | Instalacja bieżącego użytkownika we własnym zapisywalnym katalogu, obok trybu dla wszystkich w Program Files |
| 0.13.0 | Rzeczywiste źródła grafik i [kolejka importu](backlog-import-z-folderow.md), docelowe foldery Library |
| 0.13.1 | Jeden kreator Setup, aktualizacja obu trybów instalowanych z aplikacji i ciemna kolejka importu |
| 0.13.2 | Nazwane oryginały bezpośrednio w folderach kategorii Library, z odczytem dotychczasowych źródeł |
| 0.13.3 | Metadane grafik w podfolderach kategorii i kontrola blokady konkretnego EXE podczas aktualizacji |
| 0.13.x | Końcowa stabilizacja i uzgodnione testy |
| 1.0.0 | Wyłącznie oznaczenia wersji, bez nowych zmian funkcjonalnych/stylistycznych |

Wszystkie funkcje, poprawki i audyty kończą się przed1.0. Od1.0 nowsze wydania mają otwierać mapy/presety/biblioteki1.0 i kolejnych publicznych wersji. Nie wdrażamy teraz systemu przyszłych migracji ani konwersji0.x.

Merge heksów i import osadzonych zasobów mapy do Library dopiero po1.0. Pozostałe backlogi paneli, UX zapisów presetów, lokalnych ustawień/górnego paska i kolejki pozostają odrębne. High/Low **nigdy nie będzie utrwalany** także przez przyszłe ustawienia lokalne.

[Uzupełnienia UI po 0.8](backlog-ui-po-0.8.md): toasty po zakładkach (patch nieustalony), kopiowanie/wklejanie po1.0 po rozmowie z GM, widoczne źródła z metadanymi obok/kolejką w0.13 oraz ewentualne menu zamykania kart w0.11. Wyłącznie zapis pomysłów, bez wdrożenia w0.8.

## Aktualizator 0.12.0

- Automatyczna kontrola aktualizacji w tle przy zwykłym starcie na ekranie powitalnym; brak internetu nie blokuje pracy.
- Bez automatycznej instalacji: tylko informacja o dostępnej wersji i możliwość pominięcia.
- Ręczne „Sprawdź aktualizacje” na starcie.
- Uruchomienie z mapą nie wymusza ekranu powitalnego.
- Zachowanie Library, map, lokalnych ustawień i `recent-projects.json` podczas przyszłej aktualizacji.

Wdrożono link do opisu konkretnego wydania i konfigurację publicznego kanału z pustymi wartościami domyślnymi. Odbiór publicznego kanału pozostaje zależny od decyzji o źródle i publikacji przez właściciela. Szersze ustawienia, w tym zapis lokalnego układu i reset, pozostają w osobnym zakresie. [Raport 0.12.0](raport-0.12.0.md).

## Po 1.0 — szersze preferencje użytkownika

Oddzielny panel: interwał/włączenie autozapisu, preferowane wartości i kolory, użytkowy domyślny preset, konfigurowalne skróty z alternatywnymi kombinacjami i kontrolą konfliktów oraz preferencja uruchamiania w maksymalizacji. To inny zakres niż zapis układu/kolumn/paneli z resetem i fallback w 0.12.0. Przy reworku góry0.8 można przewidzieć przyszłe wejście; teraz bez aktywnego pustego okna i bez zmiany chronionego Domyślnego. Szczegóły: [backlog ustawień](backlog-ustawienia-lokalne.md). High/Low nadal wyłącznie sesyjny.

Aktualizacja kolejności po odbiorze 0.9.0: 0.9.x poprawki/biblioteki → 0.10 prawy panel → 0.11 zakładki → 0.12 instalacja/aktualizator → 0.13 źródła/kolejka importu. Starsze numery w historycznych raportach opisują wcześniejszy plan. W 0.9.1 żaden późniejszy etap nie jest wdrożony.

## Ustalenia po odbiorze 0.9.1 / zakres 0.9.2

Zbiorcze usuwanie zostało odłożone **bez nowego numeru wersji**; nie jest funkcją0.9.2. Nie zmieniamy kolejności0.10→0.11→0.12→0.13. Dalsze szczegóły i kosmetyka: [backlogUI](backlog-ui-po-0.8.md). Oryginalne pliki grafik w oryginalnym formacie i późniejsze ponowne kadrowanie należą do0.13. Domyślne kolory rysika/wskaźnika pozostają przyszłymi preferencjami.
