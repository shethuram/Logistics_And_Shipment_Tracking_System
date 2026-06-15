# COPILOT CONTEXT — Logistics & Shipment Tracking System

> Read this file first before generating any code.
> All design decisions are final. Do not suggest alternatives unless asked.
> Reference the linked docs for detailed specifications.

---

## What This System Is

An on-demand same-day logistics platform. A customer books a shipment, a driver picks up the package from the sender and delivers it to the receiver. Think Porter or Dunzo.

This is NOT an e-commerce platform. No product catalog, no inventory. The system purely moves a package from Point A to Point B.

---

## Tech Stack — Fixed, Do Not Change

| Layer | Technology |
|---|---|
| Frontend | Angular (TypeScript) |
| Backend | ASP.NET Core Web API — dotnet 10 |
| Database | PostgreSQL |
| ORM | Entity Framework Core (Npgsql) |
| Authentication | Auth0 — JWT validation middleware |
| Real-time | SignalR — built into ASP.NET Core |
| Maps | Leaflet.js + OpenStreetMap |
| Address Lookup | Nominatim API — free, no API key |
| Payment | Razorpay — test mode only |
| Local AI | Ollama (Qwen) — runs on localhost:11434 |
| Container | Docker |
| API Docs | Swagger / OpenAPI |

---

## Three User Roles

| Role | What They Do |
|---|---|
| CUSTOMER | Registers, books shipments, tracks delivery |
| DRIVER | Self-registers, gets approved by admin, claims and delivers shipments |
| ADMIN | Approves drivers, monitors shipments, manages disputes |

---

## Database — 8 Tables

See `docs/er_diagram.md` for full schema with all columns, types, and constraints.

| Table | Type | Purpose |
|---|---|---|
| users | Master | All users — role stored as VARCHAR enum |
| drivers | Master | Driver profile, approval status, live location |
| vehicles | Master | Driver vehicles — one driver can have many |
| shipments | Master | Core booking record — OTPs, status, addresses, lat/lng |
| tracking | Transactional | Every GPS ping during a delivery |
| payments | Transactional | One payment record per shipment |
| notifications | Transactional | In-app notification history |
| disputes | Transactional | Customer-raised complaints |

**All PKs are UUID. All enums are VARCHAR with fixed allowed values — not separate lookup tables.**

---

## Enums — Exact Values

```
users.role              → CUSTOMER, DRIVER, ADMIN
drivers.approval_status → PENDING, APPROVED, REJECTED, SUSPENDED
drivers.operational_status → ONLINE, OFFLINE, ON_DELIVERY
vehicles.vehicle_type   → TWO_WHEELER, THREE_WHEELER, FOUR_WHEELER, HEAVY_VEHICLE
shipments.status        → PENDING_PAYMENT, OPEN, ASSIGNED, IN_TRANSIT, DELIVERED, CANCELLED, PICKUP_FAILED, STALE
shipments.package_type  → DOCUMENT, SMALL_PARCEL, LARGE_PARCEL, FRAGILE, HOUSEHOLD
shipments.preferred_window → MORNING, AFTERNOON, EVENING
payments.method         → COD, ONLINE
payments.status         → PENDING, SUCCESS, FAILED, REFUNDED
disputes.status         → OPEN, RESOLVED, ESCALATED
```

---

## Shipment Status — Finite State Machine

Valid transitions only. Any invalid transition returns HTTP 422.

```
PENDING_PAYMENT → OPEN           (payment webhook success or COD)
OPEN            → ASSIGNED       (driver claims)
OPEN            → CANCELLED      (customer cancels or payment timeout)
ASSIGNED        → IN_TRANSIT     (sender OTP verified)
ASSIGNED        → OPEN           (driver cancels claim)
ASSIGNED        → PICKUP_FAILED  (sender not available)
IN_TRANSIT      → DELIVERED      (receiver OTP verified + geofence pass)
IN_TRANSIT      → STALE          (no GPS ping for 5+ minutes)
```

Enforce this in ShipmentService. No status change should happen outside the service layer.

---

## Architecture — 4 Layers

```
Controller    → thin, no business logic, maps HTTP to service calls
Service       → all business logic, FSM, OTP, distance calc, eligibility
Repository    → all DB queries via EF Core, no raw SQL except FOR UPDATE SKIP LOCKED
Database      → PostgreSQL, 8 tables
```

See `docs/architecture.md` for full folder structure.

---

