import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../core/services/order.service';
import { Order } from '../../core/models';
import { environment } from '../../../environments/environment';

interface ItemRating { productId: string; productName: string; rating: number; comment: string; hover: number; submitted: boolean; }

@Component({
  selector: 'app-rate-order',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
<div class="rate-page">
  <div class="rate-container">

    <!-- Back -->
    <a routerLink="/orders" class="back-link">&#x2190; Back to Orders</a>

    <!-- Header -->
    <div class="rate-header">
      <div class="rate-header-icon">&#x2B50;</div>
      <div>
        <h1 class="rate-title">Rate Your Order</h1>
        <p class="rate-sub">Order #{{ orderId().slice(0,8).toUpperCase() }}</p>
      </div>
    </div>

    @if (loading()) {
      <div class="loading-card">
        @for (i of [1,2]; track i) { <div class="skel"></div> }
      </div>
    } @else if (allSubmitted()) {
      <div class="success-card">
        <div class="success-icon">&#x1F389;</div>
        <h2 class="success-title">Thank you for your reviews!</h2>
        <p class="success-sub">Your feedback helps other customers make better choices.</p>
        <a routerLink="/orders" class="btn-back-orders">Back to Orders</a>
      </div>
    } @else {
      @for (item of items(); track item.productId) {
        <div class="item-card" [class.item-done]="item.submitted">
          <div class="item-header">
            <span class="item-name">{{ item.productName }}</span>
            @if (item.submitted) {
              <span class="submitted-badge">&#x2705; Reviewed</span>
            }
          </div>

          @if (!item.submitted) {
            <!-- Stars -->
            <div class="stars-row">
              @for (n of [1,2,3,4,5]; track n) {
                <button class="star-btn"
                  (click)="item.rating = n"
                  (mouseenter)="item.hover = n"
                  (mouseleave)="item.hover = 0"
                  [style.color]="n <= (item.hover || item.rating) ? '#f59e0b' : '#d1d5db'">
                  &#9733;
                </button>
              }
              <span class="rating-label">{{ ratingLabel(item.rating) }}</span>
            </div>

            <!-- Comment -->
            <textarea class="comment-box" [(ngModel)]="item.comment" rows="3"
              placeholder="Share your experience with {{ item.productName }}..."></textarea>

            <button class="btn-submit" (click)="submitItem(item)"
              [disabled]="item.rating === 0 || submitting() === item.productId">
              {{ submitting() === item.productId ? 'Submitting...' : 'Submit Review' }}
            </button>

            @if (errors()[item.productId]) {
              <p class="item-error">{{ errors()[item.productId] }}</p>
            }
          }
        </div>
      }
    }
  </div>
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .rate-page { min-height:100vh; background:var(--adm-bg); padding:32px 16px; }
    .rate-container { max-width:640px; margin:0 auto; display:flex; flex-direction:column; gap:20px; }

    .back-link { color:#16a34a; font-size:14px; font-weight:600; text-decoration:none; display:inline-flex; align-items:center; gap:6px; }
    .back-link:hover { text-decoration:underline; }

    .rate-header { display:flex; align-items:center; gap:16px; background:linear-gradient(135deg,#0f172a,#1e3a5f); border-radius:16px; padding:24px; border:1px solid var(--adm-border); }
    .rate-header-icon { font-size:36px; }
    .rate-title { font-size:22px; font-weight:800; color:#f1f5f9; margin:0; }
    .rate-sub { font-size:13px; color:rgba(255,255,255,.5); margin:4px 0 0; font-family:monospace; }

    .item-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; padding:24px; display:flex; flex-direction:column; gap:16px; }
    .item-done { opacity:.7; }
    .item-header { display:flex; justify-content:space-between; align-items:center; }
    .item-name { font-size:16px; font-weight:700; color:var(--adm-text); }
    .submitted-badge { background:rgba(34,197,94,.15); color:#15803d; border:1px solid rgba(34,197,94,.3); font-size:12px; font-weight:700; padding:4px 12px; border-radius:20px; }

    .stars-row { display:flex; align-items:center; gap:4px; }
    .star-btn { font-size:36px; background:none; border:none; cursor:pointer; padding:0; line-height:1; transition:transform .15s; }
    .star-btn:hover { transform:scale(1.2); }
    .rating-label { font-size:14px; font-weight:600; color:var(--adm-text2); margin-left:10px; }

    .comment-box { width:100%; background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:12px 14px; border-radius:10px; font-size:14px; resize:vertical; min-height:90px; transition:border-color .2s; font-family:inherit; }
    .comment-box:focus { outline:none; border-color:#22c55e; box-shadow:0 0 0 3px rgba(34,197,94,.1); }
    .comment-box::placeholder { color:var(--adm-text3); }

    .btn-submit { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:12px 28px; border-radius:10px; font-size:14px; font-weight:700; cursor:pointer; box-shadow:0 4px 12px rgba(34,197,94,.3); transition:all .2s; align-self:flex-start; }
    .btn-submit:hover:not(:disabled) { transform:translateY(-1px); box-shadow:0 6px 16px rgba(34,197,94,.4); }
    .btn-submit:disabled { opacity:.5; cursor:not-allowed; transform:none; }
    .item-error { color:#dc2626; font-size:13px; font-weight:600; margin:0; }

    .success-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; padding:48px 24px; text-align:center; display:flex; flex-direction:column; align-items:center; gap:12px; }
    .success-icon { font-size:56px; }
    .success-title { font-size:22px; font-weight:800; color:var(--adm-text); margin:0; }
    .success-sub { font-size:14px; color:var(--adm-text2); margin:0; }
    .btn-back-orders { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; text-decoration:none; padding:12px 28px; border-radius:10px; font-size:14px; font-weight:700; margin-top:8px; }

    .loading-card { display:flex; flex-direction:column; gap:16px; }
    .skel { height:200px; background:var(--adm-card); border-radius:16px; border:1px solid var(--adm-border); animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  `]
})
export class RateOrder implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private orderService = inject(OrderService);

  orderId = signal('');
  items = signal<ItemRating[]>([]);
  loading = signal(true);
  submitting = signal('');
  errors = signal<Record<string, string>>({});

  allSubmitted = () => this.items().length > 0 && this.items().every(i => i.submitted);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.orderId.set(id);
    this.orderService.getOrders().subscribe({
      next: orders => {
        const order = orders.find((o: Order) => o.id === id);
        if (!order) { this.router.navigate(['/orders']); return; }
        this.items.set(order.items.map(i => ({
          productId: i.productId, productName: i.productName,
          rating: 5, comment: '', hover: 0, submitted: false
        })));
        this.loading.set(false);
      },
      error: () => this.router.navigate(['/orders'])
    });
  }

  ratingLabel(r: number): string {
    return ['', 'Poor', 'Fair', 'Good', 'Very Good', 'Excellent'][r] ?? '';
  }

  submitItem(item: ItemRating) {
    if (item.rating === 0) return;
    this.submitting.set(item.productId);
    this.errors.update(e => ({ ...e, [item.productId]: '' }));

    this.http.post(`${environment.apiUrl}/api/v1/products/${item.productId}/reviews`, {
      rating: item.rating, comment: item.comment || `Rated ${item.rating}/5`
    }).subscribe({
      next: () => {
        this.items.update(list => list.map(i => i.productId === item.productId ? { ...i, submitted: true } : i));
        this.submitting.set('');
      },
      error: (e) => {
        this.errors.update(err => ({ ...err, [item.productId]: e?.error?.error ?? 'Failed to submit review' }));
        this.submitting.set('');
      }
    });
  }
}
