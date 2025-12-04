# Projekt Setup

Um mit dem Modul 223 zu starten, müssen wir unsere Geräte bereit machen für die Entwicklung von Fullstack-Applikationen. *Fullstack* bedeutet, dass alles von der Datenbank bis zum Frontend abgedeckt wird.

## Datenbank Setup mit Docker

1. **Installiere Docker:**  
   Informiere dich zunächst professionell über das Produkt und die Installation. Du kannst dazu das Web oder YouTube nutzen. Gehe bei der Installation sorgfältig vor und kontrolliere die Einstellungen.  
   [Docker – Get Docker](https://docs.docker.com/get-docker/)

2. **Lies das README:**  
   Schau dir unbedingt das [README](https://gitlab.ict-learnfactory.ch/223/bank-app/-/blob/main/README.md) File an und folge den Anweisungen.

3. **Starte den Container:**  
   Starte den Container mit der MariaDB über das bereits vorhandene `docker-compose.yml` File.

> **Achtung!**  
> Falls MariaDB bereits auf deinem System installiert ist, musst du entweder den Port in der `docker-compose.yml` Datei anpassen oder den Windows-Dienst beenden.

## Dev Setup

Eine Person in der Gruppe lädt das [Vorlagenprojekt](https://gitlab.ict-learnfactory.ch/223/bank-app) herunter und teilt es mit den anderen in eurer GitLab-Gruppe (initialer Commit). Stellt sicher, dass ihr alle Zugriff auf das Projekt habt und Git-Commits machen könnt.

### Tools

#### Für die Frontendentwicklung

- [IntelliJ](https://www.jetbrains.com/idea/)
- oder [Visual Studio Code](https://code.visualstudio.com/)
- [Node 22](https://nodejs.org/en/)
  - Am besten installierst du Node über [NVM](https://github.com/nvm-sh/nvm).  
    [Windows-Link](https://github.com/coreybutler/nvm-windows).

#### Für die Backendentwicklung

- [Rider](https://www.jetbrains.com/rider/)
- oder [Visual Studio](https://visualstudio.microsoft.com/de/vs/)
- [Dotnet 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

#### Sonstiges

- [DataGrip](https://www.jetbrains.com/datagrip/)
- [Git](https://git-scm.com/)

> **Achtung!**  
> Öffne mit Rider die Solution `./Backend/Bank.sln` und mit IntelliJ den Ordner `./Frontend`.

---

### Backend: appsettings.json

- Kopiere das `appsettings.sample.json` wie im [README](https://gitlab.ict-learnfactory.ch/223/bank-app/-/blob/main/Backend/README.md) beschrieben.
- Passe dabei den Connection String mit den Werten aus dem `.env` File an.

---

Wenn all das erledigt ist, solltest du das Frontend und Backend starten können.  
Du findest sie im Repository unter `frontend` und `backend` (nämlich `Bank.Cli` und `Bank.Web`).

- Probiere dich einzuloggen.  
  Falls das klappt, bist du bereit für das Modul 223.  
  Ein valides Login ist irgendwo im Repository "versteckt".

- Starte auch noch das `cli` Projekt.

> **Hinweis:**  
> Falls es nicht startet, könnte es daran liegen, dass das `appsettings.json` nicht sauber kopiert wird im Build-Prozess. Im `.csproj` File muss dies konfiguriert werden.  
> [Beispiel](https://gitlab.ict-learnfactory.ch/223/bank-app/-/blob/main/Backend/Bank.Web/Bank.Web.csproj?ref_type=heads#L25).