import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CouponService } from '../../core/services/coupon.service';
import { CartService } from '../../core/services/cart.service';
import { CouponDto } from '../../core/models';

@Component({
  selector: 'app-offers',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <!-- Header -->
      <div class="bg-white/80 dark:bg-gray-800/80 backdrop-blur-md shadow-lg border-b border-gray-200/50 dark:border-gray-700/50">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="flex items-center justify-between h-16">
            <div class="flex items-center">
              <button (click)="goBack()" class="mr-4 p-2 rounded-xl hover:bg-gray-100 dark:hover:bg-gray-700 transition-all duration-200 group">
                <svg class="w-6 h-6 text-gray-600 dark:text-gray-300 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                </svg>
              </button>
              <div>
                <h1 class="text-xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">🎉 Amazing Offers</h1>
                <p class="text-xs text-gray-500 dark:text-gray-400">Save big on your favorite items</p>
              </div>
            </div>
            <div class="flex items-center space-x-2">
              <div class="w-2 h-2 bg-green-400 rounded-full animate-pulse"></div>
              <span class="text-xs text-green-600 dark:text-green-400 font-medium">{{ coupons().length }} Active Offers</span>
            </div>
          </div>
        </div>
      </div>

      <div class="max-w-md mx-auto p-4 space-y-6">
        <!-- Promo Code Input -->
        <div class="bg-white/70 dark:bg-gray-800/70 backdrop-blur-md rounded-2xl p-6 shadow-xl border border-white/20 dark:border-gray-700/20">
          <div class="flex items-center mb-4">
            <div class="w-10 h-10 bg-gradient-to-r from-orange-400 to-pink-500 rounded-xl flex items-center justify-center mr-3">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"></path>
              </svg>
            </div>
            <div>
              <h2 class="text-lg font-bold text-gray-900 dark:text-white">Promo Code</h2>
              <p class="text-xs text-gray-500 dark:text-gray-400">Enter your coupon code below</p>
            </div>
          </div>
          <div class="flex gap-3">
            <input
              type="text"
              [(ngModel)]="promoCode"
              placeholder="ENTER COUPON CODE"
              class="flex-1 bg-gray-50 dark:bg-gray-700 border-0 rounded-xl px-4 py-3 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-gray-600 transition-all duration-200 uppercase font-mono"
            />
            <button
              (click)="applyPromoCode()"
              [disabled]="!promoCode()"
              class="bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-3 rounded-xl text-sm font-semibold transition-all duration-200 transform hover:scale-105 active:scale-95 shadow-lg"
            >
              Apply
            </button>
          </div>
        </div>

        <!-- Bank Offers Section -->
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h3 class="text-xl font-bold text-gray-900 dark:text-white flex items-center">
              <span class="w-8 h-8 bg-gradient-to-r from-blue-500 to-purple-500 rounded-lg flex items-center justify-center mr-3">
                <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"></path>
                </svg>
              </span>
              Bank Offers
            </h3>
            <span class="text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 px-2 py-1 rounded-full font-medium">
              {{ coupons().length }} Available
            </span>
          </div>

          @if (loading()) {
            <div class="space-y-4">
              @for (i of [1,2,3]; track i) {
                <div class="bg-white/70 dark:bg-gray-800/70 backdrop-blur-md rounded-2xl p-6 shadow-lg animate-pulse">
                  <div class="flex items-center justify-between">
                    <div class="flex items-center space-x-4">
                      <div class="w-14 h-10 bg-gray-300 dark:bg-gray-600 rounded-xl"></div>
                      <div class="space-y-2">
                        <div class="h-5 bg-gray-300 dark:bg-gray-600 rounded w-32"></div>
                        <div class="h-4 bg-gray-300 dark:bg-gray-600 rounded w-24"></div>
                      </div>
                    </div>
                    <div class="w-20 h-10 bg-gray-300 dark:bg-gray-600 rounded-xl"></div>
                  </div>
                </div>
              }
            </div>
          } @else {
            <div class="space-y-4">
              @for (coupon of coupons(); track coupon.code) {
                <div class="bg-white/70 dark:bg-gray-800/70 backdrop-blur-md rounded-2xl p-6 shadow-xl border border-white/20 dark:border-gray-700/20 hover:shadow-2xl hover:scale-[1.02] transition-all duration-300 group">
                  <div class="flex items-center justify-between mb-4">
                    <div class="flex items-center space-x-4">
                      <!-- Enhanced Coupon Icons -->
                      <div class="relative">
                        <div class="w-14 h-10 rounded-xl flex items-center justify-center text-white text-sm font-bold shadow-lg"
                             [ngClass]="getCouponIconClass(coupon.code)">
                          {{ getCouponIconText(coupon.code) }}
                        </div>
                        @if (coupon.discountType === 'Percentage') {
                          <div class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 rounded-full flex items-center justify-center">
                            <span class="text-white text-xs font-bold">%</span>
                          </div>
                        }
                      </div>

                      <div>
                        <h4 class="font-bold text-gray-900 dark:text-white text-lg">
                          @if (coupon.discountType === 'Percentage') {
                            Get {{ coupon.discountValue }}% OFF
                          } @else {
                            Flat ₹{{ coupon.discountValue }} off
                          }
                          @if (coupon.discountType === 'Percentage') {
                            <span class="text-sm text-gray-500 dark:text-gray-400">upto ₹{{ coupon.discountValue * 10 }}</span>
                          }
                        </h4>
                        <p class="text-sm text-gray-600 dark:text-gray-400 font-medium">
                          Use code <span class="font-mono bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded text-xs">{{ coupon.code }}</span>
                        </p>
                      </div>
                    </div>

                    <button
                      (click)="applyCoupon(coupon.code)"
                      [disabled]="getCurrentCartValue() < coupon.minOrderAmount"
                      [ngClass]="getCurrentCartValue() >= coupon.minOrderAmount ? 'bg-gradient-to-r from-green-500 to-emerald-500 hover:from-green-600 hover:to-emerald-600 shadow-lg hover:shadow-xl' : 'bg-gray-400 cursor-not-allowed'"
                      class="text-white px-6 py-3 rounded-xl text-sm font-bold transition-all duration-200 transform hover:scale-105 active:scale-95"
                    >
                      {{ getCurrentCartValue() >= coupon.minOrderAmount ? 'Apply' : 'Locked' }}
                    </button>
                  </div>

                  <!-- Enhanced Coupon Details -->
                  <div class="space-y-2 bg-gray-50/50 dark:bg-gray-700/30 rounded-xl p-4">
                    @if (coupon.discountType === 'Percentage') {
                      <div class="flex items-center text-xs text-gray-600 dark:text-gray-400">
                        <div class="w-2 h-2 bg-blue-400 rounded-full mr-2"></div>
                        Maximum Discount is ₹{{ coupon.discountValue * 10 }}
                      </div>
                    } @else {
                      <div class="flex items-center text-xs text-gray-600 dark:text-gray-400">
                        <div class="w-2 h-2 bg-green-400 rounded-full mr-2"></div>
                        Fixed discount amount
                      </div>
                    }
                    @if (getCurrentCartValue() < coupon.minOrderAmount) {
                      <div class="flex items-center text-xs text-orange-600 dark:text-orange-400">
                        <div class="w-2 h-2 bg-orange-400 rounded-full mr-2"></div>
                        Add items worth ₹{{ (coupon.minOrderAmount - getCurrentCartValue()).toFixed(2) }} more to unlock
                      </div>
                    } @else {
                      <div class="flex items-center text-xs text-green-600 dark:text-green-400">
                        <div class="w-2 h-2 bg-green-400 rounded-full mr-2 animate-pulse"></div>
                        ✅ Ready to apply on your cart
                      </div>
                    }
                    @if (coupon.expiresAt) {
                      <div class="flex items-center text-xs text-gray-600 dark:text-gray-400">
                        <div class="w-2 h-2 bg-red-400 rounded-full mr-2"></div>
                        Valid till {{ formatDate(coupon.expiresAt) }}
                      </div>
                    }
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <!-- Bottom Spacing -->
        <div class="h-8"></div>
      </div>
    </div>
  `
})
export class OffersComponent implements OnInit {
  private couponService = inject(CouponService);
  private cartService = inject(CartService);
  private router = inject(Router);

  coupons = signal<CouponDto[]>([]);
  loading = signal(true);
  promoCode = signal('');

  ngOnInit() {
    this.loadCoupons();
    this.loadCart();
  }

  private loadCart() {
    // Load cart data if not already loaded
    if (!this.cartService.cart()) {
      this.cartService.getCart().subscribe({
        next: (cart) => {
          console.log('Cart loaded:', cart);
        },
        error: (error) => {
          console.error('Failed to load cart:', error);
          // If cart fails to load, we can still show offers but with default values
        }
      });
    }
  }

  private loadCoupons() {
    this.couponService.getAllCoupons().subscribe({
      next: (coupons) => {
        this.coupons.set(coupons);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load coupons:', error);
        this.loading.set(false);
      }
    });
  }

  applyCoupon(code: string) {
    // Copy to clipboard and navigate back to checkout
    navigator.clipboard.writeText(code).then(() => {
      this.router.navigate(['/checkout'], {
        queryParams: { coupon: code }
      });
    });
  }

  applyPromoCode() {
    if (this.promoCode()) {
      this.applyCoupon(this.promoCode());
    }
  }

  goBack() {
    this.router.navigate(['/checkout']);
  }

  getCurrentCartValue(): number {
    const cart = this.cartService.cart();
    if (!cart) return 0;

    // Use the subTotal from the cart which is calculated on the backend
    return cart.subTotal || 0;
  }

  getCouponIconClass(code: string): string {
    switch (code) {
      case 'WELCOME10': return 'bg-gradient-to-r from-gray-800 to-gray-900';
      case 'FLAT50': return 'bg-gradient-to-r from-blue-500 to-blue-600';
      case 'FRESH20': return 'bg-gradient-to-r from-purple-500 to-purple-600';
      default: return 'bg-gradient-to-r from-gray-600 to-gray-700';
    }
  }

  getCouponIconText(code: string): string {
    switch (code) {
      case 'WELCOME10': return 'one';
      case 'FLAT50': return 'D';
      case 'FRESH20': return 'pay';
      default: return '₹';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  // Special Offers Button Handlers
  viewAllCreditCardOffers() {
    // Show alert with credit card offer details
    alert('Credit Card Offers:\n\n• Extra 5% cashback on HDFC, ICICI, SBI cards\n• Minimum order ₹1000\n• Valid on all categories\n\nUse your credit card at checkout to avail this offer!');
  }

  shopWeekendSpecial() {
    // Navigate to products page with weekend special filter
    this.router.navigate(['/products'], {
      queryParams: {
        special: 'weekend',
        offer: 'buy2get1'
      }
    });
  }

  claimFirstOrderBonus() {
    // Check if user is new customer and apply first order bonus
    const isNewCustomer = this.checkIfNewCustomer();

    if (isNewCustomer) {
      // Apply FIRSTORDER coupon automatically
      this.applyCoupon('FIRSTORDER200');
    } else {
      alert('First Order Bonus is only available for new customers.\n\nThis offer provides ₹200 off on your first order with minimum purchase of ₹500.');
    }
  }

  private checkIfNewCustomer(): boolean {
    // This would typically check with the backend if user has made any previous orders
    // For demo purposes, we'll simulate this check
    const hasOrders = localStorage.getItem('hasOrders');
    return !hasOrders || hasOrders === 'false';
  }
}
