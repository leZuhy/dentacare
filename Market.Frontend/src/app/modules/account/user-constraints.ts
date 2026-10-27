/**
 * Najveće dužine polja korisnika – isto kao MarketUserEntity.Constraints na backendu.
 * Koriste ih registracija (auth/register) i profil (account/my-profile).
 * Pravilo lozinke je u core/forms/validators.ts (AppValidators.password).
 */
export const USER_CONSTRAINTS = {
  firstnameMaxLength: 100,
  lastnameMaxLength: 100,
  emailMaxLength: 200,
  addressMaxLength: 200,
  cityMaxLength: 100
} as const;
