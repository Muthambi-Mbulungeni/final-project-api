\# Frontend API Contracts



\*\*Base URL:\*\* `http://localhost:5151`

\*\*Global Headers:\*\* `Content-Type: application/json`



\---



\## 1. Global Response Shapes



\### Standard Error Response (400 / 500)

Used for all validation and server errors to allow a single frontend error handler.

```json

{

&#x20; "message": "Validation failed.",

&#x20; "errors": \[

&#x20;   "Email is required.",

&#x20;   "Desired start date cannot be in the past."

&#x20; ]

}

