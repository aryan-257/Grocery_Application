import { Component, OnInit, inject, NgZone, PLATFORM_ID, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { Product, Category } from '../../../core/models';

interface ProductForm {
  name: string; sku: string; price: number; stockQuantity: number;
  categoryId: string; brand: string; unit: string; imageUrl: string;
  description: string; discountPercent: number; isActive: boolean;
}

@Component({
  selector: 'app-admin-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [CommonModule, FormsModule],
  template: `
<div class="page">
  <div class="page-header">
    <div>
      <h1 class="page-title">Product Management</h1>
      <p class="page-sub">Manage your store inventory</p>
    </div>
    <button class="btn-add" (click)="openAdd()">+ Add Product</button>
  </div>

  <div class="stats">
    <div class="stat-card total">
      <div class="stat-icon-box blue">&#x1F4E6;</div>
      <div><div class="stat-val">{{ products.length }}</div><div class="stat-lbl">Total Products</div></div>
    </div>
    <div class="stat-card sale">
      <div class="stat-icon-box purple">&#x1F3F7;</div>
      <div><div class="stat-val">{{ onSaleCount }}</div><div class="stat-lbl">On Sale</div></div>
    </div>
    <div class="stat-card low">
      <div class="stat-icon-box orange">&#x26A0;</div>
      <div><div class="stat-val">{{ lowStockCount }}</div><div class="stat-lbl">Low Stock</div></div>
    </div>
    <div class="stat-card out">
      <div class="stat-icon-box red">&#x1F6AB;</div>
      <div><div class="stat-val">{{ outOfStockCount }}</div><div class="stat-lbl">Out of Stock</div></div>
    </div>
  </div>

  <div class="filters">
    <div class="search-wrap">
      <span class="search-icon">&#x1F50D;</span>
      <input class="search" [(ngModel)]="searchQuery" (ngModelChange)="applyFilters()" placeholder="Search by name or SKU..." />
    </div>
    <select class="fsel" [(ngModel)]="selectedCategory" (ngModelChange)="applyFilters()">
      <option value="">All Categories</option>
      <option *ngFor="let c of categories" [value]="c.id">{{ c.name }}</option>
    </select>
    <select class="fsel" [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">
      <option value="">All Products</option>
      <option value="active">Active</option>
      <option value="inactive">Inactive</option>
      <option value="sale">On Sale</option>
      <option value="low">Low Stock</option>
    </select>
    <span class="result-count">{{ filtered.length }} results</span>
  </div>

  <div class="table-card">
    <table>
      <thead>
        <tr>
          <th>PRODUCT</th><th>CATEGORY</th><th>PRICE</th>
          <th>DISCOUNT</th><th>SALE PRICE</th><th>STOCK</th><th>STATUS</th><th>ACTIONS</th>
        </tr>
      </thead>
      <tbody>
        <ng-container *ngIf="!loading">
        <tr *ngFor="let p of filtered; let i = index" [class.row-alt]="i % 2 === 1">
          <td>
            <div class="prod-cell">
              <div class="img-wrap">
                <img [src]="p.imageUrl" [alt]="p.name" onerror="this.src='https://placehold.co/48x48/e2e8f0/64748b?text=IMG'" />
              </div>
              <div>
                <span class="prod-name">{{ p.name }}</span>
                <span class="prod-sku">{{ p.sku }}</span>
              </div>
            </div>
          </td>
          <td><span class="cat-chip">{{ p.categoryName }}</span></td>
          <td class="price">&#x20B9;{{ p.price.toFixed(2) }}</td>
          <td>
            <span class="disc-badge" *ngIf="p.discountPercent > 0">{{ p.discountPercent }}% OFF</span>
            <span class="muted" *ngIf="p.discountPercent === 0">&#x2014;</span>
          </td>
          <td class="sale-price" *ngIf="p.discountPercent > 0">&#x20B9;{{ p.discountedPrice.toFixed(2) }}</td>
          <td class="muted" *ngIf="p.discountPercent === 0">&#x2014;</td>
          <td>
            <span class="stock-val"
              [class.stock-low]="p.stockQuantity > 0 && p.stockQuantity < 10"
              [class.stock-out]="p.stockQuantity === 0">{{ p.stockQuantity }}</span>
          </td>
          <td>
            <span class="status-badge" [class.s-active]="p.isActive" [class.s-inactive]="!p.isActive">
              {{ p.isActive ? 'Active' : 'Inactive' }}
            </span>
          </td>
          <td>
            <div class="action-btns">
              <button class="btn-edit" (click)="openEdit(p)">Edit</button>
              <button class="btn-del" (click)="confirmDelete(p)">Del</button>
            </div>
          </td>
        </tr>
        </ng-container>
        <tr *ngIf="loading">
          <td colspan="8" class="empty-row">
            <div class="skeleton-rows">
              <div class="skel" *ngFor="let i of [1,2,3,4,5]"></div>
            </div>
          </td>
        </tr>
        <tr *ngIf="!loading && filtered.length === 0">
          <td colspan="8" class="empty-row">
            <p class="empty-icon">&#x1F4ED;</p>
            <p class="empty-text">No products found</p>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</div>

<!-- Modal -->
<div class="overlay" *ngIf="showModal" (click)="closeModal()">
  <div class="modal" (click)="$event.stopPropagation()">
    <div class="modal-head">
      <div>
        <h2 class="modal-title">{{ editingProduct ? 'Edit Product' : 'Add New Product' }}</h2>
        <p class="modal-sub">{{ editingProduct ? 'Update product details below' : 'Fill in the details to add a new product' }}</p>
      </div>
      <button class="close-btn" (click)="closeModal()">&#x2715;</button>
    </div>
    <div class="modal-body">
      <div class="img-preview-row">
        <div class="img-preview">
          <img [src]="form.imageUrl || 'https://placehold.co/80x80/e2e8f0/64748b?text=IMG'"
               onerror="this.src='https://placehold.co/80x80/e2e8f0/64748b?text=IMG'" alt="preview" />
        </div>
        <div class="img-url-wrap">
          <label class="field-label">Image URL</label>
          <input class="field" [(ngModel)]="form.imageUrl" placeholder="https://example.com/image.jpg" />
        </div>
      </div>

      <div class="section-divider">Basic Info</div>
      <div class="form-grid">
        <div class="field-group">
          <label class="field-label">Product Name <span class="req">*</span></label>
          <input class="field" [(ngModel)]="form.name" placeholder="e.g. Aashirvaad Atta" />
        </div>
        <div class="field-group">
          <label class="field-label">SKU <span class="req">*</span></label>
          <input class="field" [(ngModel)]="form.sku" placeholder="e.g. PA004" />
        </div>
        <div class="field-group">
          <label class="field-label">Category <span class="req">*</span></label>
          <select class="field" [(ngModel)]="form.categoryId">
            <option value="">Select category</option>
            <option *ngFor="let c of categories" [value]="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="field-group">
          <label class="field-label">Brand</label>
          <input class="field" [(ngModel)]="form.brand" placeholder="e.g. Amul" />
        </div>
      </div>

      <div class="section-divider">Pricing & Stock</div>
      <div class="form-grid">
        <div class="field-group">
          <label class="field-label">Price (&#x20B9;) <span class="req">*</span></label>
          <input class="field" [(ngModel)]="form.price" type="number" min="0" placeholder="0.00" />
        </div>
        <div class="field-group">
          <label class="field-label">Stock Quantity</label>
          <input class="field" [(ngModel)]="form.stockQuantity" type="number" min="0" placeholder="0" />
        </div>
        <div class="field-group">
          <label class="field-label">Unit</label>
          <input class="field" [(ngModel)]="form.unit" placeholder="e.g. 1kg, 500ml" />
        </div>
        <div class="field-group">
          <label class="field-label">Discount %</label>
          <div class="input-with-suffix">
            <input class="field" [(ngModel)]="form.discountPercent" type="number" min="0" max="100" placeholder="0" />
            <span class="suffix">%</span>
          </div>
        </div>
      </div>

      <div class="section-divider">Description</div>
      <div class="field-group">
        <textarea class="field" [(ngModel)]="form.description" rows="3" placeholder="Short product description..."></textarea>
      </div>

      <div class="section-divider">Visibility</div>
      <label class="toggle-label">
        <div class="toggle-track" [class.on]="form.isActive" (click)="form.isActive = !form.isActive">
          <div class="toggle-thumb"></div>
        </div>
        <div class="toggle-text">
          <span class="toggle-main">{{ form.isActive ? 'Active' : 'Inactive' }}</span>
          <span class="toggle-sub">{{ form.isActive ? 'Visible to customers in store' : 'Hidden from store' }}</span>
        </div>
      </label>

      <div class="form-error" *ngIf="formError">{{ formError }}</div>
    </div>
    <div class="modal-foot">
      <button class="btn-cancel" (click)="closeModal()">Cancel</button>
      <button class="btn-save" (click)="save()" [disabled]="saving">
        {{ saving ? 'Saving...' : (editingProduct ? 'Save Changes' : 'Add Product') }}
      </button>
    </div>
  </div>
</div>

<!-- Delete Confirm -->
<div class="overlay" *ngIf="deleteTarget" (click)="deleteTarget = null">
  <div class="modal confirm-modal" (click)="$event.stopPropagation()">
    <p class="confirm-icon">&#x1F5D1;</p>
    <h2>Delete Product?</h2>
    <p>This will hide <strong>{{ deleteTarget?.name }}</strong> from the store.</p>
    <div class="confirm-btns">
      <button class="btn-cancel" (click)="deleteTarget = null">Cancel</button>
      <button class="btn-danger" (click)="doDelete()" [disabled]="saving">
        {{ saving ? 'Deleting...' : 'Yes, Delete' }}
      </button>
    </div>
  </div>
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:28px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#0ea5e9,#6366f1); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text2); }
    .btn-add { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:11px 22px; border-radius:10px; font-weight:700; font-size:14px; cursor:pointer; box-shadow:0 4px 15px rgba(34,197,94,.3); transition:all .2s; }
    .btn-add:hover { transform:translateY(-1px); }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:16px; margin-bottom:28px; }
    .stat-card { display:flex; align-items:center; gap:16px; border-radius:14px; padding:20px; border:1px solid var(--adm-border); }
    .stat-card.total { background:var(--adm-s1); }
    .stat-card.sale  { background:var(--adm-s4); }
    .stat-card.low   { background:var(--adm-s3); }
    .stat-card.out   { background:var(--adm-s5); }
    .stat-icon-box { width:44px; height:44px; border-radius:10px; display:flex; align-items:center; justify-content:center; font-size:20px; flex-shrink:0; }
    .stat-icon-box.blue   { background:rgba(37,99,235,.15); }
    .stat-icon-box.purple { background:rgba(124,58,237,.15); }
    .stat-icon-box.orange { background:rgba(234,88,12,.15); }
    .stat-icon-box.red    { background:rgba(220,38,38,.15); }
    .stat-val { font-size:30px; font-weight:800; color:var(--adm-stat-val); line-height:1; }
    .stat-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    .filters { display:flex; align-items:center; gap:12px; margin-bottom:20px; flex-wrap:wrap; }
    .search-wrap { flex:1; min-width:200px; position:relative; }
    .search-icon { position:absolute; left:12px; top:50%; transform:translateY(-50%); font-size:14px; }
    .search { width:100%; background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px 10px 36px; border-radius:8px; font-size:14px; }
    .search:focus { outline:none; border-color:#0ea5e9; }
    .fsel { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; cursor:pointer; }
    .fsel:focus { outline:none; border-color:#0ea5e9; }
    .result-count { font-size:13px; color:var(--adm-text2); font-weight:600; white-space:nowrap; }

    .table-card { background:var(--adm-card); border-radius:14px; overflow:hidden; border:1px solid var(--adm-border); }
    table { width:100%; border-collapse:collapse; }
    thead tr { background:var(--adm-thead); }
    th { padding:13px 16px; text-align:left; font-size:11px; color:var(--adm-text2); font-weight:700; letter-spacing:.06em; border-bottom:1px solid var(--adm-border); }
    td { padding:11px 16px; font-size:13.5px; border-bottom:1px solid var(--adm-border); vertical-align:middle; color:var(--adm-text); }
    tr.row-alt td { background:var(--adm-row-alt); }
    tbody tr:hover td { background:var(--adm-row-hover); }

    .prod-cell { display:flex; align-items:center; gap:12px; }
    .img-wrap { width:46px; height:46px; border-radius:10px; overflow:hidden; background:var(--adm-card2); border:1px solid var(--adm-border); flex-shrink:0; }
    .img-wrap img { width:100%; height:100%; object-fit:cover; }
    .prod-name { display:block; font-weight:600; color:var(--adm-text); font-size:13.5px; }
    .prod-sku { display:block; font-size:11px; color:var(--adm-text2); margin-top:2px; }

    .cat-chip { background:rgba(37,99,235,.12); color:#1d4ed8; padding:3px 10px; border-radius:20px; font-size:12px; font-weight:600; border:1px solid rgba(37,99,235,.25); }
    .price { font-weight:700; color:var(--adm-text); }
    .sale-price { color:#15803d; font-weight:700; }
    .muted { color:var(--adm-text2); }
    .disc-badge { background:linear-gradient(135deg,#7c3aed,#a855f7); color:#fff; padding:3px 9px; border-radius:20px; font-size:11px; font-weight:700; }
    .stock-val { font-weight:700; color:var(--adm-text); }
    .stock-low { color:#c2410c !important; }
    .stock-out { color:#dc2626 !important; }

    .status-badge { padding:4px 12px; border-radius:20px; font-size:12px; font-weight:700; }
    .s-active   { background:rgba(22,163,74,.15); color:#15803d; border:1px solid rgba(22,163,74,.35); }
    .s-inactive { background:rgba(220,38,38,.12); color:#dc2626; border:1px solid rgba(220,38,38,.3); }

    .action-btns { display:flex; gap:8px; }
    .btn-edit { background:linear-gradient(135deg,#2563eb,#3b82f6); color:#fff; border:none; padding:7px 16px; border-radius:7px; cursor:pointer; font-size:12px; font-weight:700; transition:all .2s; }
    .btn-edit:hover { transform:translateY(-1px); box-shadow:0 4px 12px rgba(59,130,246,.4); }
    .btn-del { background:rgba(220,38,38,.12); color:#dc2626; border:1px solid rgba(220,38,38,.3); padding:7px 12px; border-radius:7px; cursor:pointer; font-size:12px; font-weight:700; transition:all .2s; }
    .btn-del:hover { background:rgba(220,38,38,.25); }

    .empty-row { text-align:center; padding:60px; }
    .empty-icon { font-size:40px; margin:0 0 8px; }
    .empty-text { font-size:15px; color:var(--adm-text2); margin:0; }
    .skeleton-rows { display:flex; flex-direction:column; gap:10px; padding:16px 0; }
    .skel { height:48px; background:var(--adm-border); border-radius:8px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }

    .overlay { position:fixed; inset:0; background:rgba(0,0,0,.6); backdrop-filter:blur(4px); display:flex; align-items:center; justify-content:center; z-index:1000; padding:16px; }
    .modal { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; width:680px; max-width:100%; max-height:92vh; overflow-y:auto; box-shadow:0 25px 80px rgba(0,0,0,.4); }
    .modal-head { display:flex; justify-content:space-between; align-items:flex-start; padding:22px 26px; border-bottom:1px solid var(--adm-border); background:var(--adm-card2); border-radius:16px 16px 0 0; }
    .modal-title { margin:0; font-size:18px; font-weight:700; color:var(--adm-text); }
    .modal-sub { margin:4px 0 0; font-size:12px; color:var(--adm-text2); }
    .close-btn { background:var(--adm-border); border:none; color:var(--adm-text); width:32px; height:32px; border-radius:8px; cursor:pointer; font-size:14px; font-weight:700; display:flex; align-items:center; justify-content:center; transition:all .2s; flex-shrink:0; }
    .close-btn:hover { background:#ef4444; color:#fff; }
    .modal-body { padding:24px 26px; display:flex; flex-direction:column; gap:0; }
    .img-preview-row { display:flex; align-items:center; gap:16px; background:var(--adm-card2); border:2px solid var(--adm-border2); border-radius:12px; padding:14px; margin-bottom:20px; }
    .img-preview { width:80px; height:80px; border-radius:10px; overflow:hidden; background:var(--adm-border); border:2px solid var(--adm-border2); flex-shrink:0; }
    .img-preview img { width:100%; height:100%; object-fit:cover; }
    .img-url-wrap { flex:1; }
    .section-divider { font-size:11px; font-weight:700; color:var(--adm-text3); text-transform:uppercase; letter-spacing:.08em; padding:16px 0 12px; border-top:1px solid var(--adm-border); margin-top:4px; }
    .form-grid { display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-bottom:4px; }
    .field-group { display:flex; flex-direction:column; gap:6px; margin-bottom:16px; }
    .field-label { font-size:12px; font-weight:700; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.05em; }
    .req { color:#ef4444; }
    .field { background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:11px 14px; border-radius:8px; font-size:14px; width:100%; transition:border-color .2s, box-shadow .2s; font-weight:500; }
    .field:focus { outline:none; border-color:#0ea5e9; box-shadow:0 0 0 3px rgba(14,165,233,.12); }
    .field::placeholder { color:var(--adm-text3); font-weight:400; }
    select.field option { background:var(--adm-card); color:var(--adm-text); }
    textarea.field { resize:vertical; min-height:80px; }
    .input-with-suffix { position:relative; }
    .input-with-suffix .field { padding-right:36px; }
    .suffix { position:absolute; right:12px; top:50%; transform:translateY(-50%); font-size:13px; font-weight:700; color:var(--adm-text2); pointer-events:none; }
    .toggle-label { display:flex; align-items:center; gap:14px; cursor:pointer; user-select:none; background:var(--adm-card2); border:2px solid var(--adm-border2); border-radius:10px; padding:14px 16px; margin-top:4px; transition:border-color .2s; }
    .toggle-label:hover { border-color:#22c55e; }
    .toggle-track { width:48px; height:26px; border-radius:13px; background:var(--adm-border2); position:relative; transition:all .25s; flex-shrink:0; }
    .toggle-track.on { background:linear-gradient(135deg,#22c55e,#16a34a); }
    .toggle-thumb { position:absolute; top:3px; left:3px; width:20px; height:20px; border-radius:50%; background:#fff; transition:transform .25s; box-shadow:0 1px 4px rgba(0,0,0,.25); }
    .toggle-track.on .toggle-thumb { transform:translateX(22px); }
    .toggle-text { display:flex; flex-direction:column; gap:2px; }
    .toggle-main { font-size:14px; font-weight:700; color:var(--adm-text); }
    .toggle-sub { font-size:12px; color:var(--adm-text2); }
    .form-error { background:rgba(239,68,68,.1); border:1px solid rgba(239,68,68,.3); color:#dc2626; padding:10px 14px; border-radius:8px; font-size:13px; font-weight:600; margin-top:12px; }
    .modal-foot { display:flex; justify-content:flex-end; gap:12px; padding:18px 26px; border-top:1px solid var(--adm-border); background:var(--adm-card2); border-radius:0 0 16px 16px; }
    .btn-cancel { background:var(--adm-card); color:var(--adm-text); border:1px solid var(--adm-border2); padding:10px 22px; border-radius:8px; cursor:pointer; font-size:14px; font-weight:600; transition:all .2s; }
    .btn-cancel:hover { background:var(--adm-border); }
    .btn-save { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 24px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; box-shadow:0 4px 15px rgba(34,197,94,.3); transition:all .2s; }
    .btn-save:hover:not(:disabled) { transform:translateY(-1px); }
    .btn-save:disabled { opacity:.5; cursor:not-allowed; }
    .confirm-modal { width:420px; padding:36px; text-align:center; }
    .confirm-icon { font-size:48px; margin:0 0 16px; }
    .confirm-modal h2 { margin:0 0 10px; font-size:20px; font-weight:700; color:var(--adm-text); }
    .confirm-modal p { color:var(--adm-text2); font-size:14px; margin:0 0 28px; line-height:1.6; }
    .confirm-btns { display:flex; justify-content:center; gap:12px; }
    .btn-danger { background:linear-gradient(135deg,#ef4444,#dc2626); color:#fff; border:none; padding:10px 24px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; transition:all .2s; }
    .btn-danger:hover:not(:disabled) { transform:translateY(-1px); }
    .btn-danger:disabled { opacity:.5; cursor:not-allowed; }
  `]
})
export class AdminProductsComponent implements OnInit {
  private productService = inject(ProductService);
  private zone = inject(NgZone);
  private platformId = inject(PLATFORM_ID);
  private cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  filtered: Product[] = [];
  categories: Category[] = [];
  searchQuery = ''; selectedCategory = ''; statusFilter = '';
  showModal = false; editingProduct: Product | null = null;
  deleteTarget: Product | null = null; saving = false; formError = '';
  form: ProductForm = this.emptyForm();
  loading = true;

