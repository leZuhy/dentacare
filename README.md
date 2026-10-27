# Market – starter projekat za seminarski rad (RS1 2026/27)

Starter je kompletna aplikacija na kojoj radite seminarski rad:

| Folder | Sadržaj |
|---|---|
| [`Market.Backend/`](Market.Backend/) | ASP.NET Core Web API, .NET 10, Clean Architecture, CQRS (MediatR), FluentValidation, EF Core (SQL Server), JWT + refresh tokeni |
| [`Market.Frontend/`](Market.Frontend/) | Angular 21 (NgModules), Angular Material, ngx-translate (bosanski/engleski) |

Starter sadrži i gotove primjere (korpa, checkout, narudžbe kupca, promocije, upload slike, konvertor valuta…). Ono što vašoj temi ne treba – obrišite. Kod iz startera se ne boduje; boduje se ono što sami napravite.

## Preduslovi

- .NET 10 SDK (10.0.100 ili noviji; verzija je zadana u `Market.Backend/global.json`)
- Visual Studio 2026 (v18) ili noviji, Rider 2025.3+ ili VS Code sa C# Dev Kit. Visual Studio 2022 ne može buildati .NET 10.
- Node.js `^20.19`, `^22.12` ili `>= 24` (zahtjev Angulara 21; preporuka: 22 ili 24 LTS)
- SQL Server na `localhost,1433`, korisnik `sa`, lozinka `test` (connection string je u `Market.Backend/Market.API/appsettings.json`)
- Repozitorij klonirajte u kratak folder (npr. `C:\rs1`) i jednom uključite duge putanje: `git config --global core.longpaths true`

## Pokretanje backenda

Jednom na računaru (HTTPS certifikat za localhost):

```bash
dotnet dev-certs https --trust
```

Pokretanje iz foldera `starter/Market.Backend`:

```bash
dotnet run --project Market.API --launch-profile https
```

U Visual Studiju: startup projekat `Market.API`, profil `https` (prvi u listi).

- API: `https://localhost:7100` (i `http://localhost:5100`, koji preusmjerava na https)
- Swagger: `https://localhost:7100/swagger`
- Baza `Market2026` se pri pokretanju sama kreira i migrira, a demo podaci se upisuju u okruženjima `Development` i `Testing`.
- Profil `https-testing` pokreće okruženje `Testing` (vidi „Okruženja”).

## Pokretanje frontenda

Iz foldera `starter/Market.Frontend`:

```bash
npm ci
npm start
```

Aplikacija: `http://localhost:4200`. Backend mora biti pokrenut.

Ako HTTPS certifikat ne radi, pokrenite backend s profilom `http` i u `src/environments/environment.development.ts` stavite `apiUrl: 'http://localhost:5100'`.

## Korisnici (demo podaci)

| Email | Lozinka | Uloga |
|---|---|---|
| `admin@market.local` | `Admin123!` | administrator (`/admin`) |
| `manager@market.local` | `Manager123!` | menadžer |
| `employee@market.local` | `Employee123!` | uposlenik |
| `nina.bijedic@email.com`, `iris.memic@email.com`, `azra.smajic@email.com` | `Customer123!` | kupci, vlasnici demo narudžbi |
| `string` | `string` | za Swagger |
| `test` | `test123` | za testove |

Administrator nakon prijave ide na `/admin`, svi ostali na `/client`. Novi kupac se može registrovati na `/auth/register` (odmah je prijavljen). Lozinka mora imati najmanje 8 znakova, bar jedno slovo i jednu cifru.

## Okruženja (environments)

| | Development | Testing | Production |
|---|---|---|---|
| Namjena | svakodnevni razvoj | lokalno, ponaša se kao produkcija | server |
| Backend profil | `https` / `http` | `https-testing` | – |
| Frontend | `npm start` | `npm run start:testing` | `npm run build` |
| Baza | `Market2026` | `Market2026_Testing` | iz konfiguracije servera |
| Demo podaci | da | da | ne |
| Swagger | da | ne | ne |
| Detalji greške (stack trace) u odgovoru | da | ne | ne |
| JWT ključ | `DEV_ONLY_…` | `TESTING_ONLY_…` | tajni ključ u `Jwt__Key` |
| CORS | `http://localhost:4200` | `http://localhost:4200` | iz konfiguracije servera |
| Email | ispis u log (`[DEV EMAIL]`) | ispis u log | SMTP (`Email__Smtp__Host`…) |
| `Frontend:BaseUrl` (link u emailu) | `http://localhost:4200` | `http://localhost:4200` | `Frontend__BaseUrl` |

**Backend** – `appsettings.json` važi za sva okruženja, a `appsettings.Development.json` / `appsettings.Testing.json` ga dopunjavaju. Connection string za Development je namjerno u `appsettings.json`, da `dotnet ef` i Package Manager Console rade bez podešavanja okruženja.

