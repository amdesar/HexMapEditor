# Plan 0.4.2

Baza `4c0a7ff`. Zastane pliki `icon.ico` i `icon.png` są materiałami użytkownika do wdrożenia. ICO 256×256 i PNG 1254×1254 odczytano lokalnie.

1. Osadzić ICO i publikować wynik kompilacji atomowo. Dodać BAT do przebudowy.
2. Oddzielić bibliotekę stanów od mapy. Podłączyć pliki, definicje i mgłę do wspólnej historii. Filtrować stany używane w pliku.
3. Dodać ramki i łączenie grup, obliczać widoczność geometrycznie. Uporządkować aktualizacje panelu i routing poleceń.
4. Zbudować EXE, wykonać selektywne sprawdzenia i opisać zakres w raporcie.

## Punkt wznowienia

Implementacja, celowane próby states/groups/controls/render oraz build przez BAT zakończone. Sprawdzono ikonę osadzoną w zwykłym EXE, dodanie stanu, łączenie grup, listę i przełącznik Dopasuj. Wyniki i ograniczenia są w raporcie.

Jedyny oczekujący dialog jest w własnym oknie z katalogu tests/_work/patch-042-e3b7a71b9a384f10916c69b7ee5b720a. Chodzi o zatwierdzenie usunięcia „Próba usunięcia 0.4.2” i natychmiastowe Ctrl+Z. Wysłano pytanie o zgodę w chwili akcji. Po odpowiedzi należy odświeżyć widok dialogu, wykonać zatwierdzoną próbę i uzupełnić raport. Zaliczonych prób nie trzeba powtarzać.