## Backend Folder Structure

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
│   └── LlmController.cs
├── Interfaces/
│   ├── Services/
│   │   ├── IShipmentService.cs
│   │   ├── IDriverService.cs
│   │   ├── ITrackingService.cs
│   │   ├── IPaymentService.cs
│   │   ├── INotificationService.cs
│   │   ├── IOtpService.cs
│   │   ├── IGeoService.cs
│   │   └── ILlmService.cs
│   └── Repositories/
│       ├── IShipmentRepository.cs
│       ├── IDriverRepository.cs
│       ├── ITrackingRepository.cs
│       ├── IPaymentRepository.cs
│       └── INotificationRepository.cs
├── Services/
│   ├── ShipmentService.cs
│   ├── TrackingService.cs
│   ├── PaymentService.cs
│   ├── DriverService.cs
│   ├── NotificationService.cs
│   ├── OtpService.cs
│   ├── GeoService.cs
│   └── LlmService.cs
├── Repositories/
│   ├── ShipmentRepository.cs
│   ├── DriverRepository.cs
│   ├── TrackingRepository.cs
│   ├── PaymentRepository.cs
│   └── NotificationRepository.cs
├── Hubs/
│   └── TrackingHub.cs
├── Models/
│   └── (one file per table)
├── DTOs/
│   └── (request and response DTOs per module)
├── BackgroundJobs/
│   ├── StaleShipmentJob.cs
│   └── PaymentTimeoutJob.cs
└── Middleware/
    └── Auth0Middleware.cs
```

---

## Dependency Injection Pattern — Always Follow This

Every service and repository must have an interface. Controllers depend on interfaces, never on concrete implementations.

**Interface → Implementation → DI binding:**

```csharp
// 1. Interface (Interfaces/Services/IShipmentService.cs)
public interface IShipmentService
{
    Task<ShipmentDto> CreateAsync(CreateShipmentDto dto, Guid customerId);
    Task<ShipmentDto> GetByIdAsync(Guid id);
    Task ClaimAsync(Guid shipmentId, Guid driverId);
    Task<ShipmentDto> ConfirmPickupAsync(Guid shipmentId, string otp, Guid driverId);
    Task<ShipmentDto> ConfirmDeliveryAsync(Guid shipmentId, string otp, decimal driverLat, decimal driverLng);
    Task CancelAsync(Guid shipmentId, Guid customerId);
    Task<PagedResult<ShipmentDto>> GetAllAsync(ShipmentFilterDto filter);
    Task<IEnumerable<ShipmentDto>> GetAvailableForDriverAsync(Guid driverId);
}

// 2. Implementation (Services/ShipmentService.cs)
public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IGeoService _geoService;
    private readonly IOtpService _otpService;
    private readonly INotificationService _notificationService;

    public ShipmentService(
        IShipmentRepository shipmentRepo,
        IGeoService geoService,
        IOtpService otpService,
        INotificationService notificationService)
    {
        _shipmentRepo = shipmentRepo;
        _geoService = geoService;
        _otpService = otpService;
        _notificationService = notificationService;
    }
}

// 3. Controller depends on interface (Controllers/ShipmentsController.cs)
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }
}

// 4. Wire up in Program.cs
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IGeoService, GeoService>();
builder.Services.AddScoped<ILlmService, LlmService>();
```

---

## Key Business Rules — Must Enforce in Code

**Driver claim concurrency:**
```sql
SELECT * FROM shipments
WHERE id = @id AND status = 'OPEN'
FOR UPDATE SKIP LOCKED
```
Wrap in a transaction. First claim wins. Return 409 if row already locked.

**Geofence on delivery:**
- Calculate Haversine distance between driver's current lat/lng and drop lat/lng
- If distance > 0.2 km → return HTTP 400, block delivery confirmation

**OTP rules:**
- 4-digit numeric, generated at booking
- Stored as bcrypt hash — never plaintext
- Max 3 attempts — after 3 failures return HTTP 429, flag admin
- Expires 30 minutes after generation

**Payment idempotency:**
- Generate key as `{userId}-{shipmentId}-{timestamp}`
- Send as header with every Razorpay request
- Store in payments.idempotency_key with UNIQUE constraint

**Driver job feed:**
- Only show shipments where vehicle type matches weight eligibility
- Sort by Haversine distance from driver current location to pickup lat/lng

**Weight to vehicle eligibility:**
```
0–5 kg      → TWO_WHEELER, THREE_WHEELER
5–20 kg     → THREE_WHEELER, FOUR_WHEELER
20–200 kg   → FOUR_WHEELER
200 kg+     → HEAVY_VEHICLE
DOCUMENT    → TWO_WHEELER only
FRAGILE     → FOUR_WHEELER or HEAVY_VEHICLE only
HOUSEHOLD   → FOUR_WHEELER or HEAVY_VEHICLE only
```

**Stale detection background job — every 1 minute:**
```
For every IN_TRANSIT shipment:
  if now − drivers.last_ping_at > 5 minutes
    → update shipments.status = STALE
    → notify admin via SignalR
