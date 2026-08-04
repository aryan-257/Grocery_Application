# Case Study: FreshMart — Grocery Delivery & Smart Cart Platform

---

## 1. Executive Summary

FreshMart is a full-stack, cloud-native grocery delivery platform built on a microservices architecture. It enables customers to browse a live product catalogue, manage a smart shopping cart with budget controls, apply coupon discounts, pay securely via Razorpay, and track orders through their full lifecycle — all in real time. The platform supports four distinct user roles (Customer, Admin, StoreManager, DeliveryDriver) and is designed for independent scalability, fault isolation, and continuous deployment using Docker containers.

---

## 2. Problem Statement

Traditional grocery shopping involves physical store visits, limited product visibility, no price comparison, and no budget tracking. Existing online grocery platforms often suffer from:

- **Monolithic architectures** that are hard to scale and maintain
- **No real-time feedback** on order status or stock levels
- **No smart cart features** like budget limits or discount previews
- **Poor role separation** — admins, managers, drivers, and customers share the same interface
- **No event-driven communication** between services, leading to tight coupling

FreshMart was designed to solve all of these problems with a modern, decoupled, event-driven system.

---

## 3. Project Objectives

| Objective | Implementation |
|---|---|
| Enable online grocery browsing and ordering | Angular 21 SPA with product catalogue, search, and filters |
| Smart cart with budget awareness | Cart service with `BudgetLimit`, `isOverBudget` flag, and real-time totals |
| Secure payment processing | Razorpay integration with HMAC-SHA256 webhook verification |
| Real-time order & notification updates | SignalR hub with user-specific and role-based groups |
| Role-based access control | JWT with `Admin`, `StoreManager`, `DeliveryDriver`, `Customer` roles |
| Decoupled microservices | 6 independent .NET services communicating via RabbitMQ events |
| Containerised deployment | Docker Compose orchestrating all 10 services |

---

## 4. System Architecture

### 4.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Angular Frontend (Port 80)              │
│   Home · Products · Cart · Checkout · Orders · Support      │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────────┐
│               API Gateway — YARP Reverse Proxy (Port 8080)  │
│         JWT Validation · Correlation ID · Route Forwarding  │
└──┬──────────┬──────────┬──────────┬──────────┬─────────────┘
   │          │          │          │          │
   ▼          ▼          ▼          ▼          ▼
 Auth      Product    Order     Payment   Notification  Support
:5001      :5002      :5003      :5004      :5005       :5006
   │          │          │          │          │
   └──────────┴──────────┴──────────┴──────────┘
                          │
                    ┌─────▼──────┐
                    │  RabbitMQ  │  (Event Bus)
                    └─────┬──────┘
                          │
                    ┌─────▼──────┐
                    │ SQL Server │  (Per-service databases)
                    └────────────┘
```

### 4.2 Microservices Breakdown

| Service | Port | Responsibility |
|---|---|---|
| **AuthService** | 5001 | Registration, login, JWT issuance, user management |
| **ProductService** | 5002 | Product catalogue, categories, reviews, stock, discounts |
| **OrderService** | 5003 | Cart management, order lifecycle, coupon validation |
| **PaymentService** | 5004 | Razorpay order creation, payment verification, webhook handling |
| **NotificationService** | 5005 | Email (SMTP), in-app notifications, SignalR real-time push |
| **SupportService** | 5006 | Customer support tickets, real-time chat via SignalR |
| **ApiGateway** | 8080 | YARP reverse proxy, JWT validation, CORS, correlation IDs |

---

## 5. Core Features & Technical Deep Dive

### 5.1 Smart Cart System

The cart is one of FreshMart's most sophisticated features. It lives in the **OrderService** and goes beyond a simple item list.

**Key capabilities:**
- **Budget Limit** — Customers can set a spending cap (`BudgetLimit`). The cart computes `isOverBudget` in real time and warns the user before checkout.
- **Live Discount Computation** — When a product has a `DiscountPercent`, the cart automatically computes the discounted unit price: `price × (1 - discount/100)`.
- **Product Data Caching** — On `AddItem`, the OrderService fetches the latest product data from ProductService and caches it locally. This avoids repeated cross-service calls for every cart render while keeping prices fresh.
- **Stock Validation** — Before adding an item, the service checks `StockQuantity` against the requested quantity and rejects the request if stock is insufficient.
- **Coupon Integration** — Customers can apply coupon codes at checkout. The `CouponsController` validates the code, discount type (percentage or flat), minimum order amount, and expiry.

```
Cart DTO Structure:
{
  customerId, items[], budgetLimit,
  subTotal, isOverBudget, totalItems
}

