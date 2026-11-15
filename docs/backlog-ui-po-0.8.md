# Uzgodnienia przyszłego UI po 0.8

Wyłącznie backlog; żaden z poniższych systemów nie jest wdrożony w 0.8.0. Pozostały [plan wersji](plan-wersji.md), [ustawienia lokalne](backlog-ustawienia-lokalne.md), backlog paneli i [kolejka importu](backlog-import-z-folderow.md) pozostają.

## Powiadomienia po zakładkach

Po etapie zakładek, bez ustalonego numeru patcha: niemodalne toasty, np. autozapis w prawym dolnym rogu, zamykalne i wygasające po 5–7 sekundach. W 0.8.0 pozostaje status autozapisu; nie ma nowego systemu toastów ani timerów do nich.

## Kopiowanie/wklejanie po 1.0

Wstępnie obiekty; dokładny zakres po rozmowie z GM. Nie dodajemy teraz Clipboard, skrótów kopiowania obiektów ani duplikowania zasobów.

## Biblioteka i kolejka 0.13.0

Docelowo widoczne źródłowe PNG/JPG/GIF i inne obsługiwane obrazy w folderach bibliotek, metadane oddzielnie; wspólnie zaprojektować z kolejką importu. Unikać zbędnych kopii źródeł. Wymaga rozstrzygnięcia rename/delete/replace, fizycznej deduplikacji, rozróżnienia nowego pliku i używanego zasobu oraz granic historii/konfliktów. Mapa i preset nadal osadzają potrzebne pełne źródła. Nie zmieniamy struktury Library i nie migrujemy prywatnych danych w 0.8.0.

## Zakładki 0.11.0

Na dole, niezależne mapy; obok krzyżyków do rozważenia menu zamykania wybranej karty. Bez placeholderów menu lub przycisków przed wdrożeniem zakładek.

## Uzupełnienie po odbiorze0.8.1

- 0.9.0: niebieska ramka aktywnego narzędzia w lewym przyborniku, spójna z rzeczywistym ChoiceStyle Jednolity/Gradient. Obecnego wyboru narzędzia nie zmieniamy w0.8.1.
- 0.11.0: niezależne mapy w zakładkach; lokalizacja zakładek do ponownej oceny na tym etapie (wcześniejszy dół jest wcześniejszą propozycją, nie wymuszeniem implementacji).
- 0.13.0: foldery rzeczywistych źródeł i kolejka importu; dopiero wtedy porządkowanie menu Biblioteka według zatwierdzonych folderów użytkowych z grafikami. Teraz nie usuwać kategorii na podstawie domysłów.
- Po1.0: konfiguracja skrótów, wiele alternatywnych kombinacji dla jednego polecenia oraz kontrola konfliktów. Bez narzucania Ctrl+Shift+Z; w0.8.1 ten skrót nadal nie uruchamia historii.

Pozostałe backlogi i etapy planu zachowane, bez implementacji powyższych funkcji.
# Obserwacja po odbiorze 0.8.2

Bardzo długa wklejona nazwa wywołała ogólny komunikat o niedozwolonych znakach. Dokładne wejście i przyczyna nie są potwierdzone; ewentualną diagnostykę odłożyć. Nie zmieniano walidacji w 0.8.3 ani 0.9.0.

## Po wykonaniu 0.9.0

Niebieskie oznaczenia wyboru i przebudowa lewego panelu zostały wykonane w 0.9.0. Nie są już aktywnym zadaniem backlogu; wcześniejsze akapity opisują historię uzgodnień.

- Plan po0.9.0 został doprecyzowany: kadrowanie importu ikon/obiektów wykonane w0.9.1; zbiorcze usuwanie odłożone bez numeru, docelowe biblioteki do ustalenia. Dalsze nieuzgodnione zmiany okien bibliotek pozostają backlogiem.
- Zgłoszone po0.9.0 etykiety switchy i odstęp pod opisem kierunku gradientu poprawiono lokalnie w0.9.1. Nie jest to pełna przebudowa edytora gradientu ani osobna kampania wszystkich dialogów.
- 0.11: niezależne mapy w zakładkach, wspólne narzędzia i szybkie zestawy. Przy pytaniu o mapę aktywować jej kartę bez resetu zoomu i położenia; lokalizacja kart do ponownej oceny.
- 0.13: rzeczywiste źródła grafik, kolejka importu i docelowe foldery Library. Aktualny układ bibliotek nie jest migrowany.
- Po 1.0: własne ciemne komunikaty, konfigurowalne/alternatywne skróty i wcześniejsze uzgodnione pomysły. Toasty nadal dopiero po zakładkach, dokładny patch nieustalony.

## Stan po 0.9.1

- Wykonano kwadratowe kadrowanie importu ikon/obiektów i zgłoszone lokalne poprawki formularzy: pełne etykiety trzech switchy aktualizacji, czytelne disabled, radio-wybór rodzaju wariantu oraz odstęp pod opisem kierunku gradientu. Bez osobnej przebudowy całego edytora gradientu.
- Zbiorcze usuwanie odłożone bez numeru: tryb wyboru → zaznaczenie → potwierdzenie; biblioteki i szczegółowy zakres do uzgodnienia. Lewy kosz pojedynczego zasobu w 0.9.1 nie jest usuwaniem zbiorczym.
- Pozostałe nieuzgodnione okna bibliotek, sortowanie/jakość starterów oraz dalsze dopracowanie geometrii nie są częścią 0.9.1.
- 0.10: prawy panel. 0.11: zakładki i wspólne narzędzia/zestawy; aktywacja pytanej mapy bez resetowania kamery. 0.12: instalacja/aktualizator. 0.13: źródła grafik, kolejka importu i docelowe foldery Library; żadnej migracji w 0.9.1.
- Własne ciemne komunikaty i alternatywne skróty nadal po 1.0; toasty po zakładkach, dokładny patch nieustalony. Obserwacja bardzo długiej nazwy pozostaje niepotwierdzona, bez zmiany walidacji.
## Odbiór0.9.1 i ograniczona łatka0.9.2

- Zbiorcze usuwanie pozostaje odłożone bez nowego terminu/numeru.0.9.2 ma tylko istniejące pojedyncze operacje ikon/obiektów, bez multiwyboru.
- 0.13: przechowywanie oryginalnych plików grafik w oryginalnym formacie, z możliwością ponownego kadrowania później. Bez zastępowania oryginału przez już zapisany PNG. Wspólnie ze źródłami i kolejką, bez migracji teraz.
- Przyszłe preferencje: domyślne kolory rysika i wskaźnika, bez konfiguratora0.9.2.
- Po przekazaniu programu GM-owi, bez nowego terminu: gumki ikon/terenów, pełny reset heksa, owal i ewentualna piąta kolumna przybornika.
- Odłożona kosmetyka: centrowanie oka, proporcje gumki, odstępy przybornika, dalsze pola nazw, położenie rozmiaru rysika/gumki i natywny obrys ComboBox presetów. Nie jest częścią tej łatki.
- Chwilowe artefakty podczas szybkiego przewijania presetów o różnych wysokościach sekcji: obraz wraca po zatrzymaniu; bez osobnej diagnozy/kampanii teraz.
- Inne typy siatek, scalanie heksów i eksport zasobów z mapy do biblioteki pozostają poza bieżącym etapem.
