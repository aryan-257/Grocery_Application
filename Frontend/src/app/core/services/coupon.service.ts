import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CouponDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class CouponService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/v1/coupons`;

  getAllCoupons(): Observable<CouponDto[]> {
    return this.http.get<CouponDto[]>(this.apiUrl);
  }
}
