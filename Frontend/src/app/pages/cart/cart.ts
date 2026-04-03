import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
<div class="cart-page">
  <div class="cart-header">
    <div>
      <h1 class="cart-title">Your Cart</h1>
      <p class="cart-sub">Review your items before checkout</p>
    </div>
    @if (cart() && cart()!.items.length > 0) {
      <span class="cart-count">{{ cart()!.totalItems }} item{{ cart()!.totalItems !== 1 ? 's' : '' }}</span>
    }
  </div>

  @if (loading()) {
    <div class="cart-skels">
      @for (i of [1,2,3]; track i) { <div class="cart-skel"></div> }
    </div>
  } @else if (!cart() || cart()!.items.length === 0) {
    <div class="cart-empty">
      <p class="cart-empty-icon">&#x1F6D2;</p>
      <p class="cart-empty-title">Your cart is empty</p>
      <p class="cart-empty-sub">Add some products to get started</p>
      <a routerLink="/products" class="cart-shop-btn">Browse Products</a>
    </div>
  } @else {
    <div class="cart-layout">
      <!-- Items -->
      <div class="cart-items">
        @if (cart()!.isOverBudget) {
          <div class="cart-budget-warn">
            &#x26A0; Over budget limit of &#x20B9;{{ cart()!.budgetLimit?.toFixed(2) }}
          </div>
        }
        @for (item of cart()!.items; track item.productId) {
          <div class="cart-item">
            <div class="cart-item-img-wrap">
              <img [src]="item.imageUrl" [alt]="item.productName" class="cart-item-img" />
            </div>
            <div class="cart-item-info">
              <p class="cart-item-name">{{ item.productName }}</p>
              @if (item.discountPercent > 0) {
                <div class="cart-item-price-row">
                  <span class="cart-price-sale">&#x20B9;{{ item.unitPrice.toFixed(2) }}</span>
                  <span class="cart-price-orig">&#x20B9;{{ item.originalPrice.toFixed(2) }}</span>
                  <span class="cart-disc-badge">{{ item.discountPercent }}% OFF</span>
                </div>
              } @else {
                <p class="cart-price-normal">&#x20B9;{{ item.unitPrice.toFixed(2) }} each</p>
              }
            </div>
            <div class="cart-qty-ctrl">
              <button class="cart-qty-btn" (click)="update(item.productId, item.quantity - 1)">&#x2212;</button>
              <span class="cart-qty-val">{{ item.quantity }}</span>
              <button class="cart-qty-btn" (click)="update(item.productId, item.quantity + 1)">&#x2B;</button>
            </div>
            <span class="cart-item-total">&#x20B9;{{ item.totalPrice.toFixed(2) }}</span>
            <button class="cart-remove-btn" (click)="remove(item.productId)">&#x2715;</button>
          </div>
        }

        <!-- Budget -->
        <div class="cart-budget-row">
          <label class="cart-budget-label">Budget limit:</label>
          <input type="number" [(ngModel)]="budgetInput" placeholder="e.g. 500" min="0" class="cart-budget-input" />
          <button (click)="saveBudget()" class="cart-budget-btn">Set</button>
        </div>
      </div>

      <!-- Summary -->
      <div class="cart-summary">
        <div class="cart-summary-title">Order Summary</div>
        <div class="cart-summary-rows">
          <div class="cart-summary-row">
            <span>Subtotal ({{ cart()!.totalItems }} items)</span>
            <span>&#x20B9;{{ cart()!.subTotal.toFixed(2) }}</span>
          </div>
          <div class="cart-summary-row">
            <span>Delivery fee</span>
            <span [class.cart-free]="cart()!.subTotal >= 500">
              {{ cart()!.subTotal >= 500 ? 'Free' : '&#x20B9;49.00' }}
            </span>
          </div>
          <div class="cart-summary-row">
            <span>Tax (5%)</span>
            <span>&#x20B9;{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
          </div>
          @if (cart()!.subTotal < 500) {
            <div class="cart-free-hint">Add &#x20B9;{{ (500 - cart()!.subTotal).toFixed(2) }} more for free delivery</div>
          }
        </div>
        <div class="cart-summary-total">
          <span>Total</span>
          <span>&#x20B9;{{ (cart()!.subTotal + (cart()!.subTotal >= 500 ? 0 : 49) + cart()!.subTotal * 0.05).toFixed(2) }}</span>
        </div>
        <a routerLink="/checkout" class="cart-checkout-btn">Proceed to Checkout &#x2192;</a>
        <a routerLink="/products" class="cart-continue-link">&#x2190; Continue Shopping</a>
      </div>
    </div>
  }
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .cart-page { padding:28px; min-height:100vh; background:var(--adm-bg); color:var(--adm-text); max-width:1000px; margin:0 auto; }
    .cart-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:28px; }
    .cart-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#22c55e,#38bdf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .cart-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text3); }
    .cart-count { font-size:13px; font-weight:600; color:var(--adm-text2); background:var(--adm-card); border:1px solid var(--adm-border); padding:6px 14px; border-radius:20px; }

    .cart-layout { display:grid; grid-template-columns:1fr 320px; gap:20px; align-items:flex-start; }

    /* Items */
    .cart-items { display:flex; flex-direction:column; gap:12px; }
    .cart-budget-warn { background:rgba(251,191,36,.12); border:1px solid rgba(251,191,36,.3); color:#b45309; padding:10px 14px; border-radius:10px; font-size:13px; font-weight:600; }
    .cart-item { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:14px; padding:16px; display:flex; align-items:center; gap:14px; transition:box-shadow .2s; }
    .cart-item:hover { box-shadow:0 4px 16px rgba(0,0,0,.1); }
    .cart-item-img-wrap { width:64px; height:64px; border-radius:10px; overflow:hidden; background:var(--adm-card2); border:1px solid var(--adm-border); flex-shrink:0; }
    .cart-item-img { width:100%; height:100%; object-fit:cover; }
    .cart-item-info { flex:1; min-width:0; }
    .cart-item-name { font-size:14px; font-weight:700; color:var(--adm-text); margin:0 0 6px; }
    .cart-item-price-row { display:flex; align-items:center; gap:8px; }
    .cart-price-sale { font-size:14px; font-weight:800; color:#dc2626; }
    .cart-price-orig { font-size:12px; color:var(--adm-text3); text-decoration:line-through; }
    .cart-disc-badge { background:rgba(220,38,38,.12); color:#dc2626; border:1px solid rgba(220,38,38,.25); font-size:10px; font-weight:800; padding:2px 7px; border-radius:20px; }
    .cart-price-normal { font-size:13px; color:var(--adm-text2); margin:0; }
    .cart-qty-ctrl { display:flex; align-items:center; gap:10px; background:var(--adm-card2); border:1px solid var(--adm-border); border-radius:10px; padding:4px 8px; }
    .cart-qty-btn { width:28px; height:28px; border-radius:8px; border:none; background:var(--adm-border); color:var(--adm-text); font-size:16px; font-weight:700; cursor:pointer; display:flex; align-items:center; justify-content:center; transition:all .15s; }
    .cart-qty-btn:hover { background:var(--adm-border2); }
    .cart-qty-val { font-size:15px; font-weight:700; color:var(--adm-text); min-width:24px; text-align:center; }
    .cart-item-total { font-size:15px; font-weight:800; color:var(--adm-text); min-width:80px; text-align:right; }
    .cart-remove-btn { background:rgba(220,38,38,.1); border:1px solid rgba(220,38,38,.2); color:#dc2626; width:30px; height:30px; border-radius:8px; cursor:pointer; font-size:13px; display:flex; align-items:center; justify-content:center; transition:all .15s; flex-shrink:0; }
    .cart-remove-btn:hover { background:rgba(220,38,38,.25); }

    .cart-budget-row { display:flex; align-items:center; gap:10px; background:var(--adm-card); border:1px solid var(--adm-border); border-radius:12px; padding:14px 16px; }
    .cart-budget-label { font-size:13px; font-weight:600; color:var(--adm-text2); white-space:nowrap; }
    .cart-budget-input { flex:1; background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:8px 12px; border-radius:8px; font-size:14px; }
    .cart-budget-input:focus { outline:none; border-color:#22c55e; }
    .cart-budget-btn { background:var(--adm-card2); border:1px solid var(--adm-border2); color:var(--adm-text); padding:8px 16px; border-radius:8px; font-size:13px; font-weight:600; cursor:pointer; transition:all .15s; }
    .cart-budget-btn:hover { background:var(--adm-border); }

    /* Summary */
    .cart-summary { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; padding:22px; position:sticky; top:80px; }
    .cart-summary-title { font-size:16px; font-weight:800; color:var(--adm-text); margin-bottom:18px; padding-bottom:14px; border-bottom:1px solid var(--adm-border); }
    .cart-summary-rows { display:flex; flex-direction:column; gap:10px; margin-bottom:14px; }
    .cart-summary-row { display:flex; justify-content:space-between; font-size:14px; color:var(--adm-text2); }
    .cart-free { color:#15803d; font-weight:700; }
    .cart-free-hint { font-size:11px; color:#15803d; background:rgba(34,197,94,.1); border:1px solid rgba(34,197,94,.2); padding:6px 10px; border-radius:8px; text-align:center; margin-top:4px; }
    .cart-summary-total { display:flex; justify-content:space-between; font-size:17px; font-weight:800; color:var(--adm-text); padding:14px 0; border-top:2px solid var(--adm-border); margin-bottom:16px; }
    .cart-checkout-btn { display:block; width:100%; background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; text-align:center; text-decoration:none; padding:14px; border-radius:12px; font-size:15px; font-weight:800; box-shadow:0 4px 16px rgba(34,197,94,.3); transition:all .2s; margin-bottom:10px; }
    .cart-checkout-btn:hover { transform:translateY(-1px); box-shadow:0 6px 20px rgba(34,197,94,.4); }
    .cart-continue-link { display:block; text-align:center; font-size:13px; color:var(--adm-text2); text-decoration:none; }
    .cart-continue-link:hover { color:var(--adm-text); text-decoration:underline; }

    /* Empty */
    .cart-empty { text-align:center; padding:60px 20px; display:flex; flex-direction:column; align-items:center; gap:12px; }
    .cart-empty-icon { font-size:56px; }
    .cart-empty-title { font-size:20px; font-weight:800; color:var(--adm-text); margin:0; }
    .cart-empty-sub { font-size:14px; color:var(--adm-text2); margin:0; }
    .cart-shop-btn { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; text-decoration:none; padding:12px 28px; border-radius:10px; font-size:14px; font-weight:700; box-shadow:0 4px 12px rgba(34,197,94,.25); }

    .cart-skels { display:flex; flex-direction:column; gap:12px; }
    .cart-skel { height:90px; background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }

    @media (max-width:768px) { .cart-layout { grid-template-columns:1fr; } }
  `]
})
export class CartPage implements OnInit {
  private cartService = inject(CartService);
  cart = this.cartService.cart;
  loading = signal(true);
  budgetInput: number | null = null;

  ngOnInit() {
    this.cartService.getCart().subscribe({ next: () => this.loading.set(false), error: () => this.loading.set(false) });
  }

  update(productId: string, qty: number) {
    if (qty <= 0) { this.remove(productId); return; }
    this.cartService.updateItem(productId, qty).subscribe();
  }

  remove(productId: string) { this.cartService.removeItem(productId).subscribe(); }

  saveBudget() {
    this.cartService.setBudget(this.budgetInput).subscribe(() => this.cartService.getCart().subscribe());
  }
}