# Plan 0.12.3

- Potwierdzono punkt wyjścia `d0ecf2b`, czysty indeks i katalog roboczy. Sprawdzono reguły Inno Setup, uprawnienia katalogu, rejestrację, deinstalację i przekazanie z aktualizatora.
- Odtworzyć odmowę 0.12.2 na własnym katalogu. Zachować chroniony tryb maszynowy i dodać tryb bieżącego użytkownika dla katalogów zapisywalnych. Rozdzielić rejestrację, kontekst instalacji i deinstalacji oraz sposób aktualizowania obu trybów. Zachować położenie danych.
- Wykonać celowane próby odizolowanym Setup i EXE. Następnie zbudować paczki 0.12.3, udokumentować granice prób i zapisać jeden lokalny commit.
- Odtworzono odmowę bazowego Setup. Sprawdzono niezależną odmowę gałęzi maszynowej, kolizję plików, świeżą instalację do własnej ścieżki i upgrade bez deinstalacji. Odczytowo sprawdzono Program Files i D:. W zwykłym EXE zapisano i ponownie otwarto mapę z odrębnej Library.
- Przypadki `channel`, `personal-update` i `close` zaliczone. Paczki zbudowano z tych samych bajtów. Pełna ścieżka z UAC, rzeczywista instalacja na D: i nośnik wymienny pozostają do odbioru odrębnego. Wyniki, sumy i instrukcja odbioru w raporcie.
- Próba deinstalacji odizolowanych kopii zaliczona. Zakończono własny proces próbny. Główny EXE, paczki i stan lokalnego Git zamyka raport.
