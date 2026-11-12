# Raport HexMapEditor 0.6.1

Stan: 12 listopada 2026. Baza `144d5a2` (ukończona0.6.0), master. Zachowano zastany kod i historię. Prywatne Library/mapy nie były danymi testowymi ani celem zmian. Bez pełnej kampanii, push, tagu, Release'a i sprzątania.

Aplikacja0.6.1, FileVersion **0.6.1.0**. Główny `HexMapEditor.exe` i zamknięta kopia `tests/_work/ui-061-bbd1b2e0099b4bd1ad7dccff6a404aaa/HexMapEditor.exe` mają identyczny SHA-256:
`74C3BEBFD69CA687963B55B56727911E5D3522C58BE5B63FEA41990DC877760A`.

## Grupy i reguła Widoku

Nie udało się odtworzyć zgłoszonego zaniku zasięgów. Nie ustalono zatem rzeczywistej przyczyny i nie przedstawiamy tej pozycji jako dowiedzionej naprawy. Sprawdzono rzeczywiste CreateObjectGroup, a nie tylko ręcznie zbudowaną strukturę: trójka ze skrajnymi źródłami, Indywidualnie, nadpisanie promienia/wyłączenie/powrót, odłączenie, ruch i Undo/Redo. Dodatkowo grupowanie poza mapą, pusty cache przed ruchem i wejście nad heksy. Ostatni wariant15×10PointyTop miał mieszaną pulę maski kolor+grafika i ruch Kursorem oraz Rączką; wynik14pól pozostawał poprawny. Bazowy zwykły EXE0.6.0 także zachował oba źródła w prostszym scenariuszu poza mapą. W oknie kandydata0.6.1 ostatnia konfiguracja15×10również działała.

**Użytkownik wyraźnie zgodził się odłożyć ten nieodtworzony błąd po ostatniej próbie** i ewentualnie podać dalsze kroki po odbiorze. Nie dodano źródła na środku grupy ani destrukcyjnego kopiowania bazy. Przeniesienie invalidacji cache na koniec ApplyAction zapewnia spójność zakończonej transakcji; nie twierdzimy, że udowodniono je jako przyczynę zgłoszenia.

Nowa uzgodniona reguła działa: **Widok → Obiekty OFF** pomija również ich źródła pola widzenia. Wraca aktualna baza maski; ON oblicza zasięgi z aktualnych bazowych/efektywnych ustawień. Bez dirty/historii/zapisu i bez cofania odkrytego Fog. Zaktualizowano konkretną asercję starej reguły w Patch060Tests, bez uruchamiania tego runnera. Ręczne edycje bazy pozostają autorytatywne; nie dodano odtwarzania dawnych snapshotów.

Nazwy użytkowe: **Ograniczenie widoczności**, **Wiadro ograniczenia widoczności**, **Pole widzenia**, **Promień widzenia**, kolumny **Pole widzenia/Widoczność**. Wynik opisuje widoczność, a nie symulację fizycznego światła. Wewnętrzne Light/Darkness i odrębny Fog zachowano. Indywidualne źródła działają niezależnie od stanu; grupa nadal ma nullable nadpisania członków.

## Rozmiar, wskaźnik i wejście

Rysik/Gumka mają jedną wartość ekranową, domyślnie5, zakres0,5–**200** logicznych px. Przeliczenie raz na gest: rozmiar × DPI/96 ÷ zoom. Zapisana kreska zachowuje grubość świata i później skaluje się z mapą. Gumka wycina trwałą geometrię tą samą przeliczoną średnicą. Obrys jest ekranowy dla bieżącego ustawienia. Zoom/MMB/Fit kończą rozpoczęty lewy fragment przed zmianą kamery. Nie zwiększano limitów punktów/fragmentów.

Walidacja dodatniej skończonej grubości świata do2 mld obejmuje200px przy minimalnym zoomie0,000001 i DPI do960. To limit obliczonego zapisu, a nie ustawienia UI. Przypadek20000 jednostek świata zapisuje się i odczytuje prawidłowo. Obwiednie i częściowe wymazywanie nadal używają geometrii świata.

Uchwyty poziome: przy wspólnym rozmiarze odcisków terenu/mgły/ograniczenia8px=1; przy rysiku/gumce4px=0,5. Wartość od przesunięcia od początku gestu, bez zmiany na sam MouseDown i bez przyspieszania w czasie. Ograniczenia, wpisywanie i zwalnianie capture pozostały.