  get onSaleCount() { return this.products.filter(p => p.discountPercent > 0).length; }
  get lowStockCount() { return this.products.filter(p => p.stockQuantity > 0 && p.stockQuantity < 10).length; }
  get outOfStockCount() { return this.products.filter(p => p.stockQuantity === 0).length; }

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.loadCategories();
    this.loadProducts();
  }

  loadProducts() {
    this.loading = true;
    this.productService.getProducts({ pageSize: 200 }).subscribe(r => {
      this.zone.run(() => {
        this.products = r.items;
        this.applyFilters();
        this.loading = false;
        this.cdr.detectChanges();
      });
    });
  }
  loadCategories() {
    this.productService.getCategories().subscribe(c => {
      this.zone.run(() => {
        this.categories = c;
        this.cdr.detectChanges();
      });
    });
  }

  applyFilters() {
    let list = [...this.products];
    if (this.searchQuery.trim()) list = list.filter(p => p.name.toLowerCase().includes(this.searchQuery.toLowerCase()) || p.sku.toLowerCase().includes(this.searchQuery.toLowerCase()));
    if (this.selectedCategory) { const cat = this.categories.find(c => c.id === this.selectedCategory); if (cat) list = list.filter(p => p.categoryName === cat.name); }
    if (this.statusFilter === 'active')   list = list.filter(p => p.isActive);
    if (this.statusFilter === 'inactive') list = list.filter(p => !p.isActive);
    if (this.statusFilter === 'sale')     list = list.filter(p => p.discountPercent > 0);
    if (this.statusFilter === 'low')      list = list.filter(p => p.stockQuantity > 0 && p.stockQuantity < 10);
    this.filtered = list;
  }

  openAdd() { this.editingProduct = null; this.form = this.emptyForm(); this.formError = ''; this.showModal = true; }
  openEdit(p: Product) {
    this.editingProduct = p;
    const cat = this.categories.find(c => c.name === p.categoryName);
    this.form = { name: p.name, sku: p.sku, price: p.price, stockQuantity: p.stockQuantity, categoryId: cat?.id ?? '', brand: p.brand ?? '', unit: p.unit ?? '', imageUrl: p.imageUrl, description: p.description, discountPercent: p.discountPercent, isActive: p.isActive };
    this.formError = ''; this.showModal = true;
  }
  closeModal() { this.showModal = false; this.editingProduct = null; }

  save() {
    if (!this.form.name.trim())  { this.formError = 'Product name is required.'; return; }
    if (!this.form.sku.trim())   { this.formError = 'SKU is required.'; return; }
    if (!this.form.categoryId)   { this.formError = 'Please select a category.'; return; }
    if (this.form.price <= 0)    { this.formError = 'Price must be greater than 0.'; return; }
    this.formError = ''; this.saving = true;
    const payload = { name: this.form.name, description: this.form.description, price: this.form.price, sku: this.form.sku, imageUrl: this.form.imageUrl, categoryId: this.form.categoryId, stockQuantity: this.form.stockQuantity, brand: this.form.brand || undefined, unit: this.form.unit || undefined, discountPercent: this.form.discountPercent, isActive: this.form.isActive };
    const req = this.editingProduct ? this.productService.updateProduct(this.editingProduct.id, payload) : this.productService.createProduct(payload);
    req.subscribe({
      next: () => { this.saving = false; this.closeModal(); this.loadProducts(); },
      error: (e) => { this.saving = false; this.formError = e?.error?.error ?? 'Something went wrong.'; }
    });
  }

  confirmDelete(p: Product) { this.deleteTarget = p; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.saving = true;
    this.productService.deleteProduct(this.deleteTarget.id).subscribe({
      next: () => { this.saving = false; this.deleteTarget = null; this.loadProducts(); },
      error: () => { this.saving = false; this.deleteTarget = null; }
    });
  }

  private emptyForm(): ProductForm {
    return { name: '', sku: '', price: 0, stockQuantity: 0, categoryId: '', brand: '', unit: '', imageUrl: '', description: '', discountPercent: 0, isActive: true };
  }
}

export { AdminProductsComponent as AdminProducts };
