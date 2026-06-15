# Logistics & Shipment Tracking System
## Architecture Document

---

## 1. System Overview

An on-demand same-day logistics platform built on a layered architecture. The frontend communicates with the backend exclusively via REST APIs and SignalR. The backend is stateless — all state lives in PostgreSQL. Auth0 handles identity. Razorpay handles payments. [LLM] Ollama runs locally for AI features.

---

## 2. Architecture Layers

```
┌─────────────────────────────────────────────┐
│              Angular Frontend               │
│         (TypeScript, Leaflet.js)            │
└────────────────────┬────────────────────────┘
                     │ HTTP / SignalR
┌────────────────────▼────────────────────────┐
│           ASP.NET Core Web API              │
│         Controllers + Middleware            │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│              Service Layer                  │
│         Business logic, FSM, OTP            │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│            Repository Layer                 │
│         Data access, DB queries             │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│              PostgreSQL                     │
│            8 tables, enums                  │
└─────────────────────────────────────────────┘
```

---

## 3. Component Responsibilities

### Angular Frontend
- Presents UI for Customer, Driver, and Admin roles
- Reads role from Auth0 JWT and activates role-based route guards
- Nominatim autocomplete for address input
- Leaflet.js renders live map with driver marker
- SignalR client receives real-time GPS updates and notifications
- TypeScript interval sends simulated GPS pings every 5 seconds (driver app)

### ASP.NET Core Web API
- Validates Auth0 JWT on every protected endpoint
- Enforces role-based access per endpoint
- Controllers are thin — delegate all logic to service layer
- SignalR Hub manages real-time connections and group broadcasts
- Background job runs every minute to detect stale shipments
- Razorpay webhook endpoint receives async payment confirmation

### Service Layer
- All business logic lives here — no logic in controllers or repositories
- Shipment FSM — validates and enforces status transitions
- OTP generation, hashing, verification, expiry management
- Haversine distance calculation for geofence and job feed sorting
- Driver eligibility check — vehicle type vs package weight
- Idempotency key validation for payments
- [LLM] Calls Ollama HTTP API for note parsing and dispute summarisation

### Repository Layer
- All database queries — no raw SQL in services
- Uses Entity Framework Core with PostgreSQL
- Claim endpoint uses `FOR UPDATE SKIP LOCKED` to handle concurrent claims
- GPS location writes use optimistic locking on `recorded_at` timestamp

### PostgreSQL
- Single database, 8 tables
- Enums defined at DB level for role, status, vehicle type, package type etc.
- All PKs are UUIDs
- Indexes on frequently queried columns

---

## 4. Request Flow — Standard API Call

```
Browser / Angular
      │
      │ HTTP Request + Auth0 JWT
      ▼
Auth0 JWT Middleware
      │ validates token, extracts role
      ▼
Controller
      │ maps request to service call
      ▼
Service Layer
      │ business logic, validation
      ▼
Repository Layer
      │ DB query via EF Core
      ▼
PostgreSQL
      │ returns data
      ▼
Service → Controller → HTTP Response
```

---

## 5. Real-Time Flow — GPS Tracking

```
Driver App (Angular)
      │
      │ TypeScript setInterval — every 5 seconds
      │ POST /api/tracking/location
      ▼
Tracking Controller
      │
      ▼
Tracking Service
      │ saves ping to tracking table
      │ updates drivers.current_lat/lng/last_ping_at
      ▼
SignalR Hub
      │ broadcasts to shipment group
      │ group = "shipment-{shipmentId}"
      ▼
Customer Browser (Angular)
      │ receives locationUpdated event
      ▼
Leaflet Map
      │ marker.setLatLng(lat, lng)
      ▼
Driver marker moves on map
```

---

## 6. Real-Time Flow — Notifications

```
Business Event (e.g. driver claims shipment)
      │
      ▼
Shipment Service
      │ creates notification row in DB
      ▼
Notification Service
      │ sends via SignalR to user group
      │ group = "user-{userId}"
      ▼
Angular Toast (if app open)
      │
      OR
      ▼
Unread notifications fetched on next login
```

---

## 7. Payment Flow — Online Payment

```
Customer submits booking
      │
      ▼
Shipment created — status PENDING_PAYMENT
      │
      ▼
POST /api/payments/initiate
      │ idempotency key generated
      ▼
Razorpay order created (test mode)
      │ returns payment URL
      ▼
Customer completes payment on Razorpay
      │
      ▼
Razorpay fires webhook → POST /api/webhooks/payment
      │ signature verified
      ▼
Payment Service
      │ updates payment.status = SUCCESS
      │ updates shipment.status = OPEN
      ▼
SignalR notifies customer — "Booking confirmed"
      │
      ▼
Eligible drivers notified via SignalR group
```

