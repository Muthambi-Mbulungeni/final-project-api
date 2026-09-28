# Frontend API Contracts

**Base URL:** `http://localhost:5151`
**Global Headers:** `Content-Type: application/json`

---

## 1. Global Response Shapes

### Standard Error Response (400 / 500)
{
  "message": "Validation failed.",
  "errors": [
    "Email is required.",
    "Desired start date cannot be in the past."
  ]
}

### Paginated List Wrapper
{
  "data": [ ... ],
  "totalRecords": 45,
  "totalPages": 5,
  "currentPage": 1
}

---

## 2. Cases Endpoints

### GET `/api/cases`
Query Params: `?page=1&pageSize=10&search=xyz`
{
  "data": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "clientName": "John Doe",
      "visaCategoryName": "Critical Skills Work Visa",
      "stage": "UnderReview",
      "isArchived": false,
      "createdAt": "2026-08-28T00:00:00Z",
      "lastUpdated": "2026-09-28T08:00:00Z"
    }
  ],
  "totalRecords": 45,
  "totalPages": 5,
  "currentPage": 1
}

### GET `/api/cases/{id}`
{
  "id": "11111111-1111-1111-1111-111111111111",
  "clientName": "John Doe",
  "consultantName": "Riaad",
  "visaCategoryName": "Critical Skills Work Visa",
  "stage": "UnderReview",
  "isArchived": false,
  "createdAt": "2026-08-28T00:00:00Z",
  "lastUpdated": "2026-09-28T08:00:00Z"
}

### GET `/api/cases/client/{clientId}`
{
  "data": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "clientId": "77777777-7777-7777-7777-777777777777",
      "clientName": "John Doe",
      "stage": "UnderReview",
      "createdAt": "2026-08-28T00:00:00Z"
    }
  ],
  "totalRecords": 1,
  "totalPages": 1,
  "currentPage": 1
}

---

## 3. Leads Endpoints

### GET `/api/leads`
Query Params: `?page=1&pageSize=10`
{
  "data": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "fullName": "Jane Smith",
      "email": "jane@example.com",
      "immigrationStatus": "Foreigner",
      "triageStatus": "Pending",
      "submittedAt": "2026-09-27T14:30:00Z"
    }
  ],
  "totalRecords": 12,
  "totalPages": 2,
  "currentPage": 1
}

---

## 4. Role-Specific Stats Endpoints

### GET `/api/stats/admin`
{
  "totalUsers": 45,
  "systemErrors": 2,
  "activeCases": 112,
  "pendingLeads": 15
}

### GET `/api/stats/consultant`
{
  "activeCases": 12,
  "pendingLeads": 5,
  "documentsAwaitingReview": 8
}

---

## 5. Admin Management Endpoints

### GET `/api/admin/users`
Query Params: `?page=1&pageSize=10`
{
  "data": [
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "fullName": "Riaad",
      "email": "riaad@nexus.com",
      "role": "Consultant",
      "isActive": true
    }
  ],
  "totalRecords": 8,
  "totalPages": 1,
  "currentPage": 1
}

### GET `/api/admin/audit-logs`
Query Params: `?page=1&pageSize=20`
{
  "data": [
    {
      "id": "44444444-4444-4444-4444-444444444444",
      "userId": "33333333-3333-3333-3333-333333333333",
      "action": "User Suspended",
      "entityType": "User",
      "entityId": "33333333-3333-3333-3333-333333333333",
      "createdAt": "2026-09-28T09:15:00Z"
    }
  ],
  "totalRecords": 250,
  "totalPages": 13,
  "currentPage": 1
}

---

## 6. Notifications

### GET `/api/notifications`
[
  {
    "id": "66666666-6666-6666-6666-666666666666",
    "message": "Your passport scan has been approved.",
    "isRead": false,
    "createdAt": "2026-09-28T08:00:00Z"
  }
]