Wskaźnik jest jednolitym dyskiem bez czarnej lub pozornej obwódki. Średnica18×DPI/96+2px zachowuje poprzedni zewnętrzny rozmiar (20px przy DPI96). Kometa ma zwężającą się geometrię, alpha i czas750ms, najwyżej128 próbek. Żywa smuga może połączyć kolejne kliknięcia; wygasła/kontekst zakończony nie łączy się ze starym punktem. Lokalny InkCanvas obsługuje WM_SETCURSOR, bez globalnego Cursor.Hide/Show. Normalne puszczenie, zmiana narzędzia, wyjście z płótna, ukrycie warstwy, utrata capture/fokusu i zamknięcie przywracają kursor. Domknięto także utratę fokusu samego płótna przy nadal aktywnym PPM. Timer33ms działa wyłącznie podczas aktywności/wygasania i invaliduje obszar smugi. Brak dirty/historii/zapisu/Fit.

## Biblioteka teł

Nowe pliki:
- `Library/Backgrounds/background-Nazwa.json`: format1, stabilne ID, nazwa, kolejność i SourceId oraz pełne parametry kadru/krycia/nasycenia/rozmycia.
- `Library/Backgrounds/Sources/source-SHA256.json`: niezmienny pełny obraz, współdzielony przez wpisy. Wykorzystano istniejące przechowywanie/porównywanie źródeł; nie zapisujemy bitmapy okna ani ścieżki oryginału.

Z biblioteki odtwarza wszystkie suwaki i prostokąt oraz pełne źródło do dalszej edycji. Cover korzysta z aktualnego płótna. Własna migawka mapy nie ma powiązania live z pozycją biblioteki. Rename/usunięcie/zastąpienie wpisu nie niszczy istniejącej mapy.

Zatwierdzenie nowego/zmienionego tła dopiero wtedy daje wybór: **Zapisz jako nowe tło**, **Nadpisz wybrane tło** (tylko jednoznaczny cel), **Tylko zastosuj do mapy**, **Wróć do edycji**. Nowe wymaga nazwy. Wybór pliku/suwaki niczego nie zapisują, anulowanie głównego dialogu nie tworzy zasobu. Akceptacja całkowicie niezmienionych parametrów lub niezmienionej pozycji biblioteki nie mnoży plików/wpisów. Błąd zapisu pozostawia edytor i wymaga świadomego dalszego wyboru.

Ciemny selektor zapewnia wybór, rename, edytuj/zastąp i usuwanie z potwierdzeniem. Pliki przechodzą przez LibraryFiles: dokładne bajty/ścieżki, jeden wspólny wpis300 z ewentualną zmianą mapy, konflikt bez cichego nadpisania/przesunięcia historii, możliwość ponowienia. Sama biblioteka nie zmienia dirty mapy. Usuwanie omija Kosz, co potwierdzenie wyjaśnia wraz z granicami sesyjnej historii. Reset historii nie usuwa aktywnych wpisów. Niezmienne źródła pozostają dla historii; nie sprzątano ich automatycznie.

MMB w kontrolce kadru przesuwa cały prostokąt, zachowuje wymiary i ogranicza go do obrazu. LPM nadal obsługuje uchwyty i nowy kadr. Capture kończy gest bez skoku. Zaakceptowana geometria tła, cover, statyczność, efekty i Anuluj pozostają.

| Zakres | Limit0.6.1 |
| --- | --- |
| Aktywne wpisy teł / metadane pojedynczego wpisu |128 /1MiB |
| Magazyn teł: źródła, także nieaktywne |64 /192MiB skompresowanych danych; JSON uwzględnia base64 i narzut |
| Pojedynczy obraz |32MiB,4096×4096 — bez zwiększania |
| Źródła mapy / plik mapy |64,96MiB /256MiB — bez zmiany |
| Bufor tła / podgląd źródła |do2048 /1024px — bez zmiany |
| Rysunki / punkty / punkty fragmentu |5000 /200000 /10000 — bez zmiany |
| Punkty historii rysika / wpisy wspólnej historii |2 mln współdzielonych /300 — bez zmiany |

Katalog zachowuje lekkie referencje do źródeł; wybrany wpis przypina potrzebne bajty. Nie trzymamy całej biblioteki rozkodowanej w RAM. Kilka teł tego samego źródła nie powiela źródła na dysku. Mapa deduplikuje je z innymi użyciami dotychczasowym mechanizmem. Bez skanowania oczekujących plików.

