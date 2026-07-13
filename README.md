# TelegramMinimalApis

A Minimal APIs backend that interfaces with TDLib to send Telegram messages.

## Overview

This project exposes REST APIs to allow client applications to send Telegram messages, enabling frontend agnosticity.

## Features
- REST API built with Minimal APIs
- TDLib integration (Authentication and Messaging)
- Authentication with JWT/Refresh tokens (web only)
- Idempotency/validation checks

## Tech Stack
- .NET 8 — Minimal APIs
- Vertical Slice Architecture
- Options pattern for application configurations
- MediatR pattern with REPR
- EntityFrameworkCore with PostgreSQL
- Logging with Serilog

## Getting Started

### Prerequisites
- .NET 8 SDK
- Telegram API credentials (`api_id` and `api_hash` from [my.telegram.org](https://my.telegram.org))
- PostgreSQL/pgAdmin (optional)

### Setup
1. Clone the repo
```bash
   git clone https://github.com/limzw/telegram-minimal-apis.git
```

2. Setup .NET User Secrets (do not put these in `appsettings.json`)
```bash
   dotnet user-secrets set "JwtSettings:JwtSecretKey" <!-- your-jwt-secret-key -->
   dotnet user-secrets set "JwtSettings:JwtIssuer" <!-- your-jwt-issuer -->
   dotnet user-secrets set "JwtSettings:JwtAudience" <!-- your-jwt-audience -->
   dotnet user-secrets set "JwtSettings:JwtExpiryMinutes" <!-- jwt-expiry-time-in-minutes -->
   
   dotnet user-secrets set "RefreshTokenSettings:RefreshTokenExpiryValue" <!-- refresh-token-expiry-value -->
   dotnet user-secrets set "RefreshTokenSettings:RefreshTokenExpiryValueType" <!-- refresh-token-expiry-value-type -->
   dotnet user-secrets set "RefreshTokenSettings:RefreshCookieExpiryOffsetValue" <!-- refresh-cookie-expiry-offset-value -->
   dotnet user-secrets set "RefreshTokenSettings:RefreshCookieExpiryOffsetValueType" <!-- refresh-cookie-expiry-offset-value-type -->
    
   dotnet user-secrets set "ConnectionStrings:DatabaseConnectionString" <!-- your-connection-string-here" -->
    
   dotnet user-secrets set "TelegramSettings:ApiId" <!-- your-telegram-api-id -->
   dotnet user-secrets set "TelegramSettings:ApiHash" <!-- your-telegram-api-hash -->
   dotnet user-secrets set "TelegramSettings:DatabasePath" <!-- your-telegram-database-path -->
```
- Refer to [CookieGenerator.cs](TelegramMinimalAPIs/Common/Services/Cookies/CookieGenerator.cs) for more information on RefreshTokenSettings

3. Apply database migrations
```bash
   dotnet ef database update
```

4. Run the project
```bash
   dotnet run
```

## Project Structure
- `/Common` — MediatR pipeline behaviors, Exception handlers, Custom middleware, Application configurations, Database settings
- `/Features` — request/handler/validator/response groupings by feature

## Status
🚧 Work in progress — early development.
- In the midst of working on a ReactJS frontend and Flutter mobile frontend!!

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
