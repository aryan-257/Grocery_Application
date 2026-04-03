import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [RouterLink],
  template: `
<div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-2xl overflow-hidden flex flex-col transition-all duration-200 hover:shadow-xl hover:-translate-y-1">
  <a [routerLink]="['/products', product.id]" class="flex flex-col flex-1" style="text-decoration:none">
    <div class="relative overflow-hidden bg-gray-50 dark:bg-gray-800" style="height:180px">
      <img [src]="product.imageUrl" [alt]="product.name" loading="lazy" class="w-full h-full object-cover transition-transform duration-300 hover:scale-105" />
      @if (product.discountPercent > 0) {
        <div class="disc-ribbon">
          <span>{{ product.discountPercent }}%</span>
          <span>OFF</span>
        </div>
      }
      @if (isBestseller && product.discountPercent === 0) {
        <span class="bestseller-badge">Bestseller</span>
      }
      @if (product.stockQuantity > 0 && product.stockQuantity < 10 && product.discountPercent === 0 && !isBestseller) {
        <span class="absolute top-2 left-2 bg-amber-500 text-white text-xs font-bold px-2 py-0.5 rounded-full">Low stock</span>
      }
      @if (product.stockQuantity === 0) {
        <div class="absolute inset-0 bg-black/50 flex items-center justify-center">
          <span class="text-white text-sm font-semibold">Out of stock</span>
        </div>
      }
    </div>
    <div class="p-4 flex-1 flex flex-col">
      <p class="text-xs font-bold text-green-600 dark:text-green-400 uppercase tracking-wider mb-1">{{ product.categoryName }}</p>
      <h3 class="text-sm font-bold text-gray-900 dark:text-white leading-snug line-clamp-2">{{ product.name }}</h3>
      @if (product.brand) { <p class="text-xs text-gray-400 mt-0.5">{{ product.brand }}</p> }
      <div class="flex items-center justify-between mt-auto pt-3">
        @if (product.discountPercent > 0) {
          <div class="flex items-baseline gap-1.5">
            <span class="text-base font-extrabold text-red-600 dark:text-red-400">&#x20B9;{{ product.discountedPrice.toFixed(2) }}</span>
            <span class="text-xs text-gray-400 line-through">&#x20B9;{{ product.price.toFixed(2) }}</span>
          </div>
        } @else {
          <span class="text-base font-extrabold text-gray-900 dark:text-white">&#x20B9;{{ product.price.toFixed(2) }}</span>
        }
        <span class="text-xs text-gray-500 bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded-full">{{ product.unit }}</span>
      </div>
      <div class="flex items-center gap-1 text-xs text-amber-500 font-semibold mt-1">
        <span>&#x2605;</span><span>{{ product.averageRating.toFixed(1) }}</span>
      </div>
    </div>
  </a>
  <div class="px-4 pb-4">
    <button (click)="addToCart.emit(product)" [disabled]="product.stockQuantity === 0"
      class="w-full bg-green-600 hover:bg-green-700 disabled:bg-gray-200 dark:disabled:bg-gray-700 disabled:cursor-not-allowed text-white disabled:text-gray-400 py-2.5 rounded-xl text-sm font-bold transition shadow-sm hover:shadow-md">
      {{ product.stockQuantity === 0 ? 'Out of stock' : 'Add to cart' }}
    </button>
  </div>
</div>
  `,
  styles: [`
    .disc-ribbon {
      position: absolute; top: 0; left: 0;
      background: linear-gradient(135deg, #2563eb, #1d4ed8);
      color: #fff; width: 52px; padding: 6px 4px 8px;
      text-align: center; font-size: 12px; font-weight: 800; line-height: 1.2;
      clip-path: polygon(0 0, 100% 0, 100% 85%, 50% 100%, 0 85%);
      display: flex; flex-direction: column; align-items: center;
      box-shadow: 2px 2px 8px rgba(0,0,0,.25);
    }
    .bestseller-badge {
      position: absolute; top: 10px; left: 0;
      background: linear-gradient(135deg, #f59e0b, #d97706);
      color: #fff; font-size: 10px; font-weight: 800;
      padding: 4px 10px 4px 8px; border-radius: 0 20px 20px 0;
      text-transform: uppercase; letter-spacing: .04em;
      box-shadow: 2px 2px 8px rgba(0,0,0,.2);
    }
  `]
})
export class ProductCard {
  @Input({ required: true }) product!: Product;
  @Output() addToCart = new EventEmitter<Product>();

  private static readonly BESTSELLER_IDS = new Set([
    '2c82054b-d93d-49f3-b709-af3df26e0a3f',
    '8183906d-c35f-4b60-99f9-2c5880b5513f',
    '01b9b052-b573-4b16-a3c8-a2e446390c2d',
    '0dee43bb-2007-46b7-9ec8-13f789d1cd5c',
    '2c04b085-eba3-424a-b994-3b5e6f3a8d27',
    '3fd93963-1525-44be-8f28-64ae18cd1341',
    '92faa99c-ecfc-43f3-940b-098cf633e7ac',
    '37181d83-ff02-45c4-9f11-fd2e738f5cec',
    'eb930c4c-fed3-4193-b69f-efc1b19eba6b',
    '5cabf50b-bbdc-4205-880a-f4b731c91c88',
    '5bbc92ca-efd9-43c1-8243-0b8ed51b15c9',
    '9bd0a852-99a5-4e07-b991-2206f324180f',
  ]);

  get isBestseller(): boolean {
    return ProductCard.BESTSELLER_IDS.has(this.product.id);
  }
}
