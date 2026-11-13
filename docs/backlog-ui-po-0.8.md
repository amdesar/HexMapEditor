# Uzgodnienia przyszłego UI po 0.8

Wyłącznie backlog; żaden z poniższych systemów nie jest wdrożony w 0.8.0. Pozostały [plan wersji](plan-wersji.md), [ustawienia lokalne](backlog-ustawienia-lokalne.md), backlog paneli i [kolejka importu](backlog-import-z-folderow.md) pozostają.

## Powiadomienia po zakładkach

Po etapie zakładek, bez ustalonego numeru patcha: niemodalne toasty, np. autozapis w prawym dolnym rogu, zamykalne i wygasające po 5–7 sekundach. W 0.8.0 pozostaje status autozapisu; nie ma nowego systemu toastów ani timerów do nich.

## Kopiowanie/wklejanie po 1.0

Wstępnie obiekty; dokładny zakres po rozmowie z GM. Nie dodajemy teraz Clipboard, skrótów kopiowania obiektów ani duplikowania zasobów.

## Biblioteka i kolejka 0.12.0

Docelowo widoczne źródłowe PNG/JPG/GIF i inne obsługiwane obrazy w folderach bibliotek, metadane oddzielnie; wspólnie zaprojektować z kolejką importu. Unikać zbędnych kopii źródeł. Wymaga rozstrzygnięcia rename/delete/replace, fizycznej deduplikacji, rozróżnienia nowego pliku i używanego zasobu oraz granic historii/konfliktów. Mapa i preset nadal osadzają potrzebne pełne źródła. Nie zmieniamy struktury Library i nie migrujemy prywatnych danych w 0.8.0.

## Zakładki 0.11.0

Na dole, niezależne mapy; obok krzyżyków do rozważenia menu zamykania wybranej karty. Bez placeholderów menu lub przycisków przed wdrożeniem zakładek.

## Uzupełnienie po odbiorze0.8.1

- 0.9.0: niebieska ramka aktywnego narzędzia w lewym przyborniku, spójna z rzeczywistym ChoiceStyle Jednolity/Gradient. Obecnego wyboru narzędzia nie zmieniamy w0.8.1.
- 0.11.0: niezależne mapy w zakładkach; lokalizacja zakładek do ponownej oceny na tym etapie (wcześniejszy dół jest wcześniejszą propozycją, nie wymuszeniem implementacji).
- 0.12.0: foldery rzeczywistych źródeł i kolejka importu; dopiero wtedy porządkowanie menu Biblioteka według zatwierdzonych folderów użytkowych z grafikami. Teraz nie usuwać kategorii na podstawie domysłów.
- Po1.0: konfiguracja skrótów, wiele alternatywnych kombinacji dla jednego polecenia oraz kontrola konfliktów. Bez narzucania Ctrl+Shift+Z; w0.8.1 ten skrót nadal nie uruchamia historii.

Pozostałe backlogi i etapy planu zachowane, bez implementacji powyższych funkcji.
