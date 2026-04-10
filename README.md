# FreshMart — Online Grocery Delivery Platform

A full-stack online grocery delivery application built with a **microservices architecture**. The backend is composed of **6 independent ASP.NET Core 10 microservices** behind a **YARP API Gateway**, with an **Angular 21** SPA frontend. Everything runs in **Docker** via a single `docker compose up --build`.

---

## Tech Stack

| Layer | Technology | Details |
|-------|-----------|---------|
| Frontend | Angular 21 + Tailwind CSS | Standalone components, lazy loading, SSR-ready |
| Web Server | Nginx (Alpine) | Serves Angular SPA on port 80 |
| API Gateway | ASP.NET Core 10 + YARP | Reverse proxy, JWT validation, CORS |
| Auth Service | ASP.NET Core 10 | JWT, BCrypt, Google OAuth2, refresh tokens |
| Product Service | ASP.NET Core 10 | Catalog, categories, reviews, stock |
| Order Service | ASP.NET Core 10 | Cart, orders, coupons, checkout |
| Payment Service | ASP.NET Core 10 | Razorpay integration, HMAC verification |
| Notification Service | ASP.NET Core 10 + SignalR + MailKit | Real-time push + transactional email |
| Support Service | ASP.NET Core 10 + SignalR | Ticketing system + live chat |
| Database | SQL Server 2022 | 6 isolated databases, one per service |
| ORM | Entity Framework Core 10 | Code-First, EnsureCreated on startup |
| Auth | JWT Bearer (HS256) | Shared secret across all services |
| Payments | Razorpay | INR — cards, UPI, netbanking, wallets |
| Email | MailKit + Gmail SMTP | 6 order lifecycle email templates |
| Containers | Docker + Docker Compose | 9 containers, one command to run |

---

## Architecture

```
Browser
   │
   ▼
Nginx :80  (Angular 21 SPA)
   │  /api/v1/*  /hubs/*
   ▼
API Gateway :8080  (YARP + JWT Validation)
   │
   ├──► AuthService        :5001  →  FreshMart_Auth        (SQL Server)
   ├──► ProductService     :5002  →  FreshMart_Product      (SQL Server)
   ├──► OrderService       :5003  →  FreshMart_Order        (SQL Server)
   ├──► PaymentService     :5004  →  FreshMart_Payment      (SQL Server)
   ├──► NotificationService:5005  →  FreshMart_Notification (SQL Server)
   └──► SupportService     :5006  →  FreshMart_Support      (SQL Server)

Inter-service HTTP calls:
  OrderService ──► PaymentService      (create Razorpay order)
  OrderService ──► NotificationService (send notifications + emails)
  OrderService ──► AuthService         (get customer email for status emails)

External integrations:
  PaymentService  ──► Razorpay API
  NotificationService ──► Gmail SMTP
  AuthService     ──► Google OAuth2
```

---

## Project Structure

```
FreshMart/
├── docker-compose.yml              # Orchestrates all 9 containers
├── Frontend/                       # Angular 21 SPA
│   ├── src/app/
│   │   ├── core/
│   │   │   ├── guards/             # AuthGuard, RoleGuard
│   │   │   ├── interceptors/       # AuthInterceptor (JWT attachment)
│   │   │   ├── models/             # TypeScript interfaces
│   │   │   └── services/           # AuthService, CartService, ProductService...
│   │   ├── pages/                  # home, products, cart, checkout, orders,
│   │   │   │                       # admin, support, delivery, profile...
│   │   └── shared/                 # Navbar, ProductCard, SearchBar
│   ├── Dockerfile                  # Multi-stage: Node build → Nginx serve
│   └── nginx.conf                  # SPA routing (try_files)
└── Microservices/
    ├── ApiGateway/                 # YARP reverse proxy + JWT validation
    ├── AuthService/                # Users, JWT, Google OAuth2
    ├── ProductService/             # Catalog, categories, reviews
    ├── OrderService/               # Cart, orders, coupons
    │   └── Services/               # PaymentServiceClient, NotificationService
    ├── PaymentService/             # Razorpay integration
    │   └── Services/               # PaymentService (implements IPaymentService)
    ├── NotificationService/        # SignalR hub + MailKit email
    ├── SupportService/             # Tickets + SignalR live chat
    └── SharedModels/               # Shared event contracts (DTOs)
```

---

## Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run everything

```bash
# From the workspace root (where docker-compose.yml is)
docker compose up --build
```

That's it. All 9 containers start in the correct order.

```bash
# Run in background
docker compose up --build -d

# Stop everything
docker compose down

# Stop and wipe all database data
docker compose down -v

# View logs
docker compose logs -f

# View logs for a specific service
docker compose logs -f order-service
```

### Access URLs

| Service | URL |
|---------|-----|
| Frontend (Angular) | http://localhost |
| API Gateway | http://localhost:8080 |
| AuthService Swagger | http://localhost:5001/swagger |
| ProductService Swagger | http://localhost:5002/swagger |
| OrderService Swagger | http://localhost:5003/swagger |
| PaymentService Swagger | http://localhost:5004/swagger |
| NotificationService Swagger | http://localhost:5005/swagger |
| SupportService Swagger | http://localhost:5006/swagger |

