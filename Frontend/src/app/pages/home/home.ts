import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Category, Product } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { RecentlyViewedService } from '../../core/services/recently-viewed.service';
import { SearchBar, Suggestion } from '../../shared/components/search-bar/search-bar';

const CATEGORY_ICONS: Record<string, string> = {
  'Fruits & Vegetables': '🥦', 'Dairy & Eggs': '🥛', 'Bakery': '🍞',
  'Beverages': '🧃', 'Snacks': '🍿', 'Meat & Seafood': '🥩',
  'Frozen Foods': '🧊', 'Pantry': '🫙',
};

const HERO_SLIDES = [
  { title: 'Fresh Groceries,\nDelivered Fast', subtitle: 'Farm-fresh produce, dairy, bakery and more — right to your door.', cta: 'Shop Now', gradient: 'from-green-600 to-emerald-500', emoji: '🥦' },
  { title: 'Unbeatable Deals\nEvery Day', subtitle: 'Save big on your weekly essentials with our daily offers.', cta: 'See Deals', gradient: 'from-orange-500 to-amber-400', emoji: '🛒' },
  { title: 'Premium Meat\n& Seafood', subtitle: 'Hand-selected cuts and fresh catch delivered chilled.', cta: 'Explore', gradient: 'from-rose-600 to-pink-500', emoji: '🥩' },
];

const FEATURES = [
  { icon: '🚚', title: 'Free Delivery', desc: 'On orders above ₹500' },
  { icon: '🌿', title: 'Farm Fresh', desc: 'Sourced directly from farms' },
  { icon: '⚡', title: 'Express Delivery', desc: 'Same-day in 2 hours' },
  { icon: '↩️', title: 'Easy Returns', desc: 'Hassle-free 7-day returns' },
];