CartItem:
{
  productId, productName, unitPrice (discounted),
  originalPrice, discountPercent, quantity, totalPrice
}
```

### 5.2 Product Catalogue & Search

The **ProductService** exposes a rich, paginated product API:

- **Full-text search** across `Name`, `Description`, `Brand`, `SKU`, and `CategoryName`
- **Filtering** by `categoryId`, `minPrice`, `maxPrice`
- **Sorting** by `price_asc`, `price_desc`, `rating`, or default alphabetical
- **Autocomplete suggestions** — returns up to 6 lightweight results for the search bar dropdown (triggers at 2+ characters)
- **On-Sale endpoint** — returns all products with `DiscountPercent > 0`, ordered by highest discount
- **Low-stock alerts** — admin/manager endpoint returning products with fewer than 10 units
- **Soft delete** — products are deactivated (`IsActive = false`) rather than hard-deleted, preserving order history

**Product model fields:** `Id`, `Name`, `Description`, `Price`, `SKU`, `ImageUrl`, `CategoryId`, `StockQuantity`, `Brand`, `Unit`, `DiscountPercent`, `AverageRating`, `IsActive`

### 5.3 Order Lifecycle & Event-Driven Flow

Orders follow a well-defined state machine:

```
Pending → PaymentPending → PaymentConfirmed → Processing
       → Shipped → OutForDelivery → Delivered
       → Cancelled / Refunded / PaymentFailed
```

**Event flow when an order is placed:**

```
Customer checks out
       │
       ▼
OrderService creates Order (status: Pending)
       │
       ▼
PaymentService creates Razorpay order
       │
       ▼
Frontend launches Razorpay checkout modal
       │
       ▼
Razorpay sends webhook → PaymentService verifies HMAC-SHA256 signature
       │
       ▼
PaymentService publishes PaymentCompletedEvent → RabbitMQ
       │
       ├──► OrderService consumer → transitions order to "Processing"
       │
       └──► NotificationService consumer → sends confirmation email + in-app notification
```

**Event-driven stock decrement:**

```
OrderService publishes OrderPlacedEvent → RabbitMQ
       │
       └──► ProductService consumer → decrements StockQuantity for each item
