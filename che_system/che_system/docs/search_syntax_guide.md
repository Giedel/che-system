# Advanced Search Syntax Guide

## Basic Text Search
Type any text to search across all text fields in the current view.

**Examples:**
- `sodium` - Searches for "sodium" in item names, descriptions, borrower names, etc.
- `CHEM101` - Finds items or slips with "CHEM101" in subject codes or references

## Field-Specific Search
Prefix with `field:` to search specific columns.

**General Syntax:**
- `field:value` - Search specific field for partial match
- `field:"exact value"` - Exact match (use if your value contains spaces)

**Supported Fields:**
- **Inventory:** `name:`, `formula:`, `category:`, `location:`, `quantity:`, `expiry:`, `status:`, `unit:`
- **Borrowing:** `borrower:`, `subject:`, `remarks:`, `received:`, `released:`, `checked:`, `datefiled:`, `dateofuse:`
- **User Management:** `first_name:`, `last_name:`, `username:`, `role:`, `birthday:`, `user_id:`
- **Reports:** `itemname:`, `category:`, `borrower:`, `subjectcode:`, `usagecount:`

**Examples:**
- `category:Chemical` - Only chemicals
- `borrower:John` - Slips for John
- `role:Custodian` - Custodian users only
- `quantity:>50` - Items with quantity greater than 50

## Operators and Ranges
Use operators for numeric and date fields.

**Operators:**
- `>` greater than (e.g., `quantity>50`)
- `<` less than (e.g., `quantity<100`)
- `>=` greater or equal
- `<=` less or equal
- `=` exact match
- `!` not equal (e.g., `role!=STA`)
- `-` or ` to ` for ranges (e.g., `quantity:10-100` or `quantity:10 to 100`)

**Examples:**
- `quantity>10` - Items with more than 10 units
- `quantity:5-50` - Items between 5 and 50 units
- `expiry:2024-12-01 to 2025-01-31` - Expiry dates in range
- `datefiled:2024-01-01 to 2024-01-31` - Slips filed in January 2024
- `birthday:1990-2000` - Users born between 1990 and 2000

## Combined Queries
Use `AND` or `OR` to combine filters (case-insensitive).

**Examples:**
- `sodium AND quantity<10` - Low stock sodium items
- `category:Chemical OR apparatus` - Chemicals or apparatus
- `borrower:John AND status:Pending` - John's pending slips
- `subject:CHEM* AND quantity>0` - Chemistry subjects with usage
- `role:STA OR custodian AND date:2024` - STA or custodian activity in 2024

## View-Specific Examples

### Inventory Search
- `sodium chloride` - Find sodium chloride items
- `category:Chemical formula:HCl` - Chemical HCl
- `quantity<10 expiry:-2025` - Low stock items expiring this year
- `location:Shelf* AND status:Available` - Available items on shelves

### Borrowing Search
- `John Doe` - Slips for John Doe
- `subjectcode:CHEM101 dateofuse:2024-01` - CHEM101 slips in January
- `pending` - All pending slips
- `received:admin AND released:*` - Admin received, anyone released
- `datefiled>2024-01-01` - Recent slips

### User Management Search
- `john` - Users with "john" in name
- `role:Custodian` - All custodians
- `birthday:1995 to 2000` - Users born 1995-2000
- `username:admin*` - Admin users
- `first_name:Jane last_name:Smith` - Jane Smith

### Reports Search
- `sodium usagecount>5` - Sodium items used more than 5 times
- `category:Chemical totalborrowed>100` - Chemicals borrowed over 100 units
- `borrower:*john* subject:Bio` - Biology reports for John*

## Tips
- Fields are case-insensitive
- Partial matches work by default
- Use quotes for phrases with spaces: `"low stock"`
- Multiple words without operators search all fields: `sodium chloride`
- Date formats: `YYYY-MM-DD` or `YYYY-MM` for year-month
- Empty field or invalid syntax falls back to simple text search

## Status Values
- Inventory: `Available`, `LowStock`
- Borrowing: `Pending`, `Active`, `Complete`
- User: `STA`, `Custodian`

For support, contact the development team to add new field types or custom operators.
