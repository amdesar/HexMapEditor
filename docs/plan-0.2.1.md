# Plan poprawki 0.2.1

Baza: 5a88fae, czysty master zgodny z origin/master po fetch. Format mapy pozostaje 2, odczyt 1. Bez nowych zależności, push i Release'a.

1. Pasek w jednym rzędzie: ograniczone grupy boczne i elastyczny środek z wielokropkiem. Dopasuj bez stałych limitów skali, z prostokątem marginesów/etykiet. Koło zgodne ze skalą po dopasowaniu.
2. Prawy panel: zapamiętywana w sesji szerokość, uchwyt z własnym capture/resetem gestów, reset szerokości. Bez ruchu kamery.
3. Paleta: odtworzenie przebudowy w pomiarze kontrolek/layoutu, aktualizowanie istniejących kafelków, wstrzymanie układu podczas rzeczywistych zmian; kafelki 52 zamiast 65 px, Dodaj nad nimi, akcje hover/fokus i klawiatura.
4. Wspólna historia: migawki roboczych palet i aktywnego wyboru w istniejącym EditAction, wraz z opcjonalnymi zmianami heksów. Migawki kopiowane, stabilne warianty przy Redo. Zapis biblioteki nigdy nie jest odtwarzany historią.
5. Presety: chroniony ID default niezależnie od nazwy; stare pliki o tym ID nie zastępują wzorca. Jawny wybór nadpisania własnego zapisanego lub nowej kopii z nowym ID, walidacja kolizji i atomowy zapis. Oddzielny wskaźnik różnic roboczej palety względem biblioteki; zapis presetu nie zapisuje projektu.
6. Rozmiar: jeden walidowany plan czterech zmian względem starego prostokąta, zakaz wycięcia całej starej osi, lista unikalnych usuwanych indeksów/koordynatów. Jedna historia i przewijane ostrzeżenie, domyślnie Nie.
7. Regresje, zwykły końcowy EXE, dokumentacja i raport. Do prób zapisu presetów użyjemy identycznej kopii końcowego EXE w tests/_work, z prywatną biblioteką testową obok tej kopii; bez zmian użytkowego Library. Testy metod i UI raportujemy oddzielnie.

Odłożone: wygląd całego paska edytora opisu, stany formatowania, zgłoszone pogrubienie przy pustym zaznaczeniu, wysokość/przewijanie pustego edytora, paleta jednokolumnowa i nieomówione funkcje. Nie oznaczamy ich jako naprawionych.

Stan końcowy 6 listopada 2026: kroki 1–7 ukończone. Końcowy build 0.2.1.0, format nadal 2; 90/90 regresji (11 historia, 12 techniczne, 11 gesty, 35 funkcje, 21 poprawka). W rzeczywistych oknach sprawdzono pasek, minimum/duże okno, skalę >170%, uchwyt/reset, paletę i zapis własnej kopii presetu, chroniony Domyślny, dwa Undo/Redo oraz cztery pola rozmiaru i odmowę ostrzeżenia z A1. Naprawiono też wykryte podczas prób obcinanie nazw kafelków, zawijanie nazwy projektu i stary poziomy obszar przewijania po zwężeniu panelu. Raport `raport-0.2.1.md` rozdziela testy metod od prób okna oraz wskazuje niewykonane próby Worda/DPI i zatwierdzonego usuwania w UI. Bez push/Release'a.
