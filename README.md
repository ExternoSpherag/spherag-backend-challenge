# Backend Technical Challenge

## Overview

In this challenge you will build a small backend service that consumes real-time cryptocurrency trade data, aggregates it into time windows, and exposes the processed data through a REST API.

The goal is to evaluate how you approach **data ingestion, aggregation, persistence, and API design**.

You will receive real-time trade data from a public WebSocket stream and compute aggregated metrics over fixed time intervals.

---

## Data Source

Connect to the following WebSocket stream:

wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade

You will receive trade events in the following format:

```json
{
  "e": "trade",
  "E": 1672515782136,
  "s": "BNBBTC",
  "t": 12345,
  "p": "0.001",
  "q": "100",
  "T": 1672515782136,
  "m": true,
  "M": true
}
```

Fields:

* `s` → symbol
* `p` → trade price
* `q` → quantity
* `T` → trade timestamp

---

## Requirements

Your service must:

1. Consume the WebSocket stream.
2. Aggregate trades into **5-second time windows**.
3. Compute the **average price** for each cryptocurrency in each window.
4. Store aggregated data in a **database of your choice**.
5. Generate an **alert if the average price difference between two consecutive windows exceeds 5%**.
6. Expose a **REST API endpoint** to retrieve stored aggregated prices.

---

## Submission

1. Fork this repository
2. Create a branch

feature/<your-name>

3. Implement the solution
4. Open a Pull Request to this repository

PR title:

[Challenge] Your Name

---

## Evaluation Criteria

We will evaluate:

* Code quality and readability
* System design decisions
* Data modeling
* Resilience of the ingestion pipeline
* Handling of streaming data
* API design
* Project structure
* Documentation

---

## Freedom of Choice

You may choose:

* Programming language
* Framework
* Database
* Architecture

.NET is preferred

---

## What We Expect

Do it the way you would like it to be deployed in production

---
