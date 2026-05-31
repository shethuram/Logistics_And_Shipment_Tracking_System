# Logistics & Shipment Tracking System
## API Endpoints

Base URL: `/api`
Authentication: Auth0 JWT — pass as `Authorization: Bearer <token>` on all protected routes.
Roles: CUSTOMER, DRIVER, ADMIN

---

## 1. Authentication

---

### POST /api/auth/register/customer
Creates customer profile in DB after Auth0 signup.

**Auth:** None
**Body:**
```json
{
  "auth0Id": "auth0|abc123",
  "fullName": "Arjun Kumar",
  "email": "arjun@email.com",
  "phone": "9876543210"
}
```
**Response: 201**
```json
{
  "id": "uuid",
  "fullName": "Arjun Kumar",
  "role": "CUSTOMER"
}
```

---

### POST /api/auth/register/driver
Creates driver profile in DB after Auth0 signup.

**Auth:** None
**Body:**
```json
{
  "auth0Id": "auth0|xyz456",
  "fullName": "Ravi Shankar",
  "email": "ravi@email.com",
  "phone": "9123456789",
  "licenseNumber": "TN3320230012345"
}
```
**Response: 201**
```json
{
  "id": "uuid",
  "fullName": "Ravi Shankar",
  "approvalStatus": "PENDING"
}
```

---

## 2. Driver Management

---

### GET /api/drivers/{id}/vehicles
Get all vehicles of a driver.

**Auth:** DRIVER, ADMIN
**Response: 200**
```json
[
  {
    "id": "uuid",
    "vehicleType": "TWO_WHEELER",
    "vehicleNumber": "TN33AB1234",
    "isActive": true,
    "createdAt": "2024-01-15T10:00:00Z"
  }
]
```

---

### POST /api/drivers/{id}/vehicles
Add a new vehicle for a driver.

**Auth:** DRIVER
**Body:**
```json
{
  "vehicleType": "TWO_WHEELER",
  "vehicleNumber": "TN33AB1234"
}
```
**Response: 201**
```json
{
  "id": "uuid",
  "vehicleType": "TWO_WHEELER",
  "vehicleNumber": "TN33AB1234",
  "isActive": false
}
```

---

### PUT /api/drivers/{id}/vehicles/{vehicleId}
Update vehicle details.

**Auth:** DRIVER
**Body:**
```json
{
  "vehicleNumber": "TN33CD5678"
}
```
**Response: 200**

---

### POST /api/drivers/{id}/vehicles/{vehicleId}/set-active
Set a vehicle as the active vehicle for a driver.

**Auth:** DRIVER
**Response: 200**
```json
{
  "activeVehicleId": "uuid",
  "vehicleType": "TWO_WHEELER"
}
```

---

### POST /api/drivers/{id}/go-online
Driver goes online — starts receiving job notifications.

**Auth:** DRIVER
**Body:**
```json
{
  "latitude": 11.6643,
  "longitude": 78.1460
}
```
**Response: 200**
```json
{
  "operationalStatus": "ONLINE",
  "activeVehicleType": "TWO_WHEELER"
}
```
**Error: 400** — if no active vehicle set

---

### POST /api/drivers/{id}/go-offline
Driver goes offline.

**Auth:** DRIVER
**Response: 200**
```json
{
  "operationalStatus": "OFFLINE"
}
```

---

## 3. Admin — Driver Approval

---

### GET /api/admin/drivers/pending
Get all drivers with approval status PENDING.

**Auth:** ADMIN
**Query params:** `page`, `pageSize`
**Response: 200**
```json
{
  "data": [
    {
      "id": "uuid",
      "fullName": "Ravi Shankar",
      "phone": "9123456789",
      "licenseNumber": "TN3320230012345",
      "vehicles": [
        { "vehicleType": "TWO_WHEELER", "vehicleNumber": "TN33AB1234" }
      ],
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ],
  "total": 10,
  "page": 1,
  "pageSize": 20
}
```

---

### POST /api/admin/drivers/{id}/approve
Approve a driver registration.

