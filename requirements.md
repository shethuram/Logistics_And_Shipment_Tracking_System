# Logistics & Shipment Tracking System
## Requirements Document

---

## 1. Project Overview

An enterprise-grade on-demand logistics and shipment tracking platform. A customer hands over a physical package to a driver who picks it up and delivers it to the receiver — same day, on-demand, similar to Porter or Dunzo.

**What the system is not:** An e-commerce platform. There is no product catalog, no inventory, no SKUs. The system purely moves a package from Point A to Point B.

---

## 2. Tech Stack

| Layer | Technology | Notes |
|---|---|---|
| Frontend | Angular (TypeScript) | |
| Backend | ASP.NET Core Web API | |
| Database | PostgreSQL | |
| Authentication | Auth0 (free tier) | |
| Real-time | SignalR | Built into ASP.NET Core |
| Maps | Leaflet.js + OpenStreetMap | Free, no API key |
| Address Lookup | Nominatim | Free, OpenStreetMap geocoding |
| Payment | Razorpay | Test mode — no real money |
| [LLM] Local AI | Ollama (Qwen) | Runs locally, free |
| Containerisation | Docker | |
| API Docs | Swagger / OpenAPI | |

---

## 3. User Roles

| Role | Description |
|---|---|
| Customer | Registers, books shipments, tracks delivery |
| Driver | Self-registers, awaits admin approval, claims and delivers shipments |
| Admin | Approves drivers, monitors all shipments, manages disputes |

---

## 4. Core Business Flow

```
Customer registers and books a shipment
                ↓
Chooses payment — COD or Online (Razorpay test mode)
                ↓
        [Online] Payment confirmed via webhook
                ↓
Shipment status = OPEN
                ↓
SignalR notifies eligible drivers based on vehicle type and package weight
                ↓
Driver sees job in feed — sorted by distance to pickup
                ↓
Driver claims job — first claim wins
                ↓
Driver travels to pickup address
                ↓
Sender gives Sender OTP to driver
                ↓
Driver enters Sender OTP → status = IN_TRANSIT
                ↓
GPS simulation starts — driver marker moves on customer map
                ↓
Driver arrives at drop address (geofence: within 200m)
                ↓
Receiver gives Receiver OTP to driver
                ↓
Driver enters Receiver OTP → status = DELIVERED
                ↓
[COD] Driver marks cash collected
                ↓
Sender and receiver notified
```

---

## 5. Shipment Status Lifecycle

```
PENDING_PAYMENT → OPEN → ASSIGNED → IN_TRANSIT → DELIVERED
```

| Status | Meaning |
|---|---|
| PENDING_PAYMENT | Online payment chosen, waiting for Razorpay webhook |
| OPEN | Payment confirmed or COD — visible to eligible drivers |
| ASSIGNED | A driver has claimed the shipment |
| IN_TRANSIT | Sender OTP verified — package collected, en route |
| DELIVERED | Receiver OTP verified — delivery complete |
| CANCELLED | Customer cancelled or payment failed |
| PICKUP_FAILED | Driver arrived but sender was not available |
| STALE | No GPS ping received for 5+ minutes — admin flagged |

No status can be skipped. Invalid transitions return HTTP 422. `status_changed_by` and `status_updated_at` recorded on every change.

---

## 6. Modules & Functional Requirements

---

### 6.1 Authentication (Auth0)

Auth0 handles all identity — registration, login, token issuance, and refresh. ASP.NET Core API validates Auth0 JWTs on every protected endpoint.

- Customer self-registration via Auth0
- Driver self-registration via Auth0
- Admin account created manually in Auth0 dashboard
- Role included in JWT claims — CUSTOMER, DRIVER, ADMIN
- Angular route guards read role from token
- Driver cannot access the system until admin approves their profile in the application DB
- Auth0 confirms identity — application DB confirms approval status

**APIs**
```
POST /api/auth/register/customer    → creates customer profile in DB after Auth0 signup
POST /api/auth/register/driver      → creates driver profile in DB after Auth0 signup
```

---

### 6.2 Driver Registration & Approval

**Driver self-registers with:**
- Full name, email, phone, password — handled by Auth0
- License number
- Vehicle details (at least one vehicle)

**After registration:**
- Driver account status = PENDING
- Driver cannot login to the app until approved
- Admin sees pending registrations in dashboard

**Admin actions:**
- Approve → driver notified, can now login and go online
- Reject with reason → driver notified with reason
- Suspend active driver at any time with reason

**Driver status values:**
- Approval: PENDING, APPROVED, REJECTED, SUSPENDED
- Operational: ONLINE, OFFLINE, ON_DELIVERY

