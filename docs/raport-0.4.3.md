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

## Próby interaktywne w zwykłym EXE

Po wcześniejszym błędzie `failed to activate captured window` wznowiono pracę w istniejącym oknie z `tests/_work/ui-043-c5bb66e950e34bcfb661f1096db5cd71`. Jedna próba aktywacji wystarczyła. Użyta kopia 0.4.3 miała SHA-256 `8143C763DB510887D7C862CEF36A119A318018302FB9A70747AAFC0609558874` i pochodziła sprzed ostatniej korekty listy członków grupy. Próby odbyły się na własnej mapie `Próba 0.4.3.hexmap` z odrębną biblioteką.

- **Prawy panel.** Ze stanu zamkniętego dwa razy otwarto szczegóły pojedynczej instancji i po jednym razie szczegóły grup Zieloni i Niebiescy. Każdy zrzut po otwarciu pokazywał kompletny, nieprzezroczysty panel. Dostępne były zrzuty po akcjach. Krótkie prześwity i etapowe odrysowanie w trakcie otwierania pozostają niezweryfikowane, podobnie jak zachowanie ostatniej korekty listy członków z głównego EXE.
- **Widok.** Wyłączono Tereny, wyłączono i włączono Obiekty w tym samym otwartym menu. Ponowne kliknięcie Widok je zamknęło. Przy próbie malowania ukrytych terenów odpowiedź Nie pozostawiła warstwę ukrytą, co potwierdziło ponowne wyświetlenie pytania. Odpowiedź Tak zamknęła pytanie bez widocznego pomalowania heksa i bez gwiazdki. Escape i kliknięcie poza menu pozostały poza tą próbą.
- **Lista obiektów.** Nagłówek Nazwa dał kolejno porządek rosnący, malejący i bazowy. Maskowany wpis miał `???` i pokazywał 57% ukrycia. Po ponownym sortowaniu akcja Zaznacz bez centrowania otworzyła właściwą instancję „Próba usunięcia 0.4.2” z przesuniętego wiersza.
- **Usunięcie stanu.** Po zgodzie usunięto używany „Stan próby 0.4.3” z zamiennikiem Neutralny. Opcja usunięcia pliku była nieaktywna. Kafelek zniknął, instancja otrzymała Neutralny i pojawiła się gwiazdka. Jedno Ctrl+Z przywróciło kafelek, przypisanie i stan bez gwiazdki. Jedno Ctrl+Y ponownie zastosowało zamiennik i oznaczyło mapę jako zmienioną.

Zapisano wyłącznie własną mapę testową i zamknięto własne okno. W tej sesji wykonano tylko powyższe próby interaktywne.

## Checklista

1. Zamknij i otwórz szczegóły obiektu oraz grupy kilka razy. Obserwuj dolne kontrolki i prawą krawędź podczas przejścia.
2. Zmień kilka warstw z otwartego Widoku. Zamknij menu drugim kliknięciem, Escape i kliknięciem poza nim. Spróbuj edycji ukrytej warstwy z odmową i zgodą.
3. Otwórz listę i kliknij nagłówek trzy razy. Sprawdź strzałki, maskę i wskazanie właściwego obiektu.
4. Usuń własny stan po wybraniu zamiennika. Sprawdź bazę pod nakładką, grupę i historię. W izolowanej bibliotece powtórz z checkboxem pliku.
5. Zmień nazwę pozycji i uruchom program ponownie, aby sprawdzić kolejność. Otwórz mapę z użytym własnym stanem oraz z wyłączonym stanem.
6. Podmień ICO w kopii, uruchom BAT i sprawdź wynikowy plik. W razie starego wyglądu powłoki porównaj zasób PE.

Zmiany pozostają lokalne. Commit wskazuje `git log -1`.
