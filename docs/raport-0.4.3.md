# Raport HexMapEditor 0.4.3

Baza `b61a256` zawiera uzupełnienie próby usunięcia i natychmiastowego Undo 0.4.2. Przed podmianą zachowano bajtowo zgodną kopię zastanego EXE w `tests/_work/backup-przed-043-86a1e795/HexMapEditor.exe`. Użyto bieżącego `assets/app/icon.ico`. SHA-256 zastanej binarki to `86A1E7958CCFE24DE018D8F487EF9C5CD307FACE9D70C12419EA4312AE420B87`. Główny build 0.4.3.0 ma SHA-256 `3A405B9C07E0F1C66CBDDA21BAA9D758C6A83E2FC3E40C717A7FA6FE5714993D`.

## Panel i rysowanie

W ścieżce otwierania 0.4.2 panel zmieniał widoczność przed końcowym pozycjonowaniem. Kolejne aktualizacje ustawiały jego z-order ponownie. Bufor rodzica nie obejmował natywnych HWND dzieci, a blokada WM_SETREDRAW była pomijana przy ukrytym panelu. To wyjaśnia możliwość pokazania pikseli płótna w miejscach, które dzieci miały dopiero odrysować. Podstawa diagnozy to kolejność operacji i zakres buforowania w kodzie. Brakuje pomiaru klatek przejściowych.

W 0.4.3 panel ma stały z-order i położenie przed pokazaniem, z kompozycją całej gałęzi HWND przez WS_EX_COMPOSITED i synchronicznym odrysowaniem rodzica i dzieci po otwarciu i zmianie struktury. Zawijanie kafelków nadal aktualizuje istniejące kontrolki. Lista członków grupy grupuje zmiany, zachowuje zaznaczenie i pozycję przewinięcia w granicach nowej listy. WPF pozostaje w istniejącej kontrolce.

Kreskowana ramka grupy ma 3 px i margines 1 px od grafiki. Złoty obrys ma 2 px i margines 6 px. Oba używają skali ekranu i wspólnego clippingu.

## Lista, stany i kolejność

Lista obiektów na mapie pokazuje grupę i kolor efektywnego stanu. Maskowany wiersz pokazuje widoczne znaki zapytania, neutralną miniaturę i neutralny znacznik stanu. Znak zapytania jest centrowany względem obwiedni glifu. Próg pozostaje 50% widoczności włącznie. Sortowanie przechodzi przez rosnąco, malejąco i kolejność dokumentu. Kluczem jest widoczny tekst lub niezaokrąglona część ukryta. Remisy używają indeksu dokumentu, a akcje używają ID.

Menu kafelka usuwa własny stan z mapy po wyborze zamiennika. Dialog pokazuje odwołania bazowe, także przykryte nakładką, oraz odwołania grupowe. Domyślne ID zamiennika to Neutral. Checkbox pliku jest domyślnie wyłączony. Włączenie dodaje operację LibraryFiles do tej samej transakcji. Undo i Redo obejmują pozycje definicji, referencje, mgłę i plik. Wcześniejsze odkrycia pozostają. Nowa mapa korzysta z bieżącej biblioteki.

Zapisy zachowują format 6 z opcjonalną listą `ExcludedStateIds`. Pole utrwala usunięcie dostępności stanu na tej mapie. Odczyt formatu 5 pozostaje. Grafiki i używane stany pozostają przenośne, włącznie z bazami pod nakładkami. Starsza aplikacja 0.4.2 może ponownie pokazać biblioteczny stan wyłączony przez nowe pole.

Biblioteki zapisują `LibraryOrder` przy normalnym zapisie pozycji. Starsze pliki bez klucza mają porządek ordinal po stabilnym ID, przed nowymi pozycjami. Podstawowe stany oraz Domyślny preset mają stałe miejsca. Edycja zachowuje klucz, Undo przywraca bajty i pozycję. Tereny zachowują kolejność tablicy palety. Wczytanie biblioteki nie przepisuje dawnych plików.

## Widok i przebudowa

