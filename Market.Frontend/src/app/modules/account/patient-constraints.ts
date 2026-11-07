/**
 * Patient profile limits – the same as PatientEntity.Constraints on the backend.
 * Used by "Moj profil" (account/my-profile).
 */
export const PATIENT_CONSTRAINTS = {
  phoneMaxLength: 20,
  phoneMinDigits: 8,
  phoneMaxDigits: 15,
  maxAgeYears: 120,
} as const;