---

## Demo Credentials

| Role | Email | Password | Access |
|------|-------|----------|--------|
| Admin | admin@grocery.com | Admin@123 | Full platform access |
| Store Manager | manager@grocery.com | Manager@123 | Products, orders, support |
| Delivery Driver | driver@grocery.com | Driver@123 | Delivery orders only |
| Customer | customer@grocery.com | Customer@123 | Shopping, orders, support |

---

## Features

### Customer
- Browse 50+ products across 8 categories with search, filters, and sorting
- Product detail with reviews, ratings, and discount pricing
- Shopping cart with budget tracker and coupon codes
- Checkout with Razorpay payment (UPI, cards, netbanking)
- Order history and real-time order tracking
- Product comparison and recently viewed products
- Support ticket system with real-time live chat (SignalR)
- Real-time in-app notifications (bell icon)
- User profile with password change and Google OAuth2 login

### Store Manager
- Product CRUD — add, edit, soft-delete
- Stock management and discount management
- Order status updates
- Support ticket handling

### Delivery Driver
- View assigned orders (Shipped / OutForDelivery / Delivered)
- Update delivery status

### Admin
- Platform statistics dashboard
- Full user management — roles, activate/deactivate, delete
- All product, order, coupon, and support management
- Real-time notifications for new orders and support tickets

---

## API Reference

All endpoints are accessed via the API Gateway at `http://localhost:8080/api/v1`.

| Service | Key Endpoints |
|---------|--------------|
| Auth | `POST /auth/register` `POST /auth/login` `POST /auth/refresh` `POST /auth/google` `GET /auth/me` |
| Users | `GET /users` `PATCH /users/:id/role` `PATCH /users/:id/toggle-active` |
| Products | `GET /products` `GET /products/on-sale` `GET /products/low-stock` `POST /products` `PATCH /products/:id/stock` `PATCH /products/:id/discount` |
| Categories | `GET /categories` |
| Reviews | `GET /products/:id/reviews` `POST /products/:id/reviews` |
| Cart | `GET /cart` `POST /cart/items` `PUT /cart/items/:id` `DELETE /cart` `PUT /cart/budget` |
| Orders | `GET /orders` `POST /orders` `POST /orders/:id/complete-payment` `PATCH /orders/:id/status` |
| Coupons | `GET /coupons` `POST /coupons/validate` |
| Payment | `POST /payment/create-order` `POST /payment/verify` `POST /payment/webhook` |
| Notifications | `GET /notifications` `GET /notifications/unread-count` `PATCH /notifications/read-all` |
| Support | `GET /support/tickets` `POST /support/tickets` `POST /support/tickets/:id/messages` |

### SignalR Hubs (via API Gateway)
```
ws://localhost:8080/hubs/notifications   # Real-time notifications
ws://localhost:8080/hubs/support         # Live support chat
```
Both require JWT via query string: `?access_token=<jwt>`

---

## Business Rules

| Rule | Value |
|------|-------|
| Delivery fee | Free if order ≥ ₹500, else ₹49 |
| Tax | 5% of subtotal |
| JWT expiry | 1 hour (access token) |
| Refresh token | 7 days (server-side rotation) |
| Password hashing | BCrypt cost factor 12 |
| Low stock threshold | StockQuantity < 10 |
| Review limit | One per customer per product (verified purchase only) |
| Notification limit | Last 50 returned per user |

---

## Design Patterns

| Pattern | Where |
|---------|-------|
| API Gateway | `Microservices/ApiGateway/` — YARP routes all traffic |
| Dependency Injection | Every `Program.cs` — ASP.NET Core DI container |
| Interface Abstraction | `PaymentService/IPaymentService.cs` |
| Repository (DbContext) | `*/Data/*DbContext.cs` — EF Core per service |
| Service Client | `OrderService/Services/PaymentServiceClient.cs` etc. |
| Seeder | `*/Data/*Seeder.cs` — seeds users, products, coupons on startup |
| Snapshot | `OrderItem.ProductName` — snapshotted at order time |
| Projection | `OrderProjection.cs`, `AppUser.cs` per service — local read copies |
| Multi-stage Docker | Every `Dockerfile` — SDK build → runtime serve |

---

## Environment Variables

All configured via `docker-compose.yml`. Key variables:

| Variable | Used By | Description |
|----------|---------|-------------|
| `Jwt__Key` | All services | Shared JWT signing secret |
| `Jwt__Issuer` / `Jwt__Audience` | All services | JWT validation params |
| `ConnectionStrings__Default` | All services | SQL Server connection string |
| `Razorpay__KeyId` / `Razorpay__KeySecret` | PaymentService | Razorpay credentials |
| `Email__Host` / `Email__Username` / `Email__Password` | NotificationService | Gmail SMTP config |
| `Services__NotificationService` | OrderService | Internal service URL |
| `Services__PaymentService` | OrderService | Internal service URL |
| `Services__AuthService` | OrderService | Internal service URL |
| `Cors__AllowedOrigins__0` | ApiGateway | Allowed CORS origin |