Widok przełącza Tereny, Ikony, Obiekty i Mgłę. Ukryte tereny mają kolor neutralny. Obiekty wyłączone w Widoku nie rysują się i nie przechwytują hit-testu. Mgła odsłania widok przy zachowaniu rzeczywistego Fog i mechaniki odkrywania. Zgoda na pokazanie ukrytej warstwy kończy bieżącą próbę edycji. Użytkownik ponawia gest lub zatwierdza nadal otwarty dialog. Flagi nie trafiają do zapisu ani historii, a Nowa/Otwórz je resetują.

Porównanie bezpośrednich bajtów RT_GROUP_ICON/RT_ICON wykazało zgodność zastanego głównego EXE z bieżącym ICO. Przebudowa odrębnej kopii przez BAT z magentowo-zielonym ICO osadziła dokładnie jego bajty. Błędny ICO zachował hash wcześniejszego wyniku. Stary zasób został wykluczony dla sprawdzonego pliku. Samo wyświetlanie przez Eksplorator, w tym wpływ jego cache, nie było mierzone. Komunikaty wskazują oczekiwany plik `assets/app/icon.ico`, docelowy EXE i stan po błędzie.

## Celowane metody i kompilacja

- `states` PASS. Nieużywany stan tylko z mapy, stan używany z zamiennikiem własnym, baza i grupa, neutralny domyślny wybór dialogu, opcjonalne usunięcie pliku, Undo/Redo bajtów i mgły, porządek po ponownym odczycie.
- `view-list` PASS. Widoczna maska, kolor efektywnego stanu, cykl sortowania, liczbowy klucz mgły i ID. Flagi zachowują mapę i historię, ukryty obiekt nie przechwytuje trafienia. Model sprawdza odmowę i zgodę przywrócenia widoczności. Próba renderowania wykazała położenie i przycięcie obrysów. Pierwsza asercja obejmowała narożniki faktycznie poza heksem. Po zawężeniu do fragmentów wewnątrz heksa test przeszedł.
- `order` PASS. Nowe ikony, wzorce i presety zapisują kolejność dodania. Edycja nazwy ikony i Undo/Redo zachowują pozycję po wczytaniu.
- `Test-Patch043Icon.ps1` PASS. Odczyt PE, przebudowa przez BAT w osobnym katalogu, błędny ICO i zachowanie EXE.
- Pełny zestaw, stare kampanie i kampania DPI pozostały poza zakresem próby.

## Zwykły EXE i punkt wznowienia

Uruchomiono zwykłą kopię EXE w `tests/_work/ui-043-c5bb66e950e34bcfb661f1096db5cd71` i zaobserwowano ekran początkowy z przyciskiem Widok. Kliknięcie i próba ponownej aktywacji zwróciły `failed to activate captured window`. Wysłano prośbę o przywrócenie własnego okna na pierwszy plan. Próby przełączników, sortowania, usuwania stanu i wielokrotnego otwarcia panelu w zwykłym oknie pozostają niewykonane. Kopia może pozostać otwarta. Ostatnia korekta grupowania listy członków jest w głównym buildzie. Nie było nagrania przejścia klatka po klatce.

## Checklista

1. Zamknij i otwórz szczegóły obiektu oraz grupy kilka razy. Obserwuj dolne kontrolki i prawą krawędź podczas przejścia.
2. Zmień kilka warstw z otwartego Widoku. Zamknij menu drugim kliknięciem, Escape i kliknięciem poza nim. Spróbuj edycji ukrytej warstwy z odmową i zgodą.
3. Otwórz listę i kliknij nagłówek trzy razy. Sprawdź strzałki, maskę i wskazanie właściwego obiektu.
4. Usuń własny stan po wybraniu zamiennika. Sprawdź bazę pod nakładką, grupę i historię. W izolowanej bibliotece powtórz z checkboxem pliku.
5. Zmień nazwę pozycji i uruchom program ponownie, aby sprawdzić kolejność. Otwórz mapę z użytym własnym stanem oraz z wyłączonym stanem.
6. Podmień ICO w kopii, uruchom BAT i sprawdź wynikowy plik. W razie starego wyglądu powłoki porównaj zasób PE.

Zmiany pozostają lokalne. Commit wskazuje `git log -1`.
