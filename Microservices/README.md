# FreshMart — Microservices

This folder contains all 7 backend projects: 6 domain microservices + 1 API Gateway + 1 shared models library.

---

## Services Overview

| Service | Port | Database | Responsibility |
|---------|------|----------|---------------|
| ApiGateway | 8080 | None | YARP reverse proxy — routes all `/api/v1/*` and `/hubs/*` traffic; validates JWT centrally |
| AuthService | 5001 | FreshMart_Auth | User registration, login, JWT issuance, refresh tokens, Google OAuth2, user management |
| ProductService | 5002 | FreshMart_Product | Product catalog, categories, stock, discounts, reviews |
| OrderService | 5003 | FreshMart_Order | Shopping cart, orders, coupons, checkout, order status |
| PaymentService | 5004 | FreshMart_Payment | Razorpay order creation, HMAC signature verification, webhook handling |
| NotificationService | 5005 | FreshMart_Notification | SignalR NotificationHub, in-app notifications, MailKit transactional emails |
| SupportService | 5006 | FreshMart_Support | Support tickets, SignalR SupportHub live chat |
| SharedModels | — | None | Shared event record types used in inter-service HTTP calls |

---

## Architecture

```
Browser / Angular SPA
        │
        ▼
  API Gateway :8080
  (YARP + JWT Validation)
        │
        ├──► AuthService        :5001  ──► FreshMart_Auth
        ├──► ProductService     :5002  ──► FreshMart_Product
        ├──► OrderService       :5003  ──► FreshMart_Order
        ├──► PaymentService     :5004  ──► FreshMart_Payment
        ├──► NotificationService:5005  ──► FreshMart_Notification
        └──► SupportService     :5006  ──► FreshMart_Support
                                              │
                                        SQL Server 2022 :1433
                                        (single instance, 6 isolated DBs)
```

### Inter-Service HTTP Calls

```
OrderService ──► PaymentService       POST /api/v1/payment/create-order
OrderService ──► NotificationService  POST /api/v1/notifications/internal/user
OrderService ──► NotificationService  POST /api/v1/notifications/internal/email/order-placed
OrderService ──► AuthService          GET  /api/v1/users/{customerId}
```

---

## Key Principles

- **Database-per-service** — each service has its own isolated SQL Server database. No cross-DB foreign keys.
- **Synchronous HTTP** — services call each other via typed `HttpClient`. No message broker in this version.
- **Shared JWT secret** — all services share the same `Jwt__Key` to verify tokens without calling AuthService.
- **Independent deployability** — each service has its own `Dockerfile` and can be built/deployed independently.
- **Local projections** — services that need data from another service maintain a local read-only copy (e.g. `OrderProjection` in ProductService, `AppUser` in each service).

---

## Folder Structure

