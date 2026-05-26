# SendMail

A bulk email campaign platform built with ASP.NET Core 8.0 and Razor Pages. It automates the full pipeline from extracting contact lists out of PDF documents to sending personalized, validated emails over Gmail SMTP.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Known Limitations](#known-limitations)

---

## Overview

SendMail solves the repetitive work of manual outreach by orchestrating three stages in sequence:

1. **PDF parsing** — contacts are extracted from an uploaded PDF (company, city, person, service, email, phone).
2. **Email validation** — a Python script resolves DNS MX records and probes the target SMTP server to discard invalid addresses before a single message is sent.
3. **Personalized delivery** — each surviving recipient receives a message generated from a user-supplied subject and body template, with per-contact placeholders substituted at send time.

Random send delays are introduced between messages to stay within Gmail rate limits.

---

## Features

- Extracts up to 100 contacts per PDF using the [PdfPig](https://github.com/UglyToad/PdfPig) library
- Pre-send email validation via DNS MX lookup and SMTP RCPT probing (Python)
- Template engine supporting `{SOCIETE}`, `{VILLE}`, `{PERSONNE}`, `{ACTIVITE}`, and `{SERVICE}` placeholders
- Gmail SMTP with STARTTLS on port 587 using an app-specific password
- Anti-rate-limit delays (10–22 seconds, randomised) between sends
- Structured logging throughout all services via `ILogger`
- Custom typed exceptions (`SmtpSendingException`, `PdfParsingException`) for clean error reporting
- Dependency injection wired through a dedicated `ServiceRegistration` extension

---

## Architecture

```
UI Layer (Razor Pages)
        |
        v
Service Layer
  ├── IEmailSending  -> EmailSending      (Gmail SMTP, template substitution)
  ├── IEmailValidator -> EmailValidator   (delegates to AntiBoucing.py)
  └── IPdfSerializer  -> PdfParserService (PdfPig extraction)
        |
        v
External Dependencies
  ├── Gmail SMTP  (smtp.gmail.com:587)
  ├── PDF files   (uploaded by user)
  └── Python 3    (AntiBoucing.py — DNS + SMTP probing)
```

Services are registered as **scoped** and resolved through ASP.NET Core's built-in DI container. Each service is hidden behind an interface to keep the pages free of implementation details.

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0 or later |
| Python | 3.8 or later |
| Python package — dnspython | latest stable |
| Gmail account | app-specific password enabled |

Install the Python dependency:

```bash
pip install dnspython
```

Enable a Gmail app password under **Google Account > Security > 2-Step Verification > App passwords**. This password is used instead of your normal Gmail password.

---

## Getting Started

**Clone the repository**

```bash
git clone <repository-url>
cd SendMail
```

**Restore and build**

```bash
dotnet restore
dotnet build
```

**Run in development**

```bash
dotnet run --project SendMail/SendMail.csproj
```

The application starts on `http://localhost:5206` and opens in the default browser automatically.

---

## Configuration

Application settings live in `appsettings.json` (excluded from source control). Create or update the file before first run:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Gmail credentials (`SenderEmail` and `AppPassword`) are supplied at runtime through the UI as part of the send request, not stored in configuration files. This is intentional — avoid committing credentials.

The Python validator script is located at `Scripts/AntiBoucing.py`. Ensure `python` (or `python3`) is accessible on the system `PATH` so the `EmailValidator` service can invoke it as a subprocess.

---

## Usage

1. Open the application in a browser.
2. Upload a PDF containing a contact list. Each page is scanned for email addresses and associated fields (company, city, person, department, etc.).
3. Review the extracted contacts. Up to 100 records are returned.
4. Enter your Gmail address and app-specific password.
5. Write a subject and body template. Use the available placeholders:

   | Placeholder | Field |
   |---|---|
   | `{SOCIETE}` | Company name |
   | `{VILLE}` | City |
   | `{PERSONNE}` | Contact person |
   | `{ACTIVITE}` | Business activity |
   | `{SERVICE}` | Department or service |

6. Submit. The application validates each email, then sends personalised messages with a random delay between each send to avoid triggering Gmail rate limits.

Progress and per-recipient results are logged to the console.

---

## Project Structure

```
SendMail/
├── Configuration/
│   └── ServiceRegistration.cs     # DI extension method
├── Dtos/
│   └── SendRequestDto.cs          # Input model for send requests
├── Exceptions/
│   ├── PdfParsingException.cs
│   └── SmtpSendingException.cs
├── Models/
│   └── PdfFormat.cs               # Extracted contact record
├── Pages/                         # Razor Pages (UI)
│   ├── Index.cshtml(.cs)
│   ├── Privacy.cshtml(.cs)
│   ├── Error.cshtml(.cs)
│   └── Shared/
│       └── _Layout.cshtml
├── Services/
│   ├── IServices/
│   │   ├── IEmailSending.cs
│   │   ├── IEmailValidator.cs
│   │   └── IPdfSerializer.cs
│   └── Impl/
│       ├── EmailSending.cs
│       ├── EmailValidator.cs
│       └── PdfSerializer.cs
├── Scripts/
│   └── AntiBoucing.py             # Email validation via DNS + SMTP
├── wwwroot/                       # Static assets (Bootstrap, jQuery)
├── Program.cs
├── SendMail.csproj
└── appsettings.json
```

---

## Known Limitations

- **No authentication.** The web interface is unauthenticated. Do not expose it on a public network without adding an authentication layer.
- **Gmail only.** The SMTP client is hardcoded to `smtp.gmail.com:587`. Supporting other providers requires a configuration change in `EmailSending.cs`.
- **Python dependency.** Email validation relies on an external Python process. If Python is not installed or `dnspython` is missing, validation silently returns `false` and emails are skipped.
- **PDF format assumption.** `PdfParserService` expects a specific column layout in the source PDF. Differently structured documents will produce incomplete or empty results.
- **No test coverage.** The project currently contains no automated tests.