```

**Payment timeout background job — every 2 minutes:**
```
For every PENDING_PAYMENT shipment:
  if now − shipments.created_at > 10 minutes
    → update shipments.status = CANCELLED
    → update payments.status = FAILED
    → notify customer via SignalR
```

---

## SignalR Groups

| Group | Who joins | What they receive |
|---|---|---|
| `user-{userId}` | Every logged-in user | Personal notifications |
| `shipment-{shipmentId}` | Customer + Admin | GPS updates, status changes |
| `vehicle-TWO_WHEELER` | Online TWO_WHEELER drivers | New job alerts |
| `vehicle-THREE_WHEELER` | Online THREE_WHEELER drivers | New job alerts |
| `vehicle-FOUR_WHEELER` | Online FOUR_WHEELER drivers | New job alerts |
| `vehicle-HEAVY_VEHICLE` | Online HEAVY_VEHICLE drivers | New job alerts |
| `admin` | All admins | Stale alerts, dispute flags |

Driver joins vehicle group on go-online. Leaves all groups on disconnect.

---

## Auth0 Setup

- Validate JWT on every protected endpoint using Auth0 middleware
- Role claim key: `https://logistics/role` (custom claim in Auth0)
- Three policies: `CustomerOnly`, `DriverOnly`, `AdminOnly`
- On register, create user profile in DB with auth0_id from token sub claim
- Driver login check: after Auth0 validates token, check `drivers.approval_status = APPROVED` in DB — reject with 401 if PENDING, REJECTED, or SUSPENDED

---

## Razorpay Integration

- Use Razorpay test mode — test key from Razorpay dashboard
- Test card: `4111 1111 1111 1111`, any future expiry, any CVV
- Always send idempotency key with payment creation request
- Verify webhook signature before processing — use Razorpay webhook secret
- On webhook success → move shipment PENDING_PAYMENT → OPEN

---

## GPS Simulation (Angular — Driver App)

```typescript
const route = interpolateRoute(pickupLat, pickupLng, dropLat, dropLng, 15);
let step = 0;
const interval = setInterval(() => {
  if (step < route.length) {
    this.trackingService.sendLocation(shipmentId, route[step]);
    step++;
  } else {
    clearInterval(interval);
  }
}, 5000);

function interpolateRoute(lat1, lng1, lat2, lng2, steps) {
  return Array.from({ length: steps }, (_, i) => ({
    lat: lat1 + (lat2 - lat1) * (i / steps),
    lng: lng1 + (lng2 - lng1) * (i / steps)
  }));
}
```

---

## LLM Integration (Ollama — localhost:11434)

Two use cases only. Both called internally from service layer — not directly from frontend.

**Use case 1 — parse delivery notes at booking:**
```
POST http://localhost:11434/api/generate
model: qwen2.5
prompt: given this delivery note, return JSON with:
  risk (bool), severity (HIGH/LOW/NONE), reason,
  preferred_delivery_after (TIME or null),
  driver_instruction (clean plain English)
```

**Use case 2 — summarise dispute complaint:**
```
POST http://localhost:11434/api/generate
model: qwen2.5
prompt: given this complaint text, return JSON with:
  summary (one line), type (WRONG_ADDRESS/LATE_DELIVERY/
  DAMAGED_PACKAGE/DRIVER_BEHAVIOUR), suggested_resolution
```

Always parse response as JSON. Wrap Ollama call in try/catch — if Ollama is unavailable, continue without LLM output, store nulls.

---

## Public Tracking Endpoint

No auth. Rate limited to 10 requests/minute/IP using AspNetCoreRateLimit NuGet package.

Three query params must all match: `orderId`, `phone`, `date`
If any mismatch → return 404 with identical response — no hints about which field failed.

---

## NuGet Packages Needed

```
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.AspNetCore.Authentication.JwtBearer
Auth0.AspNetCore.Authentication
Microsoft.AspNetCore.SignalR
BCrypt.Net-Next
AspNetCoreRateLimit
Swashbuckle.AspNetCore
Razorpay
Hangfire.PostgreSql  (for background jobs)
```

---

## Order to Build — Backend

```
1. Project setup + NuGet packages
2. PostgreSQL connection + EF Core config
3. DB models (all 8 tables)
4. Run first migration — create all tables
5. Auth0 middleware setup
6. Auth module (register endpoints)
7. Driver module (registration, vehicles)
8. Admin — driver approval
9. Shipment module (booking, FSM)
10. Payment module (Razorpay + webhook)
11. Tracking module (GPS + SignalR hub)
12. OTP module (confirm pickup + delivery)
13. Notifications module
14. Disputes module
15. Admin dashboard endpoints
16. Public tracking endpoint
17. Background jobs (stale + payment timeout)
18. LLM module (Ollama integration)
19. Swagger documentation
20. Docker setup
```

