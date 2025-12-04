# Die Applikation

Die Bank hat jemanden eingestellt, der dieses Modul nicht besucht hat. Die Person schreibt ein Programm, das Buchungen auf Kontos vornimmt, und alles funktioniert im Singleuserbetrieb perfekt.

## Aufgabenstellung

Du erstellst in diesem Auftrag eine Konsolenapplikation, welche die Probleme von Multiuseranwendungen aufzeigt.

### Teilaufgabe 1: Repetition SQL

Repetiere SQL kurz. Du solltest in der Lage sein:

- Eine Tabelle mit Primary- und Foreign Key zu erstellen und zu löschen.
- Daten einzufügen (INSERT).
- Daten zu löschen (DELETE).
- Daten zu ändern (UPDATE).
- Daten über mehrere Tabellen hinweg sortiert und gefiltert abzufragen  
  `(SELECT … FROM … WHERE … ORDER BY …)`

**Mögliche Quellen:**

- Modul 106/(105) [empfohlen]
- [Tutorialspoint](https://www.tutorialspoint.com/sql/sql-overview.htm)
- [W3 Schools](https://www.w3schools.com/sql/default.asp)
- YouTube

---

### Teilaufgabe 2: Fragen zum Programm

Es wurde schon ein Grossteil des Programms erstellt. Schau dir mal das Backend an.

Die Applikation besteht aus **3 Projekten**:

- **Cli:**  
  Im Main-Projekt `Cli` befindet sich die Datei `Program.cs` und das Command Line Interface.
  
- **Core:**  
  Hier befinden sich die Models.
  
- **DbAccess:**  
  Hier befinden sich die Datenbankzugriffe. In diesem Projekt findest du auch die Klasse `DatabaseSeeder.cs`, die die Testdaten erstellt.

> **Hinweis:** Das `Web`-Projekt wird in diesem Auftrag nicht benötigt und kann ignoriert werden.

#### Klasse `Ledger`

Steht für ein Konto.

- `id` ist der Primary Key.
- `name` ist der Name des Kontos.
- `balance` ist der aktuelle Kontostand.

---

#### Analysiere das Programm und beantworte folgende Fragen:

1. Wie wird verhindert, dass die Testdaten bei jedem Start wieder eingefügt werden?
2. Recherchiere, was ein `HashSet` und was ein `ImmutableHashSet` ist.
3. Welcher SQL-Befehl zählt alles Geld zusammen?
4. Was bedeutet `<<Property>>`?
5. Was bedeutet `<<Readonly>>`?
6. Wieso wird nicht `double` für den Kontostand verwendet?

---

Bringe das Programm zum Laufen und sieh dir mit DataGrip oder einem anderen Tool die Tabellen an.

Bringe folgenden Ablauf des Programms in die korrekte Reihenfolge:

1. Es werden alle Ledgers (Kontos) abgefragt und ausgegeben.
2. Das Geld in allen Ledgers wird am Ende zusammengezählt und ausgegeben.
3. Wenn die Tabelle `Ledgers` leer ist, werden Testdaten geseeded (eingefügt).
4. Das Geld in allen Ledgers wird zu Beginn zusammengezählt und ausgegeben.
5. Hier wäre Platz für deinen Code.
6. Wenn die Datenbank `bank` noch nicht existiert, wird sie erstellt.
7. Wenn die Tabelle `Ledgers` noch nicht existiert, wird sie erstellt.

---

Erstelle für dich eine kurze Referenz, die du beim Programmieren verwenden kannst, mit einem praktischen Codebeispiel für folgende Dinge:

1. Wie wird in C# eine Tabelle erstellt?
2. Wie wird in C# ein Datensatz eingefügt?
3. Wie wird in C# ein Datensatz geändert?
4. Wie werden in C# mehrere Datensätze abgefragt?
5. Wie wird in C# ein Wert (wie die Summe allen Geldes) abgefragt?

## Lern- und Arbeitsauftrag

Der Lern- und Arbeitsauftrag ist erfüllt, wenn:

- Du das Programm zum Laufen gebracht hast.
- Du das Programm studiert hast und den Ablauf sowie die Klassen erklären kannst.
- Du zeigen kannst, wie mit C# DDL-Befehle an die Datenbank gesendet werden können.
- Du zeigen kannst, wie mit C# DML-Befehle an die Datenbank gesendet werden können.
- Du zeigen kannst, wie mit C# Daten in der Datenbank abgefragt werden können.
- Du zeigen kannst, wie mit C# Daten in der Datenbank eingefügt und geändert werden können.