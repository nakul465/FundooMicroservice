# Fundoo Microservices Architecture

Fundoo is a cloud-ready, microservices-based notes management and user authentication system built with ASP.NET Core and PostgreSQL.

---

## 🏛 Architecture Overview

The solution consists of four primary executable services:

| Service | Port | Description |
| :--- | :--- | :--- |
| **Fundoo.ApiGateway** | `5050` / `7050` | Ocelot API Gateway routing upstream requests, rate limiting, and authenticating user requests |
| **Fundoo.User.Api** | `5284` / `7103` | User identity, registration, JWT & OAuth 2.0 token issuance, 2FA (TOTP/Email OTP), and Google SSO |
| **Fundoo.Notes.Api** | `5025` / `7264` | Notes CRUD operations, archiving, and trash bin with health checks and JWT/inter-service auth |
| **Fundoo.Notification.Api** | `5059` / `7261` | Email notification service (SMTP) protected by granular OAuth 2.0 M2M scopes |

---

## 🔐 Configuration & Secrets Management

In adherence with 12-factor application security best practices, **no credentials or secrets are stored in source code or `appsettings.json`**.

- **Local Development**: Stored safely in .NET **User Secrets** (`dotnet user-secrets`), loaded automatically in `Development` environment.
- **Production Deployment**: Provided securely via **Environment Variables**, Kubernetes Secrets, Docker Compose, or Cloud Secret Managers (Azure Key Vault, AWS Secrets Manager, etc.).

---

## 🚀 Production Deployment Environment Variables

Configure the following environment variables for each service in your deployment environment:

### 1. `Fundoo.User.Api`
```bash
# Database Connection
ConnectionStrings__FundooConnection="Host=<db_host>;Port=5432;Database=FundooMicroDb;Username=<user>;Password=<password>"

# JWT Configuration
Jwt__Key="<secure_random_key_min_32_chars>"
Jwt__Issuer="FundooApp"
Jwt__Audience="FundooUsers"
Jwt__OAuthIssuer="FundooOAuthServer"
Jwt__ExpiryMinutes="60"

# Service-to-Service URLs
NotificationService__BaseUrl="http://<notification_host>:5059"
NotesService__BaseUrl="http://<notes_host>:5025"

# Google SSO (Optional)
Authentication__Google__ClientId="<google_client_id>"
Authentication__Google__ClientSecret="<google_client_secret>"

# OAuth M2M Client Secrets
OAuth__Clients__UserService__Secret="<user_service_m2m_secret>"
OAuth__Clients__GatewayService__Secret="<gateway_service_m2m_secret>"
OAuth__Clients__NotificationService__Secret="<notification_service_m2m_secret>"
```

### 2. `Fundoo.Notes.Api`
```bash
# Database Connection
ConnectionStrings__FundooNotesConnection="Host=<db_host>;Port=5432;Database=FundooNotesDb;Username=<user>;Password=<password>"

# JWT Configuration
Jwt__Key="<secure_random_key_min_32_chars>"
Jwt__Issuer="FundooApp"
Jwt__Audience="FundooUsers"
Jwt__OAuthIssuer="FundooOAuthServer"

# Inter-Service Authentication
InternalServiceAuth__SecretKey="<shared_internal_secret_key>"
```

### 3. `Fundoo.Notification.Api`
```bash
# JWT Configuration
Jwt__Key="<secure_random_key_min_32_chars>"
Jwt__OAuthIssuer="FundooOAuthServer"

# SMTP Settings
SmtpSettings__Host="smtp.gmail.com"
SmtpSettings__Port="587"
SmtpSettings__Username="<email_address>"
SmtpSettings__Password="<app_password>"
SmtpSettings__EnableSsl="True"
```

### 4. `Fundoo.ApiGateway`
```bash
# JWT Configuration
Jwt__Key="<secure_random_key_min_32_chars>"
Jwt__Issuer="FundooApp"
Jwt__Audience="FundooUsers"

# Gateway-to-Notes Forwarding Secret
InternalServiceAuth__SecretKey="<shared_internal_secret_key>"
```

---

## 💻 Local Development Setup

To run locally, ensure the local User Secrets are configured (already populated on this development machine):

```bash
# Fundoo.User.Api
dotnet user-secrets list --project Fundoo.User.Api

# Fundoo.Notes.Api
dotnet user-secrets list --project Fundoo.Notes.Api

# Fundoo.Notification.Api
dotnet user-secrets list --project Fundoo.Notification.Api

# Fundoo.ApiGateway
dotnet user-secrets list --project Fundoo.ApiGateway
```

### Running Services

Start each service in a separate terminal:
```bash
dotnet run --project Fundoo.User.Api
dotnet run --project Fundoo.Notes.Api
dotnet run --project Fundoo.Notification.Api
dotnet run --project Fundoo.ApiGateway
```

---

## 🩺 Health Checks & Monitoring

The Notes microservice provides standard container health check endpoints for liveness and readiness:
- **Liveness Check**: `GET /health/live` (HTTP 200 Healthy)
- **Readiness Check**: `GET /health/ready` (Verifies PostgreSQL database connectivity)
