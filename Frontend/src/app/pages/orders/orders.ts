import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { Order } from '../../core/models';
import { OrderService } from '../../core/services/order.service';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [RouterLink, CommonModule, DatePipe],
  template: `
<div class="ord-page">
  <div class="ord-header">
    <div>
      <h1 class="ord-title">My Orders</h1>
      <p class="ord-sub">Track and manage your orders</p>
    </div>
    <span class="ord-count">{{ orders().length }} orders</span>
  </div>

  <div class="ord-stats">
    <div class="ord-stat c1"><div class="os-val">{{ orders().length }}</div><div class="os-lbl">Total Orders</div></div>
    <div class="ord-stat c2"><div class="os-val">{{ countByStatus('Delivered') }}</div><div class="os-lbl">Delivered</div></div>
    <div class="ord-stat c3"><div class="os-val">{{ countByStatus('Pending') + countByStatus('Processing') }}</div><div class="os-lbl">Being Prepared</div></div>
    <div class="ord-stat c4"><div class="os-val">{{ countByStatus('Shipped') + countByStatus('OutForDelivery') }}</div><div class="os-lbl">On the Way</div></div>
  </div>

  @if (loading()) {
    <div class="ord-skels">
      @for (i of [1,2,3]; track i) { <div class="ord-skel"></div> }
    </div>
  } @else if (orders().length === 0) {
    <div class="ord-empty">
      <p class="ord-empty-icon">&#x1F4E6;</p>
      <p class="ord-empty-title">No orders yet</p>
      <a routerLink="/products" class="ord-shop-btn">Start Shopping</a>
    </div>
  } @else {
    <div class="ord-list">
      @for (order of orders(); track order.id) {
        <div class="ord-card">
          <div class="ord-card-top">
            <div class="ord-id-wrap">
              <span class="ord-id">#{{ order.id.slice(0,8).toUpperCase() }}</span>
              <span class="ord-date">{{ order.createdAt | date:'dd MMM yyyy, hh:mm a' }}</span>
            </div>
            <span class="ord-status" [class]="'ost-' + order.status.toLowerCase()">{{ order.status }}</span>
          </div>
          <div class="ord-items-box">
            @for (item of order.items; track item.productId) {
              <span class="ord-item-tag">{{ item.productName }} x{{ item.quantity }}</span>
            }
          </div>
          <div class="ord-card-footer">
            <div class="ord-meta">
              <span class="ord-addr">&#x1F4CD; {{ order.deliveryAddress }}</span>
            </div>
            <span class="ord-total">&#x20B9;{{ order.totalAmount.toFixed(2) }}</span>
          </div>
          <div class="ord-actions">
            <a [routerLink]="['/orders', order.id, 'track']" class="ord-btn-track">Track Order</a>
            <button (click)="reorder(order)" class="ord-btn-reorder">Reorder</button>
            <button (click)="rateOrder(order)" [disabled]="order.status !== 'Delivered'" class="ord-btn-rate">
              Rate Order
            </button>
          </div>
        </div>
      }
    </div>
  }
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .ord-page { padding:28px; min-height:100vh; background:var(--adm-bg); color:var(--adm-text); max-width:860px; margin:0 auto; }
    .ord-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:24px; }
    .ord-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#38bdf8,#818cf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .ord-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text3); }
    .ord-count { font-size:13px; font-weight:600; color:var(--adm-text2); background:var(--adm-card); border:1px solid var(--adm-border); padding:6px 14px; border-radius:20px; }

    .ord-stats { display:grid; grid-template-columns:repeat(4,1fr); gap:12px; margin-bottom:24px; }
    .ord-stat { border-radius:12px; padding:16px; border:1px solid var(--adm-border); text-align:center; }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s2);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s4);}
    .os-val { font-size:26px; font-weight:800; color:var(--adm-stat-val); }
    .os-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    .ord-list { display:flex; flex-direction:column; gap:16px; }
    .ord-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; padding:20px; transition:box-shadow .2s; }
    .ord-card:hover { box-shadow:0 4px 20px rgba(0,0,0,.12); }

    .ord-card-top { display:flex; justify-content:space-between; align-items:center; margin-bottom:14px; }
    .ord-id-wrap { display:flex; align-items:center; gap:12px; }
    .ord-id { font-family:monospace; font-size:14px; font-weight:800; color:var(--adm-text); }
    .ord-date { font-size:12px; color:var(--adm-text2); }

    .ord-status { padding:4px 12px; border-radius:20px; font-size:12px; font-weight:700; }
    .ost-delivered{background:rgba(34,197,94,.15);color:#15803d;border:1px solid rgba(34,197,94,.3);}
    .ost-pending{background:rgba(251,191,36,.15);color:#b45309;border:1px solid rgba(251,191,36,.3);}
    .ost-processing{background:rgba(59,130,246,.15);color:#1d4ed8;border:1px solid rgba(59,130,246,.3);}
    .ost-shipped{background:rgba(139,92,246,.15);color:#6d28d9;border:1px solid rgba(139,92,246,.3);}
    .ost-outfordelivery{background:rgba(249,115,22,.15);color:#c2410c;border:1px solid rgba(249,115,22,.3);}
    .ost-cancelled{background:rgba(220,38,38,.15);color:#dc2626;border:1px solid rgba(220,38,38,.3);}

    .ord-items-box { display:flex; flex-wrap:wrap; gap:8px; margin-bottom:14px; }
    .ord-item-tag { background:var(--adm-card2); border:1px solid var(--adm-border); color:var(--adm-text2); font-size:12px; padding:4px 12px; border-radius:20px; }

    .ord-card-footer { display:flex; justify-content:space-between; align-items:center; margin-bottom:16px; }
    .ord-addr { font-size:12px; color:var(--adm-text2); }
    .ord-total { font-size:18px; font-weight:800; color:var(--adm-text); }

    .ord-actions { display:flex; gap:10px; }
    .ord-btn-track { background:var(--adm-card2); border:1px solid var(--adm-border2); color:var(--adm-text); font-size:13px; font-weight:600; padding:9px 18px; border-radius:9px; text-decoration:none; transition:all .2s; }
    .ord-btn-track:hover { background:var(--adm-border); }
    .ord-btn-reorder { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; font-size:13px; font-weight:700; padding:9px 18px; border-radius:9px; cursor:pointer; box-shadow:0 4px 12px rgba(34,197,94,.25); transition:all .2s; }
    .ord-btn-reorder:hover { transform:translateY(-1px); }
    .ord-btn-rate { background:linear-gradient(135deg,#2563eb,#1d4ed8); color:#fff; border:none; font-size:13px; font-weight:700; padding:9px 18px; border-radius:9px; cursor:pointer; box-shadow:0 4px 12px rgba(37,99,235,.25); transition:all .2s; }
    .ord-btn-rate:hover:not(:disabled) { transform:translateY(-1px); }
    .ord-btn-rate:disabled { background:var(--adm-border); color:var(--adm-text3); cursor:not-allowed; box-shadow:none; }

    .ord-empty { text-align:center; padding:60px 20px; display:flex; flex-direction:column; align-items:center; gap:12px; }
    .ord-empty-icon { font-size:56px; }
    .ord-empty-title { font-size:18px; font-weight:700; color:var(--adm-text); margin:0; }
    .ord-shop-btn { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; text-decoration:none; padding:12px 28px; border-radius:10px; font-size:14px; font-weight:700; }

    .ord-skels { display:flex; flex-direction:column; gap:16px; }
    .ord-skel { height:160px; background:var(--adm-card); border-radius:16px; border:1px solid var(--adm-border); animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  `]
})
export class Orders implements OnInit {
  private orderService = inject(OrderService);
  private cartService = inject(CartService);
  private router = inject(Router);
  orders = signal<Order[]>([]);
  loading = signal(true);

  countByStatus = (s: string) => this.orders().filter(o => o.status === s).length;

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: o => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  reorder(order: Order) {
    let done = 0;
    order.items.forEach(item => {
      this.cartService.addItem(item.productId, item.quantity).subscribe({
        next: () => { done++; if (done === order.items.length) this.router.navigate(['/cart']); },
        error: () => done++
      });
    });
  }

  rateOrder(order: Order) { this.router.navigate(['/orders', order.id, 'rate']); }
}
