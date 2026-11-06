# Testy lokalne

Źródła i skrypty pozostają w Git. Każdy przebieg tworzy własne EXE/dane tylko w `tests/_work/<rodzaj>-<GUID>/`. Logi są ignorowane; procesy należy zakończyć przed usunięciem katalogów. Nie używamy użytkowego Library, głównego bin ani globalnego TEMP/TMP.

## 0.3.0 — selektywnie

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