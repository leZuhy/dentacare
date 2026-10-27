// DEVELOPMENT – koristi se za `ng serve` i `ng build --configuration development`.
// Angular ovim fajlom zamjenjuje environment.ts (vidi "fileReplacements" u angular.json).
// Ne importuj ga direktno – u kodu uvijek importuj 'environments/environment'.
export const environment = {
  production: false,
  // Market.API lokalno; ako backend pokrećeš samo na http-u: http://localhost:5100
  apiUrl: 'https://localhost:7100'
};