## UI i lokalny pomiar panelu

Przybornik3×4 odpowiada zamówieniu, podstawowe narzędzie nazywa się Kursor. Dwie lokalnie rysowane ikony ograniczenia są różne i mieszczą się w przycisku. Selektor grafik ma domyślnie cztery kolumny z rezerwą scrollbara i ograniczeniem ekranu. Lista obiektów ma szerszy domyślny dialog, rozsądniejsze kolumny, ellipsis i tooltip pełnej jawnej nazwy; maskowane wiersze nie dostają tooltipu danych. Scrollbar pozostaje awaryjny.

Instancja: usuwanie na końcu. Grupa: ramka/kolor u dołu, usuwanie ostatnie. Próba okna ujawniła nadmierny rozmiar dolnego przycisku po zmianie kolejności wierszy. Przyczyną były domyślne style brakującego indeksu i GrowOnly utrzymujące wysokość po przejściowym rozciągnięciu. Zapewniono AutoSize dla właściwych indeksów i GrowAndShrink tabeli/przycisków z minimum30px. Pierwsza sama poprawka stylów nie wystarczyła (test zgłosił220px); po domknięciu GrowAndShrink test i zwykłe okno potwierdziły30px.

Lokalny pomiar odróżnił koszt danych/layoutu od natywnego pokazania:

| Etap | Przed poprawką wierszy | Po poprawce |
| --- | ---: | ---: |
| UpdateObjectDetails |16,75ms |17,86ms |
| PositionOverlays |0,20ms |0,23ms |
| Pokazanie poddrzewa |7,54ms |7,61ms |
| Pokazanie rodzica right |147,91ms |139,23ms |
| PaintTree |8,55ms |7,58ms |
| UpdateUI już otwartego panelu |5,60ms |6,32ms |

To pojedyncze lokalne próbki, nie gwarancja czasów ani dowód przyspieszenia o różnicę dwóch wyników. Główny pozostały koszt jest w natywnym Show kompozytowanej gałęzi. Nie wycofano ochrony odrysowania i nie dodano globalnego WS_EX_COMPOSITED, timera, DoEvents lub rekonstrukcji WPF. **Pauza pokazania panelu pozostaje ograniczeniem**; nie deklarujemy pełnej płynności na podstawie końcowych obrazów.

Mozaika pokrywa prostokąt z zapasem dodatkowych rzędów/kolumn poza krawędziami, bez obrysów. Zachowane miniheksy, limit12 reprezentantów i stabilne możliwie równe udziały osobnych pozycji. Repaint nie zużywa generatora malowania.

## Formaty i backlog

Mapa9, preset3, grafika terenu2, ikona2 i wzorzec obiektu1 bez podnoszenia numerów: wykorzystujemy istniejące pola świata/Light/Background. Poszerzono konkretną walidację grubości świata, nie semantykę wszystkich limitów. Nowa biblioteka teł ma własny format1. Brak migracji i kasowania dawnych plików.

[Kolejka importu z folderów](backlog-import-z-folderow.md) ma szczegółowy opis20grafik, opcje Nie teraz/Otwórz folder/Importuj, kolejkę odpowiednich edytorów, Dodaj/Usuń/Pomiń i usunięcie oryginału dopiero po udanym zapisie/weryfikacji. Wszystkie wskazane otwarte decyzje pozostają otwarte. **Nic z tej kolejki nie zaimplementowano**. Bez zakładek, ścian/światła fizycznego i innych funkcji backlogu.

## Testy metod i kontrolek — wykonane

Jawne pojedyncze Case w Run-Patch061Tests, własne dane wyłącznie tests/_work. Bez Run-AllTests, dawnych runnerów, DPI, audytu WPF i dużych benchmarków. Źródła dawnych testów zachowano; dostosowano tylko bezpośrednio zmienione asercje0.6.0 (reguła ukrytych źródeł, ekranowy podgląd/nowa walidacja grubości), bez przedstawiania ich jako ponownie uruchomionych.

