import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category, PaginatedResult, Product } from '../models';
import { environment } from '../../../environments/environment';

/**
 * Parameters for filtering and sorting the product catalogue.
 * All fields are optional — omitted fields are not sent to the backend.
 */
export interface ProductSearchParams {
  /** Full-text search query matched against name, description, brand, SKU, and category. */
  query?: string;
  /** Filter products to a specific category by its ID. */
  categoryId?: string;
  /** Minimum price filter in INR (inclusive). */
  minPrice?: number;
  /** Maximum price filter in INR (inclusive). */
  maxPrice?: number;
  /** Sort order: `price_asc`, `price_desc`, `rating`, or default (alphabetical). */
  sortBy?: string;
  /** Page number for pagination (1-indexed). Defaults to 1. */
  page?: number;
  /** Number of products per page. Defaults to 20. */
  pageSize?: number;
}

/**
 * Service responsible for all product catalogue operations.
 * Communicates with the ProductService API for browsing, searching,
 * and managing products and categories.
 * Write operations (create, update, delete, stock, discount) require Admin or StoreManager role.
 */
@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1`;

  /**
   * Returns a paginated list of active products with optional filtering and sorting.
   * Supports full-text search across name, description, brand, SKU, and category.
   * @param params - Optional search, filter, sort, and pagination parameters.
   * @returns Observable of a paginated result containing products and total count.
   */
  getProducts(params: ProductSearchParams = {}): Observable<PaginatedResult<Product>> {
    let httpParams = new HttpParams();
    if (params.query) httpParams = httpParams.set('query', params.query);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.minPrice != null) httpParams = httpParams.set('minPrice', params.minPrice);
    if (params.maxPrice != null) httpParams = httpParams.set('maxPrice', params.maxPrice);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    httpParams = httpParams.set('page', params.page ?? 1);
    httpParams = httpParams.set('pageSize', params.pageSize ?? 20);

    return this.http.get<PaginatedResult<Product>>(`${this.baseUrl}/products`, { params: httpParams });
  }

  /**
   * Returns the full details of a single product by its ID.
   * @param id - The product's unique identifier.
   * @returns Observable of the Product object.
   */
  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/products/${id}`);
  }

  /**
   * Returns all product categories for building the navigation menu and filter panel.
   * @returns Observable of an array of Category objects.
   */
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  /**
   * Returns all products with fewer than 10 units in stock.
   * Used by the Admin/StoreManager dashboard to identify items needing restocking.
   * Requires Admin or StoreManager role.
   * @returns Observable of an array of low-stock Product objects.
   */
  getLowStockProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products/low-stock`);
  }

  /**
   * Creates a new product in the catalogue.
   * Requires Admin or StoreManager role.
   * @param data - Product creation data including name, price, SKU, category, and stock.
   * @returns Observable of the created Product.
   */
  createProduct(data: {
    name: string; description: string; price: number; sku: string;
    imageUrl: string; categoryId: string; stockQuantity: number;
    brand?: string; unit?: string;
  }): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/products`, data);
  }

  /**
   * Updates the stock quantity for a product to the specified absolute value.
   * Requires Admin or StoreManager role.
   * @param id - The product's unique identifier.
   * @param quantity - New absolute stock quantity.
   * @returns Observable that completes when the stock is updated.
   */
  updateStock(id: string, quantity: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/products/${id}/stock`, { quantity });
  }

  /**
   * Sets the discount percentage for a product (0–100).
   * A value of 0 removes any active discount.
   * Requires Admin or StoreManager role.
   * @param id - The product's unique identifier.
   * @param discountPercent - Discount percentage between 0 and 100.
   * @returns Observable that completes when the discount is updated.
   */
  updateDiscount(id: string, discountPercent: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/products/${id}/discount`, { discountPercent });
  }

  /**
   * Returns all active products that currently have a discount applied, ordered by highest discount first.
   * Used to populate the Offers / On Sale page.
   * @returns Observable of an array of discounted Product objects.
   */
  getOnSale(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products/on-sale`);
  }

  /**
   * Fully updates an existing product's details.
   * Requires Admin or StoreManager role.
   * @param id - The product's unique identifier.
   * @param data - Updated product data including all editable fields.
   * @returns Observable of the updated Product.
   */
  updateProduct(id: string, data: {
    name: string; description: string; price: number; sku: string;
    imageUrl: string; categoryId: string; stockQuantity: number;
    brand?: string; unit?: string; discountPercent: number; isActive: boolean;
  }): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/products/${id}`, data);
  }

  /**
   * Soft-deletes a product by setting it as inactive.
   * The product is hidden from public listings but its data is preserved.
   * Requires Admin role.
   * @param id - The product's unique identifier.
   * @returns Observable that completes when the product is deleted.
   */
  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/products/${id}`);
  }

  /**
   * Returns up to 6 lightweight product suggestions for the search autocomplete dropdown.
   * Matches against product name, brand, and category name.
   * Returns an empty array for queries shorter than 2 characters.
   * @param q - The search query string (minimum 2 characters).
   * @returns Observable of an array of suggestion objects with id, name, imageUrl, categoryName, and price.
   */
  getSuggestions(q: string): Observable<{ id: string; name: string; imageUrl: string; categoryName: string; price: number }[]> {
    return this.http.get<any[]>(`${this.baseUrl}/products/suggestions`, { params: { q } });
  }
}
