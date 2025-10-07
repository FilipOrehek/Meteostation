# MeteoStation

Jednoduchá .NET konzolová aplikace, která každou hodinu stahuje data z meteostanice ve formátu XML, převádí je do JSON a ukládá do SQLite databáze.

## Požadavky
- .NET 8 SDK (nebo novější)
- NuGet balíček: `System.Data.SQLite`

Řešení je rozděleno na 3 hlavní třídy. 
Program.cs slouží k volání fukncí dvou dalších tříd, je zde navíc ošetřená logika volání funkcí v časovém intervalu 1 hodiny. 
Ve složce Data jsou uloženy třídy WeatherRecord.cs a DatabaseContext.cs. 
Weather Record.cs řeší stahování XML souboru z webu a jeho následné přetransformování na JSON a následné vracení JSON struktury. Pokud bude meteostanice offline, dojde k uložení chybové hlášky. DatabaseContext.cs se stará o vytvoření databáze a tabulek, pokud neexistují, dále ukládání dat z JSONu do databáze a ošetření v případě, kdy bude meteostanice offline.
Řešení také obsahuje appsetings.json, ve kterém je definovaná url adresa, ze které se stahuje XML soubor.

Celkové řešení bylo zhotoveno za cca 6 hodin.
