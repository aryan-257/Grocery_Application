import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [RouterLink],
  template: `
<div class="page">

  <!-- Header -->
  <div class="page-header">
    <div class="title-row">
      <span class="store-icon">&#x1F3EA;</span>
      <div>
        <h1 class="page-title">Store Manager Dashboard</h1>
        <p class="page-sub">Inventory and order operations</p>
      </div>
    </div>
  </div>

  <!-- Stats -->
  <div class="stats">
    <div class="stat-card c1">
      <div class="stat-icon">&#x1F4E6;</div>
      <div><div class="stat-val">{{ totalProducts() }}</div><div class="stat-lbl">Total Products</div></div>
    </div>
    <div class="stat-card c2">
      <div class="stat-icon">&#x26A0;</div>
      <div><div class="stat-val warn">{{ lowStock().length }}</div><div class="stat-lbl">Low Stock</div></div>
    </div>
    <div class="stat-card c3">
      <div class="stat-icon">&#x23F3;</div>
      <div><div class="stat-val">{{ pendingOrders() }}</div><div class="stat-lbl">Pending Orders</div></div>
    </div>
    <div class="stat-card c4">
      <div class="stat-icon">&#x2699;</div>
      <div><div class="stat-val">{{ processingOrders() }}</div><div class="stat-lbl">Processing</div></div>
    </div>
  </div>

  <!-- Quick Actions -->
  <div class="actions-grid">
    <a routerLink="/admin/products" class="action-card green">
      <span class="action-icon">&#x1F4E6;</span>
      <div>
        <p class="action-title">Manage Inventory</p>
        <p class="action-sub">Update stock levels and add products</p>
      </div>
    </a>
    <a routerLink="/admin/orders" class="action-card blue">
      <span class="action-icon">&#x1F4CB;</span>
      <div>
        <p class="action-title">Process Orders</p>
        <p class="action-sub">Update order statuses</p>
      </div>
    </a>
    <a routerLink="/admin/support" class="action-card purple">
      <span class="action-icon">&#x1F4AC;</span>
      <div>
        <p class="action-title">Support Tickets</p>
        <p class="action-sub">Manage customer requests</p>
      </div>
    </a>
    <a routerLink="/products" class="action-card gray">
      <span class="action-icon">&#x1F6D2;</span>
      <div>
        <p class="action-title">View Store</p>
        <p class="action-sub">See customer view</p>
      </div>
    </a>
  </div>

  <!-- Low Stock Alert -->
  @if (lowStock().length > 0) {
    <div class="table-card">
      <div class="table-card-header">
        <div class="alert-title">
          <span class="alert-dot"></span>
          Low Stock Alert
        </div>
        <span class="alert-count">{{ lowStock().length }} products need restocking</span>
      </div>
      <div class="stock-grid">
        @for (p of lowStock(); track p.id) {
          <div class="stock-item">
            <img [src]="p.imageUrl" [alt]="p.name" class="stock-img"
              onerror="this.src='https://placehold.co/40x40/1e293b/64748b?text=?'" />
            <div class="stock-info">
              <p class="stock-name">{{ p.name }}</p>
              <p class="stock-cat">{{ p.categoryName }}</p>
            </div>
            <span class="stock-badge" [class.stock-critical]="p.stockQuantity <= 5">
              {{ p.stockQuantity }} left
            </span>
          </div>
        }
      </div>
    </div>
  }

</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }

    .page-header { margin-bottom:28px; }
    .title-row { display:flex; align-items:center; gap:16px; }
    .store-icon { font-size:36px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#22c55e,#38bdf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text3); }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid var(--adm-border); }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s5);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s1);}
    .stat-icon { font-size:26px; }
    .stat-val { font-size:28px; font-weight:800; color:var(--adm-stat-val); line-height:1; }
    .stat-val.warn { color:#dc2626; }
    .stat-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    .actions-grid { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .action-card { display:flex; align-items:center; gap:14px; padding:18px 20px; border-radius:14px; text-decoration:none; transition:all .2s; border:1px solid rgba(255,255,255,.06); }
    .action-card:hover { transform:translateY(-2px); box-shadow:0 8px 24px rgba(0,0,0,.2); }
    .action-card.green  { background:linear-gradient(135deg,#166534,#14532d); }
    .action-card.blue   { background:linear-gradient(135deg,#1e40af,#1e3a8a); }
    .action-card.purple { background:linear-gradient(135deg,#5b21b6,#4c1d95); }
    .action-card.gray   { background:linear-gradient(135deg,#1e293b,#0f172a); }
    .action-icon { font-size:28px; }
    .action-title { font-size:14px; font-weight:700; color:#f1f5f9; margin:0; }
    .action-sub { font-size:12px; color:rgba(255,255,255,.5); margin:3px 0 0; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }
    .table-card-header { display:flex; justify-content:space-between; align-items:center; padding:18px 20px; border-bottom:1px solid var(--adm-border); }
    .alert-title { display:flex; align-items:center; gap:10px; font-size:15px; font-weight:700; color:var(--adm-text); }
    .alert-dot { width:10px; height:10px; border-radius:50%; background:#ef4444; box-shadow:0 0 8px rgba(239,68,68,.6); animation:blink 1.5s infinite; }
    @keyframes blink { 0%,100%{opacity:1} 50%{opacity:.3} }
    .alert-count { font-size:12px; color:var(--adm-text2); background:rgba(239,68,68,.1); border:1px solid rgba(239,68,68,.2); color:#dc2626; padding:4px 12px; border-radius:20px; font-weight:600; }

    .stock-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; padding:16px 20px; }
    .stock-item { display:flex; align-items:center; gap:12px; background:var(--adm-card2); border:1px solid var(--adm-border); border-radius:10px; padding:12px; }
    .stock-img { width:42px; height:42px; border-radius:8px; object-fit:cover; flex-shrink:0; background:var(--adm-border); }
    .stock-info { flex:1; min-width:0; }
    .stock-name { font-size:13px; font-weight:600; color:var(--adm-text); margin:0; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .stock-cat { font-size:11px; color:var(--adm-text2); margin:2px 0 0; }
    .stock-badge { background:rgba(251,191,36,.15); color:#b45309; border:1px solid rgba(251,191,36,.3); padding:3px 10px; border-radius:20px; font-size:12px; font-weight:700; white-space:nowrap; flex-shrink:0; }
    .stock-critical { background:rgba(239,68,68,.15) !important; color:#dc2626 !important; border-color:rgba(239,68,68,.3) !important; }
  `]
})
export class ManagerDashboard implements OnInit {
  private productService = inject(ProductService);
  private orderService = inject(OrderService);

  totalProducts = signal(0);
  lowStock = signal<any[]>([]);
  orders = signal<any[]>([]);

  pendingOrders = () => this.orders().filter(o => o.status === 'Pending').length;
  processingOrders = () => this.orders().filter(o => o.status === 'Processing').length;

  ngOnInit() {
    this.productService.getProducts({ pageSize: 1 }).subscribe(r => this.totalProducts.set(r.total));
    this.productService.getLowStockProducts().subscribe(p => this.lowStock.set(p));
    this.orderService.getOrders().subscribe(o => this.orders.set(o));
  }
}