const ROLE_ACTIONS: Record<string, { icon: string; label: string; route: string[] }[]> = {
  Admin: [
    { icon: '⚙️', label: 'Dashboard', route: ['/admin'] },
    { icon: '📦', label: 'Products', route: ['/products'] },
    { icon: '📋', label: 'Orders', route: ['/orders'] },
    { icon: '🏷️', label: 'Categories', route: ['/categories'] },
  ],
  StoreManager: [
    { icon: '📦', label: 'Inventory', route: ['/store-manager'] },
    { icon: '📋', label: 'Orders', route: ['/orders'] },
    { icon: '🛍️', label: 'Products', route: ['/products'] },
  ],
  DeliveryDriver: [
    { icon: '🚚', label: 'My Deliveries', route: ['/delivery'] },
    { icon: '📋', label: 'All Orders', route: ['/orders'] },
  ],
  Customer: [
    { icon: '🛒', label: 'My Cart', route: ['/cart'] },
    { icon: '📋', label: 'My Orders', route: ['/orders'] },
    { icon: '❤️', label: 'Wishlist', route: ['/products'] },
    { icon: '🛍️', label: 'Shop Now', route: ['/products'] },
  ],
};

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Administrator',
  StoreManager: 'Store Manager',
  DeliveryDriver: 'Delivery Driver',
  Customer: 'Customer',
};

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [SearchBar, RouterLink],
  styles: [`
    .hero-section { position:relative; min-height:520px; display:flex; align-items:center; background:var(--hero-grad, linear-gradient(135deg,#16a34a,#059669)); overflow:hidden; transition:background .6s; }
    .hero-bg-emoji { position:absolute; right:-2rem; top:50%; transform:translateY(-50%); font-size:22rem; opacity:.08; pointer-events:none; user-select:none; line-height:1; }
    .hero-content { position:relative; z-index:10; max-width:1000px; margin:0 auto; padding:60px 24px 80px; width:100%; }
    .hero-badge { display:inline-flex; align-items:center; gap:6px; background:rgba(255,255,255,.15); border:1px solid rgba(255,255,255,.25); color:#fff; font-size:12px; font-weight:700; padding:5px 14px; border-radius:20px; margin-bottom:20px; letter-spacing:.06em; text-transform:uppercase; backdrop-filter:blur(4px); }
    .hero-title { font-size:clamp(2.2rem,5vw,3.8rem); font-weight:900; color:#fff; line-height:1.1; margin:0 0 16px; text-shadow:0 2px 20px rgba(0,0,0,.15); }
    .hero-name { background:rgba(255,255,255,.25); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .hero-sub { font-size:16px; color:rgba(255,255,255,.85); margin:0 0 28px; max-width:520px; line-height:1.6; }
    .hero-search { max-width:520px; margin-bottom:24px; }
    .hero-btns { display:flex; gap:12px; flex-wrap:wrap; margin-bottom:32px; }
    .hero-btn-primary { background:#fff; color:#15803d; font-size:15px; font-weight:800; padding:13px 28px; border-radius:50px; border:none; cursor:pointer; box-shadow:0 4px 20px rgba(0,0,0,.2); transition:all .2s; }
    .hero-btn-primary:hover { transform:translateY(-2px); box-shadow:0 8px 28px rgba(0,0,0,.25); }
    .hero-btn-outline { background:rgba(255,255,255,.12); color:#fff; font-size:15px; font-weight:700; padding:13px 28px; border-radius:50px; border:2px solid rgba(255,255,255,.5); cursor:pointer; backdrop-filter:blur(4px); transition:all .2s; }
    .hero-btn-outline:hover { background:rgba(255,255,255,.22); }
    .hero-dots { display:flex; gap:8px; }
    .hero-dot { width:8px; height:8px; border-radius:50%; background:rgba(255,255,255,.4); border:none; cursor:pointer; transition:all .3s; padding:0; }
    .hero-dot-active { width:24px; border-radius:4px; background:#fff; }
    .hero-stats { position:absolute; bottom:0; right:0; display:flex; gap:0; background:rgba(0,0,0,.2); backdrop-filter:blur(12px); border-radius:16px 0 0 0; overflow:hidden; }
    .hero-stat { padding:16px 24px; text-align:center; border-left:1px solid rgba(255,255,255,.1); }
    .hero-stat:first-child { border-left:none; }
    .hs-num { display:block; font-size:22px; font-weight:900; color:#fff; }
    .hs-lbl { display:block; font-size:11px; color:rgba(255,255,255,.6); margin-top:2px; }

    .features-strip { display:grid; grid-template-columns:repeat(4,1fr); background:var(--adm-card); border-top:1px solid var(--adm-border); border-bottom:1px solid var(--adm-border); }
    .feature-card { display:flex; align-items:center; gap:16px; padding:22px 28px; border-right:1px solid var(--adm-border); transition:all .2s; cursor:default; position:relative; overflow:hidden; }
    .feature-card::before { content:''; position:absolute; inset:0; opacity:0; transition:opacity .3s; }
    .feature-card:nth-child(1)::before { background:linear-gradient(135deg,rgba(34,197,94,.08),transparent); }
    .feature-card:nth-child(2)::before { background:linear-gradient(135deg,rgba(16,185,129,.08),transparent); }
    .feature-card:nth-child(3)::before { background:linear-gradient(135deg,rgba(245,158,11,.08),transparent); }
    .feature-card:nth-child(4)::before { background:linear-gradient(135deg,rgba(99,102,241,.08),transparent); }
    .feature-card:hover::before { opacity:1; }
    .feature-card:last-child { border-right:none; }
    .feature-icon-wrap { width:52px; height:52px; border-radius:14px; display:flex; align-items:center; justify-content:center; font-size:24px; flex-shrink:0; transition:transform .2s; }
    .feature-card:nth-child(1) .feature-icon-wrap { background:linear-gradient(135deg,#dcfce7,#bbf7d0); box-shadow:0 4px 12px rgba(34,197,94,.2); }
    .feature-card:nth-child(2) .feature-icon-wrap { background:linear-gradient(135deg,#d1fae5,#a7f3d0); box-shadow:0 4px 12px rgba(16,185,129,.2); }
    .feature-card:nth-child(3) .feature-icon-wrap { background:linear-gradient(135deg,#fef3c7,#fde68a); box-shadow:0 4px 12px rgba(245,158,11,.2); }
    .feature-card:nth-child(4) .feature-icon-wrap { background:linear-gradient(135deg,#ede9fe,#ddd6fe); box-shadow:0 4px 12px rgba(99,102,241,.2); }
    .feature-card:hover .feature-icon-wrap { transform:scale(1.15) rotate(-5deg); }
    .feature-title { font-size:14px; font-weight:800; color:var(--adm-text); margin:0 0 3px; }
    .feature-desc { font-size:12px; color:var(--adm-text2); margin:0; }

    @media (max-width:768px) {
      .features-strip { grid-template-columns:repeat(2,1fr); }
      .feature-card { border-bottom:1px solid var(--adm-border); }
    }

    /* Category cards */
    .cat-card { display:flex; flex-direction:column; align-items:center; gap:8px; padding:14px 8px; border-radius:18px; border:2px solid transparent; background:color-mix(in srgb, var(--cat-color) 10%, white); cursor:pointer; transition:all .2s; }
    :host-context(.dark) .cat-card { background:color-mix(in srgb, var(--cat-color) 15%, #111827); }
    .cat-card:hover { transform:translateY(-4px); box-shadow:0 8px 24px color-mix(in srgb, var(--cat-color) 30%, transparent); border-color:var(--cat-color); }
    .cat-icon-wrap { width:48px; height:48px; border-radius:14px; background:color-mix(in srgb, var(--cat-color) 20%, white); display:flex; align-items:center; justify-content:center; font-size:24px; transition:transform .2s; }
    :host-context(.dark) .cat-icon-wrap { background:color-mix(in srgb, var(--cat-color) 25%, #1e293b); }
    .cat-card:hover .cat-icon-wrap { transform:scale(1.15); }
    .cat-name { font-size:11px; font-weight:700; color:#374151; text-align:center; line-height:1.3; }
    :host-context(.dark) .cat-name { color:#e2e8f0; }

    /* Featured products */
    .featured-section { padding:56px 0; background:var(--adm-bg); position:relative; overflow:hidden; }
    .featured-section::before { content:''; position:absolute; top:-120px; right:-80px; width:500px; height:500px; background:radial-gradient(circle,rgba(34,197,94,.08) 0%,transparent 65%); pointer-events:none; }
    .featured-section::after { content:''; position:absolute; bottom:-100px; left:-60px; width:400px; height:400px; background:radial-gradient(circle,rgba(99,102,241,.06) 0%,transparent 65%); pointer-events:none; }

    .feat-card { background:var(--adm-card); border-radius:20px; overflow:hidden; cursor:pointer; transition:all .3s cubic-bezier(.25,.46,.45,.94); border:1px solid var(--adm-border); box-shadow:0 2px 12px rgba(0,0,0,.06); }
    .feat-card:hover { transform:translateY(-10px); box-shadow:0 24px 60px rgba(0,0,0,.15),0 0 30px rgba(34,197,94,.1); border-color:rgba(34,197,94,.4); }

    .feat-img-wrap { position:relative; height:190px; overflow:hidden; background:var(--adm-card2); }
    .feat-img { width:100%; height:100%; object-fit:cover; transition:transform .6s cubic-bezier(.25,.46,.45,.94); }
    .feat-card:hover .feat-img { transform:scale(1.12); }
    .feat-overlay { position:absolute; inset:0; background:linear-gradient(to top,rgba(0,0,0,.65) 0%,rgba(0,0,0,.15) 45%,transparent 70%); }
    .feat-low-badge { position:absolute; top:10px; left:10px; background:linear-gradient(135deg,#f59e0b,#d97706); color:#fff; font-size:10px; font-weight:800; padding:4px 10px; border-radius:20px; box-shadow:0 2px 10px rgba(245,158,11,.5); z-index:2; }
    .feat-cat-overlay { position:absolute; bottom:12px; left:14px; font-size:10px; font-weight:800; color:rgba(255,255,255,.95); text-transform:uppercase; letter-spacing:.1em; z-index:2; background:rgba(34,197,94,.25); backdrop-filter:blur(6px); padding:3px 10px; border-radius:20px; border:1px solid rgba(34,197,94,.4); }

    .feat-body { padding:16px 18px 18px; }
    .feat-name { font-size:14px; font-weight:700; color:var(--adm-text); margin:0 0 14px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .feat-footer { display:flex; justify-content:space-between; align-items:center; }
    .feat-price { font-size:18px; font-weight:900; color:#16a34a; }
    .feat-rating { display:flex; align-items:center; gap:3px; font-size:12px; font-weight:800; color:#d97706; background:rgba(245,158,11,.1); border:1px solid rgba(245,158,11,.25); padding:4px 10px; border-radius:20px; }
    .feat-see-all { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 22px; border-radius:25px; font-size:13px; font-weight:800; cursor:pointer; box-shadow:0 4px 16px rgba(34,197,94,.35); transition:all .2s; }
    .feat-see-all:hover { transform:translateY(-2px); box-shadow:0 8px 24px rgba(34,197,94,.5); }

    /* Recently Viewed */
    .rv-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; overflow:hidden; cursor:pointer; transition:all .25s; flex-shrink:0; width:160px; }
    .rv-card:hover { transform:translateY(-6px); box-shadow:0 12px 32px rgba(0,0,0,.15); border-color:rgba(34,197,94,.3); }
    .rv-img-wrap { height:110px; overflow:hidden; background:var(--adm-card2); }
    .rv-img { width:100%; height:100%; object-fit:cover; transition:transform .4s; }
    .rv-card:hover .rv-img { transform:scale(1.08); }
    .rv-body { padding:10px 12px 12px; }
    .rv-name { font-size:12px; font-weight:700; color:var(--adm-text); margin:0 0 4px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .rv-price { font-size:14px; font-weight:900; color:#16a34a; margin:0; }
  `],
  template: `

    <!-- Hero -->
    <section class="hero-section" [style]="'--hero-grad: ' + heroGradient()">
      <div class="hero-bg-emoji">{{ currentSlide().emoji }}</div>
      <div class="hero-content">
        <div class="hero-badge">&#x1F6D2; FreshMart</div>
        @if (auth.isAuthenticated()) {
          <h1 class="hero-title">Welcome back,<br/><span class="hero-name">{{ userName() }}!</span></h1>
          <p class="hero-sub">Good to see you again. What are you shopping for today?</p>
        } @else {
          <h1 class="hero-title" style="white-space:pre-line">{{ currentSlide().title }}</h1>
          <p class="hero-sub">{{ currentSlide().subtitle }}</p>
        }
        <div class="hero-search">
          <app-search-bar placeholder="Search for fruits, dairy, snacks..."
            (searched)="goSearch($event)" (suggestionSelected)="goSuggestion($event)" />
        </div>
        <div class="hero-btns">
          <button (click)="router.navigate(['/products'])" class="hero-btn-primary">
            {{ auth.isAuthenticated() ? 'Browse Products' : currentSlide().cta }}
          </button>
          @if (!auth.isAuthenticated()) {
            <button (click)="router.navigate(['/auth/register'])" class="hero-btn-outline">
              Create Account
            </button>
          }
        </div>
        @if (!auth.isAuthenticated()) {
          <div class="hero-dots">
            @for (s of heroSlides; track $index; let i = $index) {
              <button (click)="slideIndex.set(i)" class="hero-dot" [class.hero-dot-active]="slideIndex() === i"></button>
            }
          </div>
        }
      </div>
    </section>

    <!-- Features strip -->
    <section class="features-strip">
      @for (f of features; track f.title) {
        <div class="feature-card">
          <div class="feature-icon-wrap">{{ f.icon }}</div>
          <div>
            <p class="feature-title">{{ f.title }}</p>
            <p class="feature-desc">{{ f.desc }}</p>
          </div>
        </div>
      }
    </section>

    <!-- Personalized dashboard for authenticated users -->
    @if (auth.isAuthenticated()) {
      <section class="bg-white dark:bg-gray-900 border-b border-gray-100 dark:border-gray-800">
        <div class="max-w-5xl mx-auto px-6 py-6">
          <div class="flex items-center justify-between mb-4">
            <div>
              <p class="text-sm text-gray-500 dark:text-gray-400">Signed in as</p>
              <p class="font-semibold text-gray-900 dark:text-white">
                {{ userName() }}
                <span class="ml-2 text-xs font-medium bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400 px-2 py-0.5 rounded-full">
                  {{ roleLabel() }}
                </span>
              </p>
            </div>
            @if (userRole() === 'Customer') {
              <div class="flex gap-4 text-center">
                <div class="cursor-pointer" (click)="router.navigate(['/cart'])">
                  <p class="text-xl font-bold text-gray-900 dark:text-white">{{ cartCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400">Cart items</p>
                </div>
                <div class="w-px bg-gray-200 dark:bg-gray-700"></div>
                <div class="cursor-pointer" (click)="router.navigate(['/products'])">
                  <p class="text-xl font-bold text-gray-900 dark:text-white">{{ wishlistCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400">Wishlist</p>
                </div>
              </div>
            }
          </div>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            @for (action of quickActions(); track action.label) {
              <button (click)="router.navigate(action.route)"
                class="flex items-center gap-3 bg-gray-50 dark:bg-gray-800 hover:bg-green-50 dark:hover:bg-green-900/20 border border-gray-100 dark:border-gray-700 hover:border-green-200 dark:hover:border-green-800 rounded-xl px-4 py-3 transition-all group">
                <span class="text-xl group-hover:scale-110 transition-transform">{{ action.icon }}</span>
                <span class="text-sm font-medium text-gray-700 dark:text-gray-200">{{ action.label }}</span>
              </button>
            }
          </div>
        </div>
      </section>
    }

    <!-- Categories -->
    <section class="max-w-5xl mx-auto px-6 py-12">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h2 class="text-2xl font-extrabold text-gray-900 dark:text-white">Shop by Category</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Find exactly what you need</p>
        </div>
        <button (click)="router.navigate(['/products'])" class="text-sm font-semibold text-green-600 dark:text-green-400 hover:underline">View all &#x2192;</button>
      </div>
      @if (categoriesLoading()) {
        <div class="grid grid-cols-4 md:grid-cols-8 gap-3">
          @for (i of [1,2,3,4,5,6,7,8]; track i) {
            <div class="bg-gray-100 dark:bg-gray-800 rounded-2xl h-24 animate-pulse"></div>
          }
        </div>
      } @else {
        <div class="grid grid-cols-4 md:grid-cols-8 gap-3">
          @for (cat of categories(); track cat.id; let i = $index) {
            <button (click)="browseCategory(cat.id)" class="cat-card" [style]="'--cat-color:' + catColors[i % catColors.length]">
              <span class="cat-icon-wrap">{{ icon(cat.name) }}</span>
              <span class="cat-name">{{ cat.name }}</span>
            </button>
          }
        </div>
      }
    </section>

    <!-- Featured Products -->
    <section class="featured-section">
      <div class="max-w-5xl mx-auto px-6" style="position:relative;z-index:2">
        <div class="flex items-center justify-between mb-6">
          <div>
            <h2 class="text-2xl font-extrabold" style="color:var(--adm-text)">Featured Products</h2>
            <p class="text-sm mt-1" style="color:var(--adm-text2)">Handpicked just for you</p>
          </div>
          <button (click)="router.navigate(['/products'])" class="feat-see-all">See all &#x2192;</button>
        </div>
        @if (productsLoading()) {
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-5">
            @for (i of [1,2,3,4,5,6,7,8]; track i) {
              <div class="bg-white dark:bg-gray-800 rounded-2xl h-64 animate-pulse"></div>
            }
          </div>
        } @else {
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-5">
            @for (p of featuredProducts(); track p.id; let i = $index) {
              <div (click)="router.navigate(['/products', p.id])" class="feat-card">
                <div class="feat-img-wrap">
                  <img [src]="p.imageUrl" [alt]="p.name" class="feat-img" />
                  @if (p.stockQuantity < 10 && p.stockQuantity > 0) {
                    <span class="feat-low-badge">Low stock</span>
                  }
                  <div class="feat-overlay"></div>
                  <span class="feat-cat-overlay">{{ p.categoryName }}</span>
                </div>
                <div class="feat-body">
                  <p class="feat-name">{{ p.name }}</p>
                  <div class="feat-footer">
                    <span class="feat-price">&#x20B9;{{ p.price.toFixed(2) }}</span>
                    <span class="feat-rating">&#x2605; {{ p.averageRating.toFixed(1) }}</span>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </section>

    <!-- Recently Viewed -->
    @if (recentlyViewed.items().length > 0) {
      <section class="max-w-5xl mx-auto px-6 pb-12">
        <div class="flex items-center justify-between mb-5">
          <h2 class="text-xl font-bold" style="color:var(--adm-text)">Recently Viewed</h2>
          <a routerLink="/products" class="text-sm font-semibold text-green-600 dark:text-green-400 hover:underline">Browse all</a>
        </div>
        <div class="flex gap-4 overflow-x-auto pb-2">
          @for (p of recentlyViewed.items().slice(0, 6); track p.id) {
            <div (click)="router.navigate(['/products', p.id])" class="rv-card">
              <div class="rv-img-wrap">
                <img [src]="p.imageUrl" [alt]="p.name" class="rv-img" />
              </div>
              <div class="rv-body">
                <p class="rv-name">{{ p.name }}</p>
                <p class="rv-price">&#x20B9;{{ p.price.toFixed(2) }}</p>
              </div>
            </div>
          }
        </div>
      </section>
    }

    <!-- Promo banners -->
    <section class="max-w-5xl mx-auto px-6 py-12">
      <div class="grid md:grid-cols-2 gap-5">
        <div class="bg-gradient-to-br from-green-500 to-emerald-400 rounded-2xl p-8 flex items-center gap-5">
          <span class="text-5xl">&#x1F957;</span>
          <div>
            <p class="text-white/70 text-xs font-medium mb-1 uppercase tracking-wide">Fresh &amp; Healthy</p>
            <h3 class="text-white text-xl font-bold mb-1">Fruits &amp; Veggies</h3>
            <p class="text-white/70 text-sm mb-4">Straight from the farm to your table</p>
            <button (click)="browseByName('Fruits & Vegetables')"
              class="bg-white text-green-700 text-sm font-semibold px-5 py-2 rounded-full hover:shadow transition">
              Shop Now
            </button>
          </div>
        </div>
        <div class="bg-gradient-to-br from-orange-400 to-amber-300 rounded-2xl p-8 flex items-center gap-5">
          <span class="text-5xl">&#x1F95B;</span>
          <div>
            <p class="text-white/70 text-xs font-medium mb-1 uppercase tracking-wide">Daily Essentials</p>
            <h3 class="text-white text-xl font-bold mb-1">Dairy &amp; Eggs</h3>
            <p class="text-white/70 text-sm mb-4">Fresh dairy delivered every morning</p>
            <button (click)="browseByName('Dairy & Eggs')"
              class="bg-white text-orange-600 text-sm font-semibold px-5 py-2 rounded-full hover:shadow transition">
              Shop Now
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="bg-gray-100 dark:bg-gray-950 border-t border-gray-200 dark:border-gray-800 text-gray-500 dark:text-gray-400 py-10 mt-4">
      <div class="max-w-5xl mx-auto px-6 grid grid-cols-2 md:grid-cols-4 gap-8 mb-8">
        <div>
          <p class="text-gray-900 dark:text-white font-bold text-base mb-3">&#x1F6D2; FreshMart</p>
          <p class="text-sm leading-relaxed text-gray-500 dark:text-gray-400">Your neighbourhood grocery store, online. Fresh, fast, affordable.</p>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Shop</p>
          <ul class="space-y-2 text-sm">
            <li><button (click)="router.navigate(['/products'])" class="hover:text-gray-900 dark:hover:text-white transition">All Products</button></li>
            @for (cat of categories().slice(0, 4); track cat.id) {
              <li><button (click)="browseCategory(cat.id)" class="hover:text-gray-900 dark:hover:text-white transition">{{ cat.name }}</button></li>
            }
          </ul>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Account</p>
          <ul class="space-y-2 text-sm">
            @if (auth.isAuthenticated()) {
              <li><button (click)="router.navigate(['/orders'])" class="hover:text-gray-900 dark:hover:text-white transition">My Orders</button></li>
              <li><button (click)="router.navigate(['/cart'])" class="hover:text-gray-900 dark:hover:text-white transition">My Cart</button></li>
            } @else {
              <li><button (click)="router.navigate(['/auth/login'])" class="hover:text-gray-900 dark:hover:text-white transition">Login</button></li>
              <li><button (click)="router.navigate(['/auth/register'])" class="hover:text-gray-900 dark:hover:text-white transition">Register</button></li>
            }
          </ul>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Support</p>
          <ul class="space-y-2 text-sm">
            <li>&#x1F4DE; 1-800-FRESH</li>
            <li>&#x2709; help&#64;freshmart.com</li>
            <li>&#x1F550; 24/7 Support</li>
          </ul>
        </div>
      </div>
      <div class="max-w-5xl mx-auto px-6 border-t border-gray-200 dark:border-gray-800 pt-5 flex flex-col md:flex-row items-center justify-between gap-2 text-xs text-gray-400 dark:text-gray-600">
        <p>&#xA9; 2026 FreshMart. All rights reserved.</p>
        <p>Built with &#x2764; for fresh groceries</p>
      </div>
    </footer>
  `
})
export class Home implements OnInit {
  router = inject(Router);
  auth = inject(AuthService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private wishlistService = inject(WishlistService);
  recentlyViewed = inject(RecentlyViewedService);

  heroSlides = HERO_SLIDES;
  features = FEATURES;
  slideIndex = signal(0);
  categories = signal<Category[]>([]);
  featuredProducts = signal<Product[]>([]);
  categoriesLoading = signal(true);
  productsLoading = signal(true);

  currentSlide = () => HERO_SLIDES[this.slideIndex()];
  heroGradient = () => {
    const gradients = [
      'linear-gradient(135deg,#16a34a 0%,#059669 40%,#0d9488 100%)',
      'linear-gradient(135deg,#ea580c 0%,#d97706 50%,#ca8a04 100%)',
      'linear-gradient(135deg,#e11d48 0%,#db2777 50%,#9333ea 100%)',
    ];
    return gradients[this.slideIndex()] ?? gradients[0];
  };
  private slideTimer: ReturnType<typeof setInterval> | null = null;

  userName = computed(() => this.auth.getUserName() ?? 'there');
  userRole = computed(() => this.auth.getUserRole() ?? '');
  roleLabel = computed(() => ROLE_LABELS[this.userRole()] ?? this.userRole());
  quickActions = computed(() => ROLE_ACTIONS[this.userRole()] ?? ROLE_ACTIONS['Customer']);
  cartCount = computed(() => this.cartService.cart()?.items?.length ?? 0);
  wishlistCount = computed(() => this.wishlistService.count);

  ngOnInit() {
    this.productService.getCategories().subscribe(c => { this.categories.set(c); this.categoriesLoading.set(false); });
    this.productService.getProducts({ pageSize: 8, sortBy: 'rating' }).subscribe(r => { this.featuredProducts.set(r.items); this.productsLoading.set(false); });
    this.slideTimer = setInterval(() => this.slideIndex.set((this.slideIndex() + 1) % HERO_SLIDES.length), 4500);
    if (this.auth.isAuthenticated() && this.auth.getUserRole() === 'Customer') {
      this.cartService.getCart().subscribe();
    }
  }

  ngOnDestroy() { if (this.slideTimer) clearInterval(this.slideTimer); }

  icon(name: string) { return CATEGORY_ICONS[name] ?? '🛍️'; }

  catColors = [
    '#16a34a','#2563eb','#7c3aed','#db2777',
    '#ea580c','#0891b2','#65a30d','#dc2626'
  ];
  browseCategory(id: string) { this.router.navigate(['/products'], { queryParams: { categoryId: id } }); }
  browseByName(name: string) { this.router.navigate(['/products'], { queryParams: { q: name } }); }
  goSearch(q: string) { if (q) this.router.navigate(['/products'], { queryParams: { q } }); }
  goSuggestion(s: Suggestion) { this.router.navigate(['/products'], { queryParams: { q: s.name } }); }
}
