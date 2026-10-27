import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ListProductsRequest,
  ListProductsResponse,
  GetProductByIdQueryDto,
  CreateProductCommand,
  UpdateProductCommand,
  CheckProductNameRequest,
  CheckProductNameResponse,
  UploadProductImageResponse
} from './products-api.models';
import { buildHttpParams } from '../../core/models/build-http-params';
import { CreatedResponse } from '../../core/models/created-response';

@Injectable({
  providedIn: 'root'
})
export class ProductsApiService {
  private readonly baseUrl = `${environment.apiUrl}/Products`;
  private http = inject(HttpClient);

  /**
   * GET /Products
   * List products with optional query parameters (search, categoryId, paging).
   */
  list(request?: ListProductsRequest): Observable<ListProductsResponse> {
    const params = request ? buildHttpParams(request as any) : undefined;

    return this.http.get<ListProductsResponse>(this.baseUrl, {
      params,
    });
  }

  /**
   * GET /Products/{id}
   * Get a single product by ID.
   */
  getById(id: number): Observable<GetProductByIdQueryDto> {
    return this.http.get<GetProductByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * GET /Products/name-available?name=&excludeId= (Admin)
   * Da li je naziv slobodan (bez obzira na velika/mala slova). Koristi ga async validator
   * forme; create/update na serveru i dalje sami provjeravaju jedinstvenost.
   */
  checkNameAvailable(request: CheckProductNameRequest): Observable<CheckProductNameResponse> {
    const params = buildHttpParams(request as any);
    return this.http.get<CheckProductNameResponse>(`${this.baseUrl}/name-available`, { params });
  }

  /**
   * POST /Products
   * Create a new product.
   * @returns { id } of the newly created product
   */
  create(payload: CreateProductCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /**
   * PUT /Products/{id}
   * Update an existing product.
   */
  update(id: number, payload: UpdateProductCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /**
   * DELETE /Products/{id}
   * Delete a product.
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * POST /Products/{id}/image (Admin), multipart/form-data, polje "file".
   * Content-Type (sa "boundary") postavlja browser sam kad je tijelo FormData – ne zadaj ga ručno.
   * Stara slika se na serveru briše.
   */
  uploadImage(id: number, file: File): Observable<UploadProductImageResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<UploadProductImageResponse>(`${this.baseUrl}/${id}/image`, formData);
  }

  /**
   * DELETE /Products/{id}/image (Admin) → 204
   */
  deleteImage(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/image`);
  }
}