```
Microservices/
├── ApiGateway/
│   ├── Program.cs                  # YARP config, JWT validation, CORS
│   ├── appsettings.json            # 12 YARP routes + 6 cluster addresses
│   ├── HttpVersionTransform.cs     # Downgrades HTTP/2 to HTTP/1.1
│   └── Dockerfile
│
├── AuthService/
│   ├── AppUser.cs                  # Entity: Id, Email, PasswordHash, Role, RefreshToken, GoogleId...
│   ├── AuthController.cs           # /auth/register, /auth/login, /auth/refresh, /auth/google, /auth/me
│   ├── UsersController.cs          # Admin: GET/PUT/PATCH/DELETE /users
│   ├── JwtService.cs               # GenerateAccessToken(), GenerateRefreshToken()
│   ├── AuthDtos.cs                 # RegisterRequest, LoginRequest, AuthResponse, UserDto...
│   ├── Data/AuthDbContext.cs       # EF Core DbContext → FreshMart_Auth
│   ├── Data/DbSeeder.cs            # Seeds 4 test users on startup
│   └── Dockerfile
│
├── ProductService/
│   ├── Product.cs                  # Entity: Id, Name, Price, Sku, CategoryId, DiscountPercent...
│   ├── Category.cs                 # Entity: Id, Name, ParentCategoryId (self-ref)
│   ├── Review.cs                   # Entity: Id, ProductId, CustomerId, Rating, Comment
│   ├── OrderProjection.cs          # Local copy of orders for verified-purchase review check
│   ├── ProductsController.cs       # GET/POST/PUT/DELETE /products + /on-sale /low-stock /suggestions
│   ├── CategoriesController.cs     # GET /categories
│   ├── ReviewsController.cs        # GET/POST /products/:id/reviews + /can-review
│   ├── Data/ProductDbContext.cs    # EF Core DbContext → FreshMart_Product
│   ├── Data/ProductSeeder.cs       # Seeds 8 categories + 50+ products on startup
│   └── Dockerfile
│
├── OrderService/
│   ├── Cart.cs / CartItem.cs       # Cart entity: CustomerId, BudgetLimit, Items
│   ├── Order.cs / OrderItem.cs     # Order entity: Status, SubTotal, DeliveryFee, TaxAmount...
│   ├── Coupon.cs                   # Coupon entity: Code, DiscountType, UsageLimit, UsedCount
│   ├── Product.cs                  # Local product cache for price calculation
│   ├── CartController.cs           # GET/POST/PUT/DELETE /cart + /cart/budget
│   ├── OrdersController.cs         # GET/POST /orders + /complete-payment + /status
│   ├── CouponsController.cs        # GET /coupons + POST /coupons/validate
│   ├── Services/
│   │   ├── PaymentServiceClient.cs # Typed HttpClient → PaymentService
│   │   ├── NotificationService.cs  # Typed HttpClient → NotificationService
│   │   └── ProductServiceClient.cs # Typed HttpClient → ProductService
│   ├── Data/OrderDbContext.cs      # EF Core DbContext → FreshMart_Order
│   ├── Data/OrderSeeder.cs         # Seeds 5 coupons on startup
│   └── Dockerfile
│
├── PaymentService/
│   ├── Models/Payment.cs           # Entity: Id, UserId, OrderId, RazorpayOrderId, Status (enum)...
│   ├── IPaymentService.cs          # Interface: CreatePaymentOrderAsync, VerifyPaymentAsync...
│   ├── PaymentController.cs        # POST /payment/create-order /verify /webhook; GET /payment/status
│   ├── PaymentDtos.cs              # CreatePaymentOrderRequest, VerifyPaymentRequest, PaymentStatusResponse
│   ├── Services/PaymentService.cs  # Implements IPaymentService — calls Razorpay REST API
│   ├── Services/OrderServiceClient.cs # Typed HttpClient → OrderService
│   ├── Data/PaymentDbContext.cs    # EF Core DbContext → FreshMart_Payment
│   └── Dockerfile
│
├── NotificationService/
│   ├── Notification.cs             # Entity: Id, UserId, Title, Message, Type, IsRead
│   ├── NotificationHub.cs          # SignalR Hub — groups: user:{userId}, role:{role}
│   ├── NotificationsController.cs  # GET /notifications + /unread-count + PATCH /read + DELETE
│   ├── InternalNotificationController.cs  # Internal endpoints called by other services (no JWT)
│   ├── NotificationService.cs      # Saves to DB + sends via SignalR
│   ├── EmailService.cs             # MailKit SMTP — 6 HTML email templates
│   ├── Data/NotificationDbContext.cs # EF Core DbContext → FreshMart_Notification
│   └── Dockerfile
│
├── SupportService/
│   ├── SupportTicket.cs            # Entity: Id, CustomerId, Subject, Category, Status, Priority
│   ├── SupportMessage.cs           # Entity: Id, TicketId, SenderId, SenderRole, IsStaff
│   ├── SupportHub.cs               # SignalR Hub — JoinTicket/LeaveTicket, newMessage, ticketUpdated
│   ├── SupportController.cs        # POST/GET /support/tickets + /messages + PATCH /status
│   ├── Data/SupportDbContext.cs    # EF Core DbContext → FreshMart_Support
│   └── Dockerfile
│
└── SharedModels/
    ├── Events.cs                   # OrderPlacedEvent, OrderStatusChangedEvent, PaymentCompletedEvent
    └── SharedModels.csproj         # Class library — no web dependencies
```

---

## API Gateway Routes

All routes defined in `ApiGateway/appsettings.json`:

| Route | URL Pattern | Target |
|-------|------------|--------|
| auth-route | `/api/v1/auth/**` | AuthService :5001 |
| users-route | `/api/v1/users/**` | AuthService :5001 |
| products-route | `/api/v1/products/**` | ProductService :5002 |
| categories-route | `/api/v1/categories/**` | ProductService :5002 |
| orders-route | `/api/v1/orders/**` | OrderService :5003 |
| cart-route | `/api/v1/cart/**` | OrderService :5003 |
| coupons-route | `/api/v1/coupons/**` | OrderService :5003 |
| payment-route | `/api/v1/payment/**` | PaymentService :5004 |
| notifications-route | `/api/v1/notifications/**` | NotificationService :5005 |
| notif-hub-route | `/hubs/notifications/**` | NotificationService :5005 |
| support-route | `/api/v1/support/**` | SupportService :5006 |
| support-hub-route | `/hubs/support/**` | SupportService :5006 |

---

## Run

From the **workspace root** (not this folder):

```bash
docker compose up --build
```

To run only the microservices stack (without frontend):

```bash
docker compose up --build sqlserver api-gateway auth-service product-service order-service payment-service notification-service support-service
```

---

## Swagger Docs

Each service exposes Swagger UI at `http://localhost:{port}/swagger`:

- AuthService: http://localhost:5001/swagger
- ProductService: http://localhost:5002/swagger
- OrderService: http://localhost:5003/swagger
- PaymentService: http://localhost:5004/swagger
- NotificationService: http://localhost:5005/swagger
- SupportService: http://localhost:5006/swagger
