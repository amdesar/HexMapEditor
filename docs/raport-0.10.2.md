# Raport HexMapEditor 0.10.2

## 1. Wykonanie

- **Zmiana szerokości.** W 0.10.1 każdy ruch ponownie uruchamiał układ nakładek całego okna, unieważniał drzewo szczegółów i zostawiał odrysowanie płótna kolejce komunikatów. Przy ruchu odczytywano współrzędne zdarzenia względem przesuwanego uchwytu. W 0.10.2 szerokość jest liczona względem punktu rozpoczęcia i aktualnej ekranowej pozycji kursora. Zmieniane są tylko granice prawego panelu. Odsłonięta część płótna oraz uchwyty są odrysowywane przed powrotem z obsługi ruchu. Dalsze szczegóły nadal korzystają ze zwykłego malowania kontrolek. Zachowano minimum, zwijanie i Dostosuj.
- **Opis.** Własny dokument RTF otwarty w Wordzie i skopiowany przez Ctrl+C zawierał długi tekst, PNG 700×900 i długi dalszy tekst. Po wklejeniu do 0.10.1 i poszerzeniu panelu oba fragmenty tekstu dzieliły wiersz z obrazem, na wysokości jego środka. Plik zachował oba fragmenty w tym samym akapicie co raster. W 0.10.2 WPF przydziela obrazowi osobny blok o rzeczywistej wysokości wynikającej z proporcji. Dostępna szerokość ogranicza tylko układ. Ustawiony rozmiar pozostaje w danych. Wczytanie opisu z obrazem wewnątrz akapitu rozdziela go z zachowaniem kolejności i formatowania fragmentów. Zapis pozostaje w Map12 i wykorzystuje dotychczasowy typ obrazu w akapicie. Limit 2048 px, budżety obrazów i formaty presetów oraz bibliotek są dotychczasowe.
- **Formatowanie.** Kursor wewnątrz słowa wyznacza zakres znaków słowa. Spacje po nim pozostają poza automatycznym zakresem. Jawne zaznaczenie może zawierać spacje. Zachowano dotychczasowe zachowanie kursora poza słowem i transakcje historii opisu.
- **Start i kontrolki.** Przy nazwie projektu wersja jest po prawej we wspólnym wierszu, z miejscem wyliczonym według szerokości napisu. Projekt bez wersji wykorzystuje cały wiersz na nazwę. Pola nazw trzech widoków mają 29 pt i mieszczą się w wierszu nagłówka. Heks ma ten sam punkt rozpoczęcia co obiekt i grupa. Wiersze obu promieni mieszczą dolną ramkę przy tej samej wysokości. Plus jest centrowany graficznie. W wyborze źródła ikony i obiektu wszystkie cztery przyciski korzystają z tej samej kontrolki i rozmiaru.

## 2. Ograniczenia

W zwykłym EXE dało się przeciągać uchwyt w obie strony, lecz dostępne narzędzie wykonuje jeden krótki gest i zwraca obraz po zwolnieniu przycisku. Nie można było wykonać tą drogą powolnego ruchu z pauzą przy przytrzymaniu ani ocenić pojedynczych pionowych smug w trakcie ruchu. Potwierdzono geometrię i rzeczywiste zdarzenia malowania przy aktywnym uchwycie w teście programu. Zrzuty EXE potwierdzają wygląd po ruchu. Osobny artefakt przy skrajnej prawej krawędzi heksa i zimne otwarcie pozostają odłożone.

Próba Worda dotyczy własnego dokumentu w trybie zgodności. Okno edytora miało około 1426×934 px. Bez fizycznego pomiaru Full HD/100% i bez pełnej regresji. Systemowy wybór pliku obrazu nie udostępnił pola nazwy do wpisania przez narzędzie. Wstawianie z pliku potwierdzono metodą produkcyjną uruchomioną w teście kontrolek.

## 3. Testy metod i kontrolek

| Przypadek | Wynik |
|---|---|
| `Run-Patch0102Tests.ps1 -Case panel` | PASS. Obiekt i heks po 9 pozycjach gestu, w tym małe i duże przesunięcia oraz powtórzenie tej samej pozycji z aktywnym przechwyceniem. Po 4 odsłonięcia pasa odmalowane przed puszczeniem, bez malowania od niezmienionej pozycji. Najszerszy zmierzony obszar malowania wyniósł 519 z 1130 px szerokości płótna. Próba obejmuje nagłówki, obiekt z 8 stanami, oba promienie w obiekcie i grupie, stałe 30 px przycisku usunięcia oraz geometrię czterech przycisków importu. |
| `Run-Patch0102Tests.ps1 -Case notes` | PASS. Lokalne mieszane RTF, formatowanie fragmentów, obraz 600×771 przy widoku 650 px i 361×464 przy widoku 390 px, zapisane 600 po zwężeniu, przeniesienie mapy, PNG bez tekstu, odczyt pliku i zakres podkreślenia przy kursorze oraz przy jawnym zaznaczeniu. Porównane pozycje tekstu względem wysokości obrazu. |
| `Run-Patch0101Tests.ps1 -Case notes` | PASS. Uruchomiono wyłącznie zależny scenariusz, który obejmuje dotychczasowe mieszane wklejanie, tabelę i duże źródło 2800×5400. Wynik źródła zachował przezroczystość i limit 2048 px. |

