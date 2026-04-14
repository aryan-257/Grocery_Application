import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order } from '../models';
import { environment } from '../../../environments/environment';

/**
 * Service responsible for order management operations.
 * Communicates with the OrderService API to create, retrieve, and track orders.
 * Role-based visibility is enforced on the backend — customers see only their own orders,
 * while Admin/StoreManager/DeliveryDriver see all relevant orders.
 */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/orders`;

  /**
   * Returns the list of orders visible to the authenticated user.
   * Customers receive only their own orders.
   * Admin and StoreManager receive all orders.
   * DeliveryDriver receives orders in Shipped, OutForDelivery, or Delivered status.
   * @returns Observable of an array of Order objects.
   */
  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(this.baseUrl);
  }

  /**
   * Returns the full details of a single order by its ID.
   * Customers can only access their own orders — the backend returns 403 otherwise.
   * @param id - The order's unique identifier.
   * @returns Observable of the Order object.
   */
  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  /**
   * Creates a new order from the authenticated customer's current cart.
   * The backend calculates pricing, applies the coupon if provided, and
   * initiates a Razorpay payment order. The response includes Razorpay
   * checkout details when payment integration is available.
   * @param deliveryAddress - Full delivery address for the order.
   * @param notes - Optional delivery instructions or special requests.
   * @param couponCode - Optional discount coupon code to apply at checkout.
   * @returns Observable of the created Order (may include Razorpay fields).
   */
  createOrder(deliveryAddress: string, notes?: string, couponCode?: string): Observable<Order> {
    return this.http.post<Order>(this.baseUrl, { deliveryAddress, notes, couponCode });
  }
}
