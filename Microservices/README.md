# FreshMart Microservices Architecture

## Services

| Service | Port | Responsibility |
|---|---|---|
| **API Gateway** | 8080 | Routes all requests using YARP reverse proxy |
| **Auth Service** | 5001 | Login, register, JWT tokens, user management |
| **Product Service** | 5002 | Products, categories, reviews |
| **Order Service** | 5003 | Orders, cart, coupons |
| **Payment Service** | 5004 | Razorpay integration, payment verification |
| **Notification Service** | 5005 | SignalR hub, email notifications |
| **Support Service** | 5006 | Support tickets, messages |
| **Frontend** | 80 | Angular app |

## Architecture

```
Browser → Frontend (port 80)
              ↓
         API Gateway (port 8080)  ← YARP Reverse Proxy
              ↓ routes to:
    ┌─────────┬──────────┬──────────┬──────────┬──────────┬──────────┐
    │  Auth   │ Product  │  Order   │ Payment  │ Notif.   │ Support  │
    │ :5001   │  :5002   │  :5003   │  :5004   │  :5005   │  :5006   │
    │ auth.db │prod.db   │orders.db │pay.db    │notif.db  │supp.db   │
    └─────────┴──────────┴──────────┴──────────┴──────────┴──────────┘
```

## Key Principles
- Each service has its **own database** (database-per-service pattern)
- Services communicate via **HTTP** (synchronous) 
- **API Gateway** handles routing, CORS, and JWT validation
- Each service is **independently deployable**

## Run
```bash
cd Microservices
docker-compose up --build
```

## vs Monolith
The original monolith is still in `Backend/` and runs via the root `docker-compose.yml`.
The microservices version is in `Microservices/` with its own `docker-compose.yml`.