---

## 8. Driver Self-Assignment Flow

```
Shipment status = OPEN
      │
      ▼
SignalR broadcasts to vehicle type group
e.g. group = "vehicle-TWO_WHEELER"
      │
      ▼
Driver sees job in feed
      │
      ▼
Driver taps Claim
POST /api/shipments/{id}/claim
      │
      ▼
Repository — BEGIN TRANSACTION
SELECT * FROM shipments
WHERE id = @id AND status = 'OPEN'
FOR UPDATE SKIP LOCKED
      │
      ├── Row locked → UPDATE status = ASSIGNED → COMMIT → 200 OK
      │
      └── Row already locked → ROLLBACK → 409 Conflict
```

---

## 9. OTP Flow

```
At booking creation
      │
      ▼
Service generates two 4-digit OTPs
      │ senderOtp = random 4-digit
      │ receiverOtp = random 4-digit
      ▼
Both hashed with bcrypt
      │ stored in shipments table
      │ senderOtp shown to customer once on booking confirmation
      │ receiverOtp shown on public tracking page
      ▼
At pickup — driver enters sender OTP
      │ hash entered OTP → compare with stored hash
      │ match → status IN_TRANSIT
      │ no match → increment sender_otp_attempts
      │ attempts ≥ 3 → flag admin, block driver
      ▼
At delivery — driver enters receiver OTP
      │ geofence check first — driver within 200m of drop
      │ hash entered OTP → compare with stored hash
      │ match → status DELIVERED
      │ no match → increment receiver_otp_attempts
      │ attempts ≥ 3 → flag admin, block driver
```

---

## [LLM] 10. LLM Integration Flow

```
Customer submits booking with special notes
      │
      ▼
Shipment Service
      │ POST http://localhost:11434/api/generate
      │ model: qwen
      │ prompt: parse delivery note → return JSON
      ▼
Ollama (local)
      │ returns risk_flag, severity, reason,
      │ preferred_delivery_after, driver_instruction
      ▼
Service stores LLM output in shipments table
      │ risk_flag, risk_severity, risk_reason
      │ preferred_delivery_after, driver_instruction
      ▼
If risk HIGH → warning shown to customer
If risk HIGH → OTP strictly enforced at delivery
If risk HIGH → admin alerted on delivery

---

Customer raises dispute
      │
      ▼
Dispute Service
      │ POST http://localhost:11434/api/generate
      │ model: qwen
      │ prompt: summarise complaint → return JSON
      ▼
Ollama (local)
      │ returns summary, type, suggested_resolution
      ▼
Stored in disputes table
      │ llm_summary, llm_type, llm_suggested_resolution
      ▼
Admin sees summary + suggestion in dispute dashboard
```

---

## 11. Auth Flow

```
User opens Angular app
      │
      ▼
Auth0 Universal Login
      │ user authenticates with Auth0
      │ Auth0 returns JWT with role claim
      ▼
Angular stores JWT
      │ reads role from token
      │ activates role-based route guards
      ▼
Every API request
      │ Authorization: Bearer <token>
      ▼
ASP.NET Core middleware
      │ validates token with Auth0 JWKS
      │ extracts role
      │ checks role against endpoint policy
      ▼
Controller executes if authorised
      │
      └── 401 if no token
      └── 403 if wrong role
```

---

## 12. Background Jobs

Two background jobs run on the server:

**Stale Shipment Detector — runs every 1 minute**
```
For every shipment with status IN_TRANSIT
  if now − last_ping_at > 5 minutes
    update status = STALE
    notify admin via SignalR
```

**Payment Timeout Handler — runs every 2 minutes**
```
For every shipment with status PENDING_PAYMENT
  if now − created_at > 10 minutes
    update shipment status = CANCELLED
    update payment status = FAILED
    notify customer via SignalR
```

---

## 13. SignalR Groups

| Group Name | Members | Events Received |
|---|---|---|
| `user-{userId}` | Individual user | Personal notifications |
| `shipment-{shipmentId}` | Customer + Admin | GPS updates, status changes |
| `vehicle-TWO_WHEELER` | Online TWO_WHEELER drivers | New job notifications |
| `vehicle-THREE_WHEELER` | Online THREE_WHEELER drivers | New job notifications |
| `vehicle-FOUR_WHEELER` | Online FOUR_WHEELER drivers | New job notifications |
| `vehicle-HEAVY_VEHICLE` | Online HEAVY_VEHICLE drivers | New job notifications |
| `admin` | All admins | Stale alerts, dispute flags, high cancel count |

