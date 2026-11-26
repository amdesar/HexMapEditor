# HexMapEditor 0.12.4

## 1. Poprawka

Wydane klienty 0.12.2 i 0.12.3 odrzucały odpowiedź HTTP 302 przy pobieraniu Setup. Adres GitHuba prowadził na dozwolony host, lecz w nowszej ścieżce po nazwie segmentu znajdował się ukośnik. Odczyt listy wydań i wybór załącznika działały.

`AllowedAssetRedirect` akceptuje dodatkowo pełną ścieżkę `/github-production-release-asset/<identyfikator liczbowy>/<UUID>` na `release-assets.githubusercontent.com` przez HTTPS na porcie 443. Dotychczasowa postać z myślnikiem działa na tych samych hostach co wcześniej. Nadal dozwolone jest jedno przekierowanie pobrania, z dotychczasowymi kodami HTTP. Weryfikacja źródła i metadanych paczki, limity, SHA-256, oznaczenie pochodzenia oraz sprawdzenie pliku przed uruchomieniem pozostają bez zmian. Portable i tryb „Tylko dla mnie” nadal przekazują pobranie do przeglądarki.

Obie odznaki i tekst dostępności w README wskazują opublikowaną 0.12.3. Instrukcja wydania opisuje przełączenie obu odznak po publikacji i anonimową próbę pobrania.

## 2. Próby

- **Kontrolowana regresja `channel` — PASS.** Sprawdzono ścieżkę z diagnozy wraz ze sztucznym parametrem podpisu i dotychczasową postać adresu na obu hostach. Odrzucono obcy host, nową ścieżkę na drugim dotychczasowym hoście, HTTP, nieprawidłowy port, identyfikator, UUID, dodatkowy segment i drugie przekierowanie. API nadal odrzuca przekierowania. Test zapisu paczki potwierdził odmowę przy złej SHA-256 i przy zmianie sprawdzonego pliku.
- **Publiczny załącznik — PASS.** Kod `DownloadSetup` ze zbudowanego EXE 0.12.4 pobrał publiczny Setup 0.12.3 i zweryfikował 2 753 916 bajtów o SHA-256 `675C5AF750C64E20BE0D87E34C28BD72C2ECFED81655F5341DBC1D131D7759EA`. Setup nie został uruchomiony. Do próby warstwy pobrania pobrano metadane oferty z wersją bazową 0.12.2 wyłącznie w wywołaniu testowym. Z bieżącą wersją 0.12.4 zwykła kontrola nie oferowała starszego wydania.
- **Obie odznaki — PASS.** Sprawdzono adresy z README i pobrano oba załączniki 0.12.3 bez poświadczeń. ZIP miał 462 310 bajtów i SHA-256 `AA0D78DD75F4011E5E5345460B7C728E179EE6DB562180CBC25C3630AD515C78`, zgodną z metadanymi GitHuba. Próby nie zapisywały parametrów podpisanych adresów.
- **Paczki — PASS.** Setup i ZIP zbudowano z tego samego EXE. Sprawdzono wersje plików, manifest ZIP i zgodność bajtową EXE z ZIP z głównym EXE. Kompilacja Setup użyła Inno Setup 6.7.4. Gotowych instalatorów nie uruchamiano w tej łatce.

Pełna aktualizacja online z 0.12.4 wymaga opublikowania nowszej wersji. Wtedy w odizolowanej instalacji maszynowej można sprawdzić ofertę, pobranie, weryfikację, decyzję o zamknięciu i wynik Setup. Przy przejściu z dotkniętych 0.12.2 i 0.12.3 pobierz Setup 0.12.4 ręcznie po jego publikacji i porównaj SHA-256.

## 3. Artefakty

Katalog roboczy paczek to `tests/_work/patch-0124-release`. Aplikacja ma wersję 0.12.4, a FileVersion i ProductVersion EXE oraz Setup to 0.12.4.0.

| Plik | SHA-256 |
|---|---|
| `HexMapEditor.exe` | `43FCB2DCC0F3BD8667125F7EBC8E3F6A120E25601B1046E296C0A15807CE4383` |
| `HexMapEditor-0.12.4-Setup.exe` | `0ACB136E659F432EDA0FDE30839FAAB94EBE7910E44EF909F9D093A40307231E` |
| `HexMapEditor-0.12.4-Portable.zip` | `E63F95F0DC8CF0D1A4752B8DB1B0C0F436354CD658EB5EA347CF4B23B4443C33` |

Bazą był `d149dea`, z czystym indeksem i katalogiem roboczym. Kod, EXE, regresję i dokumentację obejmuje jeden lokalny commit. Paczki, dane prób i logi są poza indeksem.
