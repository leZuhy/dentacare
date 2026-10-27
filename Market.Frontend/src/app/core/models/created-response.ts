/**
 * Tijelo odgovora "201 Created" koje vraćaju POST endpointi za kreiranje
 * (backend: CreatedAtAction(..., new { id }) → { "id": 5 }).
 */
export interface CreatedResponse {
  id: number;
}
