# HexMapEditor 0.13.0 — plan

- Baza `58058fe`, lokalny `origin/master` zgodny, indeks i katalog roboczy czyste.
- Oddzielić binarne źródła od metadanych i wykorzystać obecne edytory, ochronę zapisu i operacje bibliotek. Dotychczasowe pliki zachować bez migracji.
- Dodać asynchroniczny skan czterech folderów Import, decyzje kolejki i bezpieczne zakończenie importu po zapisie katalogu. Utrzymać import poza dokumentem i presetem.
- Sprawdzić istniejące tabele źródeł Map12 na przeniesionej mapie i zmierzyć zapis oraz odczyt przed decyzją o formacie.
- Po próbach przygotować EXE, Setup, Portable, raport i jeden lokalny commit. Pliki robocze wyłącznie w `tests/_work`.

Magazyn, biblioteczne metadane i cztery wejścia kolejki przeszły przypadki `library`, `queue`, `map`. Próbę kolejki powtórzono po dodaniu porównania bajtów mapy i presetu. Publiczne v0.12.4 i oba załączniki zwracają HTTP 200, v0.13.0 zwraca HTTP 404.

Pomiar dwóch kompilacji pokazuje zgodny Map12 po znormalizowaniu pola wersji. Powitanie, anulowanie kadru i otwarcie oraz zapis samodzielnej mapy w świeżym Portable sprawdzone. Własne okna testowe zamknięte. Paczki i wyniki są w `tests/_work/patch-0130-release`. Online pozostaje do sprawdzenia po publikacji 0.13.0.
