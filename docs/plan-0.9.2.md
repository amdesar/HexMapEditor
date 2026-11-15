# Plan 0.9.2

Baza db52f96 potwierdzona; Git czysty przed rozpoczęciem. Raport0.9.1 przeczytany, brak AGENTS.md. Główny EXE pozostaje0.9.1 do sprawdzenia kandydata. Dane prób tylko tests/_work, jeden commit po zakończeniu, bez push/tagu/Release.

1. Biblioteki ikon/obiektów: cztery akcje, zachowany jawny wybór do użycia, wspólne operacje edycji/usuwania. Import między kategoriami: trwały odpowiednik po tożsamości źródła, bez czyszczenia dawnych duplikatów i bez migracji.
2. Wiadro ikon: granica terenu oraz stan ikony ze snapshotu gestu. Podświetlenie zapisu z istniejącego WorkingPresetDirty.
3. Kompaktowy kadr ikon/obiektów + reset samego obrotu; wspólny lokalny podgląd szachownicy/otoczenia. Kadrowanie bibliotecznej grafiki terenu: nadpisz/nowy/Anuluj, konflikt-safe, bieżący wybór. Warianty5×2 bez zmniejszania kafelków.
4. Celowane jawne Case: import/biblioteki, bucket, crop/teren, status/kontrolki. Jedna pogrupowana sesja zwykłego EXE. Nie powtarzać odebranego panelu/figur/ramki/autosave/WPF/gumki.
5. Wersja0.9.2.0, identyczne bajty kopii i głównego EXE, dokumentacja/backlog, jeden lokalny commit.

Decyzje: obecne LibraryIcon/ObjectTemplate nie zapisują pochodzenia; konwersja dziś generuje losowe ID. Najwęższe rozwiązanie bez nowego schematu: deterministyczne docelowe ID od kierunku i pełnego stabilnego ID źródła (SHA256), zachowane po edycji/rename. Ponowny import używa tego ID bez nadpisania ręcznych zmian. Stare losowe kopie nie są scalane. Map11/preset5 i formaty bibliotek bez zmiany.

Stan: przygotowanie ukończone, implementacja w toku. Testy jeszcze niewykonane. Ostatnia niepewna operacja: brak. Własne procesy/okna: nie uruchamiano. Nie zakładać stanu okien użytkownika.

## Etapy1–3 — implementacja

Biblioteki korzystają z rozszerzonego IconPickerDialog (single select, dwuklik/Enter używa, Zamknij=Cancel), istniejących Import/Edit/Delete i tego samego LibraryChange. Konwersje mają deterministyczne ID kierunek+SHA256(sourceID), istniejący odpowiednik zachowuje ręczne zmiany; brak nowego schematu. Wiadro ikon używa snapshotu IconEntryId (fallback IconId) oraz terenu, stan początkowy stały w geście. Przycisk zapisu presetu korzysta wyłącznie z WorkingPresetDirty.

Kadrowanie kwadratowe640×660 (z760×760), widoczny kadr520×520 przy100% i otoczenie; wynik nadal512². Obrót0° zeruje tylko kąt. Wspólny CropPreview rysuje szachownicę i pełne otoczenie bez zmiany CropMath. TerrainCrop zachowuje640×590/heks, niebieski obrys. Warianty: klient540×430, miejsce na5×2 kafelki96px z marginesami/paddingiem/scrollbarem. Nowy dialog wyboru zapisu kadru i wspólny TryCommitGraphicCrop, bez suffixów przy nowej pozycji; biblioteczne zmiany nadal niezależną sesyjną historią. Pochodzenie wybranego kadru zapisane tylko słabą referencją sesji, dla osadzonej grafiki dopasowanie dokładnego wyglądu; wieloznaczność wymaga wyboru pozycji. Nie zgadujemy po nazwie.

Pierwszy build kandydata przechodzi (metadane jeszcze0.9.1; nie jest wydaniem). Główny EXE bez zmiany. Pozostałe: celowane Case/integracje, zwykły EXE, dokumentacja/metadane/commit. Scrollbary: brak istniejącego wspieranego mechanizmu ich ciemnego kolorowania; bez custom/hook zostaną natywne, do opisania. Ostatnia niepewna operacja: brak. Własne zwykłe procesy/okna nieuruchomione.

## Testy metod / kandydat

