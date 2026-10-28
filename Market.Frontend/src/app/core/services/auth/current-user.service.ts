// src/app/core/services/auth/current-user.service.ts
import { Injectable, inject, computed } from "@angular/core";
import { AuthFacadeService } from "./auth-facade.service";

@Injectable({ providedIn: "root" })
export class CurrentUserService {
  private auth = inject(AuthFacadeService);

  /** Signal koji UI može čitati (readonly) */
  currentUser = computed(() => this.auth.currentUser());

  isAuthenticated = computed(() => this.auth.isAuthenticated());
  isAdmin = computed(() => this.auth.isAdmin());
  isDentist = computed(() => this.auth.isDentist());
  isPatient = computed(() => this.auth.isPatient());

  get snapshot() {
    return this.auth.currentUser();
  }

  /** Pravilo: admin > ostali → client */
  getDefaultRoute(): string {
    const user = this.snapshot;
    if (!user) return "/auth/login";

    if (user.role == "Admin") return "/admin";
    return "/client";
  }
}