Na wyeksportowanych źródłach bazowych powtórzono sam pomiar ruchu, żeby odróżnić zmianę pozycji od malowania. Geometria podążała za wszystkimi dziewięcioma zadanymi pozycjami. Przy odsłonięciach 85, 5, 89 i 61 px nie odnotowano zdarzenia Paint przed powrotem z obsługi ruchu zarówno w widoku obiektu, jak i heksa. Kandydat odmalował każdy pas w tym czasie. Łączny pomiar dziewięciu kroków obejmował około 79 → 102 ms dla obiektu i 154 → 200 ms dla heksa. W kandydacie obejmuje już wykonane malowanie. To pomiar ścieżki obsługi, bez pomiaru klatek ekranu.

Próbę opisu powtórzono po wykryciu odrzucania pośredniego Viewbox przez XamlPackage i po korekcie granic wyboru bloku, które WPF umieszcza wewnątrz jego elementu. Próbę panelu powtórzono po poprawie rozmiaru pola, ponieważ natywna preferowana wysokość 59 px przekraczała 54 px nagłówka. Przypadki `fixture` przygotowały wyłącznie własne dane.

## 4. Próba zwykłego EXE

- Baza 0.10.1. Przeciągnięto panel obiektu oraz panel heksa w obie strony. Po poszerzeniu widoku opisu wklejonego z Worda fragmenty tekstu były w wierszu obrazu. W pliku testowym oba fragmenty pozostały zapisane.
- Kandydat 0.10.2. Poszerzono i zwężono oba widoki. Osobno sprawdzono duże pola nazwy w heksie, obiekcie i grupie, plus przy ośmiu stanach, pełne dolne ramki promienia grupy i widzenia obiektu oraz wygląd czterech źródeł ikony w dialogu. Na starcie sprawdzono krótką i długą nazwę z wersją oraz wpis bez metadanej.
- Z rzeczywistego Worda wklejono tekst–obraz–tekst i sam obraz. Sprawdzono zwiększenie obrazu, zmianę szerokości w obu kierunkach, Powiększ opis, Ctrl+Shift+V jako zwykły tekst oraz Ctrl+U w środku pierwszego słowa przed 15 spacjami. W zapisanym opisie tylko pięć liter pierwszego słowa miało flagę podkreślenia. Przeniesiona kopia mapy po otwarciu zachowała tekst nad i pod osadzonym obrazem oraz jego szerokość 375.

## 5. Checklista odbiorcza

| Czynność | Oczekiwany wynik |
|---|---|
| Powoli zwęż i poszerz panel obiektu, zatrzymując ruch przed puszczeniem | Uchwyt jest przy kursorze, a odsłonięte płótno pokazuje aktualną mapę jeszcze przy wciśniętym przycisku. |
| Wklej z Worda długi tekst, obraz i dalszy tekst. Zmień szerokość i otwórz zapisaną mapę ponownie | Obraz zachowuje proporcje i ma własną wysokość pomiędzy fragmentami tekstu. |
| Naciśnij Ctrl+U z kursorem wewnątrz słowa przed 15 spacjami | Podkreślone jest słowo. Przy jawnym zaznaczeniu podkreślenie obejmuje także zaznaczone spacje. |
| Obejrzyj długi wpis Ostatnich projektów | Wielokropek dotyczy nazwy, pełna wersja jest po prawej w tym samym wierszu. |
| Edytuj nazwę oraz pokaż i ukryj promień | Pole nazwy ma wielkość nagłówka. Widoczna jest cała ramka promienia, a wysokość wiersza się nie zmienia. |

## 6. EXE i Git

Baza `c8a85f8`, początkowy Git czysty. Aplikacja 0.10.2, FileVersion i ProductVersion 0.10.2.0. SHA-256 głównego EXE i kandydata `D2941C3E86A6886A263B15AE442F8480EC6E354F8EFC60125E0858863B2AA2EA`. `fc /b` potwierdził identyczność bajtową. Jeden lokalny commit kodu, EXE, źródeł testów i dokumentacji. Dane, kopie i logi pozostają w ignorowanym `tests/_work`. Identyfikator i stan po commicie są podane w podsumowaniu.
