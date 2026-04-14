import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { ProductService } from '../../core/services/product.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { CartService } from '../../core/services/cart.service';
import { User, Order, Product } from '../../core/models';
import { environment } from '../../../environments/environment';

type Tab = 'overview' | 'orders' | 'wishlist' | 'addresses' | 'settings' | 'privacy';
interface Address { id: string; label: string; line1: string; city: string; state: string; zip: string; isDefault: boolean; }

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
<div class="pf-page">

  <!-- Hero Header -->
  <div class="pf-hero">
    <div class="pf-hero-bg"></div>
    <div class="pf-hero-content">
      <div class="pf-avatar-wrap">
        <div class="pf-avatar">
          <span class="pf-avatar-initials">{{ initials() }}</span>
        </div>
      </div>
      <div class="pf-hero-info">
        <h1 class="pf-hero-name">{{ user()?.firstName }} {{ user()?.lastName }}</h1>
        <p class="pf-hero-email">{{ user()?.email }}</p>
        <span class="pf-role-badge" [class]="'pf-role-' + (user()?.role ?? 'Customer').toLowerCase()">{{ user()?.role }}</span>
      </div>
      <div class="pf-hero-stats">
        <div class="pf-stat"><span class="pf-stat-val">{{ orders().length }}</span><span class="pf-stat-lbl">Orders</span></div>
        <div class="pf-stat-div"></div>
        <div class="pf-stat"><span class="pf-stat-val">{{ wishlistCount() }}</span><span class="pf-stat-lbl">Wishlist</span></div>
        <div class="pf-stat-div"></div>
        <div class="pf-stat"><span class="pf-stat-val">&#x20B9;{{ totalSpent() }}</span><span class="pf-stat-lbl">Spent</span></div>
      </div>
    </div>
  </div>

  <div class="pf-body">
    <!-- Sidebar -->
    <aside class="pf-sidebar">
      @for (t of tabs; track t.id) {
        <button (click)="selectTab(t.id)" class="pf-tab" [class.pf-tab-active]="activeTab() === t.id">
          <span>{{ t.label }}</span>
        </button>
      }
    </aside>

    <!-- Content -->
    <div class="pf-content">

      <!-- OVERVIEW -->
      @if (activeTab() === 'overview') {
        <div class="pf-section-title">Account Summary</div>
        <div class="pf-stats-grid">
          <div class="pf-card-stat c1"><div class="pf-cs-val">{{ orders().length }}</div><div class="pf-cs-lbl">Total Orders</div></div>
          <div class="pf-card-stat c2"><div class="pf-cs-val">{{ deliveredCount() }}</div><div class="pf-cs-lbl">Delivered</div></div>
          <div class="pf-card-stat c3"><div class="pf-cs-val">{{ pendingCount() }}</div><div class="pf-cs-lbl">Pending</div></div>
          <div class="pf-card-stat c4"><div class="pf-cs-val">{{ wishlistCount() }}</div><div class="pf-cs-lbl">Wishlist</div></div>
          <div class="pf-card-stat c5"><div class="pf-cs-val">&#x20B9;{{ totalSpent() }}</div><div class="pf-cs-lbl">Total Spent</div></div>
          <div class="pf-card-stat c1"><div class="pf-cs-val">{{ addresses().length }}</div><div class="pf-cs-lbl">Addresses</div></div>
        </div>
        @if (orders().length > 0) {
          <div class="pf-card" style="margin-top:20px">
            <div class="pf-card-header">
              <span class="pf-card-title">Recent Orders</span>
              <button (click)="activeTab.set('orders')" class="pf-link">View all &#x2192;</button>
            </div>
            @for (o of orders().slice(0,3); track o.id) {
              <div class="pf-order-row">
                <div>
                  <p class="pf-order-id">#{{ o.id.slice(0,8).toUpperCase() }}</p>
                  <p class="pf-order-date">{{ o.createdAt | date:'dd MMM yyyy' }} &bull; {{ o.items.length }} item(s)</p>
                </div>
                <div class="pf-order-right">
                  <span class="pf-status" [class]="'pf-s-' + o.status.toLowerCase()">{{ o.status }}</span>
                  <span class="pf-order-amt">&#x20B9;{{ o.totalAmount.toFixed(2) }}</span>
                </div>
              </div>
            }
          </div>
        }
      }

      <!-- ORDERS -->
      @if (activeTab() === 'orders') {
        <div class="pf-section-title">Order History</div>
        <div class="pf-card">
          @if (ordersLoading()) {
            @for (i of [1,2,3]; track i) { <div class="pf-skel"></div> }
          } @else if (orders().length === 0) {
            <div class="pf-empty">No orders yet. <a routerLink="/products" class="pf-link">Start shopping</a></div>
          } @else {
            @for (o of orders(); track o.id) {
              <div class="pf-order-row">
                <div>
                  <p class="pf-order-id">#{{ o.id.slice(0,8).toUpperCase() }}</p>
                  <p class="pf-order-date">{{ o.createdAt | date:'dd MMM yyyy, HH:mm' }}</p>
                  <p class="pf-order-items">
                    @for (item of o.items; track item.productId) {
                      <span>{{ item.productName }} x{{ item.quantity }}</span>
                    }
                  </p>
                </div>
                <div class="pf-order-right">
                  <span class="pf-status" [class]="'pf-s-' + o.status.toLowerCase()">{{ o.status }}</span>
                  <span class="pf-order-amt">&#x20B9;{{ o.totalAmount.toFixed(2) }}</span>
                  <a [routerLink]="['/orders', o.id, 'track']" class="pf-link" style="font-size:11px">Track &#x2192;</a>
                </div>
              </div>
            }
          }
        </div>
      }

      <!-- WISHLIST -->
      @if (activeTab() === 'wishlist') {
        <div class="pf-section-title">Wishlist ({{ wishlistProducts().length }})</div>
        <div class="pf-wishlist-grid">
          @if (wishlistLoading()) {
            @for (i of [1,2,3,4]; track i) { <div class="pf-skel" style="height:180px;border-radius:14px"></div> }
          } @else if (wishlistProducts().length === 0) {
            <div class="pf-empty" style="grid-column:1/-1">Your wishlist is empty. <a routerLink="/products" class="pf-link">Browse products</a></div>
          } @else {
            @for (p of wishlistProducts(); track p.id) {
              <div class="pf-wish-card">
                <a [routerLink]="['/products', p.id]" class="pf-wish-img-wrap">
                  <img [src]="p.imageUrl" [alt]="p.name" class="pf-wish-img" />
                </a>
                <div class="pf-wish-body">
                  <p class="pf-wish-name">{{ p.name }}</p>
                  <p class="pf-wish-price">&#x20B9;{{ p.price.toFixed(2) }}</p>
                  <div class="pf-wish-actions">
                    <button (click)="addToCart(p)" class="pf-btn-cart">Add to cart</button>
                    <button (click)="removeWishlist(p.id)" class="pf-btn-remove">Remove</button>
                  </div>
                </div>
              </div>
            }
          }
        </div>
      }

      <!-- ADDRESSES -->
      @if (activeTab() === 'addresses') {
        <div class="pf-section-title">Saved Addresses</div>
        <div class="pf-card">
          <div class="pf-card-header">
            <span class="pf-card-title">Your Addresses</span>
            <button (click)="showAddressForm.set(!showAddressForm())" class="pf-btn-green">
              {{ showAddressForm() ? 'Cancel' : '+ Add Address' }}
            </button>
          </div>
          @if (showAddressForm()) {
            <div class="pf-addr-form">
              <div class="pf-form-grid">
                <div class="pf-fg"><label class="pf-label">Label</label><input class="pf-input" [(ngModel)]="newAddr.label" placeholder="Home, Work..." /></div>
                <div class="pf-fg"><label class="pf-label">Street</label><input class="pf-input" [(ngModel)]="newAddr.line1" placeholder="Street address" /></div>
                <div class="pf-fg"><label class="pf-label">City</label><input class="pf-input" [(ngModel)]="newAddr.city" placeholder="City" /></div>
                <div class="pf-fg"><label class="pf-label">State</label><input class="pf-input" [(ngModel)]="newAddr.state" placeholder="State" /></div>
                <div class="pf-fg"><label class="pf-label">ZIP</label><input class="pf-input" [(ngModel)]="newAddr.zip" placeholder="ZIP code" /></div>
                <div class="pf-fg pf-toggle-row"><label class="pf-toggle-label"><input type="checkbox" [(ngModel)]="newAddr.isDefault" /> Set as default</label></div>
              </div>
              <button (click)="saveAddress()" class="pf-btn-green" style="margin-top:12px">Save Address</button>
            </div>
          }
          @if (addresses().length === 0 && !showAddressForm()) {
            <div class="pf-empty">No saved addresses yet.</div>
          }
          @for (addr of addresses(); track addr.id) {
            <div class="pf-addr-row" [class.pf-addr-default]="addr.isDefault">
              <div>
                <div class="pf-addr-label">{{ addr.label }} @if (addr.isDefault) { <span class="pf-default-badge">Default</span> }</div>
                <p class="pf-addr-text">{{ addr.line1 }}, {{ addr.city }}, {{ addr.state }} {{ addr.zip }}</p>
              </div>
              <div class="pf-addr-actions">
                @if (!addr.isDefault) { <button (click)="setDefaultAddress(addr.id)" class="pf-link">Set default</button> }
                <button (click)="deleteAddress(addr.id)" class="pf-link-red">Delete</button>
              </div>
            </div>
          }
        </div>
      }

      <!-- SETTINGS -->
      @if (activeTab() === 'settings') {
        <div class="pf-section-title">Personal Information</div>
        <div class="pf-card">
          @if (profileSuccess()) { <div class="pf-alert-success">Profile updated successfully</div> }
          @if (profileError()) { <div class="pf-alert-error">{{ profileError() }}</div> }
          <div class="pf-form-grid">
            <div class="pf-fg"><label class="pf-label">First Name</label><input class="pf-input" [(ngModel)]="editFirst" /></div>
            <div class="pf-fg"><label class="pf-label">Last Name</label><input class="pf-input" [(ngModel)]="editLast" /></div>
            <div class="pf-fg"><label class="pf-label">Email</label><input class="pf-input" [value]="user()?.email" disabled /></div>
            <div class="pf-fg"><label class="pf-label">Phone</label><input class="pf-input" [(ngModel)]="editPhone" placeholder="+91 XXXXX XXXXX" /></div>
          </div>
          <button (click)="saveProfile()" [disabled]="profileSaving()" class="pf-btn-green" style="margin-top:16px">
            {{ profileSaving() ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
        <div class="pf-section-title" style="margin-top:20px">Change Password</div>
        <div class="pf-card">
          @if (pwSuccess()) { <div class="pf-alert-success">Password changed successfully</div> }
          @if (pwError()) { <div class="pf-alert-error">{{ pwError() }}</div> }
          <div class="pf-form-grid">
            <div class="pf-fg" style="grid-column:1/-1"><label class="pf-label">Current Password</label><input class="pf-input" type="password" [(ngModel)]="pwCurrent" /></div>
            <div class="pf-fg"><label class="pf-label">New Password</label><input class="pf-input" type="password" [(ngModel)]="pwNew" /></div>
            <div class="pf-fg"><label class="pf-label">Confirm Password</label><input class="pf-input" type="password" [(ngModel)]="pwConfirm" /></div>
          </div>
          <button (click)="changePassword()" [disabled]="pwSaving()" class="pf-btn-dark" style="margin-top:16px">
            {{ pwSaving() ? 'Updating...' : 'Update Password' }}
          </button>
        </div>
      }

      <!-- PRIVACY -->
      @if (activeTab() === 'privacy') {
        <div class="pf-section-title">Privacy & Notifications</div>
        <div class="pf-card">
          @for (pref of privacyPrefs(); track pref.key) {
            <div class="pf-pref-row">
              <div><p class="pf-pref-label">{{ pref.label }}</p><p class="pf-pref-desc">{{ pref.description }}</p></div>
              <button (click)="togglePref(pref.key)" class="pf-toggle" [class.pf-toggle-on]="pref.enabled">
                <span class="pf-toggle-thumb" [class.pf-toggle-thumb-on]="pref.enabled"></span>
              </button>
            </div>
          }
          <button (click)="savePrivacy()" class="pf-btn-green" style="margin-top:16px">Save Preferences</button>
        </div>
        <div class="pf-section-title pf-danger-title" style="margin-top:20px">Danger Zone</div>
        <div class="pf-card pf-danger-card">
          <div class="pf-danger-btns">
            <button (click)="clearWishlist()" class="pf-btn-warn">Clear Wishlist</button>
            <button (click)="confirmLogout()" class="pf-btn-danger">Sign Out All Devices</button>
          </div>
        </div>
      }

    </div>
  </div>

  @if (toast()) {
    <div class="pf-toast">{{ toast() }}</div>
  }
</div>
  `,
  styles: [`
    .pf-page { min-height:100vh; background:var(--adm-bg); color:var(--adm-text); }

    /* Hero */
    .pf-hero { position:relative; background:linear-gradient(135deg,#0f172a 0%,#1e3a5f 50%,#0f2d1a 100%); padding:36px 32px 28px; overflow:hidden; }
    .pf-hero-bg { position:absolute; inset:0; background:radial-gradient(ellipse at 70% 50%,rgba(34,197,94,.15) 0%,transparent 60%); pointer-events:none; }
    .pf-hero-content { position:relative; max-width:900px; margin:0 auto; display:flex; align-items:center; gap:24px; flex-wrap:wrap; }
    .pf-avatar-wrap { position:relative; flex-shrink:0; }
    .pf-avatar { width:88px; height:88px; border-radius:50%; overflow:hidden; background:linear-gradient(135deg,#22c55e,#16a34a); display:flex; align-items:center; justify-content:center; border:4px solid rgba(255,255,255,.2); box-shadow:0 8px 24px rgba(0,0,0,.3); }
    .pf-avatar-initials { font-size:28px; font-weight:800; color:#fff; }
    .pf-avatar-wrap:hover
    .pf-hero-info { flex:1; min-width:0; }
    .pf-hero-name { font-size:24px; font-weight:800; color:#fff; margin:0 0 4px; }
    .pf-hero-email { font-size:13px; color:rgba(255,255,255,.6); margin:0 0 8px; }
    .pf-role-badge { display:inline-block; padding:3px 12px; border-radius:20px; font-size:12px; font-weight:700; }
    .pf-role-customer { background:rgba(34,197,94,.2); color:#4ade80; border:1px solid rgba(34,197,94,.3); }
    .pf-role-admin { background:rgba(124,58,237,.2); color:#a78bfa; border:1px solid rgba(124,58,237,.3); }
    .pf-role-storemanager { background:rgba(59,130,246,.2); color:#60a5fa; border:1px solid rgba(59,130,246,.3); }
    .pf-role-deliverydriver { background:rgba(251,191,36,.2); color:#fbbf24; border:1px solid rgba(251,191,36,.3); }
    .pf-hero-stats { display:flex; align-items:center; gap:0; background:rgba(255,255,255,.08); border:1px solid rgba(255,255,255,.12); border-radius:14px; padding:14px 20px; gap:20px; }
    .pf-stat { display:flex; flex-direction:column; align-items:center; gap:2px; }
    .pf-stat-val { font-size:20px; font-weight:800; color:#fff; }
    .pf-stat-lbl { font-size:11px; color:rgba(255,255,255,.5); }
    .pf-stat-div { width:1px; height:32px; background:rgba(255,255,255,.15); }

    /* Body layout */
    .pf-body { max-width:900px; margin:0 auto; padding:24px 32px; display:flex; gap:20px; align-items:flex-start; }

    /* Sidebar */
    .pf-sidebar { width:180px; flex-shrink:0; background:var(--adm-card); border:1px solid var(--adm-border); border-radius:14px; padding:8px; display:flex; flex-direction:column; gap:2px; position:sticky; top:80px; }
    .pf-tab { display:flex;align-items:center; gap:10px; padding:10px 12px; border-radius:10px; border:none; background:none; color:var(--adm-text2); font-size:13px; font-weight:500; cursor:pointer; text-align:left; width:100%; transition:all .15s; }
    .pf-tab:hover { background:var(--adm-row-alt); color:var(--adm-text); }
    .pf-tab-active { background:linear-gradient(135deg,rgba(34,197,94,.15),rgba(34,197,94,.08)) !important; color:#16a34a !important; font-weight:700 !important; border:1px solid rgba(34,197,94,.2); }
    .pf-tab-icon { font-size:16px; width:20px; text-align:center; }

    /* Content */
    .pf-content { flex:1; min-width:0; display:flex; flex-direction:column; gap:0; }
    .pf-section-title { font-size:13px; font-weight:700; color:var(--adm-text3); text-transform:uppercase; letter-spacing:.07em; margin:0 0 12px; }
    .pf-danger-title { color:#dc2626; }

    /* Stat cards */
    .pf-stats-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; margin-bottom:4px; }
    .pf-card-stat { border-radius:12px; padding:18px; text-align:center; border:1px solid var(--adm-border); }
    .c1{background:var(--adm-s2);} .c2{background:var(--adm-s1);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s4);} .c5{background:var(--adm-s5);}
    .pf-cs-val { font-size:26px; font-weight:800; color:var(--adm-stat-val); }
    .pf-cs-lbl { font-size:11px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    /* Card */
    .pf-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:14px; padding:20px; }
    .pf-card-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:16px; }
    .pf-card-title { font-size:15px; font-weight:700; color:var(--adm-text); }
    .pf-link { color:#16a34a; font-size:13px; font-weight:600; text-decoration:none; cursor:pointer; background:none; border:none; }
    .pf-link:hover { text-decoration:underline; }
    .pf-link-red { color:#dc2626; font-size:12px; font-weight:600; background:none; border:none; cursor:pointer; }

    /* Order rows */
    .pf-order-row { display:flex; justify-content:space-between; align-items:flex-start; padding:12px 0; border-bottom:1px solid var(--adm-border); gap:12px; }
    .pf-order-row:last-child { border-bottom:none; }
    .pf-order-id { font-family:monospace; font-size:13px; font-weight:700; color:var(--adm-text); }
    .pf-order-date { font-size:11px; color:var(--adm-text2); margin-top:2px; }
    .pf-order-items { font-size:11px; color:var(--adm-text3); margin-top:4px; display:flex; flex-direction:column; gap:1px; }
    .pf-order-right { display:flex; flex-direction:column; align-items:flex-end; gap:4px; }
    .pf-order-amt { font-size:14px; font-weight:800; color:var(--adm-text); }
    .pf-status { padding:3px 10px; border-radius:20px; font-size:11px; font-weight:700; }
    .pf-s-delivered{background:rgba(34,197,94,.15);color:#15803d;} .pf-s-pending{background:rgba(251,191,36,.15);color:#b45309;} .pf-s-processing{background:rgba(59,130,246,.15);color:#1d4ed8;} .pf-s-shipped{background:rgba(139,92,246,.15);color:#6d28d9;} .pf-s-outfordelivery{background:rgba(249,115,22,.15);color:#c2410c;} .pf-s-cancelled{background:rgba(220,38,38,.15);color:#dc2626;}

    /* Wishlist */
    .pf-wishlist-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:14px; }
    .pf-wish-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:12px; overflow:hidden; }
    .pf-wish-img-wrap { display:block; height:130px; overflow:hidden; }
    .pf-wish-img { width:100%; height:100%; object-fit:cover; transition:transform .3s; }
    .pf-wish-card:hover .pf-wish-img { transform:scale(1.05); }
    .pf-wish-body { padding:12px; }
    .pf-wish-name { font-size:13px; font-weight:700; color:var(--adm-text); margin:0 0 4px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .pf-wish-price { font-size:14px; font-weight:800; color:#16a34a; margin:0 0 10px; }
    .pf-wish-actions { display:flex; gap:8px; }
    .pf-btn-cart { flex:1; background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:7px; border-radius:8px; font-size:12px; font-weight:700; cursor:pointer; }
    .pf-btn-remove { background:rgba(220,38,38,.1); color:#dc2626; border:1px solid rgba(220,38,38,.2); padding:7px 10px; border-radius:8px; font-size:12px; font-weight:600; cursor:pointer; }

    /* Address */
    .pf-addr-form { background:var(--adm-card2); border:1px solid var(--adm-border); border-radius:10px; padding:16px; margin-bottom:16px; }
    .pf-addr-row { display:flex; justify-content:space-between; align-items:flex-start; padding:14px 0; border-bottom:1px solid var(--adm-border); gap:12px; }
    .pf-addr-row:last-child { border-bottom:none; }
    .pf-addr-default { background:rgba(34,197,94,.05); border-radius:10px; padding:14px 12px; margin:-2px; }
    .pf-addr-label { font-size:14px; font-weight:700; color:var(--adm-text); margin-bottom:4px; display:flex; align-items:center; gap:8px; }
    .pf-addr-text { font-size:13px; color:var(--adm-text2); }
    .pf-addr-actions { display:flex; gap:12px; flex-shrink:0; }
    .pf-default-badge { background:rgba(34,197,94,.15); color:#15803d; font-size:10px; font-weight:700; padding:2px 8px; border-radius:20px; }

    /* Forms */
    .pf-form-grid { display:grid; grid-template-columns:1fr 1fr; gap:14px; }
    .pf-fg { display:flex; flex-direction:column; gap:5px; }
    .pf-label { font-size:11px; font-weight:700; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.05em; }
    .pf-input { background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:10px 13px; border-radius:8px; font-size:14px; width:100%; transition:border-color .2s; }
    .pf-input:focus { outline:none; border-color:#22c55e; box-shadow:0 0 0 3px rgba(34,197,94,.1); }
    .pf-input:disabled { opacity:.5; cursor:not-allowed; }
    .pf-toggle-row { display:flex; align-items:center; }
    .pf-toggle-label { display:flex; align-items:center; gap:8px; font-size:13px; color:var(--adm-text); cursor:pointer; }

    /* Buttons */
    .pf-btn-green { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 20px; border-radius:8px; font-size:14px; font-weight:700; cursor:pointer; box-shadow:0 4px 12px rgba(34,197,94,.25); transition:all .2s; }
    .pf-btn-green:hover { transform:translateY(-1px); }
    .pf-btn-green:disabled { opacity:.5; cursor:not-allowed; transform:none; }
    .pf-btn-dark { background:var(--adm-card2); color:var(--adm-text); border:1px solid var(--adm-border2); padding:10px 20px; border-radius:8px; font-size:14px; font-weight:700; cursor:pointer; transition:all .2s; }
    .pf-btn-dark:hover { background:var(--adm-border); }
    .pf-btn-warn { background:rgba(251,191,36,.15); color:#b45309; border:1px solid rgba(251,191,36,.3); padding:10px 20px; border-radius:8px; font-size:14px; font-weight:600; cursor:pointer; }
    .pf-btn-danger { background:rgba(220,38,38,.12); color:#dc2626; border:1px solid rgba(220,38,38,.3); padding:10px 20px; border-radius:8px; font-size:14px; font-weight:600; cursor:pointer; }
    .pf-danger-card { border-color:rgba(220,38,38,.2); }
    .pf-danger-btns { display:flex; gap:12px; flex-wrap:wrap; }

    /* Privacy toggles */
    .pf-pref-row { display:flex; justify-content:space-between; align-items:center; padding:14px 0; border-bottom:1px solid var(--adm-border); gap:16px; }
    .pf-pref-row:last-of-type { border-bottom:none; }
    .pf-pref-label { font-size:14px; font-weight:600; color:var(--adm-text); margin:0 0 3px; }
    .pf-pref-desc { font-size:12px; color:var(--adm-text2); margin:0; }
    .pf-toggle { width:48px; height:26px; border-radius:13px; background:var(--adm-border2); border:none; position:relative; cursor:pointer; transition:background .25s; flex-shrink:0; }
    .pf-toggle-on { background:linear-gradient(135deg,#22c55e,#16a34a); }
    .pf-toggle-thumb { position:absolute; top:3px; left:3px; width:20px; height:20px; border-radius:50%; background:#fff; transition:transform .25s; display:block; box-shadow:0 1px 4px rgba(0,0,0,.2); }
    .pf-toggle-thumb-on { transform:translateX(22px); }

    /* Misc */
    .pf-empty { text-align:center; padding:32px; color:var(--adm-text2); font-size:14px; }
    .pf-skel { height:60px; background:var(--adm-border); border-radius:8px; margin-bottom:10px; animation:pulse 1.5s infinite; }
    .pf-alert-success { background:rgba(34,197,94,.1); border:1px solid rgba(34,197,94,.3); color:#15803d; padding:10px 14px; border-radius:8px; font-size:13px; font-weight:600; margin-bottom:14px; }
    .pf-alert-error { background:rgba(220,38,38,.1); border:1px solid rgba(220,38,38,.3); color:#dc2626; padding:10px 14px; border-radius:8px; font-size:13px; font-weight:600; margin-bottom:14px; }
    .pf-toast { position:fixed; bottom:24px; right:24px; background:linear-gradient(135deg,#1e293b,#0f172a); color:#f1f5f9; padding:12px 20px; border-radius:12px; font-size:14px; font-weight:600; box-shadow:0 8px 24px rgba(0,0,0,.3); z-index:1000; border:1px solid #334155; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  `]
})
export class Profile implements OnInit {
  private auth = inject(AuthService);
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private wishlistService = inject(WishlistService);
  private cartService = inject(CartService);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  activeTab = signal<Tab>('overview');
  user = signal<User | null>(null);
  orders = signal<Order[]>([]);
  wishlistProducts = signal<Product[]>([]);
  ordersLoading = signal(true);
  wishlistLoading = signal(false);
  toast = signal('');

  initials = computed(() => {
    const u = this.user();
    return u ? `${u.firstName[0]}${u.lastName[0]}`.toUpperCase() : '?';
  });
  totalSpent = computed(() => this.orders().filter(o => o.status === 'Delivered').reduce((s, o) => s + o.totalAmount, 0).toFixed(2));
  deliveredCount = computed(() => this.orders().filter(o => o.status === 'Delivered').length);
  pendingCount = computed(() => this.orders().filter(o => ['Pending','Processing','Shipped','OutForDelivery'].includes(o.status)).length);
  wishlistCount = () => this.wishlistService.count;

  tabs: { id: Tab; label: string; icon: string }[] = [
    { id: 'overview',  label: 'Overview',   icon: 'O' },
    { id: 'orders',    label: 'Orders',     icon: 'O' },
    { id: 'wishlist',  label: 'Wishlist',   icon: 'W' },
    { id: 'addresses', label: 'Addresses',  icon: 'A' },
    { id: 'settings',  label: 'Settings',   icon: 'S' },
    { id: 'privacy',   label: 'Privacy',    icon: 'P' },
  ];

  editFirst = ''; editLast = ''; editPhone = '';
  profileSaving = signal(false); profileSuccess = signal(false); profileError = signal('');
  pwCurrent = ''; pwNew = ''; pwConfirm = '';
  pwSaving = signal(false); pwSuccess = signal(false); pwError = signal('');

  addresses = signal<Address[]>([]);
  showAddressForm = signal(false);
  newAddr = { label: '', line1: '', city: '', state: '', zip: '', isDefault: false };

  privacyPrefs = signal([
    { key: 'emailOrders',      label: 'Order Updates via Email',      description: 'Get notified when your order status changes.',       enabled: true  },
    { key: 'emailPromo',       label: 'Promotional Emails',           description: 'Receive deals, offers and new product alerts.',      enabled: false },
    { key: 'smsNotifications', label: 'SMS Notifications',            description: 'Receive delivery updates via SMS.',                  enabled: true  },
    { key: 'dataAnalytics',    label: 'Usage Analytics',              description: 'Help us improve by sharing anonymous usage data.',   enabled: true  },
    { key: 'personalizedAds',  label: 'Personalized Recommendations', description: 'See product recommendations based on your history.', enabled: true  },
  ]);

  ngOnInit() {
    this.auth.getProfile().subscribe({ next: u => {
      this.user.set(u);
      this.editFirst = u.firstName;
      this.editLast = u.lastName;
      this.editPhone = u.phoneNumber ?? '';
    }});
    this.orderService.getOrders().subscribe({ next: o => { this.orders.set(o); this.ordersLoading.set(false); }, error: () => this.ordersLoading.set(false) });
    this.loadAddresses();
    this.loadPrivacyPrefs();
    this.route.queryParams.subscribe(p => { if (p['tab']) { this.activeTab.set(p['tab'] as Tab); if (p['tab'] === 'wishlist') this.loadWishlist(); } });
  }

  selectTab(tab: Tab) {
    this.activeTab.set(tab);
    if (tab === 'wishlist') this.loadWishlist();
  }

  loadWishlist() {
    const ids = [...(this.wishlistService as any).ids()];
    if (ids.length === 0) return;
    this.wishlistLoading.set(true);
    let loaded: Product[] = []; let count = 0;
    ids.forEach((id: string) => {
      this.productService.getProduct(id).subscribe({
        next: p => { loaded.push(p); count++; if (count === ids.length) { this.wishlistProducts.set(loaded); this.wishlistLoading.set(false); } },
        error: () => { count++; if (count === ids.length) { this.wishlistProducts.set(loaded); this.wishlistLoading.set(false); } }
      });
    });
  }

  removeWishlist(id: string) {
    this.wishlistService.toggle(id);
    this.wishlistProducts.update(list => list.filter(p => p.id !== id));
    this.showToast('Removed from wishlist');
  }

  clearWishlist() {
    const ids = [...(this.wishlistService as any).ids()];
    ids.forEach((id: string) => this.wishlistService.toggle(id));
    this.wishlistProducts.set([]);
    this.showToast('Wishlist cleared');
  }

  addToCart(p: Product) {
    this.cartService.addItem(p.id, 1).subscribe({ next: () => this.showToast(`${p.name} added to cart`), error: () => this.showToast('Failed to add to cart') });
  }

  saveProfile() {
    this.profileSaving.set(true); this.profileSuccess.set(false); this.profileError.set('');
    this.http.put<{ user: User; accessToken: string }>(`${environment.apiUrl}/api/v1/auth/me`,
      { firstName: this.editFirst, lastName: this.editLast, phoneNumber: this.editPhone || null }
    ).subscribe({
      next: (res) => {
        this.user.set(res.user);
        localStorage.setItem('access_token', res.accessToken);
        this.profileSaving.set(false); this.profileSuccess.set(true);
        setTimeout(() => this.profileSuccess.set(false), 3000);
      },
      error: (e) => { this.profileError.set(e.error?.error ?? 'Failed to update profile'); this.profileSaving.set(false); }
    });
  }

  changePassword() {
    if (this.pwNew !== this.pwConfirm) { this.pwError.set('Passwords do not match'); return; }
    if (this.pwNew.length < 6) { this.pwError.set('Password must be at least 6 characters'); return; }
    this.pwSaving.set(true); this.pwSuccess.set(false); this.pwError.set('');
    this.http.post(`${environment.apiUrl}/api/v1/auth/change-password`,
      { currentPassword: this.pwCurrent, newPassword: this.pwNew }
    ).subscribe({
      next: () => { this.pwSaving.set(false); this.pwSuccess.set(true); this.pwCurrent = ''; this.pwNew = ''; this.pwConfirm = ''; setTimeout(() => this.pwSuccess.set(false), 3000); },
      error: (e) => { this.pwError.set(e.error?.error ?? 'Failed to change password'); this.pwSaving.set(false); }
    });
  }

  loadAddresses() {
    const saved = localStorage.getItem('user_addresses');
    if (saved) this.addresses.set(JSON.parse(saved));
  }

  saveAddress() {
    if (!this.newAddr.label || !this.newAddr.line1 || !this.newAddr.city) { this.showToast('Please fill required fields'); return; }
    const list = [...this.addresses()];
    if (this.newAddr.isDefault) list.forEach(a => a.isDefault = false);
    list.push({ ...this.newAddr, id: crypto.randomUUID() });
    this.addresses.set(list);
    localStorage.setItem('user_addresses', JSON.stringify(list));
    this.newAddr = { label: '', line1: '', city: '', state: '', zip: '', isDefault: false };
    this.showAddressForm.set(false);
    this.showToast('Address saved');
  }

  setDefaultAddress(id: string) {
    const list = this.addresses().map(a => ({ ...a, isDefault: a.id === id }));
    this.addresses.set(list); localStorage.setItem('user_addresses', JSON.stringify(list));
  }

  deleteAddress(id: string) {
    const list = this.addresses().filter(a => a.id !== id);
    this.addresses.set(list); localStorage.setItem('user_addresses', JSON.stringify(list));
    this.showToast('Address removed');
  }

  loadPrivacyPrefs() {
    const saved = localStorage.getItem('privacy_prefs');
    if (saved) {
      const map: Record<string, boolean> = JSON.parse(saved);
      this.privacyPrefs.update(prefs => prefs.map(p => ({ ...p, enabled: map[p.key] ?? p.enabled })));
    }
  }

  togglePref(key: string) {
    this.privacyPrefs.update(prefs => prefs.map(p => p.key === key ? { ...p, enabled: !p.enabled } : p));
  }

  savePrivacy() {
    const map: Record<string, boolean> = {};
    this.privacyPrefs().forEach(p => map[p.key] = p.enabled);
    localStorage.setItem('privacy_prefs', JSON.stringify(map));
    this.showToast('Preferences saved');
  }

  confirmLogout() { this.auth.logout(); }

  roleBadge() {
    const map: Record<string, string> = {
      Admin: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      StoreManager: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      DeliveryDriver: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Customer: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
    };
    return map[this.user()?.role ?? ''] ?? '';
  }

  roleLabel() {
    const map: Record<string, string> = { Admin: '[Admin]', StoreManager: '[Mgr]', DeliveryDriver: '[Driver]', Customer: '' };
    return map[this.user()?.role ?? ''] ?? '';
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Pending: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Processing: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      Shipped: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
      OutForDelivery: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      Delivered: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Cancelled: 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400',
    };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }

  private showToast(msg: string) { this.toast.set(msg); setTimeout(() => this.toast.set(''), 3000); }
}
