# DentaCare

Web aplikacija stomatološke ordinacije. Projektni zadatak iz predmeta Razvoj softvera I (FIT, Univerzitet "Džemal Bijedić" u Mostaru, 2026/27). Radi se samostalno, na početnom projektu predmeta.

## Tehnologije

- Backend (`Market.Backend`): ASP.NET Core Web API, .NET 10, Clean Architecture (Domain, Application, Infrastructure, API), CQRS (MediatR), FluentValidation, EF Core, SQL Server.
- Frontend (`Market.Frontend`): Angular 21, Angular Material, ngx-translate (bosanski i engleski).
- Baza: `localhost,1433`, korisnik `sa`, lozinka `test` (samo za lokalni razvoj).
- Pokretanje: backend `dotnet run --project Market.API --launch-profile https` (iz `Market.Backend`), frontend `npm start` (iz `Market.Frontend`). Detalji su u `README.md`.
- Backend pri pokretanju sam primijeni nove migracije (`MigrateAsync` u `DatabaseInitializer`).

## Uloge i glavni flow

Uloge: Admin, Dentist, Patient (enum `Role` na `MarketUser`).

Glavni flow: registracija pacijenta, izbor usluge i stomatologa, prikaz slobodnih termina, zakazivanje, podsjetnik (upit za termine u narednih 24 h), otkazivanje ili pomjeranje najkasnije 24 h prije početka, završetak posjete (izvedene usluge i nalaz), račun, plaćanje u KM ili EUR, loyalty bodovi, ocjena stomatologa, povrat novca pri otkazivanju plaćenog termina.

## Poslovna pravila

- Loyalty bodovi: 1 bod po plaćenoj KM. Nivoi: Bronze, Silver, Gold. Popust ne smije preći iznos računa.
- Plaćanje je simulirano (bez pravog novca). Uz svako plaćanje i povrat čuvaju se valuta, iznos, primijenjeni kurs i RateValidFromUtc. Preračun ide kroz `ICurrencyConverter` iz startera (bazna valuta KM).
- Cijena usluge se kopira u `AppointmentService` (`PriceAtTime`), da kasnija promjena cjenovnika ne mijenja stare račune.
- Ocjena stomatologa: jedna po terminu, samo za završen termin. Ocjena se može izmijeniti, ali se ne briše.
- Svi entiteti nasljeđuju `BaseEntity` (`Id`, `IsDeleted` za soft delete, `CreatedAtUtc`, `ModifiedAtUtc`). `DentalService.IsActive` znači "trenutno se ne nudi", a `IsDeleted` znači obrisano.
- Šifarnik koji koristi drugi entitet (npr. Specialization kod Dentist) ne smije se obrisati dok je u upotrebi (409).
- Podsjetnici su samo unutar aplikacije (upit, bez pozadinskog servisa i bez emaila).

## Tehnička pravila (iz uputa predmeta)

- Kod i komentari su na engleskom (nazivi klasa, metoda, varijabli). Tekst koji korisnik vidi može biti na bosanskom.
- Entitet se nikad ne vraća kroz API, nego DTO.
- Svaka forma ima validaciju te provjeru autentifikacije i autorizacije na frontendu i backendu (FluentValidation na backendu).
- Paging i filteri na svim CRUD listama.
- `async`/`await` i `CancellationToken`. Dependency Injection na frontendu i backendu.
- Konfiguracija ide u `appsettings*.json` i `environment*.ts`, ništa hardkodirano.
- Tajne (lozinke, API ključevi) nikad ne idu u repozitorij, jer je javan. Koristiti user-secrets ili environment varijable.
- Najmanje 2 unit testa nad mojom logikom. Angular komponente, moduli i lazy loading.
- Imena claimova u tokenu su kratka, iz `AppClaimTypes` (`Market.Shared/Constants`). JwtBearer ima `MapInboundClaims = false` i `RoleClaimType = AppClaimTypes.Role`. Frontend čita ista imena.

## Konvencije koda

