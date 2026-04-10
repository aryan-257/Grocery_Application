# FreshMart — Frontend

Angular 21 SPA for the FreshMart online grocery delivery platform. Built with standalone components, lazy-loaded routes, Tailwind CSS, and Server-Side Rendering (SSR) support.

---

## Tech Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| Angular | 21 | SPA framework — standalone components, lazy loading |
| Tailwind CSS | v4 | Utility-first styling |
| TypeScript | 5.x | Type-safe development |
| Nginx | Alpine | Production static file server |
| Node.js | 20 | Build environment |

---

## Project Structure

```
Frontend/src/app/
├── core/
│   ├── guards/
│   │   ├── auth.guard.ts           # Redirects unauthenticated users to /auth/login
│   │   └── role.guard.ts           # Checks JWT role claim; redirects to /unauthorized
│   ├── interceptors/
│   │   └── auth.interceptor.ts     # Attaches JWT Bearer token to every HTTP request
│   │                               # Handles 401 → auto refresh token → retry
│   ├── models/
│   │   └── index.ts                # All TypeScript interfaces: User, Product, Cart,
│   │                               # Order, Payment, Notification, SupportTicket...
│   └── services/
│       ├── auth.service.ts         # Login, register, Google OAuth2, token refresh, logout
│       ├── cart.service.ts         # Cart CRUD, budget management
│       ├── product.service.ts      # Product listing, search, filters, suggestions
│       ├── order.service.ts        # Order creation, history, status tracking
│       ├── coupon.service.ts       # Coupon listing and validation
│       ├── notification.service.ts # Notification CRUD + SignalR connection
│       ├── invoice.service.ts      # Client-side invoice/receipt generation
│       ├── wishlist.service.ts     # Local wishlist (localStorage)
│       ├── comparison.service.ts   # Product comparison state
│       ├── recently-viewed.service.ts # Recently viewed products (localStorage)
│       ├── theme.service.ts        # Dark/light theme toggle
│       └── location.service.ts     # Delivery address management
│
├── shared/
│   └── components/
│       ├── navbar/                 # Top navigation — search, cart icon, notifications bell
│       ├── product-card/           # Reusable product card with discount badge
│       └── search-bar/             # Search with autocomplete suggestions
│
└── pages/
    ├── home/                       # Home page — featured products, categories
    ├── products/                   # Product listing — search, filters, sort, pagination
    ├── product-detail/             # Full product info, reviews, add to cart
    ├── cart/                       # Cart with budget tracker
    ├── checkout/                   # Delivery address, coupon, Razorpay payment
    ├── orders/                     # Order history
    ├── order-tracking/             # Real-time order status timeline
    ├── rate-order/                 # Submit product reviews after delivery
    ├── offers/                     # All discounted products
    ├── compare/                    # Side-by-side product comparison
    ├── profile/                    # Edit profile, change password
    ├── support/                    # Create ticket, view tickets, live chat
    ├── delivery/                   # Delivery driver — assigned orders, update status
    ├── store-manager/              # Store manager dashboard
    ├── admin/
    │   ├── dashboard/              # Platform stats
    │   ├── products/               # Product CRUD, stock, discounts
    │   ├── orders/                 # All orders, status updates
    │   ├── users/                  # User management, roles
    │   └── support/                # All tickets, reply, status
    ├── auth/
    │   ├── login/                  # Email/password + Google OAuth2
    │   └── register/               # New account creation
    └── unauthorized/               # Shown when role guard blocks access
```

---

## Routes

| Route | Component | Guard | Roles |
|-------|-----------|-------|-------|
| `/` | Home | — | All |
| `/products` | Products | — | All |
| `/products/:id` | ProductDetail | — | All |
| `/cart` | CartPage | authGuard | Authenticated |
| `/checkout` | Checkout | authGuard | Authenticated |
| `/orders` | Orders | authGuard | Authenticated |
| `/orders/:id/track` | OrderTracking | authGuard | Authenticated |
| `/orders/:id/rate` | RateOrder | authGuard | Authenticated |
| `/offers` | Offers | authGuard | Authenticated |
| `/profile` | Profile | authGuard | Authenticated |
| `/support` | Support | authGuard | Authenticated |
| `/compare` | Compare | — | All |
| `/admin/dashboard` | AdminDashboard | roleGuard | Admin |
| `/admin/products` | AdminProducts | roleGuard | Admin, StoreManager |
| `/admin/orders` | AdminOrders | roleGuard | Admin, StoreManager |
| `/admin/users` | AdminUsers | roleGuard | Admin |
| `/admin/support` | AdminSupport | roleGuard | Admin, StoreManager |
| `/manager/dashboard` | ManagerDashboard | roleGuard | StoreManager, Admin |
| `/delivery` | Delivery | roleGuard | DeliveryDriver, Admin |

---

## Development Setup

### Prerequisites
- Node.js 20+
- Angular CLI: `npm install -g @angular/cli`

### Install & run

```bash
cd Frontend
npm install
ng serve
```

App runs at `http://localhost:4200`. Make sure the API Gateway is running at `http://localhost:8080`.

### Build for production

```bash
ng build --configuration production
```

Output goes to `dist/Frontend/browser/`.

---

## Docker

The `Dockerfile` uses a multi-stage build:

1. **Stage 1 (build)** — Node 20 Alpine installs dependencies and runs `ng build --configuration production`
2. **Stage 2 (serve)** — Nginx Alpine copies the `dist/` output and serves it on port 80

```bash
# Build and run standalone (API Gateway must be accessible)
docker build -t freshmart-frontend .
docker run -p 80:80 freshmart-frontend
```

Or just use Docker Compose from the workspace root:

```bash
docker compose up --build
```

---

## Environment Configuration

The API base URL is configured in `src/environments/`:

```typescript
// environment.ts (development)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080',
};

// environment.prod.ts (production / Docker)
export const environment = {
  production: true,
  apiUrl: 'http://localhost:8080',
};
```

All HTTP calls go through the API Gateway at port 8080. The `AuthInterceptor` automatically attaches the JWT Bearer token to every request.

---

## SignalR Connections

The app connects to two SignalR hubs via the API Gateway:

```
ws://localhost:8080/hubs/notifications?access_token=<jwt>   # Real-time notifications
ws://localhost:8080/hubs/support?access_token=<jwt>         # Live support chat
```

JWT is passed as a query string parameter because WebSocket upgrade requests cannot carry Authorization headers.
