# Raport HexMapEditor 0.10.0

Baza lokalna to ukończone 0.9.3, commit `d5ce5c0`. Wydanie obejmuje prawy panel, obrazy w opisie heksa i nakładki ustawień grup. Uwagi z odbioru 0.9.3 i 0.10.0 mają wspólne miejsce w planie 0.10.1.

## Panel i trzy widoki

- „Zamknij” i „Dostosuj” są po prawej stronie paska szybkich poleceń. Uchwyt na całą wysokość zwija panel bez utraty zaznaczenia. W stanie zwiniętym wybór innej pozycji aktualizuje szczegóły, a „Dostosuj” zapamiętuje szerokość 440 do następnego rozwinięcia. Uchwyt i przyciski znikają po wyczyszczeniu zaznaczenia. Zaznaczenie wielu niezgrupowanych obiektów korzysta z polecenia grupowania na pasku, bez odrębnego widoku.
- Zmiana szerokości rozdziela przesunięcie panelu i odrysowanie odsłoniętego fragmentu płótna. Panel obiektów i grup używa istniejącej nakładki WinForms. Widok heksa pozostaje oddzielną gałęzią z osadzonym WPF.
- Heks, obiekt i grupa mają wspólną edycję nazwy z ołówkiem, zielonym zatwierdzeniem, czerwonym anulowaniem i skrótami Enter/Escape. Edycja tekstu ma własne cofanie/przywracanie. Zatwierdzenie zmienia mapę pojedynczym krokiem historii. Współrzędne heksa są nazwą zastępczą wyłącznie w widoku.
- Podgląd heksa pokazuje teren i ikonę. Kolor nazwy terenu korzysta z reprezentatywnego koloru wyglądu, z kontrastowym obrysem. Przy zerowej liczbie obiektów ich sekcja znika wraz z separatorem. Klik miniatury wybiera obiekt. Przy małej wysokości albo wielu rzędach miniatur można przewijać cały panel.
- Obiekt pokazuje „Brak terenu” na niepomalowanym heksie i „Poza mapą” poza siatką. Zachowano kafelki stanów. Rozmiar 8–1024 ma ×2, ÷2 i reset do 30. Promienie pokazują się przy włączonych funkcjach. Link do grupy i informacja o używanej nakładce towarzyszą edytowalnym wartościom indywidualnym. Odłączenie i usunięcie mają osobne komunikaty o losie wystąpienia.

### Pomiar pokrycia heksa

Punkt położenia na heksie kwalifikuje widoczny obiekt niezależnie od rozmiaru. Dla pozostałych kandydatów najpierw sprawdzane są okolica i obwiednie. Następnie poziome odcinki pikseli źródłowych o alfie co najmniej 96 są rzutowane w granice obiektu używane przez renderer. Pole przecięcia odcinków z wielokątem heksa jest sumowane do 20% powierzchni heksa. Pomiar jest niezależny od zoomu i przesunięcia. Przezroczyste marginesy i słaba alfa są pomijane. Wizualna ramka grupy nie wpływa na pole. JPEG zachowuje nieprzezroczystość źródła. Dane alfa są pamiętane dla najwyżej 16 zasobów jednocześnie. Lista aktualizuje się po zmianie zaznaczenia, rewizji lub odpowiedniej widoczności, poza ścieżką malowania kontrolki.

## Grupy i trwałość

