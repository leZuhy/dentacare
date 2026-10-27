import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-logout',
  standalone: false,
  templateUrl: './logout.component.html',
  styleUrl: './logout.component.scss',
})
export class LogoutComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private auth = inject(AuthFacadeService);

  countdownSeconds = 2;
  private intervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    // Call logout (handles API call + clears state)
    this.auth.logout().subscribe({
      next: () => this.startCountdown(),
      error: () => this.startCountdown() // Even if API fails, clear local state
    });
  }

  ngOnDestroy(): void {
    // korisnik je napustio stranicu prije isteka odbrojavanja → ugasi interval
    this.stopCountdown();
  }

  private startCountdown(): void {
    this.intervalId = setInterval(() => {
      this.countdownSeconds--;

      if (this.countdownSeconds <= 0) {
        this.stopCountdown();
        this.router.navigate(['/auth/login']);
      }
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }
}
