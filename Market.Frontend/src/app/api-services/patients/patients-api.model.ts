/**
 * Response of GET /api/patients/me.
 * Corresponds to: GetMyPatientProfileQueryDto.cs
 */
export interface GetMyPatientProfileQueryDto {
  /** null = the patient has not entered a phone yet */
  phone: string | null;
  /** "yyyy-MM-dd" (DateOnly on the backend); null = not entered yet */
  dateOfBirth: string | null;
}

/**
 * Command for PUT /api/patients/me -> 204. null removes a value.
 * Corresponds to: UpdateMyPatientProfileCommand.cs
 */
export interface UpdateMyPatientProfileCommand {
  phone: string | null;
  dateOfBirth: string | null;
}
