import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Order } from '../../core/models';
import { OrderService } from '../../core/services/order.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

const DRIVER_STATUSES = ['Shipped', 'OutForDelivery', 'Delivered'];

@Component({
  selector: 'app-delivery',
  standalone: true,
  imports: [FormsModule, CommonModule, DatePipe],
  template: `
<div class="page">
  <div class="page-header">
    <div>
      <h1 class="page-title">My Deliveries</h1>
      <p class="page-sub">Manage your assigned delivery orders</p>
    </div>
    <span class="result-count">{{ allRelevant().length }} orders</span>
  </div>

  <!-- Stats -->
  <div class="stats">
    <div class="stat-card c1">
      <div class="stat-icon">&#x1F4E6;</div>
      <div><div class="stat-val">{{ allRelevant().length }}</div><div class="stat-lbl">Total Assigned</div></div>
    </div>
    <div class="stat-card c2">
      <div class="stat-icon">&#x1F69A;</div>
      <div><div class="stat-val">{{ countByStatus('Shipped') }}</div><div class="stat-lbl">Ready to Pick</div></div>
    </div>
    <div class="stat-card c3">
      <div class="stat-icon">&#x1F4CD;</div>
      <div><div class="stat-val">{{ countByStatus('OutForDelivery') }}</div><div class="stat-lbl">Out for Delivery</div></div>
    </div>
    <div class="stat-card c4">
      <div class="stat-icon">&#x2705;</div>
      <div><div class="stat-val">{{ countByStatus('Delivered') }}</div><div class="stat-lbl">Delivered</div></div>
    </div>
  </div>

  <!-- Tabs -->
  <div class="tabs">
    @for (tab of tabs; track tab.value) {
      <button class="tab-btn" [class.tab-active]="activeTab() === tab.value" (click)="activeTab.set(tab.value)">
        {{ tab.label }}
        <span class="tab-count">{{ tab.value === 'all' ? allRelevant().length : countByStatus(tab.value) }}</span>
      </button>
    }
  </div>

  <!-- Orders -->
  <div class="table-card">
    @if (loading()) {
      <div class="loading-rows">
        @for (i of [1,2,3]; track i) { <div class="skeleton"></div> }
      </div>
    } @else if (visibleOrders().length === 0) {
      <div class="empty">
        <p class="empty-icon">&#x1F4ED;</p>
        <p class="empty-text">No {{ activeTab() === 'all' ? '' : activeTab() }} deliveries</p>
      </div>
    } @else {
      @for (o of visibleOrders(); track o.id; let i = $index) {
        <div class="order-card" [class.row-alt]="i % 2 === 1">
          <div class="order-top">
            <div class="order-meta">
              <span class="order-id">#{{ o.id.slice(0,8).toUpperCase() }}</span>
              <span class="order-date">{{ o.createdAt | date:'dd MMM yyyy, HH:mm' }}</span>
            </div>
            <span class="status-chip" [class]="'sc-' + o.status.toLowerCase()">{{ o.status }}</span>
          </div>

          <div class="order-addr">
            <span class="addr-icon">&#x1F4CD;</span>
            <span>{{ o.deliveryAddress }}</span>
          </div>

          <div class="order-items">
            @for (item of o.items; track item.productId) {
              <span class="item-tag">{{ item.productName }} x{{ item.quantity }}</span>
            }
          </div>

          <div class="order-footer">
            <span class="order-total">&#x20B9;{{ o.totalAmount.toFixed(2) }}</span>
            <div class="order-actions">
              @if (o.status === 'Shipped') {
                <button class="btn-out" (click)="advance(o, 'OutForDelivery')" [disabled]="updating() === o.id">
                  {{ updating() === o.id ? 'Updating...' : '&#x1F69A; Out for Delivery' }}
                </button>
              }
              @if (o.status === 'OutForDelivery') {
                <button class="btn-delivered" (click)="advance(o, 'Delivered')" [disabled]="updating() === o.id">
                  {{ updating() === o.id ? 'Updating...' : '&#x2705; Mark Delivered' }}
                </button>
              }
              @if (o.status === 'Delivered') {
                <span class="delivered-label">&#x2705; Completed</span>
              }
            </div>
          </div>
        </div>
      }
    }
  </div>
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:28px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#38bdf8,#818cf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text3); }
    .result-count { font-size:13px; color:var(--adm-text2); background:var(--adm-card); padding:6px 14px; border-radius:20px; border:1px solid var(--adm-border); }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid var(--adm-border); }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s4);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s2);}
    .stat-icon { font-size:26px; }
    .stat-val { font-size:28px; font-weight:800; color:var(--adm-stat-val); line-height:1; }
    .stat-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    .tabs { display:flex; gap:8px; margin-bottom:20px; flex-wrap:wrap; }
    .tab-btn { display:flex; align-items:center; gap:8px; padding:9px 18px; border-radius:10px; border:1px solid var(--adm-border2); background:var(--adm-card); color:var(--adm-text2); font-size:13px; font-weight:600; cursor:pointer; transition:all .15s; }
    .tab-btn:hover { background:var(--adm-row-hover); color:var(--adm-text); }
    .tab-active { background:linear-gradient(135deg,rgba(56,189,248,.15),rgba(129,140,248,.1)) !important; color:#0ea5e9 !important; border-color:rgba(56,189,248,.4) !important; }
    .tab-count { background:var(--adm-border); color:var(--adm-text2); font-size:11px; padding:1px 7px; border-radius:20px; }
    .tab-active .tab-count { background:rgba(56,189,248,.2); color:#0ea5e9; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }

    .order-card { padding:18px 20px; border-bottom:1px solid var(--adm-border); transition:background .15s; }
    .order-card:last-child { border-bottom:none; }
    .order-card.row-alt { background:var(--adm-row-alt); }
    .order-card:hover { background:var(--adm-row-hover); }

    .order-top { display:flex; justify-content:space-between; align-items:center; margin-bottom:10px; }
    .order-meta { display:flex; align-items:center; gap:12px; }
    .order-id { font-family:monospace; font-size:13px; font-weight:700; color:var(--adm-text); }
    .order-date { font-size:12px; color:var(--adm-text2); }

    .status-chip { padding:4px 12px; border-radius:20px; font-size:12px; font-weight:700; }
    .sc-shipped { background:rgba(139,92,246,.15); color:#6d28d9; border:1px solid rgba(139,92,246,.3); }
    .sc-outfordelivery { background:rgba(249,115,22,.15); color:#c2410c; border:1px solid rgba(249,115,22,.3); }
    .sc-delivered { background:rgba(34,197,94,.15); color:#15803d; border:1px solid rgba(34,197,94,.3); }

    .order-addr { display:flex; align-items:flex-start; gap:8px; font-size:13px; color:var(--adm-text2); margin-bottom:10px; }
    .addr-icon { flex-shrink:0; font-size:14px; }

    .order-items { display:flex; flex-wrap:wrap; gap:6px; margin-bottom:14px; }
    .item-tag { background:var(--adm-card2); border:1px solid var(--adm-border); color:var(--adm-text2); font-size:12px; padding:3px 10px; border-radius:20px; }

    .order-footer { display:flex; justify-content:space-between; align-items:center; }
    .order-total { font-size:16px; font-weight:800; color:var(--adm-text); }
    .order-actions { display:flex; gap:10px; }

    .btn-out { background:linear-gradient(135deg,#7c3aed,#6d28d9); color:#fff; border:none; padding:9px 18px; border-radius:9px; font-size:13px; font-weight:700; cursor:pointer; transition:all .15s; box-shadow:0 4px 12px rgba(124,58,237,.3); }
    .btn-out:hover:not(:disabled) { transform:translateY(-1px); box-shadow:0 6px 16px rgba(124,58,237,.4); }
    .btn-out:disabled { opacity:.6; cursor:not-allowed; transform:none; }

    .btn-delivered { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:9px 18px; border-radius:9px; font-size:13px; font-weight:700; cursor:pointer; transition:all .15s; box-shadow:0 4px 12px rgba(34,197,94,.3); }
    .btn-delivered:hover:not(:disabled) { transform:translateY(-1px); box-shadow:0 6px 16px rgba(34,197,94,.4); }
    .btn-delivered:disabled { opacity:.6; cursor:not-allowed; transform:none; }

    .delivered-label { font-size:13px; font-weight:700; color:#15803d; }

    .empty { text-align:center; padding:60px; display:flex; flex-direction:column; align-items:center; gap:8px; }
    .empty-icon { font-size:48px; }
    .empty-text { font-size:15px; color:var(--adm-text2); margin:0; }
    .loading-rows { padding:16px; display:flex; flex-direction:column; gap:10px; }
    .skeleton { height:120px; background:var(--adm-border); border-radius:10px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  `]
})
export class Delivery implements OnInit {
  private orderService = inject(OrderService);
  private http = inject(HttpClient);

