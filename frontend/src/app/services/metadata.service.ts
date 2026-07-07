import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { FormMetadataDto } from '../dtos/metadata.dto';

@Injectable({
  providedIn: 'root'
})
export class MetadataService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;
  private cachedMetadata: FormMetadataDto | null = null;

  getMetadata(): Observable<FormMetadataDto> {
    if (this.cachedMetadata) {
      return of(this.cachedMetadata);
    }

    return this.http.get<FormMetadataDto>(`${this.baseUrl}/api/public/metadata`).pipe(
      tap(data => {
        this.cachedMetadata = data;
      })
    );
  }
}
