import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../../core/services/order.service';
import { ProductService } from '../../../core/services/product.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink],
  template: `
<div class="page">
  <div class="page-header">
    <div class="title-row">
      <span class="crown">👑</span>
      <div>
        <h1 class="page-title">Admin Dashboard</h1>
        <p class="page-sub">Full system overview</p>
      </div>
    </div>
  </div>

  <!-- Stats -->
  <div class="stats">
    <div class="stat-card c1"><div class="stat-icon">🛒</div><div><div class="stat-val">{{ orders().length }}</div><div class="stat-lbl">Total Orders</div></div></div>
    <div class="stat-card c2"><div class="stat-icon">📦</div><div><div class="stat-val">{{ totalProducts() }}</div><div class="stat-lbl">Total Products</div></div></div>
    <div class="stat-card c3"><div class="stat-icon">⏳</div><div><div class="stat-val">{{ pendingCount() }}</div><div class="stat-lbl">Pending Orders</div></div></div>
    <div class="stat-card c4"><div class="stat-icon">💰</div><div><div class="stat-val">₹{{ revenue() }}</div><div class="stat-lbl">Revenue</div></div></div>
    <div class="stat-card c5"><div class="stat-icon">👥</div><div><div class="stat-val">{{ totalUsers() }}</div><div class="stat-lbl">Total Users</div></div></div>
  </div>

  <!-- Quick Actions -->
  <div class="actions-grid">
    <a routerLink="/admin/products" class="action-card green">
      <span class="action-icon">📦</span>
      <div><p class="action-title">Manage Products</p><p class="action-sub">Add, edit, update stock</p></div>
    </a>
    <a routerLink="/admin/orders" class="action-card blue">
      <span class="action-icon">📋</span>
      <div><p class="action-title">Manage Orders</p><p class="action-sub">Update order statuses</p></div>
    </a>
    <a routerLink="/admin/users" class="action-card purple">
      <span class="action-icon">👥</span>
      <div><p class="action-title">Manage Users</p><p class="action-sub">Roles, status, accounts</p></div>
    </a>
    <a routerLink="/products" class="action-card gray">
      <span class="action-icon">🛍️</span>
      <div><p class="action-title">View Store</p><p class="action-sub">See customer view</p></div>
    </a>
  </div>

  <!-- Recent Orders -->
  <div class="table-card">
    <div class="table-card-header">
      <h2>Recent Orders</h2>
      <a routerLink="/admin/orders" class="view-all">View all →</a>
    </div>
    @if (loading()) {
      <div class="loading-rows">
        @for (i of [1,2,3]; track i) { <div class="skeleton"></div> }
      </div>
    } @else {
      <table>
        <thead><tr>
          <th>ORDER ID</th><th>ITEMS</th><th>TOTAL</th><th>STATUS</th>
        </tr></thead>
        <tbody>
          @for (o of orders().slice(0,8); track o.id) {
            <tr>
              <td class="mono">{{ o.id.slice(0,8).toUpperCase() }}</td>
              <td>{{ o.items.length }} item(s)</td>
              <td class="price">₹{{ o.totalAmount.toFixed(2) }}</td>
              <td><span class="status-pill" [class]="'s-' + o.status.toLowerCase()">{{ o.status }}</span></td>
            </tr>
          }
        </tbody>
      </table>
      @if (orders().length === 0) {
        <div class="empty">No orders yet</div>
      }
    }
  </div>
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }
    .page-header { margin-bottom:28px; }
    .title-row { display:flex; align-items:center; gap:14px; }
    .crown { font-size:32px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#fbbf24,#f59e0b); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text2); }

    .stats { display:grid; grid-template-columns:repeat(5,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid rgba(255,255,255,.06); }
    .c1 { background:var(--adm-s2); }
    .c2 { background:var(--adm-s1); }
    .c3 { background:var(--adm-s3); }
    .c4 { background:var(--adm-s4); }
    .c5 { background:var(--adm-s1); }
    .stat-icon { font-size:26px; }
    .stat-val { font-size:28px; font-weight:800; color:var(--adm-text); line-height:1; }
    .stat-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; }

    .actions-grid { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .action-card { display:flex; align-items:center; gap:14px; padding:18px 20px; border-radius:14px; text-decoration:none; transition:all .2s; border:1px solid rgba(255,255,255,.06); }
    .action-card:hover { transform:translateY(-2px); box-shadow:0 8px 24px rgba(0,0,0,.3); }
    .action-card.green { background:linear-gradient(135deg,#166534,#14532d); }
    .action-card.blue  { background:linear-gradient(135deg,#1e40af,#1e3a8a); }
    .action-card.purple{ background:linear-gradient(135deg,#5b21b6,#4c1d95); }
    .action-card.gray  { background:linear-gradient(135deg,#1e293b,#0f172a); }
    .action-icon { font-size:28px; }
    .action-title { font-size:14px; font-weight:700; color:#f1f5f9; margin:0; }
    .action-sub { font-size:12px; color:rgba(255,255,255,.5); margin:3px 0 0; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }
    .table-card-header { display:flex; justify-content:space-between; align-items:center; padding:18px 20px; border-bottom:1px solid var(--adm-border); }
    .table-card-header h2 { margin:0; font-size:15px; font-weight:700; color:var(--adm-text); }
    .view-all { font-size:13px; color:#2563eb; text-decoration:none; }
    .view-all:hover { color:#7dd3fc; }
    table { width:100%; border-collapse:collapse; }
    thead tr { background:var(--adm-card2); }
    th { padding:11px 18px; text-align:left; font-size:11px; color:var(--adm-text2); font-weight:700; letter-spacing:.06em; border-bottom:1px solid var(--adm-border); }
    td { padding:12px 18px; font-size:13.5px; border-bottom:1px solid var(--adm-border); }
    tbody tr:hover td { background:var(--adm-row-hover); }
    .mono { font-family:monospace; color:var(--adm-text2); font-size:12px; }
    .price { font-weight:700; color:var(--adm-text); }
    .status-pill { padding:3px 10px; border-radius:20px; font-size:11px; font-weight:600; }
    .s-delivered { background:rgba(34,197,94,.15); color:#4ade80; }
    .s-pending { background:rgba(251,191,36,.15); color:#fbbf24; }
    .s-processing { background:rgba(59,130,246,.15); color:#60a5fa; }
    .s-shipped { background:rgba(139,92,246,.15); color:#a78bfa; }
    .s-outfordelivery { background:rgba(249,115,22,.15); color:#fb923c; }
    .s-cancelled { background:rgba(239,68,68,.15); color:#f87171; }
    .empty { text-align:center; padding:40px; color:var(--adm-text3); }
    .loading-rows { padding:16px 20px; display:flex; flex-direction:column; gap:10px; }
    .skeleton { height:40px; background:var(--adm-card); border-radius:8px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.5} }
  `]
})
export class AdminDashboard implements OnInit {
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private http = inject(HttpClient);

  orders = signal<any[]>([]);
  totalProducts = signal(0);
  totalUsers = signal(0);
  loading = signal(true);

  pendingCount = () => this.orders().filter(o => o.status === 'Pending').length;
  revenue = () => this.orders().filter(o => o.status === 'Delivered').reduce((s: number, o: any) => s + o.totalAmount, 0).toFixed(2);

  ngOnInit() {
    this.orderService.getOrders().subscribe({ next: o => { this.orders.set(o); this.loading.set(false); }, error: () => this.loading.set(false) });
    this.productService.getProducts({ pageSize: 1 }).subscribe(r => this.totalProducts.set(r.total));
    this.http.get<any>(`${environment.apiUrl}/api/v1/users/stats`).subscribe(s => this.totalUsers.set(s.total));
  }
}
