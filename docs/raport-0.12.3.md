# HexMapEditor 0.12.3

## Przyczyna i rozwiązanie

Setup 0.12.2 oferował wyłącznie instalację dla wszystkich. Dla wybranej ścieżki wywoływał `RequireProtectedProgram` przed zapisem. Kontrola odrzucała możliwość podmiany folderu albo zapisu zwykłym kontem. Chroniła podniesiony instalator, maszynową rejestrację, program oraz deinstalator i jego log pod tą ścieżką. Sama zmiana komunikatu lub skasowanie kontroli pozostawiłoby podniesione operacje w katalogu, który konto bez podniesienia może zmieniać.

Setup 0.12.3 obsługuje dwa zakresy Inno. Tryb dla wszystkich nadal proponuje systemowy `Program Files\HexMapEditor`. Tryb „Tylko dla mnie” działa w kontekście zwykłego użytkownika, z zapisem w jego zakresie rejestru i skrótów. Pozwala wybrać własny zapisywalny katalog na dostępnym dysku. Jeśli w świeżym kreatorze maszynowym wybierzesz taką ścieżkę, możesz zaakceptować ponowne otwarcie tego samego Setup w trybie bieżącego użytkownika z wprowadzonym katalogiem. Możesz również wybrać ten tryb od razu. Katalog na nośniku musi pozostać dostępny pod ścieżką wybraną przy instalacji. [Zakresy Inno Setup](https://jrsoftware.org/inno-setup/documentation/topic_admininstallmode.htm).

Podniesiona gałąź sprawdza niezależnie właściciela i uprawnienia zapisu oraz podmiany w ścieżce, plikach programu i istniejących plikach deinstalatora. Używa pomocnika dołączonego do Setup i oddzielnego pliku odpowiedzi w jego katalogu tymczasowym. Wynik sondy pisania z konta użytkownika służy wyłącznie zaproponowaniu zwykłego trybu. W zapisywalnym katalogu pliki, rejestracja i log deinstalacji powstają w zakresie aktualnego użytkownika. Setup odmawia uruchomienia tej gałęzi z podniesionym tokenem i nie uruchamia edytora. Pozostawiono walidację ścieżki, dostępności, przekierowań i kolizji. Setup nie zmienia ACL wybranego dysku ani zastanego folderu.

W przejściu z 0.12.2 używa się tego samego AppId, zakresu i ścieżki. Setup odczytuje poprzedni wybór Library i zadania. Data, Library, miejsca zapisu map i formaty pozostają dotychczasowe. Upgrade nie zmienia lokalizacji istniejącej kopii. Dotychczasowa instalacja maszynowa wymaga nadal ochrony własnych plików. Odmowa, gdy uprawnienia tej instalacji zostały zmienione po jej utworzeniu, nie zmienia jej zakresu.

## Aktualizator

- Instalacja dla wszystkich zachowuje pobranie, walidację źródła, typu, przekierowania, rozmiaru i SHA-256, kontrolę integralności do chwili uruchomienia oraz oznaczenie pochodzenia internetowego. Po decyzji użytkownika cykl zamyka dokumenty i presety dotychczasową drogą. Anulowanie pozostawia edytor.
- Instalacja bieżącego użytkownika wykrywa nowszą wersję, pokazuje link do właściwego Setup i oczekiwaną SHA-256. Po pobraniu użytkownik porównuje sumę, zamyka edytor i uruchamia instalator zwykłym dwuklikiem w tym samym zakresie. Edytor nie uruchamia pobranego EXE w tej gałęzi. Tryb odróżnia znacznik wdrożenia. Biblioteka i Data zachowują ten sam układ co w trybie maszynowym.
- Portable nadal wskazuje ZIP. Sprawdzanie wydań korzysta z tego samego publicznego kanału testowego.

## Próby

**Rzeczywisty Inno Setup w izolacji.** Archiwalny skrypt 0.12.2 z bazowym EXE i włączonym oryginalnym sprawdzaniem uprawnień odmówił zapisu w zapisywalnym drzewie. W tym miejscu wcześniejsza kontrola zgłosiła odmowę na katalogu nadrzędnym. To ta sama blokada, która przy odmowie na samym folderze pokazuje komunikat zgłoszony przez użytkownika. W 0.12.3 gałąź kontroli maszynowej odrzuciła własne zapisywalne drzewo jeszcze przed utworzeniem celu. Sprawdzono również odmowę przy cudzym pliku o zarezerwowanej nazwie. Świeża instalacja z własną ścieżką i osobną Library przebiegła z kluczem HKCU. Przeszedł ręczny upgrade bez deinstalacji, z porównaniem mapy 0.12.2, MRU, ustawień, Library i obu skrótów. Deinstalatory izolowanych kopii usunęły własne skróty i zachowały mapy, Library, Data oraz ręczny skrót.

**Zwykły EXE.** Z odizolowanej świeżej instalacji uruchomiono edytor bez podniesienia. Utworzono i odczytano z MRU własną mapę. Plik Map12 zapisał się w `Hexmaps` wskazanej, oddzielnej Library, z metadaną 0.12.3.

**Próby pomocnika i aktualizatora.** Odczyt zabezpieczeń potwierdził akceptację chronionego Program Files jako katalogu nadrzędnego oraz możliwość wyboru ścieżki na osobnym dysku D: w zwykłym trybie bez tworzenia tam plików. Przypadki `channel`, `personal-update` i `close` potwierdziły wybór paczki, odrzucenie błędnej sumy, ręczne pobranie z widoczną sumą i blokadą uruchomienia instalatora przez edytor w trybie osobistym, a także decyzje o zamykaniu przed aktualizacją w dotychczasowym trybie.

**Ograniczenia.** Test Setup korzystał z izolowanych folderów i kluczy na tym samym Windows. Nie było oddzielnej VM ani odizolowanego stanowiska z UAC do instalacji maszynowej w Program Files, próby jej rzeczywistego upgrade’u lub przejścia między kreatorami po podniesieniu. D: sprawdzono na poziomie ścieżki i uprawnień, a dane prób zapisano na C: w `tests/_work`. Nośnika wymiennego nie było. Próby kanału były kontrolowane.

## Odbiór online po publikacji

1. Właściciel publikuje `v0.12.3` jako pre-release z plikami `HexMapEditor-0.12.3-Setup.exe` i `HexMapEditor-0.12.3-Portable.zip`. Porównuje sumy załączników oraz sprawdza ich dostępność bez logowania.
2. W zainstalowanej 0.12.2 otwiera **Sprawdź aktualizacje**. Oczekiwany wynik to oferta 0.12.3 z instalatorem i kontrolą SHA-256. Po potwierdzeniu Setup powinien użyć tego samego katalogu programu, Library i zadań. Anulowanie przy pytaniu o zapis pozostawia pracę w edytorze.
3. Po instalacji sprawdza wersję 0.12.3, starą mapę i skrót. Świeży wybór własnego zapisywalnego folderu sprawdza na osobnym profilu lub osobnej instalacji testowej. Skuteczne przejście aktualizacji nie przenosi istniejącego programu na inny dysk.

## Pliki

Aplikacja 0.12.3, FileVersion i ProductVersion 0.12.3.0. Budowanie z jednego EXE do `tests/_work/patch-0123-release`. EXE z ZIP, z izolowanej instalacji i główny EXE mają te same bajty. ZIP zawiera `HexMapEditor.exe`, `deployment.mode`, `README-Portable.txt` i `LICENSE`. Setup i EXE są niepodpisane.

| Plik | SHA-256 |
|---|---|
| `HexMapEditor.exe` | `4DC914AA6D6C6264DE41E492E7E46A0C6B3261FF3FC7824578B838AEA94F686C` |
| `HexMapEditor-0.12.3-Setup.exe` | `675C5AF750C64E20BE0D87E34C28BD72C2ECFED81655F5341DBC1D131D7759EA` |
| `HexMapEditor-0.12.3-Portable.zip` | `AA0D78DD75F4011E5E5345460B7C728E179EE6DB562180CBC25C3630AD515C78` |

Baza `d0ecf2b`, początkowy Git czysty. Wydanie obejmuje jeden lokalny commit. Dotychczasowe paczki i tag 0.12.2 zachowano. Wyniki prób i nowe paczki są poza Git.