**Auth:** ADMIN
**Response: 200**
```json
{
  "id": "uuid",
  "approvalStatus": "APPROVED",
  "approvedAt": "2024-01-15T11:00:00Z"
}
```

---

### POST /api/admin/drivers/{id}/reject
Reject a driver registration with reason.

**Auth:** ADMIN
**Body:**
```json
{
  "reason": "License number invalid"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "approvalStatus": "REJECTED",
  "approvalReason": "License number invalid"
}
```

---

### POST /api/admin/drivers/{id}/suspend
Suspend an active driver.

**Auth:** ADMIN
**Body:**
```json
{
  "reason": "Multiple customer complaints"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "approvalStatus": "SUSPENDED",
  "approvalReason": "Multiple customer complaints"
}
```

---

## 4. Shipments

---

### POST /api/shipments
Create a new shipment booking.

**Auth:** CUSTOMER
**Body:**
```json
{
  "pickupAddress": "Salem Junction, Salem",
  "pickupLat": 11.6643,
  "pickupLng": 78.1460,
  "dropAddress": "Big Bazaar, Salem",
  "dropLat": 11.6750,
  "dropLng": 78.1560,
  "receiverName": "Priya S",
  "receiverPhone": "9988776655",
  "packageType": "SMALL_PARCEL",
  "weightKg": 3.5,
  "preferredWindow": "EVENING",
  "specialNotes": "At work, deliver after 5pm",
  "paymentMethod": "ONLINE"
}
```
**Response: 201**
```json
{
  "id": "uuid",
  "orderId": "TRK-20240115-00001",
  "status": "PENDING_PAYMENT",
  "paymentUrl": "https://razorpay.com/pay/...",
  "senderOtp": "4821"
}
```
**Notes:**
- If COD → status immediately OPEN, no paymentUrl
- senderOtp shown only once here — not stored in plaintext after this

---

### GET /api/shipments/{id}
Get shipment details.

**Auth:** CUSTOMER (own), DRIVER (assigned), ADMIN (any)
**Response: 200**
```json
{
  "id": "uuid",
  "orderId": "TRK-20240115-00001",
  "status": "IN_TRANSIT",
  "pickupAddress": "Salem Junction, Salem",
  "dropAddress": "Big Bazaar, Salem",
  "receiverName": "Priya S",
  "receiverPhone": "9988776655",
  "packageType": "SMALL_PARCEL",
  "weightKg": 3.5,
  "preferredWindow": "EVENING",
  "specialNotes": "At work, deliver after 5pm",
  "driverInstruction": "[LLM] Customer at work, deliver after 5 PM.",
  "riskFlag": false,
  "driver": {
    "id": "uuid",
    "fullName": "Ravi Shankar",
    "vehicleType": "TWO_WHEELER",
    "vehicleNumber": "TN33AB1234"
  },
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T11:30:00Z"
}
```

---

### PUT /api/shipments/{id}
Update shipment — only allowed when status is PENDING_PAYMENT or OPEN.

**Auth:** CUSTOMER
**Body:**
```json
{
  "preferredWindow": "MORNING",
  "specialNotes": "Updated notes"
}
```
**Response: 200**

---

### DELETE /api/shipments/{id}
Cancel a shipment — only allowed before status = ASSIGNED.

**Auth:** CUSTOMER
**Response: 200**
```json
{
  "id": "uuid",
  "status": "CANCELLED",
  "refundInitiated": true
}
```

---

### GET /api/shipments
Get shipments list with search, filter, and pagination.

**Auth:** CUSTOMER (own), ADMIN (all)
**Query params:**

| Param | Type | Notes |
|---|---|---|
| search | string | Order ID or phone number |
| status | string | Filter by status |
| dateFrom | date | Filter from date |
| dateTo | date | Filter to date |
| page | int | Default 1 |
| pageSize | int | Default 20 |

**Response: 200**
```json
{
  "data": [ { "...shipment objects..." } ],
  "total": 100,
  "page": 1,
  "pageSize": 20
}
```

