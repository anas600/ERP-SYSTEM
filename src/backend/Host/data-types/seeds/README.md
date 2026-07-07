# DEC-086: JSON-Driven Seed Data

This directory contains seed data for the `RealisticSeed` BackgroundService.

## Structure

```
data-types/seeds/
├── README.md                      # This file
├── seed_meta.json                 # Tenant + admin user info
├── seed_companies.json            # 5 companies
├── seed_vendors.json              # 25 vendors
├── seed_customers.json            # 20 customers
├── seed_items.json                # 15 items
├── seed_projects.json             # 11 projects
└── ...                            # More to come
```

## JSON Schema

Each file has this structure:

```json
{
  "entity": "Vendor",
  "table": "vendors",
  "tenant_id": "f77dbedd-64ff-41ac-b77a-0731183ff744",
  "_comment": "Optional — human-readable note",
  "records": [
    { "code": "V-101", "name": "...", "...": "..." }
  ]
}
```

- `entity`: Display name (PascalCase)
- `table`: Database table (snake_case)
- `tenant_id`: Foreign key to `tenants.id` — all records must have this
- `records`: Array of objects — each becomes a row

## Loader

The `JsonSeedLoader` (in `Shared/SeedData/JsonSeedLoader.cs`) reads all `*.json` files from this directory on startup.

The `RealisticSeed` BackgroundService uses the loader to:
1. Look up the file by `entity` name
2. Iterate `records`
3. INSERT into the corresponding table

## Adding New Records

1. Add a new object to the `records` array in the relevant JSON file
2. Commit + deploy
3. Next startup will INSERT the new record (idempotency check prevents duplicates)

## Limitations (DEC-086 PoC)

- Only 5 entities are JSON-driven (companies, vendors, customers, items, projects)
- The full 518 records from the original RealisticSeed are not yet extracted
- Future DECs will extract: chart of accounts, employees, POs, GRs, bills, sales invoices, payments, JEs

## Migration Path

- **DEC-086 (this)**: Skeleton + 5 critical entities in JSON
- **DEC-087**: Extract remaining entities (POMs, GRs, bills, sales invoices)
- **DEC-088**: Extract chart of accounts + journal entries
- **DEC-089**: Remove hardcoded C# data entirely (RealisticSeed reads 100% from JSON)
