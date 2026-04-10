import { Injectable, signal, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Product } from '../models';

const KEY = 'recently_viewed';
const MAX = 10;

@Injectable({ providedIn: 'root' })
export class RecentlyViewedService {
  private platformId = inject(PLATFORM_ID);
  private _items = signal<Product[]>(this.load());

  readonly items = this._items.asReadonly();

  track(product: Product) {
    if (!isPlatformBrowser(this.platformId)) return;
    const current = this._items().filter(p => p.id !== product.id);
    const updated = [product, ...current].slice(0, MAX);
    this._items.set(updated);
    try { localStorage.setItem(KEY, JSON.stringify(updated)); } catch {}
  }

  private load(): Product[] {
    try {
      if (typeof localStorage === 'undefined') return [];
      const raw = localStorage.getItem(KEY);
      if (!raw) return [];
      const parsed: Product[] = JSON.parse(raw);
      // Filter out any items with missing/invalid id
      return parsed.filter(p => p && p.id && typeof p.id === 'string' && p.id.length > 0);
    } catch { return []; }
  }
}
