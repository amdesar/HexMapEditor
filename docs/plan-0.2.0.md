# Plan implementacji 0.2.0

Status początkowy: baza 1b1e09c (0.1.4), czysty master zgodny z origin/master. Nie publikujemy zmian.

1. Zweryfikować lokalnie edytor, potem wprowadzić model dokumentu, format 2 i migrację formatu 1. Nie ogłaszać edytora gotowym przed sprawdzeniem tabel, list i historii.
2. Wprowadzić stabilną tożsamość heksów i wariantów terenów, historię mapy, śledzenie zmian bez serializacji całej mapy przy pisaniu oraz lokalne presety.
3. Dodać Wiadro i zmianę rozmiaru z zachowaniem parzystości geometrii, danych i historii. Sprawdzić obie orientacje i wszystkie strony.
4. Przebudować oba wiersze paska i panele, dodać własne ikony GDI+, dialogi presetów/terenów/rozmiaru oraz edytor notatek.
5. Sprawdzić końcowy zwykły EXE interaktywnie, uruchomić regresje, zaktualizować dokumentację i raport. Nie mylić testów metod z testami okna ani importu RTF z próbą Worda.

## Decyzje wstępne

- **Edytor:** sprawdzany wariant to WPF RichTextBox/FlowDocument przez ElementHost, wyłącznie z istniejących składników .NET Framework. Windows Forms i GDI+ pozostają technologią aplikacji. Bez pobierania, usług i płatnych komponentów. Tabele to obiekty Table/Row/Cell, nie tabulatory. Weryfikacja możliwości poprzedza integrację. Dokumentacja: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/richtextbox i https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/table-overview . Nie dodajemy zewnętrznego zestawu ikon: symbole rysujemy lokalnie w GDI+.
- **Zapis:** jawny format mapy 2: identyfikatory heksów, zapisane właściwości/identyfikatory wariantów terenów, robocze palety, parzystość początku siatki i ograniczony model formatowanego dokumentu. Zwykły Description pozostaje czytelny. Format 1 jest odczytywany i migrowany; 0.1.x nie odczyta formatu 2. Bez wykonywalnego XAML, aktywnych obiektów i odwołań sieciowych w zapisie notatek. Zapis atomowy i potwierdzenie końcowej ścieżki pozostają.
- **Historia:** mapa do 300 operacji; małe operacje przechowują tylko zmieniane właściwości terenu. Zmiana rozmiaru zachowuje tożsamość obiektów heksów, nie miesza historii ich tekstu. Nazwa i opis mają własne historie per heks/pole, bez zapisu do projektu; planowany limit 100 kroków na pole i 32 MiB łącznie. Pisanie jest grupowane, formatowanie/tabele/wklejanie są osobnymi operacjami. Stan zapisu porównuje pojedyncze zmieniane heksy i małe metadane, nie całą mapę po znaku.
- **Tereny:** stabilny TerrainId, niezmienny VariantId i rosnąca Revision. Edycja tworzy nowy wariant; opcjonalna aktualizacja mapy obejmuje tylko poprzedni VariantId i jest jednym krokiem historii mapy. Heksy przechowują własną kopię wyglądu i typu. Starsze dane bez ID dostają jawne warianty migracyjne, ponieważ ich pierwotnego pochodzenia nie da się odzyskać. Zmiana/usunięcie presetu nie odbiera mapie danych.
- **Geometria:** naprzemienne przesunięcia z 0.1.4 pozostają. Parzystość początku wierszy/kolumn jest zapisywana i przeliczana przy zmianach od góry/lewej, aby stare heksy zachowały względny układ i sąsiedztwo.

Wszystkie własne artefakty i próby powstają w tests/_work. Library jest lokalne i ignorowane. Etapy i wyniki będą aktualizowane w raporcie; ten plan nie stanowi deklaracji ukończenia.

## Stan po realizacji

1. Weryfikacja edytora zakończona: rzeczywiste tabele, komórki, wiersze/kolumny i listy numerowane; natywnego Undo nie przyjęto jako podstawy. Wdrożono format 2 z ograniczonym modelem dokumentu i odczytem formatu 1.
2. Wdrożono ID heksów, TerrainId/VariantId/Revision, palety lokalne i projektowe, odrębne historie tekstu oraz porównywanie pojedynczego heksu przy pisaniu.
3. Wdrożono iteracyjne Wiadro i rozmiar z czterech stron, parzystości początku, ostrzeżenia i zgodne historie. Regresje obejmują obie orientacje i wszystkie strony.
4. Wdrożono dwa wiersze paska, nazwę z pliku, własne ikony, dwukolumnową paletę, pasek rozwijania oraz przewijaną prawą nakładkę z marginesami.
5. Build i 69 testów metod/kontrolek przeszły. Wykonano opisane w raporcie próby zwykłego EXE, w tym minimalne okno, tabele, schowek RTF, skróty, Wiadro i dialog rozmiaru. **Nie wykonano prób Microsoft Worda ani większego DPI.** Nie wszystkie warianty UI zastępujemy testami metod; szczegółowe ograniczenia i końcowy stan opisuje raport 0.2.0.
