import { Role } from "./role";

export interface CurrentUserDto {
  userId: number;
  email: string;
  role: Role;
  tokenVersion: number;
}
