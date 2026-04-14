import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Service responsible for managing the user's product wishlist.
 * The wishlist is stored client-side in localStorage as a set of product IDs,
 * so it persists across sessions without requiring authentication.
 * Uses a reactive signal so components update automatically when the wishlist changes.
 */
@Injectable({ providedIn: 'root' })
export class WishlistService {
  private platformId = inject(PLATFORM_ID);

  /**
   * Reactive signal holding the set of wishlisted product IDs.
   * Using a Set ensures O(1) lookup for `isWishlisted` checks.
   */
  private ids = signal<Set<string>>(new Set());

  /**
   * Initialises the wishlist by loading the persisted product IDs from localStorage.
   * No-op in SSR environments where localStorage is unavailable.
   */
  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem('wishlist');
      if (saved) this.ids.set(new Set(JSON.parse(saved)));
    }
  }

  /**
   * Checks whether a product is currently in the wishlist.
   * @param id - The product's unique identifier.
   * @returns True if the product is wishlisted, false otherwise.
   */
  isWishlisted(id: string) { return this.ids().has(id); }

  /**
   * Toggles a product's wishlist status.
   * Adds the product if not wishlisted; removes it if already wishlisted.
   * Persists the updated set to localStorage.
   * @param id - The product's unique identifier.
   */
  toggle(id: string) {
    const next = new Set(this.ids());
    next.has(id) ? next.delete(id) : next.add(id);
    this.ids.set(next);
    if (isPlatformBrowser(this.platformId))
      localStorage.setItem('wishlist', JSON.stringify([...next]));
  }

  /**
   * Returns the total number of products currently in the wishlist.
   * Used to display the wishlist item count in the navbar.
   */
  get count() { return this.ids().size; }
}
