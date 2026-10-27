# Market.Frontend

Angular 21 aplikacija (NgModules, Angular Material, ngx-translate). Pokretanje, korisnici i okruženja su opisani u [`../README.md`](../README.md).

## Komande

| Komanda | Šta radi |
|---|---|
| `npm ci` | instalira tačne verzije iz `package-lock.json` |
| `npm start` | `ng serve`, okruženje development, `http://localhost:4200` |
| `npm run start:testing` | `ng serve --configuration testing`: optimizovan build kao produkcija, ali prema lokalnom API-ju (backend profil `https-testing`) |
| `npm run build` | produkcijski build u `dist/market-frontend/browser` |
| `npm run watch` | development build uz praćenje izmjena |

Starter nema podešen test runner (`npm test` ne postoji).

## Struktura `src/app`

| Folder | Sadržaj |
|---|---|
| `api-services/` | tanak sloj prema API-ju: jedan servis i modeli po backend modulu |
| `core/` | guard, interceptori (token, greške, loading), auth servisi, bazne klase komponenti, `server-errors.ts` (greške s backenda u formi) |
| `modules/admin/` | admin dio (`/admin`) |
| `modules/client/` | dio za prijavljene kupce (`/client`) |
| `modules/public/` | javni dio (`/`) |
| `modules/auth/` | prijava i odjava (`/auth/...`) |
| `modules/shared/` | zajedničke `fit-*` komponente i servisi |

Prevodi su u `public/i18n/bs.json` i `public/i18n/en.json`; oba fajla moraju imati iste ključeve.
