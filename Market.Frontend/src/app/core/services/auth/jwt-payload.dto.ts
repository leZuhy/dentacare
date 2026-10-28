// payload kako dolazi iz JWT-a
import { Role } from "./role";

export interface JwtPayloadDto {
  sub: string;
  email: string;
  role: Role;
  ver: string;
  iat: number;
  exp: number;
  aud: string;
  iss: string;
}
