import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface ApiResponse<T> {
	success: boolean;
	data?: T;
	message: string;
	errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
	private http = inject(HttpClient);
	private baseUrl = environment.apiUrl;

	private handleError(error: any) {
		const msg = error?.error?.message || error?.message || 'Unexpected error';
		return throwError(() => new Error(msg));
	}

	get<T>(path: string, params?: Record<string, string | number>): Observable<ApiResponse<T>> {
		let httpParams = new HttpParams();
		Object.entries(params || {}).forEach(([k, v]) => httpParams = httpParams.set(k, String(v)));
		return this.http.get<ApiResponse<T>>(`${this.baseUrl}${path}`, { params: httpParams }).pipe(catchError(this.handleError));
	}

	post<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
		return this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, body).pipe(catchError(this.handleError));
	}

	put<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
		return this.http.put<ApiResponse<T>>(`${this.baseUrl}${path}`, body).pipe(catchError(this.handleError));
	}

	patch<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
		return this.http.patch<ApiResponse<T>>(`${this.baseUrl}${path}`, body).pipe(catchError(this.handleError));
	}

	delete<T>(path: string): Observable<ApiResponse<T>> {
		return this.http.delete<ApiResponse<T>>(`${this.baseUrl}${path}`).pipe(catchError(this.handleError));
	}
}
