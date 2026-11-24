# Plan HexMapEditor 0.12.1

- Potwierdzono bazę `2061ea2` i czysty Git. Przeczytano raport 0.12.0, kod Inno Setup, ścieżki danych i przekazanie sterowania instalatorowi.
- Setup maszynowy zachowa wybór katalogu i zadań. Konfigurację Library zapisze przy EXE. Dane edytora będą w oddzielnych katalogach bez przyznawania dostępu do plików programu. Dokumenty i próby zapisu będą rozpoznawane przez pomocnik uruchamiany na koncie sprzed UAC.
- Starszą instalację per użytkownik rozpoznamy osobno. Po jej odinstalowaniu kopia zasobów i ustawień będzie dobrowolna. Położenie map i oryginalne dane pozostaną zachowane.
- Inno Setup 6.7.4 jest dostępny w `tests/_work`. Stanowisko nie ma Windows Sandbox. Próby będą korzystać z izolowanych plików i kluczy, a granice takiej weryfikacji znajdą się w raporcie.
- Zbudowano kandydata i oba warianty Setup do prób przejścia. Flagi EXE to PE I386 z ILOnly, bez 32BITREQUIRED. Instalator wybiera zwykły Program Files w 64-bitowym trybie na x64 i katalog odpowiedni dla systemu 32-bitowego. Próba `paths` oraz kompilacja z Inno przeszły.
- Korekta kompilacji usunęła właściwość zawijania niedostępną na checkboxie Inno 6. Próbę przejścia od 0.12.0 ponowiono po dopasowaniu identyfikatora starego instalatora testowego do limitu Inno. Po dalszej zmianie zapisu skrótu i tworzenia katalogów powtórzono `scenario` oraz `cleanup`. Wynik PASS.
- Kreator testowy pokazał kolejno folder programu, trzy opcje Library, zadania i podsumowanie. Po cofnięciu i zmianie folderu potwierdzono ponowne obliczenie ścieżki obok programu. Kreator zakończono przed instalacją.
- Zwykły EXE z izolowanej instalacji otworzył zewnętrzną mapę 0.12.0 z odzyskanym presetem. W finalnym Portable po korekcie nakładania się kontrolek sprawdzono cały przycisk aktualizacji, pustą listę ostatnich map, fabryczny preset oraz utworzenie i ponowne otwarcie mapy obok kopii.
- Pomocnik w kontekście zwykłego użytkownika zaakceptował istniejące Program Files w próbie bez zapisu i odrzucił katalog roboczy z prawami zapisu. Pełna instalacja maszynowa z UAC pozostaje do odbioru w odizolowanym środowisku.
- Końcowe paczki i kopia EXE są w `tests/_work/patch-0121-release`. Przebieg i skróty są w raporcie. Próby i własne okna zakończone.
