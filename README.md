# CourtSync Pro

**Indoor Sports Court Rental & Management System**

> Where Athletes Meet Their Courts

---

## 📌 About The Project

CourtSync Pro is a full-stack web-based management system built for indoor sports venues in Pakistan. It bridges the gap between court owners and players by providing a centralized digital platform where courts can be listed, discovered, and booked in real time — without phone calls or physical visits.

The system supports **three user roles** (Player, Court Owner, Admin), includes an **AI-powered booking assistant** using Google Gemini, **real-time player chat** using SignalR WebSockets, a **tournament management module**, **dynamic pricing engine**, and a **live analytics dashboard**.

---

## 🚀 Key Features

**For Players**
- Register and log in securely with BCrypt password hashing
- Browse indoor sports courts filtered by city, sport type, and price
- Check real-time slot availability and book instantly online
- Pay using EasyPaisa, JazzCash, or Credit/Debit Card
- Receive a unique QR code for venue check-in after booking
- Get automatic 10% early-bird discount when booking 48+ hours ahead
- Join sports tournaments and pay registration fees online
- Chat live with other players currently online on the platform
- Ask the AI assistant to find and book courts in natural language

**For Court Owners**
- Register venue and submit for admin verification
- Add courts with sport type, pricing, photos, and location
- Manage time slots — create, block, and remove availability windows
- View all bookings, revenue, and occupancy from a dashboard
- Create and manage sports tournaments for players to join

**For Admins**
- Approve or reject court owner registrations
- Monitor all courts, bookings, and users platform-wide
- View live analytics: revenue charts, peak hours heatmap, occupancy rate
- Manage tournaments and platform-wide discount campaigns

---

## 🧠 Advanced Features

| Feature | Technology | Description |
|---|---|---|
| AI Booking Assistant | Google Gemini API (free) | Players type natural language — AI recommends and helps book courts using live database context |
| Real-Time Player Chat | Microsoft SignalR | Online player list, chat requests, live messaging without page reload |
| Dynamic Pricing Engine | Pure C# | Prices adjust based on season, time of day, day of week, court rating, and live demand |
| Live Slot Updates | SignalR WebSockets | When a slot is booked, it disappears from all browsers instantly |
| QR Code Check-in | ZXing.Net | Unique scannable QR code generated for every confirmed booking |
| Analytics Dashboard | Chart.js | Revenue line chart, sport distribution doughnut, peak hours heatmap |

---

## 🏗️ System Modules

1. **Authentication** — BCrypt hashing, session-based login, role-based navigation
2. **Admin Panel** — Owner verification, platform oversight, KPI dashboard
3. **Court Management** — Full CRUD, soft delete, sport type filtering
4. **Time Slot System** — Availability management, slot locking, maintenance blocking
5. **Booking System** — Price calculation, early-bird discount, QR code generation
6. **Payment Module** — Multi-method payments, transaction IDs, receipts
7. **Tournament Module** — Create, register, pay fee, track spots
8. **AI Assistant** — Gemini-powered natural language booking help
9. **Live Player Chat** — SignalR request-accept-chat flow
10. **Analytics Dashboard** — Live charts, occupancy, revenue tracking
11. **Dynamic Pricing** — Season, time, day, rating, and demand-based pricing

---

## ⚙️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET 8) |
| Framework | ASP.NET Core MVC |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core — Code First |
| Frontend | HTML5, CSS3, Bootstrap 5, JavaScript |
| Real-Time | Microsoft SignalR |
| AI | Google Gemini API (gemini-2.0-flash-lite) |
| Auth | BCrypt.Net — session-based |
| Charts | Chart.js |
| IDE | Visual Studio 2026 |
| Version Control | Git + GitHub |

---

## 🗄️ Database

- **13 tables** generated entirely from C# model classes using EF Core Code First
- **3 migrations** applied: InitialCreate → AddAuthSystem → AddTournamentModule
- All tables normalized to **3NF**
- Soft delete on courts — history preserved on removal

**Tables:** Users · CourtOwners · Courts · TimeSlots · Bookings · Payments · Memberships · Reviews · Admins · Tournaments · TournamentRegistrations · ChatRequests · ChatMessages

---

## 💰 Dynamic Pricing Logic

Prices adjust automatically based on multiple real-world factors:

| Factor | Example |
|---|---|
| Season | Monsoon +30%, Summer +20%, Winter -10% |
| Time of day | Evening peak 5–8 PM +35%, Off-peak 9–12 AM -15% |
| Day of week | Friday +30%, Saturday +25%, Wednesday -10% |
| Court rating | 4.6–5.0 stars +25%, below 2.0 stars -25% |
| Live demand | 20+ bookings today +20% |
| Safety cap | Never below 60% or above 250% of base price |

---

## 🛠️ How to Run Locally

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/CourtSyncPro.git

# 2. Open in Visual Studio 2022 or 2026

# 3. Install NuGet packages (Package Manager Console)
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package Microsoft.AspNetCore.SignalR
Install-Package BCrypt.Net-Next
Install-Package Newtonsoft.Json

# 4. Update appsettings.json with your connection string and Gemini API key

# 5. Run migrations to create the database
Add-Migration "InitialCreate"
Update-Database

# 6. Press F5 to run
```

---

## 🔑 Environment Variables (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CourtSyncProDb;Trusted_Connection=True;"
  },
  "Gemini": {
    "ApiKey": "your_gemini_api_key_here",
    "Model": "gemini-2.0-flash-lite"
  }
}
```

Get a free Gemini API key at: **aistudio.google.com**

---

## 👥 Team

| Name | Role |
|---|---|
| Abdullah Yousaf | Full-Stack Developer |
| M. Naqeeb Ur Rehman | Full-Stack Developer |

**Supervisor:** Ms. Ayesha Khalid
**Institution:** National University of Modern Languages, Islamabad
**Department:** Computer Science — 4-B BSCS
**Year:** 2026

---

## 📱 Future Enhancements

- React Native mobile app for Android and iOS
- Live EasyPaisa and JazzCash payment API integration
- Email and SMS notifications via Twilio
- Cloud deployment on Microsoft Azure
- Court photo uploads via Azure Blob Storage
- AI learning from past bookings for smarter suggestions
- Loyalty points system for regular players

---

## 📄 License

This project was developed as a Semester Project at NUML Islamabad.
Feel free to use it as a reference for learning purposes.

---

> *CourtSync Pro — Where Athletes Meet Their Courts* 🏟

---