**APIs**
```
GET  /api/admin/drivers/pending
POST /api/admin/drivers/{id}/approve
POST /api/admin/drivers/{id}/reject
POST /api/admin/drivers/{id}/suspend
```

---

### 6.3 Vehicle Management

A driver can register multiple vehicles. One vehicle is marked active at a time. The active vehicle type determines which job notifications the driver receives and which jobs appear in their feed.

**Vehicle types and weight eligibility:**

| Vehicle Type | Weight Range |
|---|---|
| TWO_WHEELER | 0 – 5 kg |
| THREE_WHEELER | 5 – 20 kg |
| FOUR_WHEELER | 20 – 200 kg |
| HEAVY_VEHICLE | 200 kg+ |

**Package type overrides weight:**
- DOCUMENT → TWO_WHEELER only
- FRAGILE, HOUSEHOLD → FOUR_WHEELER or HEAVY_VEHICLE only

**APIs**
```
POST /api/drivers/{id}/vehicles
GET  /api/drivers/{id}/vehicles
PUT  /api/drivers/{id}/vehicles/{vehicleId}
POST /api/drivers/{id}/vehicles/{vehicleId}/set-active
```

---

### 6.4 Shipment Booking

**Booking form fields:**

| Field | Type | Notes |
|---|---|---|
| Pickup address | Text | Nominatim autocomplete — lat/lng stored on selection |
| Drop address | Text | Nominatim autocomplete — lat/lng stored on selection |
| Receiver name | Text | |
| Receiver phone | Text | |
| Package type | Enum | DOCUMENT, SMALL_PARCEL, LARGE_PARCEL, FRAGILE, HOUSEHOLD |
| Weight (kg) | Decimal | Determines eligible vehicle types |
| Preferred window | Enum | MORNING (9–12), AFTERNOON (12–5), EVENING (5–9) |
| Special notes | Text | Free text — [LLM] parsed for risk and delivery time |
| Payment method | Enum | COD, ONLINE |

**Address input:** Nominatim autocomplete fires as user types. User picks from suggestions — no unverified free text accepted. Lat/lng stored on selection.

**[LLM] Note parsing:** On booking submission, special notes are sent to Ollama which extracts:
- Risk flag and severity (HIGH, LOW, NONE)
- Risk reason (unattended_delivery, unknown_receiver etc.)
- Preferred delivery after time (e.g. 17:00 if customer writes "after 5pm")
- Clean driver instruction in plain English

**[LLM] If risk = HIGH:**
- Warning banner shown to customer before booking confirms
- OTP strictly enforced — cannot be bypassed
- Admin flagged on delivery

**APIs**
```
POST   /api/shipments
GET    /api/shipments/{id}
PUT    /api/shipments/{id}
DELETE /api/shipments/{id}
GET    /api/shipments?search=&status=&dateFrom=&dateTo=&page=&pageSize=
GET    /api/public/track?orderId=&phone=&date=
```

---

### 6.5 Payment

**Razorpay test mode** — no real money involved. Test card `4111 1111 1111 1111` triggers the full success flow including webhooks.

**COD path:**
- Shipment moves to OPEN immediately on booking confirmation
- Driver collects cash at delivery
- Driver marks "Cash collected" as a separate step after OTP confirmation
- If skipped → flagged as COD_PENDING in admin dashboard

**Online payment path:**
1. Shipment created with status PENDING_PAYMENT
2. Razorpay payment initiated — idempotency key sent to prevent duplicate charges
3. Razorpay webhook fires on success → status moves to OPEN
4. On failure → customer sees retry option
5. After 3 failed attempts → COD suggested
6. No webhook within 10 minutes → shipment auto-cancelled

**Refund:**
- Customer cancels before driver claims → full refund via Razorpay API
- COD cancellation → no refund needed

**APIs**
```
POST /api/payments/initiate
POST /api/webhooks/payment
GET  /api/payments/{shipmentId}/status
```

---

### 6.6 Driver Job Feed & Self-Assignment

- Driver goes online → selects active vehicle → browser starts sending GPS every 5 seconds → driver joins SignalR group for their vehicle type
- New shipment becomes OPEN → SignalR notifies only matching vehicle type group
- Job feed shows only eligible shipments sorted by distance from driver to pickup
- Driver sees: pickup address, drop address, sender phone, receiver phone, package type, weight, preferred window, [LLM] driver instruction

**Claim logic:**
- Driver taps Claim → API handles concurrent claims at database level — first valid claim wins
- Status moves to ASSIGNED
- Job disappears from all drivers' feeds
- One active shipment per driver maximum — returns HTTP 400 if driver already has one

