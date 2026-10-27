import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

/**
 * Global toaster service for displaying notifications.
 * Uses Material Snackbar for consistent UI.
 * Tekst dugmeta "Zatvori" dolazi iz prevoda (COMMON.CLOSE); poruku prosljeđuje pozivalac
 * (već prevedenu, npr. translate.instant('...')).
 */
@Injectable({
  providedIn: 'root'
})
export class ToasterService {
  private snackBar = inject(MatSnackBar);
  private translate = inject(TranslateService);

  private defaultConfig: MatSnackBarConfig = {
    duration: 3000,
    horizontalPosition: 'end',
    verticalPosition: 'top'
  };

  /**
   * Show success message (green)
   */
  success(message: string, duration?: number): void {
    this.snackBar.open(message, this.translate.instant('COMMON.CLOSE'), {
      ...this.defaultConfig,
      duration: duration ?? this.defaultConfig.duration,
      panelClass: ['snackbar-success']
    });
  }

  /**
   * Show error message (red)
   */
  error(message: string, duration?: number): void {
    this.snackBar.open(message, this.translate.instant('COMMON.CLOSE'), {
      ...this.defaultConfig,
      duration: duration ?? this.defaultConfig.duration,
      panelClass: ['snackbar-error']
    });
  }

  /**
   * Show warning message (orange)
   */
  warning(message: string, duration?: number): void {
    this.snackBar.open(message, this.translate.instant('COMMON.CLOSE'), {
      ...this.defaultConfig,
      duration: duration ?? this.defaultConfig.duration,
      panelClass: ['snackbar-warning']
    });
  }

  /**
   * Show info message (blue)
   */
  info(message: string, duration?: number): void {
    this.snackBar.open(message, this.translate.instant('COMMON.CLOSE'), {
      ...this.defaultConfig,
      duration: duration ?? this.defaultConfig.duration,
      panelClass: ['snackbar-info']
    });
  }
}