- Novi CRUD (šifarnik ili jednostavan entitet) radi se po uzoru na `Catalog/Specializations`, backend i frontend.
- Backend:
  - file-scoped namespace (`namespace X;`), bez nekorištenih `using` redova;
  - svaka akcija je folder u `Modules/<Grupa>/<Entitet>/Commands|Queries/<Akcija>/` s Command/Query, Dto, Handler i (za Create/Update) Validator;
  - imena u jednini kao `ListSpecializationQuery`, `ListSpecializationQueryDto`;
  - svako pravilo validatora ima `.WithMessage(...)` na bosanskom; granice (npr. dužina) dolaze iz `Entity.Constraints`;
  - jedinstveni indeks uvijek s `HasFilter("[IsDeleted] = 0")`; validator za Update isključuje vlastiti red (`x.Id != command.Id`);
  - `AsNoTracking()` samo kod čitanja, nikad u Update i Delete handleru; brisanje je soft delete kroz `Remove`;
  - Id za Update dolazi iz rute (`[JsonIgnore]` na `Id` u komandi);
  - ruta kontrolera `api/<ime-u-mnozini>` malim slovima s crticom (npr. `api/service-categories`); `[Authorize(Policy = ...)]` na klasi kad sve akcije imaju istu ulogu;
  - referentni podaci idu u `StaticDataSeeder` (`HasData`, fiksni Id, `SeedDateUtc`), demo podaci u `DynamicDataSeeder`.
- Frontend:
  - fajlovi `ime.vrsta.ts` (`.service.ts`, `.model.ts`, `.component.ts`);
  - tipovi prate backend DTO-e i imena (`ListSpecializationQuery` → `ListSpecializationRequest`);
  - lista nasljeđuje `BaseListPagedComponent`, forma `BaseFormComponent` (ista komponenta za dodavanje i izmjenu, rute `/new` i `/:id/edit`);
  - greške sa servera kroz `applyServerErrors`, potvrda brisanja kroz `DialogHelperService.confirm`;
  - sav tekst za korisnika kroz prevode u `bs.json` i `en.json`.

## Način rada (git)

- Jedan issue = jedna grana = jedan pull request. Grana se zove `<broj>-kratak-opis` (npr. `12-slobodni-termini`).
- Nikad se ne radi direktno na `main` (osim ovog fajla).
- Commit poruka počinje brojem issuea, npr. "#12 - Potvrda termina". Pull request u opisu ima `Closes #<broj>`, opis šta je urađeno i kako je testirano.
- Ne praviš commit ni push sam. To radim ja.
- U commit ulaze samo fajlovi ili folderi koji pripadaju issueu (npr. `git add Market.Backend`), nikad `git add .`. Prije i poslije `git add` provjeravam `git status`.
- `package-lock.json` ulazi u commit samo kad je izmjena namjerna: popravka lock fajla (#3) ili dodavanje, uklanjanje ili nadogradnja paketa. Inače ga vraćam sa `git restore Market.Frontend/package-lock.json`.
- Prije izmjene koda predloži plan i sačekaj potvrdu.

## Dijagrami

ER dijagram je u `dokumenti/er-dijagram.png`. Ako se promijene entiteti, podsjeti me da ga ažuriram.

## Pravila za tebe

- Objašnjavaj jednostavnim jezikom šta si uradio i zašto. Moram razumjeti svaki red koda, jer se odbrana radi pismeno, bez interneta, bez AI alata i bez pristupa projektu.
- Smiješ dodati migraciju (`dotnet ef migrations add`) i pokazati mi njen `Up`. Pitaj me prije brisanja baze, `dotnet ef database update` ili ručne izmjene fajla migracije.
- Poslije izmjena pokreni `dotnet build`, `dotnet test` i/ili `npm run build`, zavisno šta je mijenjano.
- Na kraju napiši spisak kreiranih i izmijenjenih fajlova.
- Kad se promijeni odluka ili pravilo, predloži izmjenu ovog fajla.
