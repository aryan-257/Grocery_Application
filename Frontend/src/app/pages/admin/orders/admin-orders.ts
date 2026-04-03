import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { Order } from '../../../core/models';
import { OrderService } from '../../../core/services/order.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

const STATUSES = ['Pending','PaymentPending','PaymentConfirmed','Processing','Shipped','OutForDelivery','Delivered','Cancelled'];

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [FormsModule, CommonModule, DatePipe],
  template: `
<div class="page">
  <div class="page-header">
    <div>
      <h1 class="page-title">Order Management</h1>
      <p class="page-sub">Track and update all customer orders</p>
    </div>
    <span class="result-count">{{ filtered().length }} orders</span>
  </div>

  <!-- Stats -->
  <div class="stats">
    <div class="stat-card c1"><div class="stat-icon">🛒</div><div><div class="stat-val">{{ allOrders().length }}</div><div class="stat-lbl">Total</div></div></div>
    <div class="stat-card c2"><div class="stat-icon">⏳</div><div><div class="stat-val">{{ countByStatus('Pending') }}</div><div class="stat-lbl">Pending</div></div></div>
    <div class="stat-card c3"><div class="stat-icon">🚚</div><div><div class="stat-val">{{ countByStatus('Shipped') + countByStatus('OutForDelivery') }}</div><div class="stat-lbl">In Transit</div></div></div>
    <div class="stat-card c4"><div class="stat-icon">✅</div><div><div class="stat-val">{{ countByStatus('Delivered') }}</div><div class="stat-lbl">Delivered</div></div></div>
  </div>

  <!-- Filters -->
  <div class="filters">
    <div class="search-wrap">
      <span class="search-icon">🔍</span>
      <input class="search" [(ngModel)]="search" (ngModelChange)="filter()" placeholder="Search by order ID..." />
    </div>
    <select class="fsel" [(ngModel)]="statusFilter" (ngModelChange)="filter()">
      <option value="">All Statuses</option>
      @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
    </select>
  </div>

  <!-- Table -->
  <div class="table-card">
    @if (loading()) {
      <div class="loading-rows">@for (i of [1,2,3,4,5]; track i) { <div class="skeleton"></div> }</div>
    } @else {
      <table>
        <thead><tr>
          <th>ORDER</th><th>DATE</th><th>ITEMS</th><th>TOTAL</th><th>ADDRESS</th><th>STATUS</th>
        </tr></thead>
        <tbody>
          @for (o of filtered(); track o.id; let i = $index) {
            <tr [class.row-alt]="i % 2 === 1">
              <td class="mono">{{ o.id.slice(0,8).toUpperCase() }}</td>
              <td class="muted">{{ o.createdAt | date:'dd MMM, HH:mm' }}</td>
              <td>
                @for (item of o.items.slice(0,2); track item.productId) {
                  <p class="item-line">{{ item.productName }} ×{{ item.quantity }}</p>
                }
                @if (o.items.length > 2) { <p class="more">+{{ o.items.length - 2 }} more</p> }
              </td>
              <td class="price">₹{{ o.totalAmount.toFixed(2) }}</td>
              <td class="addr">{{ o.deliveryAddress }}</td>
              <td>
                <select [ngModel]="o.status" (ngModelChange)="updateStatus(o.id, $event)"
                  class="status-select" [class]="'ss-' + o.status.toLowerCase()">
                  @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
                </select>
              </td>
            </tr>
          }
        </tbody>
      </table>
      @if (filtered().length === 0) {
        <div class="empty"><span>📭</span><p>No orders found</p></div>
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
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text2); }
    .result-count { font-size:13px; color:var(--adm-text2); background:var(--adm-card); border:1px solid var(--adm-border); padding:6px 14px; border-radius:20px; }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid rgba(255,255,255,.06); }
    .c1 { background:var(--adm-s1); }
    .c2 { background:var(--adm-s3); }
    .c3 { background:var(--adm-s4); }
    .c4 { background:var(--adm-s2); }
    .stat-icon { font-size:26px; }
    .stat-val { font-size:28px; font-weight:800; color:var(--adm-text); line-height:1; }
    .stat-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; }

    .filters { display:flex; gap:12px; margin-bottom:20px; }
    .search-wrap { flex:1; position:relative; }
    .search-icon { position:absolute; left:12px; top:50%; transform:translateY(-50%); font-size:14px; }
    .search { width:100%; background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px 10px 36px; border-radius:8px; font-size:14px; }
    .search:focus { outline:none; border-color:#38bdf8; }
    .fsel { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; }
    .fsel:focus { outline:none; border-color:#38bdf8; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }
    table { width:100%; border-collapse:collapse; }
    thead tr { background:var(--adm-card2); }
    th { padding:12px 16px; text-align:left; font-size:11px; color:var(--adm-text2); font-weight:700; letter-spacing:.06em; border-bottom:1px solid var(--adm-border); }
    td { padding:11px 16px; font-size:13px; border-bottom:1px solid var(--adm-border); vertical-align:middle; color:var(--adm-text); }
    tr.row-alt td { background:var(--adm-row-alt); }
    tbody tr:hover td { background:var(--adm-row-hover); }
    .mono { font-family:monospace; color:var(--adm-text2); font-size:12px; }
    .muted { color:var(--adm-text2); white-space:nowrap; }
    .price { font-weight:700; color:var(--adm-text); }
    .addr { color:var(--adm-text2); font-size:12px; max-width:140px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
    .item-line { margin:0; color:var(--adm-text2); font-size:12px; max-width:160px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
    .more { margin:0; color:var(--adm-text3); font-size:11px; }

    .status-select { border:1px solid var(--adm-border2); border-radius:8px; padding:6px 10px; font-size:12px; font-weight:600; cursor:pointer; outline:none; background:var(--adm-card); color:var(--adm-text); width:100%; }
    .status-select:focus { border-color:#38bdf8; }
    .status-select option { background:var(--adm-card); color:var(--adm-text); }
    .ss-delivered { background:rgba(34,197,94,.2) !important; color:#15803d !important; border-color:rgba(34,197,94,.4) !important; }
    .ss-pending { background:rgba(251,191,36,.2) !important; color:#b45309 !important; border-color:rgba(251,191,36,.4) !important; }
    .ss-processing { background:rgba(59,130,246,.2) !important; color:#1d4ed8 !important; border-color:rgba(59,130,246,.4) !important; }
    .ss-shipped { background:rgba(139,92,246,.2) !important; color:#6d28d9 !important; border-color:rgba(139,92,246,.4) !important; }
    .ss-outfordelivery { background:rgba(249,115,22,.2) !important; color:#c2410c !important; border-color:rgba(249,115,22,.4) !important; }
    .ss-cancelled { background:rgba(239,68,68,.2) !important; color:#dc2626 !important; border-color:rgba(239,68,68,.4) !important; }
    .ss-paymentpending,.ss-paymentconfirmed { background:rgba(251,191,36,.2) !important; color:#b45309 !important; border-color:rgba(251,191,36,.4) !important; }

    .empty { text-align:center; padding:60px; display:flex; flex-direction:column; align-items:center; gap:8px; color:var(--adm-text3); }
    .empty span { font-size:40px; }
    .empty p { margin:0; font-size:15px; }
    .loading-rows { padding:16px; display:flex; flex-direction:column; gap:10px; }
    .skeleton { height:44px; background:var(--adm-card); border-radius:8px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.5} }
  `]
})
export class AdminOrders implements OnInit {
  private orderService = inject(OrderService);
  private http = inject(HttpClient);

  allOrders = signal<Order[]>([]);
  filtered = signal<Order[]>([]);
  loading = signal(true);
  search = '';
  statusFilter = '';
  statuses = STATUSES;

  countByStatus = (s: string) => this.allOrders().filter(o => o.status === s).length;

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: o => { this.allOrders.set(o); this.filtered.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  filter() {
    const s = this.search.toLowerCase();
    this.filtered.set(this.allOrders().filter(o =>
      (!s || o.id.toLowerCase().includes(s)) &&
      (!this.statusFilter || o.status === this.statusFilter)
    ));
  }

  updateStatus(id: string, status: string) {
    this.http.patch(`${environment.apiUrl}/api/v1/orders/${id}/status`, { status }).subscribe(() => {
      this.allOrders.update(orders => orders.map(o => o.id === id ? { ...o, status: status as any } : o));
      this.filter();
    });
  }
}