---

### GET /api/shipments/available
Get open shipments eligible for the requesting driver's active vehicle type. Sorted by distance to pickup.

**Auth:** DRIVER
**Response: 200**
```json
[
  {
    "id": "uuid",
    "orderId": "TRK-20240115-00001",
    "pickupAddress": "Salem Junction, Salem",
    "dropAddress": "Big Bazaar, Salem",
    "packageType": "SMALL_PARCEL",
    "weightKg": 3.5,
    "preferredWindow": "EVENING",
    "senderPhone": "9876543210",
    "receiverPhone": "9988776655",
    "distanceToPickupKm": 1.2,
    "driverInstruction": "[LLM] Customer at work, deliver after 5 PM."
  }
]
```

---

### POST /api/shipments/{id}/claim
Driver claims an open shipment.

**Auth:** DRIVER
**Response: 200**
```json
{
  "id": "uuid",
  "status": "ASSIGNED",
  "orderId": "TRK-20240115-00001"
}
```
**Error: 409** — shipment already claimed by another driver
**Error: 400** — driver already has an active shipment

---

### POST /api/shipments/{id}/cancel-claim
Driver cancels a claimed shipment before pickup.

**Auth:** DRIVER
**Body:**
```json
{
  "reason": "Vehicle breakdown"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "status": "OPEN",
  "driverCancelCount": 2
}
```

---

### POST /api/shipments/{id}/confirm-pickup
Driver enters sender OTP to confirm package collection.

**Auth:** DRIVER
**Body:**
```json
{
  "otp": "4821"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "status": "IN_TRANSIT"
}
```
**Error: 400** — wrong OTP
**Error: 400** — OTP expired
**Error: 429** — max attempts reached, flagged to admin

---

### POST /api/shipments/{id}/confirm-delivery
Driver enters receiver OTP to confirm delivery.

**Auth:** DRIVER
**Body:**
```json
{
  "otp": "7362",
  "driverLat": 11.6750,
  "driverLng": 78.1560
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "status": "DELIVERED",
  "deliveredAt": "2024-01-15T14:30:00Z"
}
```
**Error: 400** — wrong OTP
**Error: 400** — driver not within 200m of drop address
**Error: 400** — OTP expired
**Error: 429** — max attempts reached, flagged to admin

---

### POST /api/shipments/{id}/cash-collected
Driver confirms cash collected for COD shipments.

**Auth:** DRIVER
**Response: 200**
```json
{
  "id": "uuid",
  "cashCollected": true
}
```

---

### POST /api/shipments/{id}/pickup-failed
Driver marks pickup as failed — sender not available.

**Auth:** DRIVER
**Body:**
```json
{
  "reason": "No one at pickup location"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "status": "PICKUP_FAILED"
}
```

---

## 5. Public Tracking

---

### GET /api/public/track
Track a shipment without login.

**Auth:** None
**Rate limit:** 10 requests / minute / IP
**Query params:** `orderId`, `phone`, `date`

**Example:** `/api/public/track?orderId=TRK-20240115-00001&phone=9876543210&date=2024-01-15`

**Response: 200**
```json
{
  "orderId": "TRK-20240115-00001",
  "status": "IN_TRANSIT",
  "pickupLocation": "Salem Junction, Salem",
  "dropLocation": "Big Bazaar, Salem",
  "preferredWindow": "EVENING",
  "driverInstruction": "[LLM] Deliver after 5 PM.",
  "timeline": [
    { "status": "OPEN", "time": "2024-01-15T10:00:00Z" },
    { "status": "ASSIGNED", "time": "2024-01-15T10:15:00Z" },
    { "status": "IN_TRANSIT", "time": "2024-01-15T11:00:00Z" }
  ],
  "driverLocation": {
    "latitude": 11.6700,
    "longitude": 78.1510
  },
  "receiverOtp": "7362"
}
```
**Error: 404** — no match found (same response whether wrong orderId, phone, or date — no hints)

---

