# Plan 0.4.1 i punkt wznowienia

Baza: czysty master 8d422b10de2bf37f281c5f7b98b065859b114ffe (0.4.0), origin https://github.com/amdesar/HexMapEditor.git. Przeczytano README, raport 0.4.0, sekcję 27 specyfikacji i bezpośrednio związany kod. Brak AGENTS.md w sprawdzonym repozytorium/katalogach nadrzędnych. Nie powtarzać prac ani testów 0.4.0; bez pełnej kampanii.

1. Format 5: ObjectStates (stabilne ID, nazwa/kolor/CanReveal), Groups (ID/nazwa/Members i opcjonalne nadpisania), zachowane bazowe MapObject. Side jest referencją ID stanu, nie nazwą. Efektywne właściwości obliczane bez modyfikacji bazy, rozmiar Indywidualnie/Offset/Common z clamp 8–1024. Walidacja i samowystarczalne zasoby bez konwersji dawnych map.
2. Wspólne EditAction: delta obiektów + opcjonalne migawki grup/stanów oraz końcowa delta Fog. Uzgadnianie zasięgów po właściwych operacjach, nigdy podczas Undo/Redo/odczytu. Odłącz/rozwiąż z decyzją zachowaj/przywróć/kategorie/anuluj w jednym wpisie. Nie niszczyć bazowych ustawień grupową nakładką.
3. Widoczne Select/Rectangle/Lasso, Rączka na obiekcie/grupie bez otwarcia panelu i na tle pan. Pusty klik poza mapą odznacza heks bez historii, pan go nie zamyka. Bieżące właściwości, scalanie sesji nazwy/liczb, dyskretne checkboxy/stany. Domyślny rozmiar 30; instancje niezależne od nazwy wzorca. Kosz wzorca ma właściwe Enabled.
4. Panel grupy/członka, kafelki stanów i dodawanie własnego; podgląd bez mgły jako flaga widoku z bezpieczną odmową gestu; maskowana lista i wspólny algorytm Dopasuj. Bez światła i redesignu.
5. Kilka nowych krótkich scenariuszy: model/grupy/stany/odłączanie/zapis; mgła/podgląd/transakcje; wejścia/właściwości/kadr. Potem krótki zwykły EXE we własnym tests/_work. Bez starych kampanii i prywatnych danych. Przy błędzie powtarzać tylko dotknięte próby.
6. Wersja/build 0.4.1, README/spec/raport, cleanup tylko własnych danych po procesach i lokalny commit. Bez push/tagu/Release'a. Nie zamykać okna użytkownika przy blokadzie EXE.

Plan implementacji wykonany; szczegółowy stan poniżej.

## Punkt wznowienia — 7 listopada 2026

Implementacja i główny build 0.4.1.0 zakończone. Format 5, bazowe/efektywne grupy, stany, live properties, preview i lista oraz wspólny fit są w kodzie. README/specyfikacja (sekcja 28) i tests/README zaktualizowane. Nie uruchomiono dawnych kampanii.

Zaliczone: model (813bfb481a0144a2b8e7a1244b44b910), fog (b5e48466bd7848768d96b9f25d5fc281 i po zmianie śledzenia dirty a8ecbc28e88f40e5a8150ef735ebb9e0), controls (ostatni 7b7042d7810d4d5fa1d17278676b3341). Jeden błąd fixture controls: punkt rzekomo pusty leżał wewnątrz dużej grafiki; poprawiono punkt, nie maskowano błędu aplikacji. Interaktywnie wykryte odświeżanie liczby dopiero po blur poprawiono i sprawdzono ponownie tylko bezpośredni przypadek. Sesje property nie haszują wszystkich opisów po każdej literze.

Zwykły własny EXE w tests/_work/ui-041-2d77aaabd332462794a5b24dd455d677: Rączka bez panelu, prostokąt, tworzenie grupy, bieżący stan i rozmiar, zapis/odczyt, ruch grupy pod mgłę, maskowana lista oraz preview z realnymi nazwami i zapisem Fog wykonane. Wciąż otwarty modal Rozwiąż grupę (1248070) nad własnym oknem (1838264). Czeka wcześniejsze pytanie request_user_input_async o zatwierdzenie wyłącznie testowej grupy z Przywróć indywidualne i Undo. Nie klikaj OK bez odpowiedzi, nie pytaj ponownie. Jeśli zgoda: świeży Sky snapshot modalu, OK, obserwacja, Undo, zapis i zamknięcie własnego okna. Jeśli odmowa: Anuluj i jawnie odnotuj niewykonaną próbę UI. Prywatna Library nietknięta.

Główny EXE zbudowany już po ostatniej zmianie dirty i korekcie komunikatu formatu: SHA256 5C0A53B212DAA9B2EA301C764819CC807CE6A4517ED5D46372A49858014E888F. Własny aktualnie otwarty EXE jest wcześniejszą zwykłą kompilacją (UI live number już zawiera, sparse dirty jeszcze nie). Po zamknięciu można skopiować końcowy EXE do tego samego własnego run i krótko uruchomić; nie powtarzać całego scenariusza.

Pozostało: dokończyć oczekującą próbę UI zgodnie z odpowiedzią, zapisać raport 0.4.1 z dokładnym zakresem/ograniczeniami, startup końcowego EXE, cleanup wyłącznie własnych artefaktów po procesach (zachować results.log), diff/check-ignore/staging i lokalny commit. Stan bazowy master 8d422b1, wszystkie zmiany nasze; bez fetch/push/tagu/Release'a.

Końcowy startup identycznej kopii głównego EXE w smoke-041-eca1077a3d6747458c007ea3e147b5cb PASS, czyste zamknięcie potwierdzone przez Sky. Własne smoke/build artefakty sprzątnięte; pozostał results.log. Raport zapisany. Tylko modal rozwiązania własnej grupy i końcowy cleanup ui-041 pozostają zależne od odpowiedzi. Nie powtarzać wykonanych prób.