Produkcija se lokalno ne pokreće: bez pravog ključa (varijabla okruženja `Jwt__Key`, najmanje 32 znaka), dozvoljenih domena (`Cors__AllowedOrigins__0`), SMTP servera (`Email__Smtp__Host`) i adrese frontenda (`Frontend__BaseUrl`) API se ne starta. Ključevi `DEV_ONLY_…` i `TESTING_ONLY_…` su javni (u repozitoriju su) i dozvoljeni su samo u svom okruženju. Automatski testovi koriste posebno okruženje `IntegrationTests` (in-memory baza).

**Frontend** – uvijek importujte `environments/environment`:

| Fajl | Kada se koristi |
|---|---|
| `src/environments/environment.development.ts` | `npm start` / `ng serve` (API `https://localhost:7100`) |
| `src/environments/environment.testing.ts` | `npm run start:testing` (optimizovan build kao produkcija, API `https://localhost:7100`) |
| `src/environments/environment.ts` | `npm run build` (produkcija; `apiUrl` postavite prije objave) |

Access token se šalje samo na `environment.apiUrl`, nikad na druge domene.

## Migracije

Iz foldera `starter/Market.Backend` (prvi put `dotnet tool restore` instalira odgovarajuću verziju `dotnet-ef` iz `dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add NazivMigracije -p Market.Infrastructure -s Market.API -o Migrations
```

Package Manager Console (Default project: `Market.Infrastructure`, startup projekat `Market.API`):

```powershell
Add-Migration NazivMigracije
```

Baza se ažurira sama pri pokretanju API-ja (ili `Update-Database`).

## Testovi

```bash
dotnet test
```

Integracijski testovi koriste in-memory bazu (okruženje `IntegrationTests`).

## Šta starter sadrži

| Dio | Rute | Ko |
|---|---|---|
| Landing (promocije, kategorije, najnoviji proizvodi), pretraga, korpa, checkout, konvertor valuta | `/`, `/search`, `/cart`, `/checkout`, `/converter` | svi (checkout traži prijavu) |
| Registracija, zaboravljena lozinka, nova lozinka | `/auth/register`, `/auth/forgot-password`, `/auth/reset-password` | svi |
| Moje narudžbe, moj profil | `/client`, `/client/orders`, `/client/orders/:id`, `/client/profile` | prijavljeni kupac |
| Kategorije, proizvodi (slika, zalihe), promocije, narudžbe (pregled, promjena statusa, uređivanje stavki), kursna lista, moj profil (`/admin/profile`), prazna stranica za postavke aplikacije (`/admin/settings`) | `/admin/...` | administrator |

### Primjeri formi

Svaki obrazac je urađen jednom, na mjestu gdje mu je prirodno (putanje su u `Market.Frontend/src/app/modules/`):

| Obrazac | Gdje |
|---|---|
| Forma na stranici (add/edit) sa `BaseFormComponent` | `admin/catalogs/products` |
| Forma u dijalogu | `admin/catalogs/product-categories/product-category-upsert` |
| Async validator (jedinstven naziv, poziv API-ja) | `admin/catalogs/products/validators` |
| Upload slike (provjera tipa i veličine, pregled, brisanje) | `admin/catalogs/products/product-image` |
| Datepicker + cross-field validator (kraj poslije početka) | `admin/catalogs/promotions/promotion-upsert` |
| Typed FormArray (master-detail) + upozorenje pri napuštanju nesnimljene forme | `admin/orders/order-edit` |
| Autocomplete sa serverskom pretragom | `admin/orders/order-edit/product-autocomplete` |
| Stepper (wizard) | `public/checkout` |
| Cross-field validator za potvrdu lozinke | `auth/register`, `auth/reset-password`, `account/my-profile` |
| Dvije nezavisne forme na jednoj stranici | `account/my-profile` |
| Filteri sačuvani u URL-u + straničenje | `public/search-products` |
| Greške s backenda po polju | `core/forms/server-errors.ts`, korišteno u svim formama |
| Prevedene poruke validacije | `shared` komponenta `fit-field-error`, `core/forms/validation-messages.ts` |

### Valute

- Bazna valuta je KM (BAM). Kursna lista ima historiju: svaki kurs važi od `validFromUtc`, pa se iznos može preračunati po kursu na bilo koji datum. EUR je fiksno 1,95583.
- Backend: `ICurrencyConverter.ConvertAsync(iznos, iz, u, naDan)` vraća preračunati iznos, primijenjeni kurs i datum kursa. Endpointi: `GET /Currencies`, `GET /Currencies/convert`, kursna lista za admina (`/Currencies/rates`).
- Frontend: izbor valute u headeru, `money` pipe za prikaz cijena, stranica `/converter`, admin stranica „Kursna lista”.
- Cijene u drugoj valuti su informativne: narudžbe u starteru se obračunavaju u KM. Plaćanje u drugoj valuti, kurs koji se pamti uz transakciju, povrat novca i promjena valute su dio vašeg rješenja.
- Automatsko preuzimanje kurseva ECB-a (Frankfurter) je isključeno. Uključuje se sa `"ExchangeRates": { "Provider": "Frankfurter" }` u `appsettings.json` ili varijablom okruženja `ExchangeRates__Provider=Frankfurter`; ECB ne objavljuje RSD, pa se taj kurs unosi ručno.

### Nalog: registracija, zaboravljena lozinka, profil

- Registracija: `/auth/register` → `POST /api/auth/register`.
- Zaboravljena lozinka: `/auth/forgot-password` šalje jednokratni link koji važi 30 minuta (`/auth/reset-password?email=…&token=…`). Odgovor je uvijek isti, da se ne otkrije postoji li nalog. U bazi se čuva samo hash tokena.
- U Development i Testing okruženju email se ne šalje nego ispisuje u konzolu backenda i u `Logs/app-*.log` (red počinje sa `[DEV EMAIL]`), pa link kopirate odatle. Za pravi pregled emailova pokrenite Mailpit ili Papercut i postavite `Email:Smtp:Host=localhost`, `Port=1025`, `EnableSsl=false`.
- Moj profil (`/client/profile`, za admina `/admin/profile`): ime, prezime, adresa i grad (`GET/PUT /api/auth/me`) i promjena lozinke (`POST /api/auth/change-password`). Promjena ili reset lozinke odjavljuje sve ostale uređaje.
- Adresa i grad iz profila se prikazuju uz narudžbu.

### Upload slika

Slike proizvoda se snimaju u `Market.API/wwwroot/uploads/products/` (folder je u `.gitignore`) i dostupne su anonimno na `https://localhost:7100/uploads/...`. Dozvoljeni su JPEG, PNG i WEBP do 2 MB; provjerava se sadržaj fajla, ne samo ekstenzija.

### Redis

Keš je podešen, ali ga nijedan handler ne koristi. Dok je `ConnectionStrings:Redis` prazan, `ICacheService` radi u memoriji i Redis server nije potreban. Za Redis upišite npr. `localhost:6379`. Primjer upotrebe je u XML komentarima `ICacheService` i `ICatalogCacheVersionService` (keširanje `GET /Catalog/home` i poništavanje keša nakon izmjene kataloga).

## Kako obrisati ono što vam ne treba

Obrišite dio na oba kraja, pa napravite novu migraciju.

| Dio | Backend | Frontend |
|---|---|---|
| Valute | `Modules/Finance`, `Entities/Finance`, `Configurations/Finance`, `CurrenciesController`, `CurrencyConverter`, `FrankfurterExchangeRateProvider`, registracije u DI, seed | `core/services/currency`, `api-services/currencies`, `public/converter`, `admin/finance`, izbor valute u headeru; `\| money` zamijenite sa `\| currency` |
| Promocije | `Modules/Catalog/Promotions`, `PromotionEntity`, konfiguracija, `PromotionsController`, seed | `admin/catalogs/promotions`, `api-services/promotions`, `promotions-carousel` |
| Korpa i checkout | – (koristi `POST /Orders`) | `core/services/cart`, `public/cart-page`, `public/checkout`, `cart-items`, ikona korpe u headeru |
| Redis | `Abstractions/Caching`, `Infrastructure/Caching`, paket `Microsoft.Extensions.Caching.StackExchangeRedis` | – |

Uz svaki obrisani entitet uklonite i njegov `DbSet` u `IAppDbContext`/`DatabaseContext`, rute, stavke menija i prevode (`bs.json` i `en.json`).

## Izmjene u odnosu na template 2025/26

- **Sigurnost:** guard sada stvarno štiti `/admin` i `/client`; backend provjerava ulogu (`PolicyNames.Admin`, 403); log više ne sadrži lozinke ni tokene; token se šalje samo na naš API; login ne otkriva postoji li nalog (401); refresh token se rotira, a ponovna upotreba starog gasi sve sesije korisnika.
- **.NET 10:** backend je prebačen sa .NET 8 na .NET 10 (EF Core 10, Swashbuckle 10); `dotnet-ef` je lokalni alat (`dotnet tool restore`).
- **Okruženja:** Development, Testing (lokalno, kao produkcija) i Production; razvojni ključ i CORS su u `appsettings.Development.json`; frontend koristi `environment.development.ts` / `environment.testing.ts`.
- **Portovi:** API je na `https://localhost:7100` (ranije 7260); baza se zove `Market2026`.
- **Baza:** jedna nova `Init` migracija; unique indeksi poštuju soft delete; tabela `Orders` (ranije `Oders`).
- **Greške:** `ErrorDto.errors` vraća greške po polju i forme ih prikazuju; create vraća `201 { id }`, akcije bez rezultata `204`.
- **Frontend:** Angular 21.2 (bez poznatih ranjivosti), produkcijski build prolazi, folder je `Market.Frontend` (ranije `rs1-frontend-2025-26`).
- **Novo:** sve iz poglavlja „Šta starter sadrži” osim osnovnih kategorija, proizvoda i narudžbi, uključujući registraciju, zaboravljenu lozinku i profil (ranije prazne stranice).
