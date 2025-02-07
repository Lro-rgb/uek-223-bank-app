# Projekt Setup

Um mit dem Modul 223 zu starten, müssen wir unsere Geräte bereit machen für die Entwicklung 
von Fullstack Applikationen. Fullstack heisst von der Datenbank bis zum Frontend.

## Datenbank Setup mit Docker

Installier Docker. Gehe dabei professionell vor und informieren dich zuerst über das Produkt und die Installation. Du kannst dabei das Web oder auch YouTube verwenden. Gehe bei der Installation sorgfältig vor und kontrollieren die Einstellungen jeweils. [docker](https://docs.docker.com/get-docker/)

Schau dir unbedingt das [README](https://gitlab.ict-learnfactory.net/223/bank-app/-/blob/main/README.md) File an und folge den Anweisungen.

Starte den Container mit der MariaDB über das bereits vorhandene `docker-compose.yml` file.

**Achtung!**: Falls MariaDB bereits auf ihrem System installiert ist, musst du den Port in der `docker-compose.yml` Datei anpassen oder den Windows Dienst beenden.

## Dev Setup

Eine Person in der Gruppe lädt das [Vorlagenprojekt](https://gitlab.ict-learnfactory.net/223/bank-app) herunter 
und teilt es mit den anderen in über eure GitLab Gruppe (Initialer Commit). Stellt sicher, dass ihr alle Zugriff auf das Projekt habt und Git Commits machen könnt.

### Tools

Für die Frontendentwicklung:

- [IntelliJ](https://www.jetbrains.com/idea/)
- oder [Visual Studio Code](https://code.visualstudio.com/)
- [Node 22](https://nodejs.org/en/)
    - Am besten installierst du Node über [NVM](https://github.com/nvm-sh/nvm). [Windows Link](https://github.com/coreybutler/nvm-windows).


Für die Backendentwicklung:

- [Rider](https://www.jetbrains.com/rider/)
- oder [Visual Studio](https://visualstudio.microsoft.com/de/vs/)
- [Dotnet 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)


Sonstiges:

- [Datagrip](https://www.jetbrains.com/datagrip/)
- [Git](https://git-scm.com/)

### Backend appsettings.json

Kopier das `appsettings.sample.json` wie im [README](https://gitlab.ict-learnfactory.net/223/bank-app/-/blob/main/Backend/README.md) beschrieben. Dabei musst du den Connection String anpassen mit den Werten aus dem `.env` File.

Wenn all das gemacht ist, solltes du das Frontend und Backend starten können.
Diese finden du im Repository unter `frontend` und `backend` (`Bank.Cli` und `Bank.Web`).

Probiere dich einzuloggen. Falls das klappt bist du bereit für das Modul 223. Ein Valides Login ist irgendwo im Repository "versteckt".

Wenn das geklappt hat, starte auch noch das `cli` Projekt.

Falls es nicht startet, könnte es daran liegen, dass das `appsettings.json` nicht sauber kopiert wird im Build Prozess. 
Im `.csproj` File muss das konfiguriert werden. [Beispiel](https://gitlab.ict-learnfactory.net/223/bank-app/-/blob/main/Backend/Bank.Web/Bank.Web.csproj?ref_type=heads#L25).