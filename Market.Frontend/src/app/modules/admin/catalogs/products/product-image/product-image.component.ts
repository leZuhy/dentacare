import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { filter, switchMap, tap } from 'rxjs';
import { ProductsApiService } from '../../../../../api-services/products/products-api.service';
import { ErrorDto } from '../../../../../core/models/error-dto';
import { getErrorMessage } from '../../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../../shared/services/dialog-helper.service';
import { DialogButton } from '../../../../shared/models/dialog-config.model';

/** Ista pravila kao na backendu (UploadProductImageCommandValidator). */
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_SIZE_BYTES = 2 * 1024 * 1024;

/**
 * PRIMJER: UPLOAD SLIKE (multipart/form-data).
 *
 * 1. <input type="file"> → provjera tipa i veličine NA KLIJENTU (brza poruka, bez slanja 5 MB na server)
 * 2. pregled odabrane slike prije slanja (URL.createObjectURL – oslobađa se poslije)
 * 3. POST /Products/{id}/image sa poljem "file" → server ponovo provjerava (i potpis fajla) i vraća { imageUrl }
 * 4. DELETE /Products/{id}/image uklanja sliku
 *
 * Slika se ne šalje zajedno sa formom proizvoda: upload je odvojen poziv, pa radi samo za
 * postojeći proizvod (zato je na stranici za izmjenu, ne za dodavanje).
 */
@Component({
  selector: 'app-product-image',
  standalone: false,
  templateUrl: './product-image.component.html',
  styleUrl: './product-image.component.scss'
})
export class ProductImageComponent {
  @Input({ required: true }) productId!: number;
  /** trenutna slika sa servera (relativna putanja) ili null */
  @Input() imageUrl: string | null = null;
  @Output() imageUrlChange = new EventEmitter<string | null>();

  private api = inject(ProductsApiService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private dialogHelper = inject(DialogHelperService);

  readonly accept = ALLOWED_TYPES.join(',');
  readonly maxSizeMb = MAX_SIZE_BYTES / 1024 / 1024;

  selectedFile: File | null = null;
  previewUrl: string | null = null;
  /** greška provjere na klijentu ili sa servera (polje "file") */
  fileError: string | null = null;
  isBusy = false;

  constructor() {
    // object URL drži fajl u memoriji dok se ne oslobodi
    inject(DestroyRef).onDestroy(() => this.clearSelection());
  }

  onFileSelected(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;
    // isti fajl se može ponovo odabrati (change se inače ne bi okinuo)
    input.value = '';
    this.clearSelection();

    if (!file) {
      return;
    }
    if (!ALLOWED_TYPES.includes(file.type)) {
      this.fileError = this.translate.instant('VALIDATION.FILE_TYPE', { types: 'JPG, PNG, WEBP' });
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      this.fileError = this.translate.instant('VALIDATION.FILE_SIZE', { max: this.maxSizeMb });
      return;
    }

    this.selectedFile = file;
    this.previewUrl = URL.createObjectURL(file);
  }

  upload(): void {
    if (!this.selectedFile || this.isBusy) {
      return;
    }

    this.isBusy = true;
    this.api.uploadImage(this.productId, this.selectedFile).subscribe({
      next: ({ imageUrl }) => {
        this.isBusy = false;
        this.clearSelection();
        this.imageUrlChange.emit(imageUrl);
        this.toaster.success(this.translate.instant('PRODUCTS.IMAGE.UPLOADED'));
      },
      error: (err: HttpErrorResponse) => {
        this.isBusy = false;
        // greška validacije je na polju "file"; inače opšta poruka
        const fileFieldError = (err.error as Partial<ErrorDto> | null)?.errors?.find(
          (e) => e.field?.toLowerCase() === 'file'
        );
        this.fileError = fileFieldError?.message ?? getErrorMessage(err);
      }
    });
  }

  cancelSelection(): void {
    this.clearSelection();
  }

  remove(): void {
    this.dialogHelper
      .confirm('PRODUCTS.IMAGE.DELETE_TITLE', 'PRODUCTS.IMAGE.DELETE_MESSAGE')
      .pipe(
        // "Ne" ili zatvoren dijalog → ništa
        filter((result) => result?.button === DialogButton.YES),
        tap(() => (this.isBusy = true)),
        // jedan lanac umjesto subscribe unutar subscribe
        switchMap(() => this.api.deleteImage(this.productId))
      )
      .subscribe({
        next: () => {
          this.isBusy = false;
          this.imageUrlChange.emit(null);
          this.toaster.success(this.translate.instant('PRODUCTS.IMAGE.DELETED'));
        },
        error: (err: HttpErrorResponse) => {
          this.isBusy = false;
          this.fileError = getErrorMessage(err);
        }
      });
  }

  private clearSelection(): void {
    if (this.previewUrl) {
      URL.revokeObjectURL(this.previewUrl);
    }
    this.previewUrl = null;
    this.selectedFile = null;
    this.fileError = null;
  }
}
