# DentaCare

Web aplikacija stomatološke ordinacije. Projektni zadatak iz predmeta Razvoj softvera I (FIT, Univerzitet "Džemal Bijedić" u Mostaru, 2026/27). Radi se samostalno, na početnom projektu predmeta.

## Tehnologije

- Backend (`Market.Backend`): ASP.NET Core Web API, .NET 10, Clean Architecture (Domain, Application, Infrastructure, API), CQRS (MediatR), FluentValidation, EF Core, SQL Server.
- Frontend (`Market.Frontend`): Angular 21, Angular Material, ngx-translate (bosanski i engleski).
- Baza: `localhost,1433`, korisnik `sa`, lozinka `test` (samo za lokalni razvoj).
- Pokretanje: backend `dotnet run --project Market.API --launch-profile https` (iz `Market.Backend`), frontend `npm start` (iz `Market.Frontend`). Detalji su u `README.md`.

## Uloge i glavni flow

Uloge: Admin, Dentist, Patient (enum `Role` na `MarketUser`).

Glavni flow: registracija pacijenta, izbor usluge i stomatologa, prikaz slobodnih termina, zakazivanje, podsjetnik (upit za termine u narednih 24 h), otkazivanje ili pomjeranje najkasnije 24 h prije početka, završetak posjete (izvedene usluge i nalaz), račun, plaćanje u KM ili EUR, loyalty bodovi, ocjena stomatologa, povrat novca pri otkazivanju plaćenog termina.

## Poslovna pravila

- Loyalty bodovi: 1 bod po plaćenoj KM. Nivoi: Bronze, Silver, Gold. Popust ne smije preći iznos računa.
- Plaćanje je simulirano (bez pravog novca). Uz svako plaćanje i povrat čuvaju se valuta, iznos, primijenjeni kurs i RateValidFromUtc. Preračun ide kroz `ICurrencyConverter` iz startera (bazna valuta KM).
- Cijena usluge se kopira u `AppointmentService` (`PriceAtTime`), da kasnija promjena cjenovnika ne mijenja stare račune.
- Ocjena stomatologa: jedna po terminu, samo za završen termin. Ocjena se može izmijeniti, ali se ne briše.
- Svi entiteti nasljeđuju `BaseEntity` (`Id`, `IsDeleted` za soft delete, `CreatedAtUtc`, `ModifiedAtUtc`). `DentalService.IsActive` znači "trenutno se ne nudi", a `IsDeleted` znači obrisano.
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
- Imena claimova u tokenu su kratka, iz AppClaimTypes (Market.Shared/Constants). JwtBearer ima
  MapInboundClaims = false i RoleClaimType = AppClaimTypes.Role. Frontend čita ista imena.

## Način rada (git)

- Jedan issue = jedna grana = jedan pull request. Grana se zove `<broj>-kratak-opis` (npr. `12-slobodni-termini`).
- Nikad se ne radi direktno na `main` (osim ovog fajla).
- Commit poruka sadrži broj issuea, npr. "Potvrda termina (#12)". Pull request u opisu ima `Closes #<broj>`, opis šta je urađeno i kako je testirano.
- Ne praviš commit ni push sam. To radim ja.
- U commit ne ulaze fajlovi koji ne pripadaju issueu. Fajlove dodajem pojedinačno (`git add <fajl>`), ne `git add .`.
- `package-lock.json` ulazi u commit samo kad je izmjena namjerna: popravka lock fajla (#3) ili dodavanje, uklanjanje ili nadogradnja paketa. Inače ga vraćam sa `git restore Market.Frontend/package-lock.json`.
- Prije izmjene koda predloži plan i sačekaj potvrdu.

## Dijagrami

ER dijagram je u `dokumenti/er-dijagram.png`. Ako se promijene entiteti, podsjeti me da ga ažuriram.

## Pravila za tebe

- Objašnjavaj jednostavnim jezikom šta si uradio i zašto. Moram razumjeti svaki red koda, jer se odbrana radi pismeno, bez interneta, bez AI alata i bez pristupa projektu.
- Pitaj me prije brisanja baze ili pokretanja migracija.
- Kad se promijeni odluka ili pravilo, predloži izmjenu ovog fajla.
