# Plan 0.2.2

Baza: czysty master fd23a23947217e166074f1834f7b89d3746be6a8, zgodny z origin/master po fetch. Bez push, Release'a i zmiany formatu mapy 2 (odczyt 1).

1. Biblioteka: sprawdzić rzeczywiste pliki, wdrożyć jawny katalog Terrains/Presets oraz migrację z bajtowymi kopiami w Presets/_backups. Poprawny kolidujący default odzyskać z nowym ID i nazwą, zachowując ID/wersje terenów; nie powielać równoważnej kopii. Konflikty niejednoznaczne i uszkodzenia zgłaszać, bez zmiany źródła. Najpierw syntetyczne regresje, potem jednorazowa migracja prywatnych plików (bez użycia ich jako testów).
2. Przepływy presetów: Nowy zestaw od razu zapisuje neutralny preset po nazwaniu; pierwszy zapis starego niezapisanego wpisu zastępuje wpis roboczy zamiast dublować. Zapisana kopia i Domyślny wymagają nowego ID przy Zapisz jako nowy. Jawne usuwanie własnego pliku do Kosza, bez trwałego zastępstwa, z zachowaniem danych mapy i oddzieleniem plików od Undo.
3. Paleta: jedna kolumna, stałe zewnętrzne akcje, stabilne kontrolki, skracanie długich nazw. Nazwę projektu pozostawić na dotychczasowym miejscu.
4. Odtworzyć ciągłe zwężanie panelu; sprawdzić odsłanianie canvas i współpracę ElementHost/WPF, poprawić odrysowanie odkrywanego obszaru bez przebudowy edytora/opóźnienia. Zmierzyć/rysować całe etykiety wewnątrz bez zawijania i z jawnym progiem ukrywania całości. Przycisk: Osie / W heksach / Brak.
5. Przestrzenny rozmiar i rozłączne grupy danych: teren, tekst, teren+tekst; pełne koordynaty w przewijaniu, zachowane atomowe wykonanie, domyślne Nie.
6. Menu kontekstowe opisu: istniejąca ścieżka Paste(true), wyłącznie Unicode tekst, jedna historia opisu, niedostępne bez tekstu; zachowane standardowe akcje i formatowane wklejanie.
7. Stare i nowe regresje, końcowy zwykły EXE w izolowanym tests/_work (prywatna biblioteka tylko migracja), próby okna, README/specyfikacja/raport, lokalny commit.

Format presetu pozostaje 1: zmieniają się lokalizacja i tożsamość odzyskanej kopii, nie schemat. Migracja nie zmienia map ani ich wariantów. Pliki dostarczone ręcznie czytane po restarcie, bez rekurencji. W przyszłości: zakładki projektów z własnymi danymi/ścieżkami/stanem, kamerą/zaznaczeniem i wszystkimi historiami (bez resetu przy przełączeniu), ochrona zamykania; bez przypisania do konkretnej wersji. Poza przyrostem: przebudowa edytora/stanu formatowania/tabel oraz treści i wyglądu statusu presetów.

## Stan końcowy

Kroki 1–6 wdrożono. Krok 7: wszystkie 104 regresje zakończone kodem 0, zwykły główny EXE 0.2.2.0 zbudowany; identyczną kopię sprawdzono interaktywnie z własną biblioteką. Wykonano i zweryfikowano migrację 4 prywatnych plików; drugi odczyt: 0 migracji, 5 zestawów, brak ostrzeżeń. Kopie bajtowe pozostały w lokalnym Presets/_backups, bez ponawiania po wznowieniu.

Próby okna, diagnostyka odrysowania i ograniczenia są szczegółowo rozdzielone w `raport-0.2.2.md`. Ciągły ruch wielu klatek pozostaje do oceny ręcznej (automat drag dostarczał głównie końcowy MouseMove); test metod sprawdza Paint przed puszczeniem po każdym z 30 ruchów. Nie wykonano Worda, większego DPI ani niedostępnego Kosza. Nie oznaczamy tych prób jako zaliczonych.

README/specyfikacja/raport uzupełnione. Własne mapy i kopie EXE po zamknięciu procesów sprzątnięto z przebiegu UI oraz diagnostyki panelu; pozostawiono ignorowane logi. Nie ruszano prywatnych map, kopii migracji ani wcześniejszych śladów 0.1.4. Lokalny commit po kontroli przygotowanych plików; bez publikacji.
