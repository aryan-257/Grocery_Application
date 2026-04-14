import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CouponDto } from '../models';

/**
 * Service responsible for fetching available discount coupons.
 * Communicates with the OrderService coupons API to retrieve active,
 * non-expired coupons for display on the checkout and offers pages.
 */
@Injectable({
  providedIn: 'root'
})
export class CouponService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/v1/coupons`;

  /**
   * Returns all currently active and non-expired coupons.
   * Used to display available promotions to the user before or during checkout.
   * This endpoint is public — no authentication required.
   * @returns Observable of an array of CouponDto objects.
   */
  getAllCoupons(): Observable<CouponDto[]> {
    return this.http.get<CouponDto[]>(this.apiUrl);
  }
}