**Cancellation:**
- Driver can cancel before entering sender OTP
- Status returns to OPEN
- Job re-broadcasts to eligible drivers
- `cancel_count` on driver increments — admin flagged at 3

**APIs**
```
GET  /api/shipments/available
POST /api/shipments/{id}/claim
POST /api/shipments/{id}/cancel-claim
```

---

### 6.7 GPS Tracking & Simulation

**Simulation:**
- On IN_TRANSIT, Angular driver app interpolates a straight-line route between pickup lat/lng and drop lat/lng (10–15 steps)
- TypeScript interval sends one coordinate every 5 seconds to the API
- API saves to tracking table and broadcasts via SignalR to customer map
- Leaflet.js moves driver marker in real time on customer screen

**Distance calculation:**
- Haversine formula used server-side for all distance calculations
- Used for: job feed sorting, ETA estimate, geofence check

**Stale detection:**
- Background job runs every minute
- If `now − last_ping_at > 5 minutes` → shipment marked STALE, admin alerted

**Geofence on delivery:**
- Driver taps "Mark Delivered" → API checks distance between driver location and drop lat/lng
- If distance > 200 metres → HTTP 400, delivery blocked
- Driver must physically be at drop address

**APIs**
```
POST /api/tracking/location
GET  /api/tracking/{shipmentId}/live
GET  /api/tracking/{shipmentId}/history
```

---

### 6.8 Dual OTP Confirmation

Two OTPs generated at booking time. Stored as bcrypt hashes. Never plaintext, never logged.

**Sender OTP — pickup confirmation:**
- Shown to customer on booking confirmation screen
- Driver arrives at pickup → customer reads OTP to driver
- Driver enters OTP → API verifies → status moves to IN_TRANSIT

**Receiver OTP — delivery confirmation:**
- Shown on public tracking page (no login needed)
- Driver arrives at drop → receiver reads OTP to driver
- Driver enters OTP → API verifies → status moves to DELIVERED

**Rules:**
- 4-digit numeric
- Max 3 attempts — after 3 failures flagged to admin, driver cannot self-complete
- Expires 30 minutes after generation — new OTP auto-generated on expiry
- [LLM] If risk_flag = HIGH → OTP strictly enforced, cannot be waived under any circumstance

**APIs**
```
POST /api/shipments/{id}/confirm-pickup
POST /api/shipments/{id}/confirm-delivery
```

---

### 6.9 Public Tracking

Available on home page — no login required.

**Three fields — all must match:**

| Field | Notes |
|---|---|
| Order ID | Format: TRK-YYYYMMDD-NNNNN |
| Sender phone | Phone number used at registration |
| Date | Date of booking |

If any field is wrong → same response returned, no hints about which field failed.

**Returns:**
- Order ID, current status
- Pickup and drop location names (not full addresses)
- Status timeline with timestamps
- Live map with driver marker — read only
- Preferred delivery window
- [LLM] Driver instruction note if present

**Does not return:**
- Driver name or phone
- Sender full details
- Receiver details
- Payment info
- Internal IDs

**Rate limiting:** 10 requests per minute per IP.

**API**
```
GET /api/public/track?orderId=&phone=&date=
```

---

### 6.10 Notifications

In-app only via SignalR. Real-time toasts when app is open. Stored in DB and shown as unread history when app reopens.

| Event | Notified |
|---|---|
| Booking confirmed | Sender |
| Driver assigned | Sender |
| Driver nearby pickup | Sender |
| Package collected (sender OTP verified) | Sender + Receiver |
| Driver nearby drop | Receiver |
| Delivered | Sender + Receiver |
| Driver cancelled claim | Sender |
| Pickup failed | Sender |
| Registration approved / rejected | Driver |
| [LLM] High risk shipment flagged | Admin |

**Smart routing:** New shipment notification sent only to matching vehicle type SignalR group.

**APIs**
```
GET  /api/notifications/my?page=&pageSize=
POST /api/notifications/{id}/read
```

---

### 6.11 Admin Dashboard

**Driver management:**
- Pending approvals tab
- Active drivers — online/offline, vehicle type, location on map
- Approve, reject, suspend

**Shipment management:**
- All shipments — search by order ID or phone, filter by status and date range, server-side pagination
- STALE shipments highlighted
- Drivers with cancel_count ≥ 3 flagged
- COD_PENDING flagged
- [LLM] HIGH risk shipments flagged
- Manual reassign for stale or stuck shipments

**Disputes:**
- Customer raised issues
- [LLM] Auto-generated summary, type tag, and suggested resolution shown
- Admin resolves with notes
- Status: OPEN, RESOLVED, ESCALATED

