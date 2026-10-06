-- Läuft nur beim ersten Start mit leerem Volume (nach docker compose down -v).
-- Eigene Datenbank für die Integrationstests, damit die Tests nie die Daten in "bank" verändern.
CREATE DATABASE IF NOT EXISTS bank_test;
GRANT ALL PRIVILEGES ON bank_test.* TO 'bank_user'@'%';
FLUSH PRIVILEGES;