## 6. GPS Tracking

---

### POST /api/tracking/location
Driver sends GPS ping.

**Auth:** DRIVER
**Body:**
```json
{
  "shipmentId": "uuid",
  "latitude": 11.6700,
  "longitude": 78.1510
}
```
**Response: 200**
**Side effect:** Updates `drivers.current_lat`, `drivers.current_lng`, `drivers.last_ping_at`. Saves row to tracking table. Broadcasts via SignalR to customer map.

---

### GET /api/tracking/{shipmentId}/live
Get current driver location for a shipment.

**Auth:** CUSTOMER (own), ADMIN
**Response: 200**
```json
{
  "shipmentId": "uuid",
  "driverLocation": {
    "latitude": 11.6700,
    "longitude": 78.1510,
    "recordedAt": "2024-01-15T13:45:00Z"
  }
}
```

---

### GET /api/tracking/{shipmentId}/history
Get full GPS ping history for a shipment.

**Auth:** ADMIN
**Response: 200**
```json
[
  {
    "latitude": 11.6643,
    "longitude": 78.1460,
    "recordedAt": "2024-01-15T11:00:00Z"
  },
  {
    "latitude": 11.6670,
    "longitude": 78.1480,
    "recordedAt": "2024-01-15T11:00:05Z"
  }
]
```

---

## 7. Payments

---

### POST /api/payments/initiate
Initiate Razorpay payment for a shipment.

**Auth:** CUSTOMER
**Body:**
```json
{
  "shipmentId": "uuid"
}
```
**Response: 200**
```json
{
  "razorpayOrderId": "order_xyz",
  "amount": 150.00,
  "currency": "INR",
  "idempotencyKey": "usr_uuid-shp_uuid-1705300000"
}
```

---

### POST /api/webhooks/payment
Razorpay webhook — fires on payment success or failure.

**Auth:** None (verified by Razorpay signature)
**Body:** Razorpay webhook payload
**Response: 200**
**Side effect:** On success → shipment status moves to OPEN. On failure → status stays PENDING_PAYMENT.

---

### GET /api/payments/{shipmentId}/status
Get payment status for a shipment.

**Auth:** CUSTOMER (own), ADMIN
**Response: 200**
```json
{
  "shipmentId": "uuid",
  "method": "ONLINE",
  "amount": 150.00,
  "status": "SUCCESS",
  "createdAt": "2024-01-15T10:00:00Z"
}
```

---

## 8. Notifications

---

### GET /api/notifications/my
Get notifications for the logged-in user.