PASS libraries6e6da17fe7634cd29b22aa2759aa9cd6: oba kierunki i powtórzenia, ponowne odczytanie biblioteki w nowej instancji, ręczna edycja odpowiednika, nazwa niejestkluczem, single-select/Close bez zatwierdzenia, shared delete/conflict/Undo/Redo, map/preset zachowane. PASS bucket8cc91baafb524982b644ce81a4da9103: produkcyjny gest, bariera jednego i różnych ID, obie strony, remove/replace/połączenie, teren i Undo/Redo. PASS crop0dd5486432b44924bb75376f03c590cb: kompakt/reset, obraz poza dawnym kwadratem, overwrite/new/nameconflict/fileconflict, map/preset bez propagacji, ApplyOff/On, pełne5×2. PASS status96206d594f5542c2a4cc31cf6c3c60de: bieżący/inny draft, partial/restore/select i niezależność mapy. Nie powtarzać bez wpływającej poprawki.

Przygotowano fixtureb25054ebc74243188347ffa1c0acf048 i zwykłego kandydata0.9.2.0. Główny EXE nadal0.9.1. Następnie jedna pogrupowana próba okna oraz rzeczywisty restart dla importu. Nie uruchamiano jeszcze zwykłej kopii; brak niepewnej operacji. Scrollbary zostają natywne (brak istniejącej wspieranej lokalnej kolorystyki; bez nowych kontrolek/hooków).

## Próba zwykłego EXE / poprawki zależne

W kopii fixtureb250… sprawdzono wybór Enter/dwuklik, import obiekt→ikona, wiadro według obu stron bariery/remove/replace/union oraz Ctrl+Z/Y, zapis; pełne5×2 z11wariantami; heksowy podgląd z otoczeniem i zapis nowego kadru automatycznie wybranego; ApplyOff bez zmiany mapy; dirty/clean przycisku presetu po edycji i zapisie; podmianę grafiki odpowiednika, kwadratowy kadr/pan/zoom/obrót108°/Obrót0°. Stanowisko nie jest fizycznym FullHD; nie deklarujemy braku flasha ani płynności ze zrzutów.

Pierwsza próba ujawniła niedziałające DoubleClick/Enter na natywnym Button: lokalna obsługa WM_LBUTTONDBLCLK/ProcessCmdKey w ResourceTileButton; libraries376abe… PASS i rzeczywiste obie aktywacje PASS. Następnie potwierdzenie usunięcia schowało się za modalną biblioteką (właściciel MainWindow). Użytkownik zamknął wyłącznie kopię testową. Poprawiono przekazywanie aktywnego owner do wspólnych Import/Edit/Delete, wyboru pliku/kadru i komunikatów, bez zmiany LibraryChange. libraries45cef09e… PASS; powtórzenie wyłącznie zależnej ścieżki bibliotek. Pozostałe zaliczone Case bez powtarzania.

Aktualnie: przebudowany kandydat w fixtureb250…, główny EXE nadal0.9.1. Pozostało rzeczywiste potwierdzenie/usunięcie/UndoRedo, ponowny import po restarcie, dokumentacja, publikacja sprawdzonego artefaktu i jeden commit. Ostatnia niepewna operacja: brak; dawne potwierdzenie zamknięte przez użytkownika. Własnych procesów/okien obecnie brak.

## Końcowa weryfikacja i artefakt

Rzeczywisty restart: reimport Postać niebieska wybrał istniejący ręcznie edytowany odpowiednik (nazwa i nowy PNG zachowane, jeden wyliczony ID). Poprawione potwierdzenie usunięcia nad biblioteką działa; delete i Ctrl+Z/Y/Z przeszły, preset czysty. Własny proces zamknięto normalnie, inventory potwierdziło brak okien HexMapEditor.

W końcowym przeglądzie poprawiono nazwę celu nadpisania przy kilku identycznych dopasowaniach (nazwa wskazanego wpisu zamiast pierwszego). Wyłącznie zależny crop87338da0… PASS; na finalnym EXE krótka próba bezpośredniego recrop/nadpisania w bibliotece, wynik wybrany i mapa/preset czyste. Bez ponawiania bibliotek/bucket/status po tej niezależnej poprawce.

Zbudowano finalną kopię0.9.2.0, sprawdzono zwykły EXE, zachowano poprzedni0.9.1 w fixture i przeniesiono identyczne bajty do głównego EXE. SHA2561A5B41D42E7AEDC80B9D1C094BEF06E6548A759B04FD3485BD223CE2BFED075C, FileVersion/ProductVersion0.9.2.0. README/specyfikacja/roadmap/backlog/raport uzupełnione. Scrollbary natywne; fizyczny FullHD nie był dostępny, bez deklaracji braku flasha/płynności. Pełny opis i checklista w raporcie.

Pozostałe: końcowy diff/staging/jeden lokalny commit i odczyt statusu. Ostatnia niepewna operacja: brak. Własne procesy/okna: brak (potwierdzona zamknięta końcowa kopia); nie zamykano okien użytkownika. Wcześniejsze akapity opisują historię, najnowszy punkt jest obowiązującym stanem wznowienia.
