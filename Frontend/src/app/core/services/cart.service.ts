import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Cart } from '../models';
import { environment } from '../../../environments/environment';

/**
 * Service responsible for managing the authenticated customer's shopping cart.
 * Communicates with the OrderService cart API and maintains a reactive local
 * cart signal so components can reactively display cart state without polling.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/cart`;

  /**
   * Reactive signal holding the current cart state.
   * Updated automatically after every cart mutation (add, update, remove, clear).
   * Null when the cart has not been loaded yet.
   */
  cart = signal<Cart | null>(null);

  /**
   * Fetches the current cart from the backend and updates the local signal.
   * Should be called on app init or after login to hydrate the cart state.
   * @returns Observable of the current Cart.
   */
  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl).pipe(tap(c => this.cart.set(c)));
  }

  /**
   * Adds a product to the cart or increments its quantity if already present.
   * Validates stock availability on the backend before adding.
   * Updates the local cart signal with the returned cart state.
   * @param productId - ID of the product to add.
   * @param quantity - Number of units to add.
   * @returns Observable of the updated Cart.
   */
  addItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.post<Cart>(`${this.baseUrl}/items`, { productId, quantity })
      .pipe(tap(c => this.cart.set(c)));
  }

  /**
   * Sets the quantity of a specific cart item to an absolute value.
   * If quantity is 0 or less, the item is removed from the cart.
   * Updates the local cart signal with the returned cart state.
   * @param productId - ID of the product to update.
   * @param quantity - New absolute quantity.
   * @returns Observable of the updated Cart.
   */
  updateItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`${this.baseUrl}/items/${productId}`, { quantity })
      .pipe(tap(c => this.cart.set(c)));
  }

  /**
   * Removes a product from the cart entirely.
   * Updates the local cart signal with the returned cart state.
   * @param productId - ID of the product to remove.
   * @returns Observable of the updated Cart.
   */
  removeItem(productId: string): Observable<Cart> {
    return this.http.delete<Cart>(`${this.baseUrl}/items/${productId}`)
      .pipe(tap(c => this.cart.set(c)));
  }

  /**
   * Removes all items from the cart and resets the local cart signal to null.
   * @returns Observable that completes when the cart is cleared.
   */
  clearCart(): Observable<void> {
    return this.http.delete<void>(this.baseUrl).pipe(tap(() => this.cart.set(null)));
  }

  /**
   * Sets or removes the optional budget limit for the cart.
   * When the cart total exceeds the budget, the frontend shows a warning.
   * @param budgetLimit - The budget limit in INR, or null to remove the limit.
   * @returns Observable that completes when the budget is updated.
   */
  setBudget(budgetLimit: number | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/budget`, { budgetLimit });
  }
}
