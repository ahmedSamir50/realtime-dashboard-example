# 📈 Stock Pulse: Professional Real-Time Analytics Dashboard

A state-of-the-art, high-performance stock monitoring ecosystem built on **.NET 10** and **Ionic/Angular**. This application delivers precision real-time price updates, deep historical trend analysis (1W to 5Y), and a sophisticated caching architecture designed for enterprise-grade scalability.

---

## 🛠 Technology Stack

### **Backend (.NET 10 & Aspire)**
- **Minimal APIs & Mediator**: Clean, lightweight endpoints using the **Cortex.Mediator** (CQRS) pattern.
- **SignalR**: Real-time WebSocket communication for instantaneous price updates.
- **Data Persistence**: **PostgreSQL** via Npgsql with a range-aware historical data caching strategy.
- **High-Performance Caching**: **Redis**-backed **HybridCache** (L1/L2) for sub-millisecond response times.
- **Aspire Integration**: Unified service orchestration for cloud-native development.
- **External Data**: Real-time and historical data integration via **Yahoo Finance API**.

### **Frontend (Ionic 8 & Angular 19)**
- **Ionic Standalone Components**: Modern, modular, and high-performance UI architecture.
- **Reactive State Management**: **RxJS**-driven data pipelines for seamless real-time updates.
- **Pro Charting**: **Chart.js** integration with interactive crosshairs and dynamic visual feedback.
- **Pulse Design System**: A premium, glassmorphism-inspired "Dark Mode" aesthetic.

---

## 🏗 Architecture & Patterns

The application follows **Clean Architecture** principles with a strict separation of concerns:

- **CQRS (Cortex.Mediator)**: Every operation is a discrete Query or Command, ensuring the API is a pure entry point delegating to specialized feature handlers.
- **Mediator-Driven Endpoints**: Controllers/Endpoints strictly use `IMediator.SendQueryAsync()` to execute market logic.
- **Self-Healing Cache**: The backend automatically validates historical data coverage. If the database lacks the requested timeframe (e.g., 5 years), it intelligently fetches missing ranges from the API and persists them.
- **Real-Time Subscription Model**: Users subscribe to specific tickers via SignalR, and the backend efficiently manages active feeds via a centralized manager.

---

## 🚀 Key Functionalities

1. **Real-Time Ticker Tracking**: Add your favorite stocks and watch live price movements through established WebSocket connections.
2. **Pulse Insights**: A dedicated history page featuring professional-grade interactive charts.
3. **Flexible Timeframes**: Analyze market trends across **1W, 1M, 3M, 1Y, and 5Y** cycles with a single click.
4. **Infinite Scroll Pagination**: Explore years of detailed price history without UI lag thanks to a robust paginated list implementation.
5. **Global Stock Search**: Instantly discover new assets with an integrated, high-speed search interface.

---

## 📦 How to Run

### **Prerequisites**
- **.NET 10 SDK**
- **Docker** (for PostgreSQL and Redis via Aspire)
- **Node.js** (for Ionic frontend)

### **Backend Setup**
1. Navigate to the `src/BE/API/Stock.RealTime.API` directory.
2. Ensure Docker is running.
3. Run the application via the **Aspire AppHost** or using:
   ```bash
   dotnet run
   ```

### **Frontend Setup**
1. Navigate to `src/FE/ionic/stock_realtime_example`.
2. Install dependencies:
   ```bash
   npm install
   ```
3. Launch the development server:
   ```bash
   ionic serve
   ```

---

## ✍️ Author & Lead

Developed and Architected with passion by:

**Ahmed Samir**  
*Lead Software Developer*  
[Connect on GitHub](https://github.com/ahmedSamir50)

---
*Built for the next generation of financial analysis.*