**Auth:** CUSTOMER, DRIVER, ADMIN
**Query params:** `page`, `pageSize`
**Response: 200**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Driver Assigned",
      "message": "A driver has been assigned to your shipment TRK-20240115-00001",
      "isRead": false,
      "createdAt": "2024-01-15T10:15:00Z"
    }
  ],
  "unreadCount": 3,
  "total": 10,
  "page": 1,
  "pageSize": 20
}
```

---

### POST /api/notifications/{id}/read
Mark a notification as read.

**Auth:** CUSTOMER, DRIVER, ADMIN
**Response: 200**
```json
{
  "id": "uuid",
  "isRead": true
}
```

---

## 9. Disputes

---

### POST /api/disputes
Customer raises a dispute against a shipment.

**Auth:** CUSTOMER
**Body:**
```json
{
  "shipmentId": "uuid",
  "complaintText": "Driver marked delivered but I never received the package"
}
```
**Response: 201**
```json
{
  "id": "uuid",
  "status": "OPEN",
  "createdAt": "2024-01-15T15:00:00Z"
}
```

---

### GET /api/admin/disputes
Get all disputes.

**Auth:** ADMIN
**Query params:** `status`, `page`, `pageSize`
**Response: 200**
```json
{
  "data": [
    {
      "id": "uuid",
      "shipmentId": "uuid",
      "orderId": "TRK-20240115-00001",
      "raisedBy": "Arjun Kumar",
      "complaintText": "Driver marked delivered but I never received the package",
      "llmSummary": "[LLM] Driver marked delivered but customer did not receive package.",
      "llmType": "[LLM] WRONG_ADDRESS",
      "llmSuggestedResolution": "[LLM] Investigate driver GPS history for delivery timestamp.",
      "status": "OPEN",
      "createdAt": "2024-01-15T15:00:00Z"
    }
  ],
  "total": 5,
  "page": 1,
  "pageSize": 20
}
```

---

### POST /api/admin/disputes/{id}/resolve
Admin resolves a dispute.

**Auth:** ADMIN
**Body:**
```json
{
  "resolutionNotes": "Verified GPS history — driver delivered to wrong building. Refund initiated.",
  "status": "RESOLVED"
}
```
**Response: 200**
```json
{
  "id": "uuid",
  "status": "RESOLVED",
  "resolvedAt": "2024-01-15T16:00:00Z"
}
```

---

## 10. Admin

---

### GET /api/admin/shipments
Get all shipments with search, filter, and pagination.

**Auth:** ADMIN
**Query params:** `search`, `status`, `dateFrom`, `dateTo`, `page`, `pageSize`
**Response: 200** — same structure as GET /api/shipments

---

### POST /api/admin/shipments/{id}/reassign
Manually reassign a stale or stuck shipment — clears driver and resets to OPEN.

**Auth:** ADMIN
**Response: 200**
```json
{
  "id": "uuid",
  "status": "OPEN",
  "driverId": null
}
```

---

### GET /api/admin/metrics
Get system metrics.

**Auth:** ADMIN
**Response: 200**
```json
{
  "totalShipments": 500,
  "delivered": 420,
  "pending": 45,
  "cancelled": 20,
  "failed": 15,
  "avgDeliveryTimeMinutes": 48,
  "staleShipments": 3,
  "codPending": 2,
  "driversOnline": 12,
  "driversWithHighCancelCount": 1
}
```

---

### GET /api/admin/export/shipments
Export shipments as CSV.

**Auth:** ADMIN
**Query params:** `status`, `dateFrom`, `dateTo`
**Response: 200** — CSV file download

---

## [LLM] 11. LLM Endpoints

---

### [LLM] POST /api/llm/parse-delivery-note
Parse customer special notes — called internally at booking.

**Auth:** Internal (called from shipment service, not directly from frontend)
**Body:**
```json
{
  "shipmentId": "uuid",
  "notes": "At work, deliver after 5pm. Leave with security if not home."
}
```
**Response: 200**
```json
{
  "riskFlag": true,
  "riskSeverity": "HIGH",
  "riskReason": "unattended_delivery",
  "preferredDeliveryAfter": "17:00",
  "driverInstruction": "Customer at work, deliver after 5 PM. Do not leave with security — OTP required."
}
```

---

### [LLM] POST /api/llm/summarise-dispute
Summarise dispute complaint — called internally when dispute is created.

**Auth:** Internal (called from dispute service, not directly from frontend)
**Body:**
```json
{
  "disputeId": "uuid",
  "complaintText": "Driver marked delivered but I never received the package. I was home all day."
}
```
**Response: 200**
```json
{
  "summary": "Driver marked delivered but customer claims non-receipt despite being home.",
  "type": "WRONG_ADDRESS",
  "suggestedResolution": "Investigate driver GPS history for delivery timestamp and location."
}
```

---

## Error Responses

All endpoints return consistent error format:

```json
{
  "statusCode": 400,
  "error": "Bad Request",
  "message": "OTP has expired. A new OTP has been generated."
}
```

| Code | Meaning |
|---|---|
| 400 | Bad request — validation error or business rule violation |
| 401 | Unauthorized — missing or invalid token |
| 403 | Forbidden — valid token but wrong role |
| 404 | Not found |
| 409 | Conflict — e.g. shipment already claimed |
| 422 | Unprocessable — invalid status transition |
| 429 | Too many requests — rate limit or OTP attempts exceeded |
| 500 | Internal server error |

---