Driver joins vehicle group on go-online. Leaves all groups on go-offline or disconnect.

---

## 14. Database Indexes

| Table | Index | Reason |
|---|---|---|
| users | email, phone | Login and lookup |
| users | auth0_id | Auth0 token validation |
| drivers | user_id | Join with users |
| drivers | operational_status | Filter online drivers for job feed |
| shipments | order_id | Public tracking lookup |
| shipments | customer_id | Customer's own shipments |
| shipments | driver_id | Driver's active shipment |
| shipments | status | Filter by status |
| tracking | shipment_id, recorded_at DESC | Latest position + history |
| notifications | user_id, is_read | Unread notifications per user |
| payments | shipment_id | Payment per shipment |

---

## 15. Project Folder Structure

### Backend — ASP.NET Core
```
src/
├── Controllers/
│   ├── AuthController.cs
│   ├── ShipmentsController.cs
│   ├── TrackingController.cs
│   ├── PaymentsController.cs
│   ├── DriversController.cs
│   ├── NotificationsController.cs
│   ├── DisputesController.cs
│   ├── AdminController.cs
│   ├── PublicController.cs
│   └── [LLM] LlmController.cs
├── Services/
│   ├── ShipmentService.cs
│   ├── TrackingService.cs
│   ├── PaymentService.cs
│   ├── DriverService.cs
│   ├── NotificationService.cs
│   ├── OtpService.cs
│   ├── GeoService.cs
│   └── [LLM] LlmService.cs
├── Repositories/
│   ├── ShipmentRepository.cs
│   ├── DriverRepository.cs
│   ├── TrackingRepository.cs
│   ├── PaymentRepository.cs
│   └── NotificationRepository.cs
├── Hubs/
│   └── TrackingHub.cs
├── Models/
│   ├── Users.cs
│   ├── Drivers.cs
│   ├── Vehicles.cs
│   ├── Shipments.cs
│   ├── Tracking.cs
│   ├── Payments.cs
│   ├── Notifications.cs
│   └── Disputes.cs
├── DTOs/
├── BackgroundJobs/
│   ├── StaleShipmentJob.cs
│   └── PaymentTimeoutJob.cs
└── Middleware/
    └── Auth0Middleware.cs
```

### Frontend — Angular
```
src/app/
├── core/
│   ├── auth/
│   ├── guards/
│   └── interceptors/
├── shared/
│   ├── components/
│   └── services/
├── features/
│   ├── customer/
│   │   ├── book-shipment/
│   │   ├── tracking/
│   │   └── history/
│   ├── driver/
│   │   ├── job-feed/
│   │   ├── active-delivery/
│   │   └── registration/
│   └── admin/
│       ├── driver-approvals/
│       ├── shipments/
│       ├── disputes/
│       └── metrics/
└── public/
    └── track/
```

---

## 16. Key Technology Decisions

| Decision | Chose | Reason |
|---|---|---|
| Assignment model | Driver self-assign | Realistic — Uber/Porter model |
| Auth | Auth0 | Production-grade, free tier, saves build time |
| Maps | Leaflet.js + OpenStreetMap | Free, no API key, no billing |
| Address lookup | Nominatim | Free, OpenStreetMap ecosystem |
| Real-time | SignalR | Built into ASP.NET Core, zero cost |
| Payment | Razorpay test mode | India-first, free sandbox, webhooks included |
| GPS | Simulated in browser | Capstone — no real device needed |
| [LLM] Local AI | Ollama (Qwen) | Free, runs locally, no API cost, no data leaves machine |
| Delivery scope | Same day only | Keeps scope focused |

---

## 17. Phase 2 — Future Architecture

| Enhancement | Approach |
|---|---|
| Kubernetes | Dockerize API + Angular + PostgreSQL, write K8s manifests, deploy on Minikube locally |
| Azure Maps | Swap Leaflet for Azure Maps SDK |
| Azure Service Bus | Replace SignalR direct broadcast with Service Bus topics for scale |
| Scheduled bookings | Add background job to move SCHEDULED → OPEN at pickup time |
| SMS notifications | Twilio free tier for OTP and delivery alerts |
| Mobile app | Angular → Ionic or separate React Native app |