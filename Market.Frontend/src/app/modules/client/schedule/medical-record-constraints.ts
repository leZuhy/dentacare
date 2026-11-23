/**
 * Limits of the "Complete visit" form.
 * The same as MedicalRecordEntity.Constraints on the backend
 * (CompleteAppointmentCommandValidator). If they change, change them in both places.
 */
export const MEDICAL_RECORD_CONSTRAINTS = {
  findingsMaxLength: 2000,
  therapyMaxLength: 2000,
  /** max rows of extra performed services in one completion */
  additionalServicesMax: 20,
} as const;