- `groups` PASS:14→14 po rzeczywistym CreateObjectGroup; nadpisania/powrót/Undo/Redo/odłączenie i nowa reguła Widoku (`patch-061-groups-0938c65ecac044aeb4a23f6f406b1f0f`). Wcześniejsze próby bazowe kończyły się dopiero na celowo nowej asercji OFF; nie były reprodukcją zgłoszonego błędu.
- `outside` PASS: cache pusty poza mapą, CreateObjectGroup i realna ścieżka ruchu0→14, Undo0, Redo14 (`patch-061-outside-f30da8428ac44d0a97f49973756726ef`).
- `acceptance` PASS:15×10,mieszana maska, grupowanie poza mapą, ruch Kursor/Rączka; błąd nieodtworzony (`patch-061-acceptance-6c089b539f1a42eca5ff334683389c7d`).
- `size-pointer` PASS:1,46/0,73, wspólny rozmiar/200, świat20000 round-trip, częściowe wymazanie i dokładna historia, brak edycji przez pointer, wygaśnięcie750ms, uchwyt zależny od drogi (`patch-061-size-pointer-1fd699e3186949bfbcbff0309e34d26a`).
- `pointer-focus` PASS: utrata fokusu płótna przywraca kursor, kończy źródło/smugę/timer bez dokumentu/historii (`patch-061-pointer-focus-b269a482f2d44a56828146e3902d374c`).
- `backgrounds` PASS: źródło i wszystkie parametry, rename bez dirty/duplikatu, Undo/Redo pliku, usunięcie bez utraty mapy, konflikt odtworzenia/ponowienie, round-trip bez oryginału; zdarzenia kontrolki MMB zachowują wymiary (`patch-061-backgrounds-7628618562234447a31ae874fe912439`).
- `background-controls` PASS: ścisły odczyt metadanych, miniatura/życie Bitmap, odtworzenie suwaków, pojedyncze zastosowanie i no-op bez duplikatu (`patch-061-background-controls-f2f7a0c2fab54948a4170cb40261e264`).
- `background-overwrite` PASS: nadpisanie tego samego ID/pliku, wspólne dokładne Undo/Redo mapy/bajtów, jeden źródłowy plik, Anuluj bez zmian (`patch-061-background-overwrite-1d90e74753044aa38fc33412e62919dd`).
- `presentation` PASS: stabilne równe przypisania, mozaiki obu orientacji, kolejność narzędzi i dolne akcje; kontrolka selektora4kolumny (`patch-061-presentation-853059fa575d48ce8904b30a8e7392a5`). Pierwszy build tej próby zatrzymała kolizja nazwy Screen; poprawiono pełną nazwą WinForms przed PASS.
- `panel-layout` PASS po opisanej poprawce: dolny przycisk30px, właściwy AutoSize (`patch-061-panel-layout-14fa4c25acb848629a55cd69f1fdbef8`). `panel-profile` dostarczył powyższe pomiary (`...24fe47c633bd461e95b620367d0af408`, po zmianie `...42f64f6237b942e9a5e6c5c1fc58c3e2`).
- Runner bez Case sprawdzony raz: błąd przed buildem/próbami. Nie uruchomiono wszystkich przypadków. Fixture służy wyłącznie do stworzenia własnej mapy wejściowej i nie jest dodatkową zaliczoną próbą interfejsu.

## Rzeczywiste próby zwykłego EXE

Własna kopia w ui-061-bbd1b2e0099b4bd1ad7dccff6a404aaa. Operacje na rzeczywistych oknach przez Computer Use, bez prywatnej biblioteki. Poszczególne przyrosty nie zostały ponownie testowane bez zależnej zmiany:

- Kandydat0.6.1 SHA362AC69E…: Rysik40 przy146%, oddalenie/reset100%, nowa kreska40. Starsza zwęziła się ze światem; zapisane szerokości świata27,48483 i40. Import własnego tła, LPM wyznaczający prostokąt, suwaki49/156/5, Zatwierdź→nowe→nazwa, widoczny wynik. Wybór z biblioteki odtworzył kadr/suwaki; niezmieniona akceptacja nie pytała o kolejny zapis. Mapa z kreskami i tłem zapisana we własnym folderze.
- W tym kandydacie:15×10PointyTop,pula maski kolor+grafika; prostokąt wybiera trójkę poza siatką, Grupuj wybrane, Rączka przenosi nad mapę. Oba indywidualne zasięgi nadal działają. Widok Obiekty OFF przywrócił bazę, ON oba zasięgi; menu pozostało otwarte. Panel grupy/pojedynczego członka pokazał nowe nazwy i dolne akcje; tu zauważono i następnie naprawiono zbyt wysoki przycisk.
- Po tej zależnej poprawce: kopia SHA3BD56CCA… ponownie odczytała własną zapisaną grupę; oba zasięgi działały, panel grupy/instancji potwierdził kompaktowy dolny przycisk. Nie powtarzano importu/puli i niezmienionej logiki rozmiaru wyłącznie z powodu przebudowy.
- Ostateczny EXE SHA74C3BEBF… (ostatnia zmiana dotyczyła wyłącznie zakończenia wskaźnika na LostFocus): po restarcie wybrano tło z własnej biblioteki do pustego dialogu nowej mapy. Przywrócił kadr oraz **krycie49%, nasycenie156%, rozmycie5px**. Anuluj pozostawiło czystą mapę bez tła/gwiazdki. Krótka kontrola pustej listy: wszystkie nagłówki i racjonalne szerokości mieszczą się w domyślnym dialogu na dostępnym ekranie; nie była to ponowna kampania wierszy/sortowania/maskowania. Przybornik3×4 i różne ikony masek widoczne.