- Stan, rozmiar, mgła, widzenie i blokada mają osobne switche dla każdego członka. Nowy członek zaczyna z wartościami indywidualnymi. Nagłówek kolumny włącza wszystkim, jeśli choć jeden switch jest Off, i wyłącza wszystkim, jeśli wszystkie są On. Operacja jest jednym krokiem historii.
- Wspólny stan pochodzi z biblioteki stanów. Wspólny rozmiar zastępuje rozmiar członka. Przesunięcie dodaje podpisaną różnicę do indywidualnej podstawy, z limitem 8–1024. Reset ustawia 30 albo 0 zgodnie z trybem. Wyłączona funkcja grupowa jest prawidłową wartością wynikową. Reguły uprawnień mgły zachowują zastosowanie.
- Odłączenie i rozwiązanie grupy pozwalają pozostawić wartości indywidualne, zachować wynikowe albo wybrać je osobno przed operacją. Rozwiązanie używa jednej tabeli dla wszystkich członków. Pola bez aktywnej nakładki nie wymagają decyzji. Usunięcie grupy usuwa jej wystąpienia z mapy. Operacje korzystają z historii mapy i zachowują definicje biblioteczne.
- **Map12** zapisuje maski właściwości każdej grupy oraz dane osadzonych obrazów. Stary format mapy jest odrzucany przed wczytaniem dokumentu i przed zapisem. Zapis presetów pozostaje w formacie 5, odczyt obejmuje 4 i 5. Formaty bibliotek są takie jak w 0.9.3.

## Opis i schowek

Wspólny edytor WPF zachowuje formatowanie, listy, wyrównanie, historię i operacje na tabelach. Pasek ikon z tooltipami zawija się. Pole czcionki obsługuje wpis i poziome przeciąganie. W zwykłym widoku opis wypełnia wolną wysokość. Powiększenie ukrywa pozostałe sekcje i zachowuje ten sam dokument WPF.

Wstawianie przyjmuje PNG/JPEG z pliku. Wklejanie korzysta z PNG lub bitmapy schowka, osadzonego PNG/JPEG w RTF oraz danych PNG/JPEG w HTML. W mieszanej treści osadzenia zachowują kolejność. Obsługa HTML obejmuje tekst, podziały wierszy i obrazy z danymi w treści. Odwzorowanie typografii i innych formatów obrazu zależy od tego, co udostępnia źródło. Zewnętrzne odwołania pozostają bez obrazu. „Wklej jako zwykły tekst” używa tekstowej reprezentacji. Schowek jest interpretowany lokalnie, bez wykonywania aktywnej treści.

Źródło plikowe, PNG/JPEG z RTF lub HTML i PNG schowka mają limit 2 MiB, a dane rastrowe maksymalnie 2048×2048. Osadzenie normalizuje do PNG o maksymalnym boku 512 i rozmiarze do 1 MiB. Jeden opis może zawierać maksymalnie 12 obrazów i do 2 Mi znaków zapisu bogatej treści. Widoczna szerokość jest od 24 do 900 jednostek logicznych i zmienia się proporcjonalnie do wysokości. Limit mapy pozostaje 256 MiB.

## Weryfikacja celowana

| Próba | Zakres i wynik |
|---|---|
| Metody i kontrolki, `-Case panel` | PASS. Heks z 0, 1 i 14 obiektami, zawijanie miniatur, przejście do obiektu, brak zbędnego przewijania w typowym układzie, próg powierzchni poniżej i powyżej 20%, przezroczyste marginesy, anulowanie nazwy i osobne cofanie tekstu oraz mapy, zwinięcie, zmiana widoku podczas zwinięcia, reset, przejście z grupy do członka, ×2/÷2/30 i wyczyszczenie zaznaczenia. |
| Metody, `-Case groups` | PASS. Bazowe wartości, rozmiar i limit, grupowe Off, przełączenie kolumny w obie strony, Map12, różne decyzje dwóch członków, odłączenie, dołączenie z Off, anulowanie usunięcia, usunięcie i Undo. |
| Metody i kontrolki, `-Case notes` | PASS. Kolejność tekst–obraz–tekst w RTF/HTML, odrzucenie zewnętrznego obrazu, sam PNG ze schowka testowego, wstawienie z pliku, tabela i dodanie wiersza. Mapę przeniesiono bez pliku obrazu i odczytano z osadzoną grafiką o zapisanej szerokości. |
| Zwykły EXE, własna mapa i własny schowek systemowy | Sprawdzono widoki heksa, grupy i obiektu, aktualizację grupy podczas zwinięcia, „Dostosuj” i rozwinięcie, mieszaną kolumnę rozmiaru oraz przejście do członka. Wpisano nazwę heksa i zatwierdzono ją przez Enter. Sprawdzono powiększenie opisu, wklejenie lokalnej próbki RTF przez Ctrl+V, zwiększenie szerokości obrazu, Ctrl+Shift+V na innym heksie i Ctrl+S. Po ponownym otwarciu przez Ostatnie projekty wrócił tekst po obu stronach obrazu i zapisany rozmiar. |
| Zwykły EXE, układ | Przeciągnięto granicę panelu, a rozmiar okna zmieniono też przez systemowe polecenie „Rozmiar”. Obserwowane szerokości zrzutów okna to 1426 i 1393 px. Po tych operacjach opis pozostał dostępny, pasek narzędzi dostosował liczbę rzędów, a odsłonięty fragment mapy został narysowany. |

