# Equity Performance Tracker – AI Application Summary
Equity Performance Tracker is a Razor Pages web application built on .NET 9 and C# 13.0, designed to help users manage and analyze their equity investment portfolios.
## Core Features ##
-	Portfolio Management:
Users can create, update, and delete portfolios, each containing multiple holdings. Portfolios track initial and current values, descriptions, and performance history.
-	Holdings Tracking:
Each portfolio contains holdings representing individual equities. Holdings store details such as symbol, company name, shares owned, purchase price, current price, and calculated metrics like gain/loss.
-	Transaction Management:
Users can record buy and sell transactions for holdings. Transactions include details like type (buy/sell), shares, price, date, and notes. The system validates share sales and maintains transaction history.
-	Performance Analysis:
The application generates performance snapshots for portfolios, tracking daily and total changes in value and gain/loss percentages.
-	User-Specific Data:
Portfolios and holdings are associated with individual users, supporting multi-user scenarios.
## Architecture
###	Core Models:
-	Portfolio: Represents a user's investment portfolio.
-	PortfolioHolding: Represents an equity holding within a portfolio.
-	Transaction: Records buy/sell actions for holdings.
-	PerformanceSnapshot: Captures periodic performance data for a portfolio.
### Services:
-	IPortfolioService: Interface defining portfolio, holding, transaction, and performance operations.
-	Additional services for user management, subscriptions, and external data integration (e.g., Alpaca API).
###	Data Layer:
- Uses Entity Framework Core for data persistence, with migrations for schema management.
###	UI:
- Razor Pages for portfolio creation, listing, details, and transaction management. Shared views for authentication and validation.
---
Purpose:
The application enables users to track, analyze, and optimize their equity investments by providing detailed portfolio analytics, transaction history, and real-time performance metrics.
Let me know if you need a more technical breakdown or details on a specific feature!

