# Testy lokalne

Źródła i skrypty pozostają w Git. Każdy przebieg tworzy własne EXE/dane tylko w `tests/_work/<rodzaj>-<GUID>/`. Logi są ignorowane; procesy należy zakończyć przed usunięciem katalogów. Nie używamy użytkowego Library, głównego bin ani globalnego TEMP/TMP.

## 0.4.0 — nowe, selektywne scenariusze

Run-Object040Tests.ps1 wymaga jednego Case, bez automatycznego uruchamiania wszystkich. Własne kompilacje/mapy/grafiki/logi tylko tests/_work/object-040-GUID; po zakończeniu pozostaje results.log.

- geometry: przenośny format 4, deduplikacja wspólnej grafiki ikony/obiektów, obiekt poza mapą, odczyt bez biblioteki i bez ponownego odkrywania, odmowa błędnego obiektu, offsety i mieszany resize w obu orientacjach z Undo/Redo.
- gestures: jeden obiekt na klik, bez drag-stamp i wiadra; promień 0/1, strona/checkbox/rozmiar, droga i jedno Undo/Redo z zachowaniem wcześniejszej mgły, blokada, prostokąt/lasso/Ctrl i hit-test ukrytego obiektu.
- library: czytelne pliki obiektów, edycja z/bez aktualizacji instancji, exact-byte Undo/Redo/usunięcie, konflikt odtworzenia i ponowienie, kopiowana grafika/niezależne ID, brak materiału, Pokaż i reset bez kasowania biblioteki.
- view: piksele przycięcia części grafiki przez mgłę i widoczność poza mapą, proporcje, rzeczywiste bounds Dopasuj wszystko i równoważność bez obiektów, grupa z zachowaniem offsetów i blokady, brak łącznika nieznanej drogi, ograniczenie próbkowania poza mapą i Undo orientacji.
- input: tylko nowa bezpośrednia zależność skrótów: Delete nie zabiera edycji tekstu ani dawnego skrótu terenu, usuwa instancję z płótna i działa Undo; zamykany dialog/pozostałość dwukliku nie umieszcza obiektu.

Create-Object040Ui.ps1 przygotowuje zwykły EXE i własną mapę/bibliotekę, nie testuje okna. Faktyczne próby i ograniczenia w docs/raport-0.4.0.md. Nie uruchamiano Run-AllTests ani starych zestawów. Historyczne fixture/oczekiwania formatu 3 nie są automatycznie wymaganiem zachowania odczytu 3 w 0.4.0; źródeł nie usunięto i nie dodano konwertera dla ich zaliczenia.
## 0.3.2 — trzy celowane scenariusze

Run-Patch032Tests.ps1 wymaga jednego Case: controls, library albo presets. Nie uruchamia wszystkich ani dawnego zestawu. Dane i kompilacje są w osobnym tests/_work/patch-032-GUID; po zakończeniu pozostaje tylko results.log.

- controls: krok jednego wiersza, clamp i równa/odwrócona geometria strzałek; świadomy materiał po mgle, zachowanie zwykłego Wiadra i innych narzędzi/ustawień; refresh bez autoswitch; współdzielony selektor oraz lokalny podgląd/anulowanie edycji bez zmiany głównego stanu.
- library: nowy pusty katalog ma pięć czytelnych nazw startowych; ponowny odczyt nie odtwarza usuniętej pozycji ani nie resetuje znacznika; pojedynczy kontrolny Undo/Redo ikony jako bezpośrednia zależność wyodrębnionego LibraryFiles.
- presets: kopia Domyślnego, nowy neutralny preset, nadpisanie i robocza nazwa/zapis przemianowania, usunięcie z dokładnymi plikami/stanu Undo/Redo, brak fałszywej gwiazdki i uszkodzenia heksów, konflikt ścieżki i ponowienie, zewnętrzna zmiana/zablokowany plik, reset historii po sukcesie bez usunięcia aktywnych plików.

Create-Patch032Ui.ps1 buduje zwykły EXE i własną mapę/preset w tests/_work/ui-032-GUID. Nie używa prywatnej biblioteki i nie jest automatycznym testem okna. Rzeczywiste próby i ograniczenia: docs/raport-0.3.2.md. Historyczne testy usuwania presetów przez callback Kosza i oczekiwania braku plikowego Undo odzwierciedlają poprzednią wersję; zachowano je bez uruchamiania. Nie przywracamy starej semantyki tylko dla ich zaliczenia.
## 0.3.1 — pięć krótkich scenariuszy do wyboru (historia)

`Run-Patch031Tests.ps1 -Case regions|controls|icons|import|names` wymaga **jednego** konkretnego Case, bez automatycznego uruchomienia wszystkich. Każda próba jest osobnym przebiegiem w _work i zostawia tylko results.log.

- regions: plamy mgły/nazwa/kolor/stan, przeciąganie trzech wiader i jeden wpis historii;
- controls: Domyślny/API/F2/Delete, edytowalna kopia, Punkt i złota ramka, granice strzałek, kompensacja końcowa płótna, krótki odcinek szybkiego pędzla i reset po wyjściu;
- icons: logiczne ID i dedupe, edycja z aktualizacją/bez, nazwa/podmiana/usunięcie i Undo/Redo, pliki/kolizje/konflikty/IO, stary płaski JSON tylko przy świadomej edycji;
- import: własny PNG, Undo/Redo importu, metadane w zapisie/odczycie formatu 3, historia po błędnym otwarciu i reset po udanym Nowa/Otwórz bez usuwania aktywnych plików.

