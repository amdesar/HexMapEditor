# Plan 0.10.0

Baza d5ce5c0, początkowo czysty Git. Prace obejmują model nakładek i historię grup, trzy widoki prawego panelu, obrazy w istniejącym edytorze opisu oraz celowane sprawdzenie zapisu i zwykłego EXE. Dane prób wyłącznie w tests/_work. Główny EXE zostaje podmieniony po sprawdzeniu kandydata.


- Model Map12 i kategorie per członek zintegrowane z obliczeniami obiektów, historią oraz rozwiązaniem grup. Presety i biblioteki pozostają przy swoich formatach.
- Wstępne Case panel, groups i notes PASS. Pierwsze notes ujawniło błędny wzorzec RTF w inicjalizatorze, zastąpiony prostym parserem pict. Próby RTF, HTML, PNG i przeniesienia mapy przeszły.
- Zwykły kandydat 56F5A78E… odsłonił trzy widoki i poprawnie przełączył mieszaną kolumnę grupy. Przeciągnięto granicę panelu. Wykryto niewidoczny tekst/ołówek wspólnego nagłówka, mimo obecności w UI Automation. Zastąpiono wewnętrzny układ nagłówka przez wyliczanie geometrii. Próba przeciągnięcia krawędzi całego okna nie zmieniła jego rozmiaru. Powtórzyć zależnie na kolejnym kandydacie. Pierwszą kopię zamknięto po zapisaniu własnej mapy. Pomocnicze okno RTF nie wystawiło okna przy starcie w trybie Hidden.
- Kandydat CA37325B… pokazał poprawione nagłówki wszystkich trzech widoków. Próg pokrycia jest polem przecięcia heksagonu z odcinkami pikseli o alfie ≥96. Case panel PASS po zmianie nagłówka i pokrycia. Case groups i notes PASS po dodaniu asercji odłączenia, dołączenia, usunięcia i wstawienia obrazu z pliku.
- Zwykły EXE przeszedł próbę zmiany szerokości panelu i systemowej zmiany rozmiaru okna, aktualizacji podczas zwinięcia oraz grupowej kolumny i przejścia do członka. Po zapisie i ponownym otwarciu zachował tekst–obraz–tekst i szerokość obrazu. Ctrl+Shift+V wstawił tekst bez obrazu. Własna kopia i pomocnicze okno schowka zostały zamknięte.
- Dodatkowy Case panel po rozszerzeniu prób o 0/1/14 miniatur i anulowanie nazwy przeszedł. Szczegóły przypadków i ograniczenia zapisano w raporcie 0.10.0.
- Główny EXE podmieniono sprawdzonym kandydatem. Potwierdzono wersję 0.10.0.0 i zgodność bajtów. Ostatnia operacja to jeden commit lokalny z weryfikacją statusu. Brak otwartych własnych okien i procesów.
