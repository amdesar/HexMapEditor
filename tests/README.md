# Testy lokalne

Źródła i skrypty pozostają w Git. Każdy przebieg tworzy własne EXE/dane tylko w `tests/_work/<rodzaj>-<GUID>/`. Logi są ignorowane; procesy należy zakończyć przed usunięciem katalogów. Nie używamy użytkowego Library, głównego bin ani globalnego TEMP/TMP.

## 0.3.1 — pięć krótkich scenariuszy do wyboru

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