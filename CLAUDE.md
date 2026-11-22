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
- Cijena usluge se kopira u `AppointmentService` (`PriceAtTimeBam`), da kasnija promjena cjenovnika ne mijenja stare račune.
- Ocjena stomatologa: jedna po terminu, samo za završen termin. Ocjena se može izmijeniti, ali se ne briše.
- Svi entiteti nasljeđuju `BaseEntity` (`Id`, `IsDeleted` za soft delete, `CreatedAtUtc`, `ModifiedAtUtc`). `DentalService.IsActive` znači "trenutno se ne nudi", a `IsDeleted` znači obrisano.
- Šifarnik koji koristi drugi entitet (npr. Specialization kod Dentist, ServiceCategory kod DentalService) ne smije se obrisati dok je u upotrebi (409). Neaktivna usluga se računa kao upotreba.
- Podsjetnici su samo unutar aplikacije (upit, bez pozadinskog servisa i bez emaila).
- Radno vrijeme (WorkingHours) unosi samo admin. Početak mora biti prije kraja. Intervali istog stomatologa u istom danu se ne preklapaju, a dodirivanje (08–12 i 12–16) je dozvoljeno. Brisanje stomatologa briše i njegova radna vremena.
- Usluga (DentalService) se ne briše, nego isključuje (`IsActive = false`). Nema Delete endpointa, jer bi obrisana usluga nestala iz starih termina i računa. Naziv usluge je jedinstven u cijeloj ordinaciji. Cijena je veća od 0 KM, najviše 2 decimale, do 10 000 KM. Trajanje je od 15 do 480 minuta, u koracima od 15 (zbog slobodnih termina).
- Loyalty nivoi (LoyaltyTier): naziv i prag bodova (MinPoints) su jedinstveni. Viši nivo mora imati strogo veći popust (0–40 %, najviše 2 decimale). Početni nivo (0 bodova) se ne briše (409) i prag mu ostaje 0; prepoznaje se po bodovima, ne po nazivu. Nivo koji koristi neki loyalty račun se ne briše (409). Seed: Bronze 0/0 %, Silver 500/5 %, Gold 1500/10 % (StaticDataSeeder).
- Registracija pravi MarketUser (uloga Patient), Patient i LoyaltyAccount na početnom nivou (0/0 bodova) jednim `SaveChangesAsync`, tj. u jednoj transakciji baze. Telefon i datum rođenja su opcionalni i unose se u profilu pacijenta (#9). PointsBalance se smanjuje pri korištenju bodova, LifetimePoints samo raste i po njemu se određuje nivo. Pacijent bez profila (stariji nalozi, demo seed) dobija ga u `SeedPatientProfilesAsync`.
- Slobodni termini se računaju iz radnog vremena, koje je u lokalnom vremenu ordinacije (zona iz `Clinic:TimeZoneId` u `appsettings.json`), pa se pretvaraju u UTC. Koraci su 15 minuta od početka intervala, a termin mora cijeli stati u interval. Termin zauzima svaki termin koji nije otkazan (`Status != Cancelled`); dodirivanje je dozvoljeno. Termini u prošlosti se ne nude.
- Slobodne termine vidi i neprijavljen posjetilac (`GET api/public/free-slots`), a za zakazivanje mora biti prijavljen (#13). Raspon je najviše 14 dana (oba datuma uključena, `FreeSlotCalculator.MaxRangeDays`). Lista nema paging, jer nije CRUD lista nego izračun nad ograničenim rasponom.
- Zakazivanje (`POST api/appointments`) smije samo pacijent; pacijent se uzima iz tokena (u body-ju nema `PatientId`). Pacijent bira jednu uslugu, `EndUtc` = početak + trajanje usluge, a puna cijena se kopira u `PriceAtTimeBam` (loyalty popust ide tek na račun).
- `StartUtc` mora biti u UTC (završava sa "Z"), inače 400. Početak mora biti tačno jedan od slobodnih slotova, provjeren ponovo kroz `FreeSlotCalculator` (radno vrijeme, korak 15 min, ne u prošlosti, ne preko zauzetih), inače 409.
- Pacijent ne smije imati dva termina koja se preklapaju, ni kod različitih stomatologa (409). Dodirivanje je dozvoljeno.
- Termin se može zakazati najviše `Clinic:MaxBookingDaysAhead` (60) dana unaprijed, po lokalnom datumu ordinacije (409). Free-slots poštuje istu granicu: iza nje vraća praznu listu.
- Zaštita od istovremenog zakazivanja: `Dentist.ScheduleVersion` je concurrency token. Svako zakazivanje mijenja `ScheduleVersion` u istom `SaveChangesAsync` kao i termin. Ako je neko drugi u međuvremenu zakazao kod istog stomatologa, EF baci `DbUpdateConcurrencyException`, ništa se ne snimi, a handler vraća 409. Unique indeks `(DentistId, StartUtc)` ostaje kao dodatna zaštita. Svaka buduća izmjena termina koja zauzima vrijeme (npr. pomjeranje u #15) mora takođe promijeniti `ScheduleVersion`.
- Otkazivanje (`POST api/appointments/{id}/cancel`, bez body-ja, 204) smiju pacijent i stomatolog (svako samo svoj termin; tuđi → 404), najkasnije `Clinic:CancellationMinHoursBefore` (24) sati prije početka, i samo termin u statusu Scheduled; inače 409. Pamte se `CancelledAtUtc` i `CancelledByUserId`, a detalji termina vraćaju `CancelledByRole`. Frontend (`environment.clinic.cancellationMinHoursBefore`) samo onemogući dugme; pravilo provjerava server.
- Pomjeranje termina (#58): samo pacijent, za vlastiti zakazan termin, najkasnije `Clinic:CancellationMinHoursBefore` sati prije starog početka (`AppointmentCancellationPolicy`). Mijenja se samo vrijeme: isti stomatolog, ista usluga, isti Id, a trajanje ostaje `EndUtc - StartUtc`. Novo vrijeme prolazi ista pravila kao zakazivanje (slobodan slot, pacijent nije zauzet, najviše `MaxBookingDaysAhead` dana unaprijed) i mijenja `Dentist.ScheduleVersion`. Termin koji se pomjera ne zauzima sam sebe (`excludeAppointmentId`). Pravila za zauzimanje vremena su na jednom mjestu, u `AppointmentSlotChecker`.

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
    - komanda koja ništa ne vraća je `IRequest` (handler `IRequestHandler<Command>`, `Task Handle`), ne `IRequest<Unit>` i `return Unit.Value` (starije Update/Delete komande se prebacuju u issueu "Sitnice");
  - svako pravilo validatora ima `.WithMessage(...)` na bosanskom; granice (npr. dužina) dolaze iz `Entity.Constraints`;
  - jedinstveni indeks uvijek s `HasFilter("[IsDeleted] = 0")`; validator za Update isključuje vlastiti red (`x.Id != command.Id`);
  - `AsNoTracking()` samo kod čitanja, nikad u Update i Delete handleru; brisanje je soft delete kroz `Remove`;
  - Id za Update dolazi iz rute (`[JsonIgnore]` na `Id` u komandi);
  - kod brisanja povezanih entiteta s `Restrict` prvo se brišu djeca, pa roditelj;
  - ruta kontrolera `api/<ime-u-mnozini>` malim slovima s crticom (npr. `api/service-categories`); `[Authorize(Policy = ...)]` na klasi kad sve akcije imaju istu ulogu;
  - referentni podaci idu u `StaticDataSeeder` (`HasData`, fiksni Id, `SeedDateUtc`), demo podaci u `DynamicDataSeeder`.
  - svaki kontroler ima `[Authorize(Policy = ...)]`, osim `PublicCatalogController` (`[AllowAnonymous]`, ruta `api/public`): javni podaci se vraćaju kroz posebne upite i DTO-e (samo aktivne usluge, stomatolozi s aktivnim nalogom, bez emaila), a admin endpointi ostaju admin-only; integracijski test `List_AsNonAdmin_Returns403` je obavezan za svaki novi admin kontroler.
  - zapisi koji moraju nastati zajedno (npr. korisnik, pacijent i loyalty račun) povezuju se navigacijama i snimaju jednim `SaveChangesAsync`, bez `BeginTransaction`, jer InMemory baza u testovima ne podržava transakcije.
  - soft delete je UPDATE, pa FK `Restrict` ne štiti od brisanja: provjera "u upotrebi" (409) uvijek ide u Delete handler.
  - IDOR zaštita: svaki handler koji čita ili mijenja termin, račun, plaćanje, karton ili ocjenu
    provjerava da zapis pripada trenutnom korisniku (`IAppCurrentUser.UserId`):
  - pacijent: `x.Patient.UserId == userId` (za račun i ostalo preko `Appointment.Patient`);
  - stomatolog: `x.Dentist.UserId == userId`;
  - admin: sve.
    Provjera ide u sam upit (`Where`), ne poslije učitavanja. Tuđi zapis vraća 404, ne 403,
    da se ne otkrije da zapis postoji. Id pacijenta ili stomatologa nikad ne dolazi od klijenta
    (iz body-ja ili rute), nego se nalazi preko `UserId` iz tokena.
    Za svaki takav endpoint je obavezan integracijski test "tuđi zapis → 404".
- Termin (Appointment) čuva StartUtc i EndUtc u UTC; trajanje se ne čuva posebno (EndUtc - StartUtc).
  Statusi: Scheduled = 1, Cancelled = 2, Completed = 3. CancelledAtUtc je popunjen samo za otkazan termin.
- Pacijent pri zakazivanju bira jednu uslugu. Stomatolog pri završetku može dodati izvedene usluge.
  Svaki red u AppointmentService je jedan zahvat (ista usluga može biti više puta), cijena se kopira
  u PriceAtTimeBam, a račun je zbir tih cijena.
- Stomatolog sa bar jednim terminom u statusu Scheduled ne može se obrisati (409).
- trenutno vrijeme se uzima iz `TimeProvider` (DI), nikad `DateTime.UtcNow` u handlerima i servisima, da se može testirati;
- logika bez baze (npr. `FreeSlotCalculator`) ide u posebnu klasu koja prima sve podatke kao parametre, pa se testira unit testovima.
- Frontend:
  - fajlovi `ime.vrsta.ts` (`.service.ts`, `.model.ts`, `.component.ts`);
  - tipovi prate backend DTO-e i imena (`ListSpecializationQuery` → `ListSpecializationRequest`);
  - lista nasljeđuje `BaseListPagedComponent`, forma `BaseFormComponent` (ista komponenta za dodavanje i izmjenu, rute `/new` i `/:id/edit`);
  - greške sa servera kroz `applyServerErrors`, potvrda brisanja kroz `DialogHelperService.confirm`;
  - sav tekst za korisnika kroz prevode u `bs.json` i `en.json`;
  - admin meni: KATALOG (šifarnici), KLINIKA (stomatolozi, kasnije termini), SISTEM.
    - granice validacije (dužina, min, max, korak) postoje i u `Entity.Constraints` na backendu i u formi na frontendu, pa se mijenjaju na oba mjesta;
  - novi validator ide u `AppValidators` (`core/forms/validators.ts`), uz red u `validation-messages.ts` i ključ u `bs.json` i `en.json`;
  - javne liste (npr. `/services`, `/dentists`) čuvaju filtere i stranicu u URL-u: `queryParamMap` je jedini izvor istine, promjena filtera ide kroz `router.navigate` (`queryParamsHandling: 'merge'`, stranica se vraća na 1, default vrijednost se briše iz URL-a), a `goToPage`/`changePageSize` mijenjaju URL umjesto direktnog učitavanja. Vrijednosti iz URL-a se provjeravaju (`public-catalog-query-params.ts`) i neispravne se zamijene defaultom prije slanja serveru.
  - "Moji termini" (`/client/appointments`, `/client/appointments/:id`, samo pacijent: `myAuthGuard` + `requirePatient` na child rutama) čuvaju filtere (`period`, `status`) i stranicu u URL-u po istom pravilu kao javne liste (`my-appointments-query-params.ts`, uz `readPaging`/`urlValue` iz `public-catalog-query-params.ts`). Status se u URL i zahtjev šalje isto kako ga server vraća (`Scheduled`, `Cancelled`, `Completed`). Link na detalje šalje URL liste kroz router `state` (`listUrl`), a dugme "Nazad na moje termine" ga koristi samo ako je `/client/appointments` ili počinje sa `/client/appointments?`, inače ide na listu bez filtera.
  - "Moj raspored" (`/client/schedule`, #16, samo stomatolog: `myAuthGuard` + `requireDentist`) čuva dan u URL-u (`?date=yyyy-MM-dd`) po istom pravilu kao javne liste: današnji dan (u zoni ordinacije) se briše iz URL-a, neispravan datum postaje danas. "Otkaži" se ne prikazuje kad je prekasno (pravilo u `isTooLateToCancel`, `modules/shared/utils/appointment-cancellation.ts`, zajedničko sa stranicom pacijenta, gdje je dugme samo onemogućeno). `readDateOnly` je u `core/forms/date-only.ts`.
    - zakazivanje (`/book`, #14) je javna ruta bez guarda: koraci usluga → stomatolog → datum i termin rade za svakoga, a potvrda samo za pacijenta (neprijavljen ide na login sa `returnUrl`, admin i stomatolog vide poruku). Izbori se čuvaju u URL-u (`serviceId`, `dentistId`, `date`, `start`): forme su izvor istine, URL se pročita jednom pri otvaranju (`setValue(..., { emitEvent: false })`), a poslije se samo upisuje (`replaceUrl: true`). `start` iz URL-a se izabere samo ako je među slobodnim terminima koje je server vratio; nikad se ne šalje direktno.
  - vremena sa servera su UTC stringovi (`startUtc: string`, ne `Date`) i šalju se nazad netaknuta. Prikazuju se pipeom `clinicTime` u zoni ordinacije (`environment.clinic.timeZoneId`), ne Angularovim `date` pipeom (on prima samo pomak, pa griješi oko ljetnog računanja vremena). Kalendarski dan (`DateOnly`) ide kroz `toDateOnly`, nikad `toISOString()`.
  - `environment.clinic` (zona, `maxBookingDaysAhead`) mora odgovarati sekciji `Clinic` u `appsettings.json`, u sva tri environment fajla. Granica raspona free-slots (14 dana) je u `booking-constraints.ts`, kao `FreeSlotCalculator.MaxRangeDays`.
  - greška pri zakazivanju: 409 → poruka servera, povratak na izbor termina i ponovno učitavanje slobodnih termina; 404 → povratak na prvi korak. 401 i 403 rješavaju interceptori.
  - pomjeranje termina (#58) je modal (`RescheduleAppointmentDialogComponent`, MatDialog) na detaljima termina; dugme "Pomjeri termin" je vidljivo i onemogućeno pod istim uslovima kao "Otkaži termin". Modal dobija podatke kroz `data` (`MAT_DIALOG_DATA`) i sam šalje POST: pri 409 ostaje otvoren, prikaže poruku servera i ponovo učita slotove. `afterClosed()` vraća `true` samo pri uspjehu, pa stranica prikaže poruku i ponovo učita detalje. Grupisanje slotova po danu je u `groupSlotsByDay` (`modules/shared/utils/slot-days.ts`).

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
