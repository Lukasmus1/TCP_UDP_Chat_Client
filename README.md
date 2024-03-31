# Dokumentace k IPK projektu "Klient k chatovacímu serveru"
### Autor: Lukáš Píšek (xpisek02)
## Obsah <a name="obsah"></a>
- [Obsah](#obsah)
- [Úvod](#uvod)
- [Implementace](#impl)
    - [Použité nástroje](#impl1)
    - [Začátek programu](#impl2)
    - [Funkcionalita](#impl3)
        - [Společná](#impl3-1)
        - [TCP Protokol](#impl3-2)
        - [UDP Protokol](#impl3-3)
- [Testování](#test)
    - [TCP](#test1)
    - [UDP](#test2)
- [Makefile](#make)
- [Závěr](#end)

## Úvod <a name="uvod"></a>
Cílem řešeného projektu bylo vytvořit konzolovou aplikaci, která bude umožňovat komunikaci mezi uživatelem a serverem za pomocí `IPK24-CHAT` protokolu. Klient podporuje připojení pomocí protokolů TCP a UDP s potvrzováním příchodu zpráv. Veškeré zprávy a důležité informace o stavu serveru / klienta se vypisují na `stdout`. Uživatel zadává příkazy / posílá zprávy psaním do příkazové řádky (`stdin`). 

## Implementace <a name="impl"></a>
### 1. Použité nástroje <a name="impl1"></a>
Na vypracování tohoto projektu jsem použil následující nástroje:
-	Operační systém – Windows 11 pro celkový vývoj a částečně [WSL](https://en.wikipedia.org/wiki/Windows_Subsystem_for_Linux) (Ubuntu) pro testování
-	Programovací jazyk – C#
-	IDE – [Rider](https://www.jetbrains.com/rider/) pro vývoj na Windows a [VSCode](https://code.visualstudio.com) pro vývoj na Linux.
-   Verzovací systém – Git
-	Jako pomoc při programování a pro obecné otázky – Github Copilot
### Začátek programu (Metoda `Main()`) <a name="impl2"></a>
Za pomocí knihovny `CommandLine` a mnou vytvořené knihovny `ArgParserOptions` se jako první věc zpracujou argumenty programu zadané uživatelem. 
Následně se pomocí knihovny `System.Net.Sockets` vytvoří nová instance `TcpClient` nebo `UdpClient` třídy, kde TCP verze se také okamžitě pokusí připojit k danému serveru na daném portu a provede tzv. [3-Way Handshake](https://www.geeksforgeeks.org/tcp-3-way-handshake-process/)."
Po těchto procesech se následně zavolá metoda `MainBegin()`, která má vlastní implementaci podle zvoleného protokolu pro připojení.
### Funkcionalita <a name="impl3"></a>
#### 1. Společná <a name="impl3-1"></a>
Celá funkcionalita je založená na spuštění dvou asynchronních metod a jedné "hlavní" metody.

Jako první věc se spustí 2 asnychronní metody `GetInputAsync` a `GetResponseAsync`, které neustále běží na pozadí v cyklu a vykonávají:
- `GetInputAsync` neustále po řádcích čte text zadaný do `stdin`, který následně ukládá do atributu typu `Queue<string> _inputs` se kterým se potom synchronně pracuje.
- `GetResponseAsync` neustále čte příchozí zprávy ze serveru a ukládá je do atributu typu `Queue<string> _responses` u TCP a `Queue<string> _responsesStr`u UDP se kterými se poté synchronně pracuje.

Stavový automat je implementován pomocí speciální třídy `enum`, která v sobě uchovává názvy stavů. V hlavní metodě se potom pomocí cyklu neustále kontroluje v jakém stavu se program momentálně nachází a následně se volají metody se stejným názvem jako mají stavy. Tyto metody jsou uloženy ve třídě `StatesBehaviour` a chovají se jako hlavní funkcionalita daného stavu.
Ukončení programu přes klávesovou zkratku je řešena přes `event` s názvem `CancelKeyPress`, který je implementován třídou `Console`. Tento `event` se následně přidává mnou vytvořené metodě `EndProgram`, která ještě před ukončením programu pošle serveru zprávu `BYE`. 

#### 2. TCP Protokol <a name="impl3-2"></a>
Implementace tohoto protokolu je napsána ve třídě s názvem `TcpChatClient`, která implementuje `interface` s názvem `IClient` pro jednoduchost volání hlavní třídní metody ve hlavní třídě. 
Jelikož byla TCP verze psána jako první, tak se v mnoha ohledech chová jako taková "předloha," kterou poté implementuje UDP verze. 
Metoda `SendInput`, která posílá zadané zprávy na server je zde implementována tak, aby pro jednoduchost jako atribut měla `string`, jelikož TCP posílá zprávy přes třídu `NetworkStream`.
Na konci programu se musí uzavřít daný `NetworkStream` za pomocí metody `Close()`.

#### 3. UDP Protokol <a name="impl3-3"></a>
UDP protokol je dle implementace udělán aby byl "[connectionless](https://en.wikipedia.org/wiki/User_Datagram_Protocol)," to znamená, že se nepřipojuje na žádný server. Proto namísto připojení jako u TCP varianty musel být vytvořen `IPEndPoint` s adresou a portem serveru, na který se budou dané zprávy posílat.
Pro komunikaci mezi klientem a serverem se využívá protkol `IPK24-CHAT`, jehož zprávy vypadají následovně: 
```
+----------------+------------+------------+--------------+
|  Local Medium  |     IP     |     UDP    |  IPK24-CHAT  |
+----------------+------------+------------+--------------+
```
Pro znovuvyužití metod ze třídy `StatesBehaviour`, které očekávají argumenty typu, který se používá při TCP variantě byly vytvořeny 2 pomocné metody:
- `ConvertToBytes` překonvertuje vstup typu `string` na `List<byte>`, který se následně pošle na server.
- `ConvertToString` překonvertuje vstup typu `byte[]` na `string`, který má stejné formátování jako vstupy z TCP varianty.

Za pomocí těchto 2 metod můžu znovupoužít stejné metody pro zpracování stavů jako u TCP. 
Kvůli zpracovávání potvrzení o přijetí zpráv je zde také metoda s názvem `HandleOutput`, která se volá namísto metody `SendIpnut`. Tato metoda po odeslání zprávy na server asynchronně čeká na zpáteční potvrzující zprávu tak, že pomocí knihovny `System.Diagnostics` vytvoří novou instanci třídy `Stopwatch` a pomocí té následně počítá čas. Pokud tento čas překročí uřivatelem zadanout hodnotu pro časový limit pro odpověď. Inkrementuje se proměnná `tries` a pokud tato proměnná překročí uživatelem zadanou hodnotu pro maximální počet pokusů, program vyšle na server zprávu `BYE` a ukončí se.
Samotná třída `UdpClient` používá pro posílání zpráv metodu `Send`. Z tohoto důvodu metoda `SendInput` má jaku typ atributu `List<byte>`.

## Testování programu <a name="test"></a>
#### 1. TCP <a name="test1"></a>
Testování TCP ze začátku probíhalo pomocí nástroje `netcat`. Na mém WSL jsem spustil příkaz `sudo nc -lkp 1000`, kde jsem naslouchal na zadaný port (pro testovací účely jsem zvolil čistě náhodný port 1000, který v ten moment žádný jiný program nevyužíval). Toto mi umožňovalo připojit se na `localhost` a poté si mimo jiné posílat jedoduché odpovědi.
Později byla nutnost testovat pokročilejší chování. K tomuto účelu byl využit jednoduchý TCP server.
Bylo testováno odesílání a poté přijímaní odpovídajících zpráv. Celá tato komunikace byla následně monitorována aplikací [Wireshark](https://www.wireshark.org) za pomocí přiloženého skriptu `ipk24-chat.lua`.
#### 2. UDP <a name="test2"></a>
Testování UDP probíhalo obdobně jako TCP, jelikož většina metod je sdílená. 
Pouze byla nutnost otestovat časomíru pro ukončení programu při neobdržření odpovědi do daného času. Toto bylo otestováno za pomocí jednoduchého skriptu, který posíal tyto potvrzující zprávy s nastavitelným zpožděním.

## Makefile používání <a name="make"></a>
- `make build` Překlad programu a uložení spustitelného souboru do složky `publish`
- `make run ARGS="arg1 arg2..."` Spuštení programu s argumenty `arg1` `arg2`...
- `make clean` Vyčištění adresáře + smazání složky `publish` 

## Závěr <a name="end"></a>
Tento projekt mě naučil spoustu nových věcí. Naučil jsem se co jsou protokoly TCP a UDP, jak se liší a jak s nimi pracovat. 
Naučil jsem se posílat zprávy na server a následně tyto zprávy zpracovávat.
Také jsem poprvé aktivně využil a naučil se pracovat se softwarem Wireshark pro kontrolu provozu dat na mé síti.
Dokázal jsem následovat zadání a naprorgamovat rozsáhlý program s částečným využitím OOP.
Obohatil jsem si znalosti co se týče vícevláknového programování v C#.