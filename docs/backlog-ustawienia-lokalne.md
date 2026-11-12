# Lokalne ustawienia układu — przyszła przebudowa górnego paska

Nie implementowane w 0.6.2. Przy przyszłym grupowaniu Nowa/Otwórz/Zapisz/Zapisz jako w menu Plik i porządkowaniu pozostałych kategorii trzeba jawnie omówić trwałość lokalnego układu. To część tej aktualizacji, nie domyślnie ukończona funkcja.

## Cel i granica danych

Zapamiętywać lokalnie między restartami rozmiar głównego okna, szerokość prawego panelu, rozmiary okien bibliotek, szerokości i kolejność kolumn listy obiektów oraz inne osobno uzgodnione ustawienia widoku interfejsu. Nie są danymi mapy ani zasobami eksportu: nie trafiają do hexmap/presetu, nie zależą od otwartej mapy, a cudza mapa nie może ich nadpisywać. Nie wprowadzać teraz systemu profili; ich potrzeba pozostaje decyzją na przyszłość.

## Bezpieczeństwo i przywracanie

Zapewnić przycisk przywracania ustawień domyślnych. Walidować zapisane wartości i dać bezpieczny fallback dla uszkodzonego pliku, zmiany monitora, obszaru roboczego i DPI. Nie odtwarzać okna poza dostępnym ekranem; zachować czytelne minima i możliwość dostępu do kontrolek. Miejsce i format zapisu, zakres zapamiętywanych flag widoku oraz moment zapisu wymagają decyzji przy realizacji. Nie traktować samego otwarcia mapy jako zgody na zmianę lokalnych preferencji.

## Lewy panel — oddzielne przyszłe dopracowanie

- Czytelniejszy wygląd uchwytów przeciągania rozmiaru.
- Etykiety X/Y albo szerokość/wysokość dla figur dwuwymiarowych.
- Uchwyt drugiej wartości takich figur, spójny z pierwszym.

Nie przebudowywać tych kontrolek w 0.6.2. Prawy brzeg i header jitter przy zmianie szerokości nadal należą do przyszłej przebudowy prawego panelu. Jeżeli pozostałe etapowe natywne pokazanie szczegółów wymaga zmiany kompozycji, ocenić je tam z zachowaniem izolacji ElementHost/WPF.

Istniejąca [kolejka importu z folderów](backlog-import-z-folderow.md) pozostaje osobnym, niewdrożonym backlogiem.