Pierwszy kandydat ujawnił niewidoczny nagłówek, a pierwszy test RTF błędny wzorzec parsera. Próby zależne powtórzono po zmianach nagłówka, obliczeń pokrycia i odczytu RTF. W kolejnych uruchomieniach trzech przypadków dodano asercje zawijania miniatur, anulowania nazwy, odłączenia, usuwania, dołączenia i wstawienia z pliku. Danych próbnych i pomocniczego schowka używano tylko we własnej kopii. Procesy próbne zamknięto.

Próba zwykłego EXE użyła lokalnego RTF przez systemowy schowek. Word, OneNote i przeglądarka nie były bezpośrednimi źródłami w tej sesji. Układ oceniono w dostępnym oknie, bez potwierdzenia fizycznego stanowiska Full HD przy 100%. Obserwacje po zmianie rozmiaru nie przesądzają o pojedynczych klatkach w trakcie gestu. Wiele rzędów miniatur, obiekt poza siatką oraz pełna sekwencja tekstowego Undo/Redo nie były osobno odbierane w zwykłym EXE. Testów pełnej regresji nie uruchamiano.

## Checklista odbiorcza

| Czynność | Oczekiwany wynik |
|---|---|
| Zwiń heks, wybierz grupę, użyj „Dostosuj” i rozwiń | Zaznaczenie pozostaje, dopiero rozwinięcie pokazuje aktualną grupę w domyślnej szerokości. |
| Użyj ołówka, wpisz nazwę, anuluj przez Escape, potem powtórz z Enter | Anulowanie zachowuje nazwę, a zatwierdzoną zmianę można cofnąć jako jeden krok mapy. |
| Wklej tekst–obraz–tekst z osadzonym obrazem, powiększ obraz i zapisz | Kolejność i szerokość wracają po przeniesieniu samej mapy. Wklejenie jako zwykły tekst pomija obraz. |
| Zmień indywidualny rozmiar członka z aktywną nakładką i wyłącz ją w macierzy | Po wyłączeniu pojawia się zmieniona indywidualna wartość. Nagłówek kolumny przełącza wszystkich w jednej akcji. |
| Rozwiąż grupę i wybierz różne wartości do zachowania u dwóch członków | Oba obiekty pozostają na mapie, Undo przywraca członkostwo i wartości. |
| Otwórz mapę poprzedniego formatu | Program informuje o nieobsługiwanej wersji, oryginalny plik pozostaje dostępny. |

## Artefakt i Git

Aplikacja 0.10.0, FileVersion i ProductVersion 0.10.0.0. SHA-256 sprawdzonej kopii i głównego `HexMapEditor.exe` to `CA37325B98ED0DF078009E9D9B69CA67D6E4A99F0B213F7969FF21FDEDD54BDB`. Porównanie `fc /b` zwróciło zgodność wszystkich bajtów. Jeden lokalny commit obejmuje kod, binarium, źródła celowanych testów i dokumentację. Identyfikator i końcowy status są raportowane po wykonaniu commita. Dane `tests/_work` są poza commitem.
