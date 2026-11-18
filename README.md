# 🚌 Travel Buddy — Integrated Public Transport Pass & Digital Fare Management Platform

**Version:** 4.0  
**Stack:** C#, ASP.NET Core MVC / Web API, Entity Framework Core, SQL Server / SQLite, Razorpay Sandbox  

---

## 📖 Table of Contents
1. [Overview & Product Goals](#-overview--product-goals)
2. [Key Capabilities & Business Rules](#-key-capabilities--business-rules)
   - [Pass Boarding Policy](#1-pass-boarding-policy)
   - [Razorpay Test Mode Integration](#2-razorpay-test-mode-integration)
   - [Dynamic Cryptographic QR System](#3-dynamic-cryptographic-qr-system)
   - [Refund & Cancellation Policy](#4-refund--cancellation-policy)
3. [Architecture & Technology Stack](#-architecture--technology-stack)
4. [Project Structure](#-project-structure)
5. [Database Schema & Data Model](#-database-schema--data-model)
6. [API Contract & Endpoints](#-api-contract--endpoints)
7. [Getting Started & Installation](#-getting-started--installation)
8. [Default Seeded Credentials](#-default-seeded-credentials)
9. [Automated Testing Suite](#-automated-testing-suite)
10. [Security & Compliance](#-security--compliance)

---

## 🌟 Overview & Product Goals

**Travel Buddy** is a comprehensive, production-oriented public transport fare management solution designed for passengers, bus conductors, and municipal transport administrators. 

### Core Goals:
- **Simplify Commuting**: Mobile-first pass discovery, checkout, and digital ticket presentation.
- **Anti-Fraud Boarding**: Dynamic rotating cryptographic QR credentials that prevent screenshot sharing and unauthorized reuse.
- **Conductor POS Terminal**: Rapid on-bus QR validation with instant feedback (`VALID`, `EXPIRED`, `DUPLICATE_SCAN`, `ALREADY_USED`).
- **Administrative Control**: Real-time revenue analytics, active pass tracking, immutable audit logs, and refund handling.

---

## 🚀 Key Capabilities & Business Rules

### 1. Pass Boarding Policy
| Pass Type | Allowed Boardings | Validity Window | Usage Rules |
| :--- | :--- | :--- | :--- |
| **Single Journey** | 1 boarding journey | 2 hours from issue | Automatically consumed upon 1st scan; marked Expired. |
| **Daily Pass** | Unlimited boardings | 1 calendar day | Multi-boarding across city corridors until midnight. |
| **Weekly Pass** | Unlimited boardings | 7 consecutive days | Unlimited rides with duplicate scan protection. |
| **Monthly Pass** | Unlimited boardings | 30 consecutive days | Best value multi-ride corridor coverage. |

* **Anti-Passback & Replay Guard**: Any re-scan of the same credential within **30 seconds** on the same vehicle is rejected with status `DUPLICATE_SCAN` to prevent pass sharing.

---

### 2. Razorpay Test Mode Integration
- **Server-Calculated Orders**: Client-side prices are never trusted. All totals are computed authoritative server-side.
- **Simulated Payment Gateways**: Supports instant UPI (`success@razorpay`), Debit/Credit Cards, and Net Banking.
- **Cryptographic Verification**: Verifies checkout HMAC-SHA256 signature prior to pass creation.
- **Idempotency**: Prevents double-charging or duplicate pass issuance.

---

### 3. Dynamic Cryptographic QR System
- **Opaque High-Entropy Bearer Tokens**: Contains no PII (phone number, name, or Aadhaar) to protect privacy. Tokens are formatted as `TB1.<256-bit-url-safe-token>`.
- **SHA-256 Hashing at Rest**: Only cryptographic hashes are stored in the database.
- **Live 30-Second Rotation**: Passenger wallet renders real-time dynamic QR codes with an animated countdown timer and auto-refresh.

---

### 4. Refund & Cancellation Policy
- **100% Full Refund**: Eligible if the pass is cancelled **before** validity starts or before any boarding scan is made.
- **Non-Refundable After Boarding**: Once scanned and verified by a conductor, the pass is locked and ineligible for cancellation.
- **Audited Workflow**: Every cancellation records gateway refund references, updates payment ledger, and revokes QR tokens.

---

## 🛠️ Architecture & Technology Stack

- **Framework**: .NET 10 / ASP.NET Core
- **Data Access**: Entity Framework Core 10 (SQLite for local zero-dependency development; ready for SQL Server in production)
- **Identity & Security**: ASP.NET Core Identity with role-based authorization (`Passenger`, `Operator`, `Admin`)
- **Payment Gateway**: Razorpay .NET SDK (Sandbox Mode)
- **QR Engine**: QRCoder (High-performance PNG Byte Matrix)
- **Frontend**: Razor Pages, Bootstrap 5, Plus Jakarta Sans, Inter, Bootstrap Icons
- **Testing**: xUnit Unit Test Suite

---

## 📁 Project Structure

```text
TravelBuddy/
├── Controllers/
│   └── ApiControllers.cs            # Auth, Routes, Orders, Payments, Passes, Validations APIs
├── Data/
│   ├── TravelBuddyDbContext.cs      # EF Core DbContext with relations and indexes
│   └── DbInitializer.cs             # Automatic seeding (Roles, Users, Routes, Fare Products)
├── DTOs/
│   └── TravelBuddyDtos.cs           # Request and response transfer models
├── Models/
│   ├── Entities.cs                  # Order, Payment, Pass, QrCredential, Route, Stop, Validation, Refund
│   └── Enums.cs                     # Status enums for all domain entities
├── Pages/
│   ├── Shared/_Layout.cshtml        # Responsive layout with modern design system
│   ├── Index.cshtml                 # Pass catalogue, route sequence & Razorpay checkout
│   ├── MyPasses.cshtml              # Passenger wallet with dynamic rotating QR
│   ├── Operator.cshtml              # Conductor POS scanner terminal
│   ├── Admin.cshtml                 # System metrics, ledger, audit trails
│   └── Login.cshtml                 # Role switcher & authentication
├── Services/
│   ├── QrCredentialService.cs       # Token generation, SHA-256 hashing, PNG QR rendering
│   ├── RazorpayService.cs          # Order creation, HMAC verification, refund simulation
│   └── PassService.cs               # Business logic, anti-passback, cancellation eligibility
├── tests/
│   └── TravelBuddy.UnitTests/       # xUnit unit test project
├── Program.cs                       # Dependency injection & HTTP pipeline
├── appsettings.json                 # Connection strings and Razorpay configuration
└── README.md                        # Documentation
```

---

## 🗄️ Database Schema & Data Model

- **Users**: Extended `IdentityUser` with full name and account status.
- **Routes & Stops**: Bus transit corridors with ordered sequences (`RouteStops`).
- **FareProducts**: Pass catalog with price, currency, validity duration, and route coverage.
- **Orders & OrderItems**: Immutable purchase records.
- **Payments**: Gateway payment records with references and HMAC verification state.
- **Passes**: Issued transit credentials linked to orders.
- **QrCredentials**: Cryptographic rotating tokens and SHA-256 hashes.
- **BoardingValidations**: Conductor validation logs with timestamp, result code, and vehicle ID.
- **Refunds**: Audited refund ledger.
- **AuditLogs**: System-wide immutable activity logs.

---

## 🔌 API Contract & Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/auth/register` | Register new passenger account |
| `POST` | `/api/v1/auth/login` | Sign in and create persistent session |
| `GET` | `/api/v1/routes` | List published routes and stop sequences |
| `GET` | `/api/v1/fare-products` | List available transit pass products |
| `POST` | `/api/v1/orders` | Create pending order with server-calculated price |
| `POST` | `/api/v1/payments/verify` | Verify Razorpay checkout signature and activate pass |
| `GET` | `/api/v1/passes` | Retrieve passenger passes |
| `GET` | `/api/v1/passes/{id}/qr` | Generate dynamic rotating QR credential |
| `POST` | `/api/v1/passes/{id}/cancel` | Cancel pass and initiate refund |
| `POST` | `/api/v1/validations/qr` | Validate conductor scan against boarding policy |

---

## 💻 Getting Started & Installation

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

### Run Locally
```bash
# Clone the repository
git clone https://github.com/Vinay-Budde/travelbuddy.git
cd travelbuddy

# Restore dependencies
dotnet restore

# Run unit tests
dotnet test

# Start the application
dotnet run --urls "http://localhost:5000"
```

Open your browser to:
- **Passenger Portal**: `http://localhost:5000`
- **Pass Wallet**: `http://localhost:5000/MyPasses`
- **Conductor Scanner**: `http://localhost:5000/Operator`
- **Admin Dashboard**: `http://localhost:5000/Admin`

---

## 🔑 Default Seeded Credentials

| Role | Email | Password |
| :--- | :--- | :--- |
| **Passenger** | `passenger@travelbuddy.local` | `Passenger@123456` |
| **Conductor** | `conductor1@travelbuddy.local` | `Operator@123456` |
| **Administrator** | `admin@travelbuddy.local` | `Admin@123456` |

---

## 🧪 Automated Testing Suite

Run all unit tests using:
```bash
dotnet test
```

Verified test scenarios:
1. `QrCredentialService_Generates_Opaque_URLSafe_Token`: Validates prefix, entropy, and 64-character SHA-256 hash.
2. `RazorpayService_Simulated_Signature_Verification_Works`: Validates test signature confirmation and rejects spoofed payments.
3. `QrCredentialService_Generates_Valid_Base64_QrCode`: Validates base64 data URL rendering and refresh interval.

---

## 🛡️ Security & Compliance
- **No Plaintext Bearer Tokens**: Scanned tokens are hashed before storage.
- **PII Privacy**: QR contains zero identifiable passenger information.
- **Anti-Tampering**: Server recalculates prices and verifies payment signatures.
- **Device & Concurrency Guard**: Blocks simultaneous replay scans on the same validator.
