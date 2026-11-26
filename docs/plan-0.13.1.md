# HexMapEditor 0.13.1 — plan

- Baza aplikacji to `96c1e9c`. Lokalna gałąź została przesunięta fast-forward do `dfbea07` z README po publikacji 0.13.0.
- Jeden kreator zbiera wybory przed instalacją. Zapis dostępny zwykłemu użytkownikowi odbywa się pod jego tokenem. Chroniony cel obsługuje drugi, niemy etap tego samego Setup, po UAC i ponownej kontroli ACL.
- Aplikacja sprawdza pakiet i zapis pracy przed zamknięciem. Zwykły proces Setup czeka na zakończenie edytora, sprawdza zakres i ścieżkę w rejestrze oraz zachowuje blokadę sprawdzonego pakietu. Aktualizacja osobista odbywa się w tym procesie. W zakresie maszynowym zwykły pomocnik uruchamia drugi etap tego samego Setup. Inno prosi o UAC, a wznowienie edytora pozostaje po stronie zwykłego procesu.
- Ciemna tabela kolejki bez zmian importu. Odbiór w odizolowanych katalogach i kluczach. Wersja, paczki, końcowy README i raport w jednym lokalnym commicie.

Sonda zwykłego konta rozpoznaje chroniony Program Files i zapisywalne ścieżki na C: oraz D:. Odizolowany kreator pokazał jedną ścieżkę stron od katalogu przez Library do podsumowania. Przypadki `channel`, `personal-update`, `close` i regresja `queue` zaliczone. Po powtórzeniu próby układu na końcowym Portable nagłówki są ciemne, długa nazwa jest skracana, a poziomy scrollbar nie pojawia się przy minimalnej szerokości.

Rzeczywisty upgrade osobisty 0.13.0 → 0.13.1 zakończył się kodem 0. Porównanie próbnej Library, mapy, MRU, ustawień i skrótów przeszło bez różnic. Poprzedni błąd bezpośredniego Startu drugiego Setup sprawdzono osobną sondą Inno. Windows zwracał 5 również bez blokady pliku. Zwykły pomocnik uruchomił drugi etap z utrzymaną blokadą. Powtórzenie dotyczy tylko poprawionego przekazania i ścieżki instalatora.

Próba publicznego kanału wykazała `v0.13.0` jako ostatnie opublikowane wydanie. Świeży Setup w izolowanym Program Files oraz upgrade maszynowy przeszły z podniesionym etapem, zapisem w HKLM i zgodnym EXE. Powtórzenie świeżej próby nastąpiło po zwolnieniu blokady własnego edytora. Próba upgrade’u potwierdziła zachowanie danych i skrótów oraz wznowienie EXE. Oddzielnie zaliczono uruchomienie końcowego Portable, jego lokalne dane, zawartość ZIP i zgodność bajtową.