**Metrics:**
- Total shipments, delivered, pending, failed, cancelled
- Average delivery time
- Export to CSV

**APIs**
```
GET  /api/admin/shipments?search=&status=&dateFrom=&dateTo=&page=&pageSize=
POST /api/admin/shipments/{id}/reassign
GET  /api/admin/metrics
GET  /api/admin/disputes
POST /api/admin/disputes/{id}/resolve
GET  /api/admin/export/shipments
```

---

### [LLM] 6.12 LLM Integration

Local Ollama instance running Qwen. Two use cases. Both backend-only — no UI changes required.

**Use case 1 — Delivery note parser (at booking):**

Single Ollama call when customer submits special notes. Returns:
```json
{
  "risk": true,
  "severity": "HIGH",
  "reason": "unattended_delivery",
  "preferred_delivery_after": "17:00",
  "driver_instruction": "Customer at work, deliver after 5 PM. Do not leave unattended."
}
```

Stored in shipments table. Drives system behaviour — risk flag affects OTP enforcement and admin alerting.

**Use case 2 — Dispute summariser (admin dashboard):**

Single Ollama call when dispute is created. Returns:
```json
{
  "summary": "Driver marked delivered but customer did not receive package.",
  "type": "WRONG_ADDRESS",
  "suggested_resolution": "Investigate driver GPS history for delivery timestamp."
}
```

Stored in disputes table. Admin sees summary and suggestion — makes final decision.

**API**
```
POST /api/llm/parse-delivery-note
POST /api/llm/summarise-dispute
```

---

## 7. Database — 8 Tables

| Table | Type | Purpose |
|---|---|---|
| users | Master | All users — customer, driver, admin |
| drivers | Master | Driver profile, approval, location |
| vehicles | Master | Driver vehicles — one driver, many vehicles |
| shipments | Master | Booking record, OTPs, status, [LLM] columns |
| tracking | Transactional | GPS pings during delivery |
| payments | Transactional | Payment record per shipment |
| notifications | Transactional | In-app notification history |
| disputes | Transactional | Customer issues, [LLM] columns |

---

## 8. Angular Pages

**Public:**
- Home — public shipment tracker (order ID + phone + date)
- Login / Register — Auth0 universal login

**Customer:**
- Book shipment — Nominatim autocomplete, package details, payment
- Booking confirmation — sender OTP displayed
- Live tracking — Leaflet map, status timeline
- Shipment history

**Driver:**
- Registration form
- Pending approval screen
- Go Online / Go Offline — active vehicle selector
- Job feed — filtered by vehicle type, sorted by distance
- Active delivery — OTP entry, mark pickup, mark delivered
- [LLM] Driver instruction note shown on job card

**Admin:**
- Driver approvals — pending + active tabs
- Shipments dashboard — search, filter, pagination
- Live fleet map — all active drivers
- Disputes — [LLM] summary and resolution suggestion shown
- Metrics + CSV export

---

## 9. Non-Functional Requirements

| Requirement | Target |
|---|---|
| API response time | < 300ms |
| GPS update frequency | Every 5 seconds |
| OTP expiry | 30 minutes |
| Stale detection | Within 5 minutes of GPS loss |
| Public tracking rate limit | 10 requests / minute / IP |
| Authentication | Auth0 JWT on every protected endpoint |
| OTP storage | bcrypt hashed, never plaintext |
| Payment | Razorpay test mode, idempotency key on every request |
| Concurrent claims | Handled at database level |

---

## 10. Key Design Decisions

| Decision | Chose | Over | Reason |
|---|---|---|---|
| Assignment | Driver self-assign | Auto-assign | More realistic, Uber/Porter model |
| Maps | Leaflet + OpenStreetMap | Azure Maps | Free, no API key |
| Address lookup | Nominatim | Google Places | Free, no billing |
| Auth | Auth0 | Manual JWT | Saves time, production grade, free tier |
| Real-time | SignalR | Azure Service Bus | Built into ASP.NET, zero cost |
| Payment | Razorpay test mode | Stripe | India-first, free sandbox |
| [LLM] Local AI | Ollama (Qwen) | OpenAI API | Free, runs locally, no API cost |
| Delivery scope | Same day only | Multi-day scheduling | Keeps scope manageable |
| GPS | Simulated | Real GPS device | Capstone — simulation is sufficient |

---

## 11. Phase 2 — Out of Scope for v1

- Kubernetes deployment
- Azure Maps integration
- Azure Service Bus
- Scheduled future bookings
- SMS notifications (Twilio)
- Driver performance scoring
- Mobile app
- Multi-attempt re-delivery