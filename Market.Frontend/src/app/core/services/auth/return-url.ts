/**
 * returnUrl: stranica na koju se korisnik vraća nakon prijave (/auth/login?returnUrl=/checkout).
 *
 * returnUrl dolazi iz adrese, a adresu može napisati bilo ko (link u mailu, poruci...).
 * Zato poslije prijave idemo SAMO na putanju unutar aplikacije – inače bi se neko mogao
 * "vratiti" na tuđi sajt koji izgleda kao naš (open redirect).
 *
 * Dozvoljeno:  "/checkout", "/admin/orders?status=1"
 * Odbijeno:    "https://evil.com", "//evil.com", "/\evil.com", "javascript:...", "/auth/..."
 */
export function isSafeReturnUrl(url: string | null | undefined): url is string {
  if (!url) {
    return false;
  }
  return (
    url.startsWith('/') &&
    !url.startsWith('//') &&
    // browser "\" tretira kao "/" → "/\evil.com" je isto što i "//evil.com"
    !url.includes('\\') &&
    // povratak na login/logout nema smisla (i mogao bi napraviti petlju)
    !url.startsWith('/auth')
  );
}
