import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Category, Product } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { SearchBar, Suggestion } from '../../shared/components/search-bar/search-bar';

const CATEGORY_ICONS: Record<string, string> = {
  'Fruits & Vegetables': '🥦', 'Dairy & Eggs': '🥛', 'Bakery': '🍞',
  'Beverages': '🧃', 'Snacks': '🍿', 'Meat & Seafood': '🥩',
  'Frozen Foods': '🧊', 'Pantry': '🫙',
};

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [FormsModule, ProductCard, SearchBar],
  template: `

<div class="shop-page">

  <!-- Sidebar -->
  <aside class="shop-sidebar">
    <p class="shop-sidebar-title">Categories</p>
    <button class="shop-cat-btn" [class.shop-cat-active]="categoryId === ''" (click)="selectCategory('')">
      <span class="shop-cat-icon">&#x1F6D2;</span>
      <span class="shop-cat-name">All Products</span>
      <span class="shop-cat-count">{{ total() }}</span>
    </button>
    @for (cat of categories(); track cat.id) {
      <button class="shop-cat-btn" [class.shop-cat-active]="categoryId === cat.id" (click)="selectCategory(cat.id)">
        <span class="shop-cat-icon">{{ icon(cat.name) }}</span>
        <span class="shop-cat-name">{{ cat.name }}</span>
      </button>
    }

    <!-- Filter Panel -->
    <div class="filter-divider"></div>
    <p class="shop-sidebar-title">Filters</p>

    <!-- Rating Filter -->
    <div class="filter-section">
      <p class="filter-label">⭐ Min Rating</p>
      <div class="rating-options">
        @for (r of [4,3,2,1]; track r) {
          <button class="rating-btn" [class.rating-active]="minRating() === r" (click)="setRating(r)">
            {{ r }}★ & up
          </button>
        }
      </div>
    </div>

    <!-- Brand Filter -->
    @if (availableBrands().length > 0) {
      <div class="filter-section">
        <p class="filter-label">🏷️ Brand</p>
        <div class="brand-list">
          @for (brand of availableBrands(); track brand) {
            <label class="brand-item">
              <input type="checkbox" [checked]="selectedBrands().includes(brand)" (change)="toggleBrand(brand)" class="brand-check" />
              <span>{{ brand }}</span>
            </label>
          }
        </div>
      </div>
    }

    <!-- Unit/Weight Filter -->
    @if (availableUnits().length > 0) {
      <div class="filter-section">
        <p class="filter-label">⚖️ Weight / Unit</p>
        <div class="brand-list">
          @for (unit of availableUnits(); track unit) {
            <label class="brand-item">
              <input type="checkbox" [checked]="selectedUnits().includes(unit)" (change)="toggleUnit(unit)" class="brand-check" />
              <span>{{ unit }}</span>
            </label>
          }
        </div>
      </div>
    }

    <!-- In Stock Filter -->
    <div class="filter-section">
      <label class="brand-item">
        <input type="checkbox" [(ngModel)]="inStockOnly" (ngModelChange)="applyFilters()" class="brand-check" />
        <span>✅ In Stock Only</span>
      </label>
    </div>

    @if (hasActiveFilters()) {
      <button (click)="clearSidebarFilters()" class="filter-clear-btn">Clear Filters</button>
    }
  </aside>

  <!-- Main -->
  <div class="shop-main">

    <!-- Toolbar -->
    <div class="shop-toolbar">
      <div class="shop-search-wrap">
        <app-search-bar (searched)="onSearch($event)" (suggestionSelected)="onSuggestion($event)" />
      </div>
      <div class="shop-filters">
        <input type="number" [(ngModel)]="minPriceInput" (ngModelChange)="onPriceChange()" placeholder="Min &#x20B9;" min="0" class="shop-input shop-price-input" />
        <span class="shop-dash">&#x2014;</span>
        <input type="number" [(ngModel)]="maxPriceInput" (ngModelChange)="onPriceChange()" placeholder="Max &#x20B9;" min="0" class="shop-input shop-price-input" />
        <select [(ngModel)]="sortBy" (ngModelChange)="load()" class="shop-input shop-select">
          <option value="">Sort: Name</option>
          <option value="price_asc">Price Low-High</option>
          <option value="price_desc">Price High-Low</option>
          <option value="rating">Top Rated</option>
        </select>
        <button (click)="toggleSale()" class="shop-sale-btn" [class.shop-sale-active]="onSaleOnly">
          On Sale
        </button>
      </div>
    </div>

    <!-- Active filter chips -->
    @if (query || categoryId || minPrice() != null || maxPrice() != null) {
      <div class="shop-chips">
        @if (query) {
          <span class="shop-chip chip-green">"{{ query }}" <button (click)="clearQuery()" class="chip-x">x</button></span>
        }
        @if (activeCategoryName()) {
          <span class="shop-chip chip-blue">{{ activeCategoryName() }} <button (click)="selectCategory('')" class="chip-x">x</button></span>
        }
        @if (minPrice() != null || maxPrice() != null) {
          <span class="shop-chip chip-purple">&#x20B9;{{ minPrice() ?? 0 }} - {{ maxPrice() ?? 'any' }} <button (click)="clearPrice()" class="chip-x">x</button></span>
        }
        <button (click)="clearAll()" class="shop-clear-all">Clear all</button>
      </div>
    }

    <!-- Loading skeleton -->
    @if (loading()) {
      <div class="shop-grid">
        @for (i of [1,2,3,4,5,6,7,8]; track i) {
          <div class="shop-skel"></div>
        }
      </div>

    <!-- Grouped home view -->
    } @else if (!isFiltered()) {
      @if (saleProducts().length > 0) {
        <div class="shop-group">
          <div class="shop-group-header">
            <div class="shop-group-title">
              <span class="shop-group-icon sale-icon">%</span>
              <span>On Sale</span>
              <span class="shop-group-badge sale-badge">{{ saleProducts().length }} deals</span>
            </div>
            <button (click)="toggleSale()" class="shop-see-all">See all &#x2192;</button>
          </div>
          <div class="shop-grid">
            @for (product of saleProducts().slice(0, 4); track product.id) {
              <app-product-card [product]="product" (addToCart)="addToCart($event)" />
            }
          </div>
        </div>
      }
      @for (group of groupedProducts(); track group.category) {
        <div class="shop-group">
          <div class="shop-group-header">
            <div class="shop-group-title">
              <span class="shop-group-icon">{{ icon(group.category) }}</span>
              <span>{{ group.category }}</span>
            </div>
            <button (click)="selectCategory(group.categoryId)" class="shop-see-all">See all &#x2192;</button>
          </div>
          <div class="shop-grid">
            @for (product of group.products.slice(0, 4); track product.id) {
              <app-product-card [product]="product" (addToCart)="addToCart($event)" />
            }
          </div>
        </div>
      }

    <!-- Filtered grid -->
    } @else {
      @if (activeCategoryName()) {
        <div class="shop-filtered-header">
          <span class="shop-filtered-icon">{{ icon(activeCategoryName()) }}</span>
          <div>
            <h2 class="shop-filtered-title">{{ activeCategoryName() }}</h2>
            <p class="shop-filtered-count">{{ total() }} products</p>
          </div>
        </div>
      } @else if (query) {
        <p class="shop-filtered-count" style="margin-bottom:16px">{{ total() }} results for "{{ query }}"</p>
      }
      <div class="shop-grid">
        @for (product of products(); track product.id) {
          <app-product-card [product]="product" (addToCart)="addToCart($event)" />
        }
        @if (products().length === 0) {
          <div class="shop-empty">
            <p class="shop-empty-icon">&#x1F50D;</p>
            <p class="shop-empty-title">No products found</p>
            <p class="shop-empty-sub">Try adjusting your search or filters</p>
            <button (click)="clearAll()" class="shop-empty-btn">Clear all filters</button>
          </div>
        }
      </div>
      @if (totalPages() > 1) {
        <div class="shop-pagination">
          <button (click)="changePage(page() - 1)" [disabled]="page() === 1" class="shop-page-btn">&#x2190; Prev</button>
          <span class="shop-page-info">{{ page() }} / {{ totalPages() }}</span>
          <button (click)="changePage(page() + 1)" [disabled]="page() === totalPages()" class="shop-page-btn">Next &#x2192;</button>
        </div>
      }
    }
  </div>
</div>

@if (toast()) {
  <div class="shop-toast">{{ toast() }}</div>
}
  `,
  styles: [`
    * { box-sizing:border-box; }
    .shop-page { display:flex; min-height:100vh; background:var(--adm-bg); color:var(--adm-text); }

    /* Sidebar */
    .shop-sidebar { width:220px; flex-shrink:0; background:var(--adm-card); border-right:1px solid var(--adm-border); padding:20px 12px; display:flex; flex-direction:column; gap:2px; position:sticky; top:56px; height:calc(100vh - 56px); overflow-y:auto; }
    .shop-sidebar-title { font-size:10px; font-weight:700; color:var(--adm-text3); text-transform:uppercase; letter-spacing:.1em; padding:0 10px; margin-bottom:8px; }
    .shop-cat-btn { display:flex; align-items:center; gap:10px; padding:9px 10px; border-radius:10px; border:none; background:none; color:var(--adm-text2); font-size:13px; font-weight:500; cursor:pointer; text-align:left; width:100%; transition:all .15s; }
    .shop-cat-btn:hover { background:var(--adm-row-alt); color:var(--adm-text); }
    .shop-cat-active { background:linear-gradient(135deg,rgba(34,197,94,.15),rgba(34,197,94,.08)) !important; color:#16a34a !important; font-weight:700 !important; border:1px solid rgba(34,197,94,.2); }
    .shop-cat-icon { font-size:16px; width:22px; text-align:center; flex-shrink:0; }
    .shop-cat-name { flex:1; }
    .shop-cat-count { font-size:11px; color:var(--adm-text3); background:var(--adm-border); padding:1px 7px; border-radius:20px; }

    /* Main */
    .shop-main { flex:1; padding:24px; min-width:0; }

    /* Toolbar */
    .shop-toolbar { display:flex; gap:12px; margin-bottom:20px; flex-wrap:wrap; align-items:center; }
    .shop-search-wrap { flex:1; min-width:240px; }
    .shop-filters { display:flex; gap:8px; align-items:center; flex-wrap:wrap; }
    .shop-input { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:9px 12px; border-radius:8px; font-size:13px; }
    .shop-input:focus { outline:none; border-color:#22c55e; }
    .shop-price-input { width:90px; }
    .shop-select { cursor:pointer; }
    .shop-dash { color:var(--adm-text3); font-size:14px; }
    .shop-sale-btn { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text2); padding:9px 16px; border-radius:8px; font-size:13px; font-weight:600; cursor:pointer; transition:all .2s; white-space:nowrap; }
    .shop-sale-active { background:linear-gradient(135deg,#ef4444,#dc2626) !important; color:#fff !important; border-color:#ef4444 !important; }

    /* Filter chips */
    .shop-chips { display:flex; flex-wrap:wrap; gap:8px; margin-bottom:16px; align-items:center; }
    .shop-chip { display:inline-flex; align-items:center; gap:6px; padding:4px 12px; border-radius:20px; font-size:12px; font-weight:600; }
    .chip-green { background:rgba(34,197,94,.15); color:#15803d; border:1px solid rgba(34,197,94,.3); }
    .chip-blue { background:rgba(59,130,246,.15); color:#1d4ed8; border:1px solid rgba(59,130,246,.3); }
    .chip-purple { background:rgba(139,92,246,.15); color:#6d28d9; border:1px solid rgba(139,92,246,.3); }
    .chip-x { background:none; border:none; cursor:pointer; color:inherit; font-size:12px; font-weight:700; padding:0; line-height:1; opacity:.7; }
    .chip-x:hover { opacity:1; }
    .shop-clear-all { background:none; border:none; color:var(--adm-text3); font-size:12px; cursor:pointer; text-decoration:underline; }

    /* Group sections */
    .shop-group { margin-bottom:36px; }
    .shop-group-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:16px; padding-bottom:12px; border-bottom:1px solid var(--adm-border); }
    .shop-group-title { display:flex; align-items:center; gap:10px; font-size:18px; font-weight:800; color:var(--adm-text); }
    .shop-group-icon { font-size:22px; width:36px; height:36px; background:var(--adm-card); border:1px solid var(--adm-border); border-radius:10px; display:flex; align-items:center; justify-content:center; }
    .sale-icon { background:linear-gradient(135deg,#ef4444,#dc2626); color:#fff; border:none; font-size:14px; font-weight:800; }
    .shop-group-badge { font-size:11px; font-weight:700; padding:3px 10px; border-radius:20px; }
    .sale-badge { background:rgba(239,68,68,.15); color:#dc2626; border:1px solid rgba(239,68,68,.3); }
    .shop-see-all { background:none; border:none; color:#16a34a; font-size:13px; font-weight:700; cursor:pointer; }
    .shop-see-all:hover { text-decoration:underline; }

    /* Product grid */
    .shop-grid { display:grid; grid-template-columns:repeat(4,1fr); gap:16px; }

    /* Filtered header */
    .shop-filtered-header { display:flex; align-items:center; gap:14px; margin-bottom:20px; }
    .shop-filtered-icon { font-size:36px; }
    .shop-filtered-title { font-size:20px; font-weight:800; color:var(--adm-text); margin:0; }
    .shop-filtered-count { font-size:13px; color:var(--adm-text2); margin:4px 0 0; }

    /* Empty state */
    .shop-empty { grid-column:1/-1; text-align:center; padding:60px 20px; }
    .shop-empty-icon { font-size:48px; margin:0 0 12px; }
    .shop-empty-title { font-size:18px; font-weight:700; color:var(--adm-text); margin:0 0 6px; }
    .shop-empty-sub { font-size:14px; color:var(--adm-text2); margin:0 0 20px; }
    .shop-empty-btn { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 24px; border-radius:8px; font-size:14px; font-weight:700; cursor:pointer; }

    /* Pagination */
    .shop-pagination { display:flex; justify-content:center; align-items:center; gap:12px; margin-top:32px; }
    .shop-page-btn { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:9px 18px; border-radius:8px; font-size:13px; font-weight:600; cursor:pointer; transition:all .2s; }
    .shop-page-btn:hover:not(:disabled) { background:var(--adm-border); }
    .shop-page-btn:disabled { opacity:.4; cursor:not-allowed; }
    .shop-page-info { font-size:13px; color:var(--adm-text2); font-weight:600; }

    /* Skeleton */
    .shop-skel { height:300px; background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }

    /* Toast */
    .shop-toast { position:fixed; bottom:24px; right:24px; background:linear-gradient(135deg,#1e293b,#0f172a); color:#f1f5f9; padding:12px 20px; border-radius:12px; font-size:14px; font-weight:600; box-shadow:0 8px 24px rgba(0,0,0,.3); z-index:1000; border:1px solid #334155; }

    /* Filter Panel */
    .filter-divider { border:none; border-top:1px solid var(--adm-border); margin:14px 0 10px; }
    .filter-section { margin-bottom:14px; padding:0 4px; }
    .filter-label { font-size:11px; font-weight:700; color:var(--adm-text3); text-transform:uppercase; letter-spacing:.08em; margin:0 0 8px; }
    .rating-options { display:flex; flex-direction:column; gap:4px; }
    .rating-btn { background:var(--adm-row-alt); border:1px solid var(--adm-border); color:var(--adm-text2); padding:6px 10px; border-radius:7px; font-size:12px; font-weight:500; cursor:pointer; text-align:left; transition:all .15s; }
    .rating-btn:hover { border-color:#f59e0b; color:#f59e0b; }
    .rating-active { background:rgba(245,158,11,.15) !important; border-color:#f59e0b !important; color:#d97706 !important; font-weight:700 !important; }
    .brand-list { display:flex; flex-direction:column; gap:5px; max-height:140px; overflow-y:auto; }
    .brand-item { display:flex; align-items:center; gap:8px; font-size:12px; color:var(--adm-text2); cursor:pointer; padding:3px 0; }
    .brand-item:hover { color:var(--adm-text); }
    .brand-check { accent-color:#22c55e; width:14px; height:14px; cursor:pointer; }
    .filter-clear-btn { width:100%; margin-top:6px; background:rgba(239,68,68,.1); border:1px solid rgba(239,68,68,.3); color:#ef4444; padding:8px; border-radius:8px; font-size:12px; font-weight:700; cursor:pointer; transition:all .15s; }
    .filter-clear-btn:hover { background:rgba(239,68,68,.2); }

    @media (max-width: 1024px) { .shop-grid { grid-template-columns:repeat(3,1fr); } }
    @media (max-width: 768px) { .shop-sidebar { display:none; } .shop-grid { grid-template-columns:repeat(2,1fr); } }
  `]
})
export class Products implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  products = signal<Product[]>([]);
  allLoadedProducts = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  total = signal(0);
  page = signal(1);
  toast = signal('');
  saleProducts = signal<Product[]>([]);

  query = ''; categoryId = ''; sortBy = ''; onSaleOnly = false; inStockOnly = false;
  minPrice = signal<number | null>(null);
  maxPrice = signal<number | null>(null);
  minPriceInput: number | null = null;
  maxPriceInput: number | null = null;
  minRating = signal<number | null>(null);
  selectedBrands = signal<string[]>([]);
  selectedUnits = signal<string[]>([]);
  private priceTimer: ReturnType<typeof setTimeout> | null = null;

  availableBrands = computed(() =>
    [...new Set(this.allLoadedProducts().map(p => p.brand).filter((b): b is string => !!b))].sort()
  );
  availableUnits = computed(() =>
    [...new Set(this.allLoadedProducts().map(p => p.unit).filter((u): u is string => !!u))].sort()
  );
  hasActiveFilters = computed(() =>
    this.minRating() !== null || this.selectedBrands().length > 0 ||
    this.selectedUnits().length > 0 || this.inStockOnly
  );

  totalPages = computed(() => Math.ceil(this.total() / 20));
  activeCategoryName = computed(() => this.categories().find(c => c.id === this.categoryId)?.name ?? '');
  groupedProducts = computed(() => {
    const map = new Map<string, { category: string; categoryId: string; products: Product[] }>();
    for (const p of this.products()) {
      if (!map.has(p.categoryName)) map.set(p.categoryName, { category: p.categoryName, categoryId: '', products: [] });
      map.get(p.categoryName)!.products.push(p);
    }
    for (const cat of this.categories()) {
      if (map.has(cat.name)) map.get(cat.name)!.categoryId = cat.id;
    }
    return [...map.values()];
  });

  icon(name: string) { return CATEGORY_ICONS[name] ?? '🛍️'; }

  ngOnInit() {
    this.productService.getCategories().subscribe(c => this.categories.set(c));
    this.productService.getOnSale().subscribe(s => this.saleProducts.set(s));
    this.route.queryParams.subscribe(params => {
      if (params['q']) this.query = params['q'];
      if (params['categoryId']) this.categoryId = params['categoryId'];
      if (this.isFiltered()) this.load(); else this.loadAll();
    });
  }

  private loadAll() {
    this.loading.set(true);
    this.productService.getProducts({ pageSize: 100 }).subscribe({
      next: r => {
        this.allLoadedProducts.set(r.items);
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  load() {
    this.loading.set(true);
    this.productService.getProducts({
      query: this.query || undefined, categoryId: this.categoryId || undefined,
      sortBy: this.sortBy || undefined, minPrice: this.minPrice() ?? undefined,
      maxPrice: this.maxPrice() ?? undefined, page: this.page(), pageSize: 100
    }).subscribe({
      next: r => {
        this.allLoadedProducts.set(r.items);
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  applyFilters() {
    let filtered = this.allLoadedProducts();
    if (this.minRating() !== null)
      filtered = filtered.filter(p => p.averageRating >= this.minRating()!);
    if (this.selectedBrands().length > 0)
      filtered = filtered.filter(p => p.brand && this.selectedBrands().includes(p.brand));
    if (this.selectedUnits().length > 0)
      filtered = filtered.filter(p => p.unit && this.selectedUnits().includes(p.unit));
    if (this.inStockOnly)
      filtered = filtered.filter(p => p.stockQuantity > 0);
    this.products.set(filtered);
    this.total.set(filtered.length);
  }

  setRating(r: number) {
    this.minRating.set(this.minRating() === r ? null : r);
    this.applyFilters();
  }

  toggleBrand(brand: string) {
    const cur = this.selectedBrands();
    this.selectedBrands.set(cur.includes(brand) ? cur.filter(b => b !== brand) : [...cur, brand]);
    this.applyFilters();
  }

  toggleUnit(unit: string) {
    const cur = this.selectedUnits();
    this.selectedUnits.set(cur.includes(unit) ? cur.filter(u => u !== unit) : [...cur, unit]);
    this.applyFilters();
  }

  clearSidebarFilters() {
    this.minRating.set(null);
    this.selectedBrands.set([]);
    this.selectedUnits.set([]);
    this.inStockOnly = false;
    this.applyFilters();
  }

  selectCategory(id: string) {
    this.categoryId = id; this.query = ''; this.page.set(1);
    if (this.isFiltered()) this.load(); else this.loadAll();
  }

  onSearch(q: string) {
    this.query = q; this.categoryId = ''; this.page.set(1);
    if (q) this.load(); else this.loadAll();
  }

  onSuggestion(s: Suggestion) { this.query = s.name; this.categoryId = ''; this.page.set(1); this.load(); }

  onPriceChange() {
    if (this.priceTimer) clearTimeout(this.priceTimer);
    this.minPrice.set(this.minPriceInput ?? null);
    this.maxPrice.set(this.maxPriceInput ?? null);
    this.priceTimer = setTimeout(() => { this.page.set(1); this.load(); }, 500);
  }

  clearQuery() { this.query = ''; this.page.set(1); this.isFiltered() ? this.load() : this.loadAll(); }
  clearPrice() {
    this.minPrice.set(null); this.maxPrice.set(null);
    this.minPriceInput = null; this.maxPriceInput = null;
    this.page.set(1); this.isFiltered() ? this.load() : this.loadAll();
  }
  clearAll() {
    this.query = ''; this.categoryId = ''; this.sortBy = ''; this.onSaleOnly = false;
    this.minPrice.set(null); this.maxPrice.set(null);
    this.minPriceInput = null; this.maxPriceInput = null;
    this.minRating.set(null); this.selectedBrands.set([]); this.selectedUnits.set([]); this.inStockOnly = false;
    this.page.set(1); this.loadAll();
  }

  isFiltered() { return !!(this.query || this.categoryId || this.minPrice() != null || this.maxPrice() != null || this.sortBy || this.onSaleOnly); }

  toggleSale() {
    this.onSaleOnly = !this.onSaleOnly;
    this.query = ''; this.categoryId = ''; this.page.set(1);
    if (this.onSaleOnly) {
      this.loading.set(true);
      this.productService.getOnSale().subscribe(items => {
        this.products.set(items); this.total.set(items.length); this.loading.set(false);
      });
    } else {
      this.loadAll();
    }
  }

  changePage(p: number) { this.page.set(p); this.load(); window.scrollTo(0, 0); }

  addToCart(product: Product) {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartService.addItem(product.id, 1).subscribe({
      next: () => this.showToast(`${product.name} added to cart`),
      error: () => this.showToast('Failed to add item')
    });
  }

  private showToast(msg: string) { this.toast.set(msg); setTimeout(() => this.toast.set(''), 2500); }
}
