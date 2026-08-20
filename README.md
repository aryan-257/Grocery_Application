# FreshMart — Backend Microservices

Pure .NET 10 backend. No Docker, no RabbitMQ, no frontend. Just run and go.

---

## Architecture

```
Client / Postman
      │
      ▼
┌─────────────────────────────────┐
│  API Gateway  :8080             │  YARP reverse proxy + JWT validation
└────────┬────────────────────────┘
         │ routes by path prefix
    ┌────┴────────────────────────────────┐
    │                                     │
    ▼            ▼            ▼           ▼
AuthService  ProductService  OrderService  PaymentService
  :5001         :5002          :5003         :5004
  SQLite        SQLite         SQLite        SQLite
```

### Design Patterns
- **CQRS** (MediatR) — ProductService and OrderService separate reads (Queries) from writes (Commands)
- **API Gateway** (YARP) — single entry point, validates JWT, routes to downstream services
- **Repository via EF Core** — each service owns its own SQLite database
- **JWT Auth** — AuthService issues tokens; all other services validate them independently

---

## Microservices

| Service | Port | Swagger | Responsibility |
|---|---|---|---|
| **ApiGateway** | 8080 | http://localhost:8080/swagger | Routes all `/api/v1/*` traffic |
| **AuthService** | 5001 | http://localhost:5001/swagger | Register, login, JWT, user management |
| **ProductService** | 5002 | http://localhost:5002/swagger | Products, categories, reviews, stock, discounts |
| **OrderService** | 5003 | http://localhost:5003/swagger | Orders, shopping cart, coupons |
| **PaymentService** | 5004 | http://localhost:5004/swagger | Razorpay integration, payment lifecycle |

---

## Quick Start

Run each service in a separate terminal:

```bash
# Terminal 1
cd Microservices/AuthService && dotnet run

# Terminal 2
cd Microservices/ProductService && dotnet run

# Terminal 3
cd Microservices/OrderService && dotnet run

# Terminal 4
cd Microservices/PaymentService && dotnet run

# Terminal 5 (optional — gateway)
cd Microservices/ApiGateway && dotnet run
```

Each service auto-creates its SQLite database and seeds data on first run.

---

## Seed Credentials

| Role | Email | Password |
|---|---|---|
| Admin | aryandalal081@gmail.com | Admin@123 |
| StoreManager | manager@grocery.com | Manager@123 |
| DeliveryDriver | driver@grocery.com | Driver@123 |
| Customer | kajaldalal081@gmail.com | Customer@123 |

---

## API Overview

### Auth Service (`/api/v1/auth`)
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Register new customer |
| POST | `/login` | — | Login, returns JWT + refresh token |
| POST | `/refresh` | — | Renew access token |
| POST | `/logout` | ✓ | Invalidate refresh token |
| GET | `/me` | ✓ | Get own profile |
| PUT | `/me` | ✓ | Update profile |
| POST | `/change-password` | ✓ | Change password |

### Product Service (`/api/v1/products`, `/api/v1/categories`)
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/products` | — | Paginated product listing with filters |
| GET | `/products/{id}` | — | Single product |
| GET | `/products/suggestions` | — | Autocomplete (≥2 chars) |
| GET | `/products/on-sale` | — | Discounted products |
| GET | `/products/low-stock` | Admin/Manager | Products with stock < 10 |
| POST | `/products` | Admin/Manager | Create product |
| PUT | `/products/{id}` | Admin/Manager | Update product |
| DELETE | `/products/{id}` | Admin | Soft-delete product |
| PATCH | `/products/{id}/stock` | Admin/Manager | Update stock |
| PATCH | `/products/{id}/discount` | Admin/Manager | Set discount % |
| GET | `/products/{id}/reviews` | — | Product reviews |
| POST | `/products/{id}/reviews` | ✓ | Submit review |
| GET | `/categories` | — | All categories |

### Order Service (`/api/v1/orders`, `/api/v1/cart`, `/api/v1/coupons`)
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/orders` | ✓ | List orders (role-scoped) |
| GET | `/orders/{id}` | ✓ | Single order |
| POST | `/orders` | ✓ | Create order from cart |
| PATCH | `/orders/{id}/status` | Admin/Manager/Driver | Update order status |
| GET | `/cart` | ✓ | Get cart |
| POST | `/cart/items` | ✓ | Add item to cart |
| PUT | `/cart/items/{productId}` | ✓ | Update item quantity |
| DELETE | `/cart/items/{productId}` | ✓ | Remove item |
| DELETE | `/cart` | ✓ | Clear cart |
| PUT | `/cart/budget` | ✓ | Set budget limit |
| GET | `/coupons` | — | Active coupons |
| POST | `/coupons/validate` | ✓ | Validate coupon code |

### Payment Service (`/api/v1/payment`)
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/payment/create-order` | ✓ | Create Razorpay order |
| POST | `/payment/verify` | ✓ | Verify payment signature |
| GET | `/payment/{id}/status` | ✓ | Payment status by ID |
| GET | `/payment/order/{razorpayOrderId}/status` | ✓ | Status by Razorpay order ID |
| GET | `/payment/my-payments` | ✓ | User's payment history |
| POST | `/payment/webhook` | — | Razorpay webhook handler |

---

## CQRS Pattern (MediatR)

ProductService and OrderService implement CQRS via MediatR:

```
Controller → ISender.Send(Query/Command) → Handler → DbContext
```

**ProductService CQRS:**
- Queries: `GetProductsQuery`, `GetProductByIdQuery` → read-only
- Commands: `CreateProductCommand`, `UpdateProductCommand`, `DeleteProductCommand`, `UpdateStockCommand`, `UpdateDiscountCommand`

**OrderService CQRS:**
- Queries: `GetOrdersQuery`, `GetOrderByIdQuery` → read-only
- Commands: `CreateOrderCommand`, `UpdateOrderStatusCommand`

---

## Default Coupon Codes

| Code | Type | Value | Min Order |
|---|---|---|---|
| WELCOME10 | Percentage | 10% | ₹200 |
| SAVE50 | Fixed | ₹50 | ₹300 |
| FRESH20 | Percentage | 20% | ₹500 |
| FLAT100 | Fixed | ₹100 | ₹800 |
| NEWUSER15 | Percentage | 15% | ₹100 |
