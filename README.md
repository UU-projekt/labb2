# PlacementInfraLab – Skalprojekt

Detta är skalprojektet för **Laboration II** i kursen om informationsinfrastrukturer.  

Syftet med projektet är att ge dig en tydlig struktur att utgå från, så att du kan fokusera på laborationens centrala frågor: hur data modelleras, hur olika källor kopplas samman, hur ett API-kontrakt utformas och hur information presenteras på ett begripligt sätt.

Skalprojektet innehåller därför redan den grundläggande strukturen för både API och gränssnitt. Din uppgift är inte att bygga allt från grunden, utan att fylla i den logik som saknas och samtidigt förstå varför lösningen ser ut som den gör.

## Förutsättningar

För att kunna köra projektet behöver du ha följande installerat:

- **.NET 10 SDK**
- **VS Code** med **C#**-tillägget från Microsoft

Du kan kontrollera att rätt .NET-version finns installerad genom att köra följande kommando i en terminal:

```bash
dotnet --version
```

## Öppna rätt mapp

Om du arbetar i VS Code är det viktigt att du öppnar mappen `starter` som din "workspace root".

De konfigurationsfiler som finns i .vscode-mappen utgår nämligen från att det är just denna mapp som är öppnad. D.v.s. att om du istället öppnar en annan mapp så kan debugkonfigurationerna peka fel och det är då inte möjligt att debugga projektet.

## Komma igång
Det enklaste sättet att komma igång är att först starta API-projektet och därefter gränssnittet.

1. Öppna mappen `starter` i VS Code.
2. Se till att API‑projektet och UI‑projektet använder samma basport. I `PlacementService.Ui/appsettings.Development.json` sätter du `Api:BaseUrl` till den adress där API:et körs, t.ex.:
    ```json
    "Api": {
        "BaseUrl": "http://localhost:5000"
    }
    ```
3. Starta API:t i en terminal:
   ```bash
   dotnet run --project PlacementService.Api
   ```
4. Starta UI:t i en separat terminal:
    ```bash
    dotnet run --project PlacementService.Ui
    ```
5. När båda projekten är igång kan du normalt nå:
    - API:t via Swagger på `https://localhost:5001/swagger` 
    - och UI:t på `http://localhost:5002`.
    - Om portarna skiljer sig från detta på din dator kan du kontrollera dem i respektive projekts `Properties/launchSettings.json`.

### Om du fastnar tidigt

Om någonting inte fungerar direkt är det nästan alltid bäst att börja med API:t. Börja därför med att kontrollera att:

* API:t faktiskt startar utan fel

* Swagger går att öppna

* endpointen `search` returnerar data

När du vet att API:t fungerar blir det betydligt enklare att avgöra om ett senare problem ligger i wrappern eller i gränssnittet.

### Arbeta med TODO-markeringarna

Skalprojektet innehåller ett antal `TODO`-kommentarer som markerar de delar där du förväntas skriva egen kod. Ett bra första steg är därför att söka efter `TODO` i projektet. För att undvika irrelevanta träffar kan det dock tänkas vara klokt att framför allt fokusera på:

* `PlacementService.Api`

* `PlacementService.Ui`

Du behöver inte förstå exakt allt i koden från början. Tanken är snarare att du ska kunna orientera dig i strukturen, följa dataflödet, och stegvis fylla i de delar som saknas.

## Debugga i VS Code

I projektet finns färdiga konfigurationsfiler i .vscode som gör det möjligt att bygga och debugga projekten direkt i VS Code.

### Vad innehåller mappen `.vscode`?

* `tasks.json` innehåller en build-uppgift som kör `dotnet build` för hela lösningen snarare än enbart det ena eller det andra projektet.

* `launch.json` definierar istället två debugkonfigurationer:
    - Där `Starter API` startar API‑projektet.
    - och där `Starter UI` startar UI‑projektet.

Detta innebär att du **inte** behöver skapa egna debuginställningar för att komma igång.

### Så kör du projektet i debugläge

1. Öppna debugpanelen (Ctrl + Shift + D på Windows eller CMD + Shift + D på macOS) och välj önskad konfiguration i rullistan (t.ex. `"Starter API"`).

2. Placera breakpoints i den kod du vill inspektera, t.ex. i `ScbPxWebClient` eller `PlacementServiceFacade`.

3. Du kan sedan starta debugkörningen med F5, där VS Code automatiskt kommer att bygga projektet och därefter starta applikationen i debugläge.

4. När exekveringen stannar på din breakpoint kan du exempelvis inspektera variabler, payloads, eller en call stack via debugpanelen. Detta är ofta det snabbaste sättet att förstå varför ett visst anrop ger ett oväntat resultat.

### När är debugging särskilt användbart?
Debugging är särskilt hjälpsamt i den här laborationen när du vill förstå:

* hur data ser ut när den kommer från `JobTech`

* hur `SSYK` normaliseras

* hur payloaden till `SCB PxWeb` byggs upp

* hur resultatet berikas med lönedata

* varför ett visst fält blir `null`

Om du till exempel vill förstå varför en viss lön inte hittas kan det vara klokt att sätta en breakpoint i `ScbPxWebClient` och följa hur tabellkod, årtal och urval byggs upp _innan_ anropet skickas.

Mer information om hur `tasks.json` och `launch.json` fungerar finns i VS Code‑dokumentationen: [Tasks](https://code.visualstudio.com/docs/debugtest/tasks), [Debug configuration](https://code.visualstudio.com/docs/debugtest/debugging-configuration), [Debugging in VS Code](https://code.visualstudio.com/docs/debugtest/debugging)

## Några praktiska råd på vägen

* Börja gärna med att få `search` att fungera innan du arbetar vidare med `summary`.

* Testa API:t i `Swagger` innan du försöker felsöka gränssnittet.

* Om UI:t inte får kontakt med API:t är det oftast `Api:BaseUrl` som pekar fel (eller att portarna i skalprojektet redan nyttjas av andra tjänster på din egen dator).

* Om du får träffar i `search` men ingen lön i `salary`, kontrollera vilken `SSYK`-kod som faktiskt skickas vidare.

* Kom ihåg att `salary` slår upp lön för en yrkesgrupp (`SSYK`), **inte** för en specifik annons.