- names: bezpieczne litery/nazwy/ścieżki i zachowany prefiks icon- także przy odmowie skrajnie długiej kolizji.

Create-Patch031Ui.ps1 przygotowuje zwykły EXE i własną mapę z długą paletą oraz bibliotekę w osobnym ui-031-GUID. Nie jest testem wykonanym na oknie. Wyniki rzeczywistych prób i ograniczenia są w docs/raport-0.3.1.md. Źródła programu są teraz w src; skrypty korzystają ze wspólnego Build.ps1. Nie uruchomiono historycznych zestawów tylko z powodu przenosin. Pomocnicze fixture/oczekiwania FogIconTests dostosowano do LibraryIcon, nieedytowalnego Domyślnego i strzałek; nie deklarujemy ich ponownego wykonania. Historyczne testy edycji Domyślnego wymagają własnej kopii według nowych wymagań, nie przywrócenia zabronionej edycji wzorca.
## 0.3.0 — selektywnie (historia poprzedniego przyrostu)

Nie uruchamiaj Run-AllTests ani równoważnika pełnej kampanii bez osobnego wyraźnego polecenia użytkownika. Zmiana wspólnego modelu nie znosi tej zasady. Nie powtarzaj zaliczonej próby bez istotnej zmiany/błędu/niepewnego wyniku.

`Run-FogIconTests.ps1 -Case "prefiks"` wybiera jeden przypadek według początku nazwy. Brak dopasowania kończy się błędem. Dostępne prefiksy:

- `eight deterministic` — osiem geometrii, pierścienie/obrys i osie w obu orientacjach;
- `icon overlap` — gest, wspólne Undo/Redo, zachowanie tekstu i ogon dialogu;
- `fog gesture` — stały tryb, maskowanie i zablokowana edycja szczegółów;
- `icon bucket` — mały region ikon i mgły, różne predykaty na wspólnej kolejce;
- `portable format` — import bez blokady, tylko użyte zasoby, odczyt bez źródła i odrzucenie uszkodzonego/starego pliku bez zmiany projektu;
- `crop warns` — ikona/mgła w ostrzeżeniu, jedno liczenie, ID i historia;
- `rendering hides` — renderowanie nieprzezroczystej mgły i ikony;
- `minimum sidebar` — minimalny układ kontrolek, osobne przewijanie, pasek i Dopasuj;
- `preset json` — `.json`, polskie nazwy, kolizja i świadome przemianowanie jednego pliku.

Bez filtra runner obejmuje tylko powyższe nowe przypadki; nie wywołuje dawnych zestawów. Preferuj jednak pojedynczy potrzebny przypadek. Runner usuwa własne wyniki poza results.log. To testy metod/kontrolek z odwołaniami przez refleksję, nie pełne próby interaktywnego okna. `Create-LayersUi.ps1` tworzy osobną kopię zwykłego EXE i własny PNG do takich prób. Rzeczywisty zakres wykonanego sprawdzenia: `docs/raport-0.3.0.md`.

## Testy historyczne

Zachowano istniejące źródła i skrypty 0.1.x–0.2.3, w tym Run-AllTests. Nie uruchomiono ich ponownie w zadaniu 0.3.0. Założenia o odczycie/migracji map formatu 1/2 i nazwach `.terrainset.json` są **nieaktualne dla 0.3.0**: wymagania świadomie znoszą tę kompatybilność. Dawne fixture generators tworzące format 2 nie są generatorami map 0.3.0. Historyczne testy migracji pozostają świadectwem poprzednich wydań, a nie wymaganiem dodania fallbacku. Zestaw zbiorczy nie jest obecnie deklarowany jako zgodny ani zaliczony dla formatu 3; przegląd jego poszczególnych oczekiwań wymaga osobnego zakresu testowania.
## 0.4.1 — trzy celowane scenariusze

Run-Groups041Tests.ps1 wymaga jednego Case: model, fog albo controls. Nie wywołuje starych zestawów ani Run-AllTests. Osobny tests/_work/groups-041-GUID; po procesie pozostaje results.log.

- model: początkowa grupa bez modyfikacji bazy, Ctrl/Alt, własne stany i nieinwazyjne nakładki, odłączanie (kategorie/keep/restore/cancel), rozwiązanie, czyszczenie referencji po Delete, Undo/Redo i przenośny format 5 bez biblioteki; odmowa wadliwego członkostwa.
- fog: efektywny własny stan/grupa, ochrona aktywnego zasięgu przed ręcznym wiadrem i końcowa delta, ruch Rączką bez przebudowy heksowego panelu, Undo bez utraty dawnych odkryć, preview/odmowa/zgoda, maskowanie listy i zapis rzeczywistej mgły przy podglądzie.
- controls: scalanie nazwy i wpisywanego tekstu liczbowego, dyskretny checkbox i gwiazdka, wspólny kadr (także efektywny rozmiar grupy), pusty klik, osobne narzędzia, nazwy instancji niezależne od edycji wzorca oraz aktywność kosza.

Create-Groups041Ui.ps1 przygotowuje zwykły EXE i własną mapę w ui-041-GUID, nie jest testem interaktywnego okna. Faktyczne próby i ograniczenia: docs/raport-0.4.1.md. Historyczne fixture/oczekiwania formatu 4 pozostają w Git; nie są wymaganiem konwersji do 5 i nie zostały ponownie uruchomione. Nie uruchamiać całej kampanii z powodu zmiany modelu.
