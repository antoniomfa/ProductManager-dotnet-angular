# Product Manager

A product catalogue with a **.NET 10 (LTS)** REST API and an **Angular 22** web app.

## What's needed

| Tool | Version |
| --- | --- |
| .NET SDK | 10.0 |
| Node.js | 20.19+ |
| SQL Server | LocalDB |

## Run it

Two terminals. **Start the API first** — the web app talks to it.

```bash
# Terminal 1 - API
cd backend/src/ProductManager.Api
dotnet run
```

```bash
# Terminal 2 - web app
cd frontend/product-manager-web
npm install     # first time only
npm start
```

Open:

- **<http://localhost:4200>** - the web app
- **<http://localhost:5186/swagger>** - the API docs

The first time the API starts it creates the database and fills it with 6 categories and 25 products. Some are out of stock and some are low on stock on purpose.

## Run the tests

```bash
cd backend
dotnet test          # 113 tests
```

```bash
cd frontend/product-manager-web
npm test             # 25 tests
```

The backend tests need SQL Server.

## Formatting

Both halves follow the root `.editorconfig`. Check before committing:

```bash
cd backend && dotnet format --verify-no-changes
cd frontend/product-manager-web && npm run format:check   # npm run format to fix
```

## The endpoints

| Method | Route | What it does |
| --- | --- | --- |
| `GET` | `/api/products` | List products (one page at a time) |
| `POST` | `/api/products` | Create a product |
| `GET` | `/api/products/{id}` | One product, with its stock history |
| `PUT` | `/api/products/{id}` | Update a product |
| `DELETE` | `/api/products/{id}` | Delete a product |
| `POST` | `/api/products/{id}/add-to-stock/{quantity}` | Add stock |
| `POST` | `/api/products/{id}/decrement-stock/{quantity}` | Remove stock |
| `GET` | `/api/products/search?name={name}` | Find by part of the name |
| `GET` | `/api/products/stock-level?min={min}&max={max}` | Find by stock range |

Every list response includes each product's stock level and a status of `OutOfStock`,
`LowStock` or `InStock`.

## How product IDs work

Every product gets a unique 6-digit ID. The database generates it from a **sequence**, set as
the default value of the `Id` column.

This matters because the ID must stay unique even if several copies of the API run at once.
Anything that counts inside one API process would hand out the same number twice. The database is the one thing every copy shares, and it never gives the same value to two callers.

## Concurrent stock changes

Each product row carries a `rowversion` concurrency token. If two requests read the same stock
level and both try to save, the second save is rejected instead of overwriting the first.

- `add-to-stock` and `decrement-stock` reload the product and reapply the change, up to 3 times.
  Stock can never be oversold, and no addition is lost.
- `PUT` and `DELETE` return `409 Conflict` rather than silently overwriting someone else's change.

## The structure

```
backend/
  src/
    ProductManager.Domain          business rules, no dependencies
    ProductManager.Application     use cases, DTOs, validation, repository interfaces
    ProductManager.Infrastructure  EF Core, repositories, migrations, seeding
    ProductManager.Api             controllers, error handling, Swagger
  tests/                           unit and integration tests

frontend/product-manager-web/      Angular app
docker-compose.yml                 SQL Server, if no LocalDB
```