```

This ensures stock is only decremented after a confirmed payment, not at cart-add time.

### 5.4 Payment Integration (Razorpay)

The **PaymentService** integrates with Razorpay for secure payment processing:

1. **Create Order** — Generates a Razorpay order ID and returns it with the API key to the frontend
2. **Frontend Checkout** — Angular opens the Razorpay modal using the order ID
3. **Verify Payment** — After the modal closes, the frontend sends `razorpay_order_id`, `razorpay_payment_id`, and `razorpay_signature` for server-side HMAC-SHA256 verification
4. **Webhook** — Razorpay sends a `payment.captured` event to the anonymous `/webhook` endpoint; the service verifies the `X-Razorpay-Signature` header and publishes `PaymentCompletedEvent` to RabbitMQ

The webhook endpoint is intentionally `[AllowAnonymous]` since Razorpay cannot send a JWT.

### 5.5 Real-Time Notifications (SignalR)

The **NotificationService** uses ASP.NET Core SignalR to push notifications to connected clients:

- On connection, each client joins two groups:
  - `user:{userId}` — for targeted personal notifications (order updates, payment confirmations)
  - `role:{role}` — for broadcast notifications to all users of a role (e.g., all Admins)
- Notifications are also persisted to the database so users can view history after reconnecting
- The Angular frontend connects to the hub using `@microsoft/signalr` and displays a live notification bell with unread count badge
- Notification types: `order`, `success`, `error`, `warning`, `info`

### 5.6 Role-Based Access Control

FreshMart implements four roles with distinct capabilities:

| Role | Capabilities |
|---|---|
| **Customer** | Browse products, manage cart, place orders, apply coupons, write reviews, raise support tickets |
| **Admin** | Full access — manage products, categories, users, orders, view support tickets, access dashboard |
| **StoreManager** | Manage inventory (stock, discounts), view and update orders, handle support |
| **DeliveryDriver** | View and update assigned deliveries |

JWT claims carry the role, and every API endpoint is decorated with `[Authorize(Roles = "...")]`. The API Gateway validates the JWT before forwarding requests, so individual services trust the gateway's validation.

### 5.7 Frontend Architecture (Angular 21)

The frontend is a standalone Angular 21 SSR application using:

- **Signals** (`signal()`, `computed()`) for reactive state management — no NgRx needed
- **Standalone components** — no NgModules
- **Tailwind CSS v4** for utility-first styling
- **Role-aware UI** — the navbar, home page quick actions, and route guards all adapt based on the user's role
- **Recently Viewed** — a client-side service tracks the last 6 viewed products and shows them on the home page
- **Wishlist** — persisted wishlist with count displayed in the navbar
- **Location selector** — modal with GPS geolocation and address search for delivery area awareness
- **Dark mode** — full dark/light theme toggle via `ThemeService`
- **Hero carousel** — auto-rotating slides with gradient backgrounds, pausing when the user is authenticated (shows personalised welcome instead)

---

## 6. Database Design

Each microservice owns its own database (Database-per-Service pattern), preventing tight coupling at the data layer.

| Service | Database | Key Tables |
|---|---|---|
| AuthService | FreshMart_Auth | Users, RefreshTokens |
| ProductService | FreshMart_Product | Products, Categories, Reviews |
| OrderService | FreshMart_Order | Orders, OrderItems, Carts, CartItems, Coupons |
| PaymentService | FreshMart_Payment | Payments |
| NotificationService | FreshMart_Notification | Notifications |
| SupportService | FreshMart_Support | SupportTickets, SupportMessages |

**Price snapshotting** — `OrderItem.UnitPrice` and `OrderItem.ProductName` are snapshotted at order creation time. This means historical orders remain accurate even if a product's price or name changes later.

**Customer data snapshotting** — `Order.CustomerEmail` and `Order.CustomerFirstName` are stored directly on the order so transactional emails can be sent without a cross-service lookup to AuthService.

---

## 7. Messaging & Event Architecture

FreshMart uses **RabbitMQ** with **MassTransit** as the event bus. Three domain events drive the asynchronous workflows:

### OrderPlacedEvent
- **Published by:** OrderService (after payment confirmation)
- **Consumed by:**
  - NotificationService → sends order confirmation email + in-app notification
  - ProductService → decrements stock for each ordered item

### OrderStatusChangedEvent
- **Published by:** OrderService (when admin/driver updates status)
- **Consumed by:**
  - NotificationService → sends status update email + in-app notification to customer

### PaymentCompletedEvent
- **Published by:** PaymentService (on Razorpay `payment.captured` webhook)
- **Consumed by:**
  - OrderService → transitions order from `PaymentPending` to `Processing`

This event-driven design means services are fully decoupled — ProductService doesn't need to know about OrderService, and NotificationService doesn't need to know about PaymentService.

---

## 8. Infrastructure & Deployment

The entire stack is containerised and orchestrated with **Docker Compose**:

```
Services:        10 containers
Infrastructure:  SQL Server 2022, RabbitMQ 3 (with management UI)
Frontend:        Nginx serving the Angular SSR build
Networking:      Internal Docker network; only gateway (8080) and frontend (80) exposed
Persistence:     Named volumes for SQL Server data and RabbitMQ data
Health checks:   SQL Server and RabbitMQ have health checks; dependent services wait
```

**Environment configuration** is fully externalised via Docker Compose environment variables — no secrets are hardcoded in application code. JWT keys, DB connection strings, SMTP credentials, and Razorpay keys are all injected at runtime.

---

## 9. Security Design

| Concern | Approach |
|---|---|
| Authentication | JWT Bearer tokens (HS256), issued by AuthService |
| Authorisation | Role claims in JWT, enforced at API Gateway + individual services |
| Payment security | Razorpay HMAC-SHA256 signature verification on every webhook |
| Password storage | ASP.NET Core Identity with bcrypt hashing |
| CORS | Configured at API Gateway level, restricting allowed origins |
| Soft deletes | Products are deactivated, not deleted — preserves audit trail |
| Token refresh | Refresh token rotation for session continuity |
| Correlation IDs | Every request gets a `X-Correlation-ID` header for distributed tracing |

---

## 10. Key Design Decisions & Trade-offs

### Decision 1: Database-per-Service
**Choice:** Each microservice has its own SQL Server database.
**Benefit:** Services can be deployed, scaled, and modified independently. A schema change in ProductService doesn't affect OrderService.
**Trade-off:** Cross-service queries require API calls or event-driven data replication (e.g., product data cached in OrderService for cart display).

### Decision 2: Product Data Caching in OrderService
**Choice:** When a product is added to the cart, OrderService fetches and caches the product locally.
**Benefit:** Cart rendering doesn't require a live call to ProductService on every page load.
**Trade-off:** Cached data can become stale. The system refreshes it on every `AddItem` call to mitigate this.

### Decision 3: Price Snapshotting on Orders
**Choice:** `UnitPrice` and `ProductName` are stored on `OrderItem` at creation time.
**Benefit:** Historical orders are immutable and accurate regardless of future price changes.
**Trade-off:** Slightly more storage; no automatic price updates on existing orders (which is the correct behaviour for an e-commerce system).

### Decision 4: Asynchronous Stock Decrement
**Choice:** Stock is decremented via `OrderPlacedEvent` after payment, not at cart-add time.
**Benefit:** Avoids holding stock for abandoned carts; simpler cart logic.
**Trade-off:** In high-concurrency scenarios, two customers could theoretically both pay for the last unit. A reservation system would be needed for strict inventory control.

### Decision 5: SignalR for Real-Time Notifications
**Choice:** SignalR over polling or SSE.
**Benefit:** Bidirectional, low-latency, works with JWT auth, supports group-based broadcasting.
**Trade-off:** Requires sticky sessions or a Redis backplane in a multi-instance deployment. Currently single-instance per service.

---

## 11. Challenges & Solutions

| Challenge | Solution |
|---|---|
| Cart items showing stale prices | Product data refreshed from ProductService on every `AddItem` call |
| JWT claims not resolving in CartController | Fallback chain: `sub` → `NameIdentifier` → `"sub"` claim lookup with diagnostic error output |
| Razorpay webhook can't send JWT | `[AllowAnonymous]` on webhook endpoint; security via HMAC-SHA256 signature verification instead |
| Services starting before DB is ready | Docker Compose `healthcheck` on SQL Server and RabbitMQ; dependent services use `condition: service_healthy` |
| Cross-service data consistency | Event sourcing via RabbitMQ; each service maintains its own read model |
| Dark mode across all components | CSS custom properties (`--adm-bg`, `--adm-card`, etc.) toggled by `ThemeService` |

---

## 12. Results & Outcomes

| Metric | Value |
|---|---|
| Microservices | 6 independent services + 1 API Gateway |
| User roles | 4 (Customer, Admin, StoreManager, DeliveryDriver) |
| Product categories | 8 (Fruits & Vegetables, Dairy & Eggs, Bakery, Beverages, Snacks, Meat & Seafood, Frozen Foods, Pantry) |
| Order statuses | 10 lifecycle states |
| Real-time features | Notifications (SignalR), Support chat (SignalR) |
| Payment gateway | Razorpay (test + production ready) |
| Deployment | Fully containerised, single `docker compose up` |
| Frontend framework | Angular 21 with SSR, Signals, Tailwind CSS v4 |

---

## 13. Future Enhancements

1. **Inventory reservation system** — Reserve stock at cart-add time with a TTL to handle high-concurrency scenarios
2. **Redis backplane for SignalR** — Enable horizontal scaling of NotificationService and SupportService
3. **Elasticsearch integration** — Replace SQL full-text search with Elasticsearch for faster, fuzzy product search
4. **Delivery tracking map** — Real-time GPS tracking for DeliveryDriver using SignalR + Google Maps
5. **Recommendation engine** — "Customers also bought" based on order history
6. **Progressive Web App (PWA)** — Offline support and push notifications for mobile users
7. **A/B testing for promotions** — Feature flags for discount campaigns
8. **Kubernetes deployment** — Helm charts for production-grade orchestration with auto-scaling

---

## 14. Technology Stack Summary

| Layer | Technology |
|---|---|
| Frontend | Angular 21, TypeScript, Tailwind CSS v4, SignalR client |
| Backend | ASP.NET Core 8, C#, Entity Framework Core |
| API Gateway | YARP (Yet Another Reverse Proxy) |
| Message Broker | RabbitMQ 3 with MassTransit |
| Database | Microsoft SQL Server 2022 |
| Payment | Razorpay |
| Real-time | ASP.NET Core SignalR |
| Auth | JWT Bearer, ASP.NET Core Identity |
| Containerisation | Docker, Docker Compose |
| Email | SMTP (Gmail) via MailKit |

---

## 15. Conclusion

FreshMart demonstrates how a modern grocery delivery platform can be built with clean separation of concerns, event-driven communication, and a rich user experience. The microservices architecture ensures each domain — products, orders, payments, notifications, and support — can evolve independently. The smart cart with budget tracking, real-time notifications, and role-aware UI make it a production-ready foundation for a real-world grocery delivery business.

The project showcases practical application of distributed systems patterns including Database-per-Service, Event Sourcing via message queues, API Gateway pattern, CQRS-lite (separate read models per service), and the Saga pattern for the order-payment workflow.

---

*© 2026 FreshMart. Case Study prepared for academic and portfolio purposes.*
