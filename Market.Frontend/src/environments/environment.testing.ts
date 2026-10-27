// TESTING – lokalno okruženje koje se ponaša kao produkcija (optimizovan build, bez razvojnih
// pomagala, npr. bez popunjenih podataka za prijavu), a radi na vašem računaru:
//   npm run start:testing      (= ng serve --configuration testing)
// Backend se tada pokreće profilom "https-testing" (okruženje Testing, baza Market2026_Testing).
// Angular ovim fajlom zamjenjuje environment.ts (vidi "fileReplacements" u angular.json).
export const environment = {
  production: true,
  apiUrl: 'https://localhost:7100'
};
