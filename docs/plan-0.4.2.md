# Plan 0.4.2

Baza `4c0a7ff`. Zastane pliki `icon.ico` i `icon.png` są materiałami użytkownika do wdrożenia. ICO 256×256 i PNG 1254×1254 odczytano lokalnie.

1. Osadzić ICO i publikować wynik kompilacji atomowo. Dodać BAT do przebudowy.
2. Oddzielić bibliotekę stanów od mapy. Podłączyć pliki, definicje i mgłę do wspólnej historii. Filtrować stany używane w pliku.
3. Dodać ramki i łączenie grup, obliczać widoczność geometrycznie. Uporządkować aktualizacje panelu i routing poleceń.
4. Zbudować EXE, wykonać selektywne sprawdzenia i opisać zakres w raporcie.

## Punkt wznowienia

Implementacja, celowane próby states/groups/controls/render oraz build przez BAT zakończone. Sprawdzono ikonę osadzoną w zwykłym EXE, dodanie stanu, łączenie grup, listę i przełącznik Dopasuj. Wyniki i ograniczenia są w raporcie.

Próbę usunięcia i natychmiastowego Ctrl+Z zakończono 8 listopada 2026 po ponownym uruchomieniu komputera. Odtworzono scenariusz na jednoobiektowej mapie w tests/_work z kopią końcowego EXE. Po udanej próbie zamknięto okno, usunięto katalog tego przebiegu i uzupełniono raport.