Końcowa kopia została zamknięta bez odrzucania danych, a brak jej okna potwierdzono aktualną listą okien. Nie zamykano okien użytkownika. Żadne usuwanie danych przez interfejs nie było potrzebne do tych prób, więc nie ma wstrzymanej prośby o zgodę.

## Niewykonane próby i ograniczenia

- Nie odtworzono krytycznego zgłoszenia grup; dalsza diagnoza odłożona za wyraźną zgodą użytkownika, bez fikcyjnej przyczyny/obietnicy naprawy.
- Pozostaje mierzalna pauza natywnego pokazania panelu. Końcowe zrzuty nie dowodzą płynności przejścia klatka po klatce.
- Sky.drag nie udostępnia parametru przycisku: brak wiarygodnej ciągłej próby PPM komety oraz MMB przeciągania kadru w zwykłym EXE. Zweryfikowano zdarzenia/metody, lecz nie zaliczono tych gestów wizualnie. To samo dotyczy subiektywnej płynności.
- Nie wykonano pełnej macierzy DPI/zoomów, wszystkich decyzji plikowych przez okna, benchmarków, audytu starych funkcji lub pełnej przenośności. Round-trip metod bez źródła nie jest kampanią przeniesienia do czystego komputera; ta pozostaje świadomie odłożona.
- Ostateczna zmiana LostFocus nie wpływała na wcześniej zaliczone rysowanie/kadrowanie/źródła, więc nie ponawiano całych scenariuszy. Raport jawnie rozróżnia kandydatów i końcowy artefakt.

## Checklista odbiorcza

- [ ] Własna trójka poza mapą: grupuj, przesuń; Indywidualnie, wspólne włączenie/promień, powrót, Undo/Redo; w razie ponowienia błędu podaj dokładne kroki.
- [ ] Widok Obiekty OFF/ON usuwa/przywraca udział źródeł; Fog i ręcznie usunięta baza nie cofają się.
- [ ] Wspólny rozmiar5→40→200 w Rysiku/Gumce; nowe kreski przy dwóch zoomach, stare skalują się; częściowa gumka i historia.
- [ ] Uchwyty poziome obu pól: brak zmiany bez ruchu, prawo/lewo zależnie od drogi, wpisywanie działa.
- [ ] PPM: pełne koło bez obwódki, kometa750ms, kursor wraca, żywe kliknięcia łączą się bez starego łącznika po wygaśnięciu.
- [ ] Tło: nowe / jawne nadpisanie / tylko mapa / powrót / Anuluj; brak nowych wpisów po no-op.
- [ ] Wybór tła po restarcie odtwarza pełne źródło, kadr i wszystkie suwaki; MMB przesuwa cały prostokąt.
- [ ] Historia tła przywraca plik i mapę; konflikt nie nadpisuje danych i pozwala ponowić.
- [ ] Przybornik3×4, cztery kolumny grafik, pełna mozaika, dolne kompaktowe usuwanie i szersza lista.
- [ ] Krótki własny zapis/odczyt nowych parametrów; przenośność do czystej kopii nadal świadomie odłożona.

## Git

Kod, EXE i testy: **`0bcbee21af27931bd9e9a9c466997ded5936ed22`**. Raport, README, specyfikacja, plan i backlog zapisano w osobnym końcowym commicie dokumentacji. Gałąź **master**, po zatwierdzeniu dokumentacji i końcowym sprawdzeniu stan roboczy czysty. Testowe artefakty tests/_work pozostają ignorowane; nie dodano Library ani map użytkownika. Bez push/tagu/Release'a. Nie deklarujemy kompleksowego przetestowania aplikacji.