  orders = signal<Order[]>([]);
  loading = signal(true);
  activeTab = signal('all');
  updating = signal<string>('');

  tabs = [
    { label: 'All',             value: 'all' },
    { label: 'Ready to Pick',   value: 'Shipped' },
    { label: 'Out for Delivery',value: 'OutForDelivery' },
    { label: 'Delivered',       value: 'Delivered' },
  ];

  allRelevant = () => this.orders().filter(o => DRIVER_STATUSES.includes(o.status));

  visibleOrders = () => {
    const tab = this.activeTab();
    return tab === 'all' ? this.allRelevant() : this.allRelevant().filter(o => o.status === tab);
  };

  countByStatus = (status: string) => this.allRelevant().filter(o => o.status === status).length;

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: o => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  advance(order: Order, status: string) {
    // Optimistic update — change UI instantly, then sync with server
    this.updating.set(order.id);
    this.orders.update(list => list.map(o => o.id === order.id ? { ...o, status: status as any } : o));

    this.http.patch(`${environment.apiUrl}/api/v1/orders/${order.id}/status`, { status }).subscribe({
      next: () => this.updating.set(''),
      error: () => {
        // Revert on failure
        this.orders.update(list => list.map(o => o.id === order.id ? { ...o, status: order.status } : o));
        this.updating.set('');
      }
    });
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Shipped: 'sc-shipped', OutForDelivery: 'sc-outfordelivery', Delivered: 'sc-delivered',
    };
    return m[s] ?? '';
  }
}
