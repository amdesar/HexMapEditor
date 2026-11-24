# Plan HexMapEditor 0.11.1

- Baza `b441ab1` potwierdzona, Git czysty przed pracą. Przeczytano raport 0.11.0 i sprawdzono ścieżki kart, zapisu i presetów.
- Gotowe są wspólny obrys i osobna gwiazdka, strzałki liczone po pełnych kartach oraz cienka linia pod obiektami. Autozapis ma wspólny termin 5 minut. Zamykanie pokazuje mapę i zachowane drafty. Dopasowanie uwzględnia pełne płótno przed prawą nakładką.
- Zaliczone przypadki `tabs-fit`, `autosave`, `close` oraz sesja zwykłego EXE. Powtórzono `autosave` i `close` po dodaniu odpowiednio próby nieaktualnej migawki oraz rzeczywistego częściowego zapisu odłączonego presetu. W pierwszej próbie nieaktualnej migawki użyte polecenie edycji zatrzymała ochrona zapisywanej karty. Ponowna próba wstrzyknęła zmianę rewizji bezpośrednio w złączu testowym.
- Sprawdzoną kopię podmieniono jako główny EXE. `fc.exe /b` potwierdził identyczność. Wersje pliku i produktu 0.11.1.0. Własny PID 11324 zamknięto w aplikacji po próbach. Brak otwartych własnych okien.
- Kontrola końcowa obejmuje jeden lokalny commit wymienionych zmian i odczyt statusu Git.