---

## Order to Build — Frontend

```
1. ng new logistics-frontend
2. Auth0 Angular SDK setup
3. Route guards (CustomerOnly, DriverOnly, AdminOnly)
4. Shared map component (Leaflet.js)
5. Customer — book shipment page (Nominatim autocomplete)
6. Customer — booking confirmation (show sender OTP)
7. Customer — live tracking page (Leaflet map + SignalR)
8. Customer — shipment history
9. Driver — registration page
10. Driver — pending approval screen
11. Driver — go online / job feed
12. Driver — active delivery (OTP entry, GPS simulation)
13. Admin — driver approvals
14. Admin — shipments dashboard
15. Admin — disputes
16. Admin — metrics
17. Public — home page tracker
```

---

## Reference Docs

| Doc | Contents |
|---|---|
| `docs/requirements.md` | Full functional requirements, all features |
| `docs/er_diagram.md` | All 8 tables with columns, types, constraints |
| `docs/api_endpoints.md` | All 41 endpoints with request/response shapes |
| `docs/architecture.md` | System flows, folder structure, component breakdown |

---

## Important Rules for Code Generation

1. Controllers must be thin — no business logic, only service calls
2. All business logic in service layer
3. All DB queries in repository layer
4. Never store OTP in plaintext — always bcrypt hash
5. Never skip geofence check on delivery confirmation
6. Always validate status transitions via FSM in ShipmentService
7. Always wrap claim endpoint in a DB transaction with FOR UPDATE SKIP LOCKED
8. Return consistent error format: `{ statusCode, error, message }`
9. All responses paginated where list is returned: `{ data[], total, page, pageSize }`
10. LLM calls must be wrapped in try/catch — system must work without Ollama running

---

## Project-Wide Code Conventions — Always Follow

These conventions apply to every file generated throughout this project.

1. **Model class names are SINGULAR** — `User`, `Driver`, `Vehicle`, `Shipment`, `Payment`, `Notification`, `Dispute`, `Tracking`. Model file names are singular too. Keep `DbSet<>` property names PLURAL (`Users`, `Drivers`, ...), and keep `Entity<>` and navigation property types in sync with the singular class names.
2. **No basic comments in code** — do not add section dividers, `// Navigation`, or obvious explanatory comments. Only keep a comment when it captures crucial, non-obvious intent (e.g. why a relationship avoids cascade).
3. **Always use `#region` blocks in `Program.cs`** to group setup (e.g. `#region Services`, `#region Pipeline`).
4. **After every task, post a chat summary** of what changed plus the working flow, so it can be noted down.
5. **Always use user-defined exceptions + a global exception middleware.** Never return error objects from `try/catch` inside controllers, and never throw raw framework exceptions (`KeyNotFoundException`, `InvalidOperationException`, etc.) for domain errors.
   - Custom exceptions live in `backend/Exceptions/` and all derive from the abstract `AppException`, which carries a `StatusCode` and an `Error` title. Available types:
     - `NotFoundException` → 404 Not Found
     - `ConflictException` → 409 Conflict
     - `ValidationException` → 400 Bad Request
     - `BusinessRuleException` → 422 Unprocessable Entity (e.g. invalid shipment FSM transitions)
     - Add a new `AppException` subclass when a new error category is needed — do not reuse a poorly-fitting one.
   - Services throw these exceptions; controllers stay thin and contain **no** `try/catch` for domain errors.
   - `ExceptionHandlingMiddleware` (in `backend/Middleware/`) catches every exception, logs unexpected ones, and writes a consistent JSON body: `{ statusCode, error, message }`. Any unhandled exception becomes a 500 with a generic message. It is registered first in the pipeline via `app.UseExceptionHandling()`.
6. **Centralized entity → DTO mapping.** Never write inline `return new SomeDto { ... }` blocks inside services, and never map inside repositories.
   - Mapping lives in `backend/Mappings/`, one static class per aggregate: `UserMappings`, `DriverMappings`, `VehicleMappings`, etc.
   - Mappings are **extension methods** named `To<DtoName>` (target DTO type), e.g. `driver.ToDriverApprovalResponse()`, `vehicle.ToVehicleDto()`, `user.ToRegisterCustomerResponse()`. When a map needs more than one source entity, pass the extra one as an argument: `user.ToRegisterDriverResponse(driver)`.
   - Single entity → `entity.ToDto()`. List of entities → `list.Select(e => e.ToDto()).ToList()` (in the service). Nested DTOs reuse the child aggregate's mapping (e.g. `DriverMappings.ToPendingDriverDto` calls `VehicleMappings.ToPendingDriverVehicleDto`).
   - **Layer rule:** repositories return entities only; services do all mapping and return DTOs; controllers stay thin.