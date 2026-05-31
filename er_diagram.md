# Logistics & Shipment Tracking System — ER Diagram

```mermaid
erDiagram

    users {
        UUID id PK
        VARCHAR auth0_id
        VARCHAR full_name
        VARCHAR email
        VARCHAR phone
        VARCHAR role
        BOOLEAN is_active
        TIMESTAMP created_at
    }

    drivers {
        UUID id PK
        UUID user_id FK
        VARCHAR license_number
        VARCHAR approval_status
        VARCHAR operational_status
        UUID active_vehicle_id FK
        DECIMAL current_lat
        DECIMAL current_lng
        TIMESTAMP last_ping_at
        TEXT approval_reason
        TIMESTAMP approved_at
        INT cancel_count
        TIMESTAMP created_at
    }

    vehicles {
        UUID id PK
        UUID driver_id FK
        VARCHAR vehicle_type
        VARCHAR vehicle_number
        BOOLEAN is_active
        TIMESTAMP created_at
    }

    shipments {
        UUID id PK
        VARCHAR order_id
        UUID customer_id FK
        UUID driver_id FK
        UUID vehicle_id FK
        VARCHAR status
        TEXT pickup_address
        DECIMAL pickup_lat
        DECIMAL pickup_lng
        TEXT drop_address
        DECIMAL drop_lat
        DECIMAL drop_lng
        VARCHAR receiver_name
        VARCHAR receiver_phone
        VARCHAR package_type
        DECIMAL weight_kg
        VARCHAR preferred_window
        TEXT special_notes
        BOOLEAN risk_flag
        VARCHAR risk_severity
        VARCHAR risk_reason
        TIME preferred_delivery_after
        TEXT driver_instruction
        VARCHAR sender_otp_hash
        INT sender_otp_attempts
        TIMESTAMP sender_otp_expires_at
        VARCHAR receiver_otp_hash
        INT receiver_otp_attempts
        TIMESTAMP receiver_otp_expires_at
        BOOLEAN cash_collected
        UUID status_changed_by FK
        TIMESTAMP status_updated_at
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    tracking {
        UUID id PK
        UUID shipment_id FK
        UUID driver_id FK
        DECIMAL latitude
        DECIMAL longitude
        TIMESTAMP recorded_at
    }

    payments {
        UUID id PK
        UUID shipment_id FK
        VARCHAR method
        DECIMAL amount
        VARCHAR status
        VARCHAR idempotency_key
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    notifications {
        UUID id PK
        UUID user_id FK
        UUID shipment_id FK
        VARCHAR title
        TEXT message
        BOOLEAN is_read
        TIMESTAMP created_at
    }

    disputes {
        UUID id PK
        UUID shipment_id FK
        UUID raised_by FK
        TEXT complaint_text
        TEXT llm_summary
        VARCHAR llm_type
        TEXT llm_suggested_resolution
        VARCHAR status
        UUID resolved_by FK
        TEXT resolution_notes
        TIMESTAMP created_at
        TIMESTAMP resolved_at
    }

    users ||--o| drivers : "is a"
    users ||--o{ shipments : "books"
    users ||--o{ notifications : "receives"
    users ||--o{ disputes : "raises"
    drivers ||--o{ vehicles : "owns"
    drivers ||--o{ shipments : "claims"
    drivers ||--o{ tracking : "sends"
    vehicles ||--o{ shipments : "used in"
    shipments ||--o{ tracking : "has"
    shipments ||--|| payments : "has"
    shipments ||--o{ notifications : "triggers"
    shipments ||--o{ disputes : "has"
```

---

## Tables Summary

| Table | Type | LLM Columns |
|---|---|---|
| users | Master | None |
| drivers | Master | None |
| vehicles | Master | None |
| shipments | Master | risk_flag, risk_severity, risk_reason, preferred_delivery_after, driver_instruction |
| tracking | Transactional | None |
| payments | Transactional | None |
| notifications | Transactional | None |
| disputes | Transactional | llm_summary, llm_type, llm_suggested_resolution |

## Enums

| Column | Table | Values |
|---|---|---|
| role | users | CUSTOMER, DRIVER, ADMIN |
| approval_status | drivers | PENDING, APPROVED, REJECTED, SUSPENDED |
| operational_status | drivers | ONLINE, OFFLINE, ON_DELIVERY |
| vehicle_type | vehicles | TWO_WHEELER, THREE_WHEELER, FOUR_WHEELER, HEAVY_VEHICLE |
| status | shipments | PENDING_PAYMENT, OPEN, ASSIGNED, IN_TRANSIT, DELIVERED, CANCELLED, PICKUP_FAILED, STALE |
| package_type | shipments | DOCUMENT, SMALL_PARCEL, LARGE_PARCEL, FRAGILE, HOUSEHOLD |
| preferred_window | shipments | MORNING, AFTERNOON, EVENING |
| risk_severity | shipments | HIGH, LOW, NONE |
| method | payments | COD, ONLINE |
| status | payments | PENDING, SUCCESS, FAILED, REFUNDED |
| llm_type | disputes | WRONG_ADDRESS, LATE_DELIVERY, DAMAGED_PACKAGE, DRIVER_BEHAVIOUR |
| status | disputes | OPEN, RESOLVED, ESCALATED |

