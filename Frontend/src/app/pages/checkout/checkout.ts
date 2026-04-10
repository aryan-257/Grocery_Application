import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../../core/services/cart.service';
import { NotificationService } from '../../core/services/notification.service';
import { InvoiceService } from '../../core/services/invoice.service';
import { OrderService } from '../../core/services/order.service';
import { Coupon } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="max-w-xl mx-auto px-4 py-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Checkout</h1>

      @if (cart()) {
        <div class="bg-gray-50 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-700 rounded-xl p-4 mb-6">
          <p class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Order summary</p>
          <div class="space-y-1.5">
            @for (item of cart()!.items; track item.productId) {
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>{{ item.productName }} x {{ item.quantity }}</span>
                <span class="text-gray-800 dark:text-gray-200">Rs.{{ item.totalPrice.toFixed(2) }}</span>
              </div>
            }
          </div>
          <div class="border-t border-gray-200 dark:border-gray-700 mt-3 pt-3 space-y-1.5">
            <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
              <span>Subtotal</span><span>Rs.{{ cart()!.subTotal.toFixed(2) }}</span>
            </div>
            <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
              <span>Delivery</span>
              <span>{{ cart()!.subTotal >= 500 ? 'Free' : 'Rs.49.00' }}</span>
            </div>
            <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
              <span>Tax (5%)</span><span>Rs.{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
            </div>
            @if (coupon()?.discountAmount) {
              <div class="flex justify-between text-sm text-green-600 dark:text-green-400 font-medium">
                <span>Discount ({{ couponCode }})</span>
                <span>- Rs.{{ coupon()!.discountAmount.toFixed(2) }}</span>
              </div>
            }
            <div class="flex justify-between text-sm font-semibold text-gray-800 dark:text-gray-200 border-t border-gray-200 dark:border-gray-700 pt-1.5">
              <span>Total</span><span>Rs.{{ finalTotal().toFixed(2) }}</span>
            </div>
          </div>
        </div>
      }

      <!-- Coupon -->
      <div class="mb-5">
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Promo Code</label>
        <div class="flex gap-2">
          <input type="text" [(ngModel)]="couponCode" placeholder="Enter coupon code"
            class="flex-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition uppercase" />
          <button (click)="validateCoupon()" [disabled]="couponLoading() || !couponCode"
            class="bg-gray-800 dark:bg-gray-700 hover:bg-gray-900 dark:hover:bg-gray-600 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg transition">
            {{ couponLoading() ? '...' : 'Apply' }}
          </button>
        </div>
        @if (coupon()) {
          <p [class]="coupon()!.valid ? 'text-green-600 dark:text-green-400' : 'text-red-500'"
            class="text-xs mt-1.5">{{ coupon()!.message }}</p>
        }
      </div>

      <!-- Free Delivery Banner -->
      <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 mb-5">
        <div class="flex items-center">
          <div class="w-10 h-10 bg-blue-500 rounded-full flex items-center justify-center mr-3">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path>
            </svg>
          </div>
          <div class="flex-1">
            @if (getFreeDeliveryAmount() > 0) {
              <h3 class="text-blue-900 dark:text-blue-100 font-medium">Get FREE delivery</h3>
              <p class="text-blue-700 dark:text-blue-300 text-sm">Add products worth ₹{{ getFreeDeliveryAmount().toFixed(2) }} more</p>
              <div class="w-full bg-blue-200 dark:bg-blue-800 rounded-full h-2 mt-2">
                <div class="bg-blue-500 h-2 rounded-full" [style.width.%]="getFreeDeliveryProgress()"></div>
              </div>
            } @else {
              <h3 class="text-green-900 dark:text-green-100 font-medium">🎉 FREE delivery unlocked!</h3>
              <p class="text-green-700 dark:text-green-300 text-sm">Your order qualifies for free delivery</p>
            }
          </div>
        </div>
        <button routerLink="/offers" class="text-blue-600 dark:text-blue-400 text-sm font-medium mt-3 flex items-center">
          See all coupons
          <svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
          </svg>
        </button>
      </div>

      @if (error()) {
        <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
          {{ error() }}
        </div>
      }

      <form (ngSubmit)="proceedToPayment()" #f="ngForm" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Delivery address</label>
          <textarea name="address" [(ngModel)]="address" required rows="3"
            placeholder="123 Main St, City, State, ZIP"
            class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 resize-none transition"></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes (optional)</label>
          <input type="text" name="notes" [(ngModel)]="notes" placeholder="Leave at door, ring bell, etc."
            class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
        </div>
        <button type="submit" [disabled]="loading() || f.invalid"
          class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-3 rounded-xl font-medium transition">
          {{ loading() ? 'Processing...' : 'Place Order & Pay' }}
        </button>
      </form>
    </div>
  `
})
export class Checkout implements OnInit {
  private cartService = inject(CartService);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private notifService = inject(NotificationService);
  private invoiceService = inject(InvoiceService);
  private orderService = inject(OrderService);

  cart = this.cartService.cart;
  address = ''; notes = '';
  couponCode = '';
  coupon = signal<Coupon | null>(null);
  couponLoading = signal(false);
  loading = signal(false); error = signal('');

  // Free delivery threshold
  private readonly FREE_DELIVERY_THRESHOLD = 500;

  ngOnInit() {
    // Check for coupon code in query parameters
    this.route.queryParams.subscribe(params => {
      if (params['coupon']) {
        this.couponCode = params['coupon'];
        this.validateCoupon();
      }
    });
  }

  finalTotal() {
    const c = this.cart();
    if (!c) return 0;
    const delivery = c.subTotal >= 500 ? 0 : 49;
    const tax = c.subTotal * 0.05;
    const discount = this.coupon()?.valid ? (this.coupon()!.discountAmount ?? 0) : 0;
    return Math.max(0, c.subTotal + delivery + tax - discount);
  }

  validateCoupon() {
    if (!this.couponCode.trim()) return;
    this.couponLoading.set(true);
    this.http.post<Coupon>(`${environment.apiUrl}/api/v1/coupons/validate`, {
      code: this.couponCode.toUpperCase(),
      orderAmount: this.cart()?.subTotal ?? 0
    }).subscribe({
      next: r => { this.coupon.set(r); this.couponLoading.set(false); },
      error: () => { this.coupon.set({ valid: false, message: 'Failed to validate coupon', discountValue: 0, discountAmount: 0 }); this.couponLoading.set(false); }
    });
  }

  proceedToPayment() {
    this.loading.set(true); this.error.set('');

    // Create order first
    this.http.post<any>(`${environment.apiUrl}/api/v1/orders`, {
      deliveryAddress: this.address,
      notes: this.notes,
      couponCode: this.coupon()?.valid ? this.couponCode.toUpperCase() : undefined
    }).subscribe({
      next: (response) => {
        // Check if Razorpay order was created
        if (response.razorpayOrderId) {
          // Show Razorpay payment dialog
          this.showRazorpayPayment(response);
        } else {
          // Fallback if Razorpay is not available
          alert('Order created successfully! Payment gateway is currently unavailable.');
          this.router.navigate(['/orders']);
        }
      },
      error: (e) => {
        this.error.set('Failed to create order: ' + (e.error?.message || 'Unknown error'));
        this.loading.set(false);
      }
    });
  }

  private showRazorpayPayment(response: any) {
    const options = {
      key: response.razorpayKey || 'rzp_test_SUcMytUklz6zQh',
      amount: Math.round(this.finalTotal() * 100),
      currency: 'INR',
      name: 'FreshMart',
      description: 'Order Payment',
      order_id: response.razorpayOrderId,
      handler: (paymentResponse: any) => {
        this.verifyPayment(paymentResponse, response.order.id);
      },
      prefill: {
        name: 'Customer Name',
        email: 'customer@example.com',
        contact: '9999999999'
      },
      theme: {
        color: '#16a34a'
      },
      // Explicitly enable all payment methods
      method: {
        netbanking: true,
        card: true,
        wallet: true,
        upi: true,
        paylater: true,
        emi: true,
        cardless_emi: true,
        app: true,
        qr: true // Explicitly enable QR
      },
      // Custom configuration to force UPI/QR visibility
      config: {
        display: {
          blocks: {
            utib: { // Axis Bank
              name: 'Pay using Axis Bank',
              instruments: [
                { method: 'netbanking', banks: ['UTIB'] },
                { method: 'card', networks: ['VISA', 'MC', 'RUPAY'] },
                { method: 'upi' }
              ]
            },
            hdfc: { // HDFC Bank
              name: 'Pay using HDFC Bank',
              instruments: [
                { method: 'netbanking', banks: ['HDFC'] },
                { method: 'card', networks: ['VISA', 'MC', 'RUPAY'] },
                { method: 'upi' }
              ]
            },
            other: {
              name: 'Other Payment Methods',
              instruments: [
                { method: 'upi' }, // UPI with QR
                { method: 'card' },
                { method: 'netbanking' },
                { method: 'wallet' },
                { method: 'paylater' }
              ]
            }
          },
          sequence: ['block.utib', 'block.hdfc', 'block.other'],
          preferences: {
            show_default_blocks: true
          }
        }
      },
      modal: {
        ondismiss: () => {
          this.loading.set(false);
          this.error.set('Payment cancelled');
        }
      }
    };

    const rzp = new (window as any).Razorpay(options);
    rzp.open();
  }

  private verifyPayment(response: any, orderId: string) {
    this.http.post(`${environment.apiUrl}/api/v1/payment/verify`, {
      RazorpayOrderId: response.razorpay_order_id,
      RazorpayPaymentId: response.razorpay_payment_id,
      RazorpaySignature: response.razorpay_signature
    }).subscribe({
      next: () => {
        this.http.post(`${environment.apiUrl}/api/v1/orders/${orderId}/complete-payment`, {}).subscribe({
          next: () => {
            setTimeout(() => this.notifService.loadAll(), 1000);
            // Auto-download invoice
            this.orderService.getOrder(orderId).subscribe({
              next: (order) => {
                this.invoiceService.downloadInvoice(order);
                this.router.navigate(['/orders']);
              },
              error: () => this.router.navigate(['/orders'])
            });
          },
          error: () => {
            setTimeout(() => this.notifService.loadAll(), 1000);
            this.router.navigate(['/orders']);
          }
        });
      },
      error: () => {
        this.error.set('Payment verification failed. Please contact support.');
        this.loading.set(false);
      }
    });
  }

  getCurrentCartValue(): number {
    const cart = this.cart();
    if (!cart) return 0;
    return cart.subTotal || 0;
  }

  getFreeDeliveryAmount(): number {
    const currentValue = this.getCurrentCartValue();
    const remaining = this.FREE_DELIVERY_THRESHOLD - currentValue;
    return Math.max(0, parseFloat(remaining.toFixed(2)));
  }

  getFreeDeliveryProgress(): number {
    const currentValue = this.getCurrentCartValue();
    const progress = (currentValue / this.FREE_DELIVERY_THRESHOLD) * 100;
    return Math.min(100, Math.max(0, progress));
  }
}
