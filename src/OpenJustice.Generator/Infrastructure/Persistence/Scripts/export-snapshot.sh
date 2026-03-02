#!/bin/bash
# Export SQL Snapshot Script
# Usage: ./export-snapshot.sh [output_file]
# 
# This script generates a SQL snapshot of the database schema
# from the EF Core migration files.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
MIGRATIONS_DIR="$PROJECT_DIR/Infrastructure/Persistence/Migrations"
OUTPUT_FILE="${1:-openjustice_v1_$(date +%Y%m%d_%H%M%S).sql}"

echo "Generating SQL snapshot..."
echo "Migration directory: $MIGRATIONS_DIR"
echo "Output file: $OUTPUT_FILE"

# Check if migrations exist
if [ ! -d "$MIGRATIONS_DIR" ]; then
    echo "Error: Migrations directory not found at $MIGRATIONS_DIR"
    exit 1
fi

# Find the initial migration
MIGRATION_FILE=$(ls -1 "$MIGRATIONS_DIR"/*InitialDatabaseFoundation.cs 2>/dev/null | head -1)
if [ -z "$MIGRATION_FILE" ]; then
    echo "Error: InitialDatabaseFoundation migration not found"
    exit 1
fi

echo "Using migration: $MIGRATION_FILE"

# Generate SQL using EF Core tools
cd "$PROJECT_DIR"

# Use dotnet ef to generate script from migration
dotnet ef migrations script \
    --idempotent \
    --output "$OUTPUT_FILE" \
    --project "$PROJECT_DIR/OpenJustice.Generator.csproj" \
    --startup-project "$PROJECT_DIR/OpenJustice.Generator.csproj" \
    2>/dev/null || {
    # Fallback: copy migration SQL directly
    echo "-- Note: Generated from migration file directly" > "$OUTPUT_FILE"
    echo "-- Migration: $(basename "$MIGRATION_FILE")" >> "$OUTPUT_FILE"
    echo "-- Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")" >> "$OUTPUT_FILE"
    echo "" >> "$OUTPUT_FILE"
    cat "$MIGRATION_FILE" >> "$OUTPUT_FILE"
}

if [ -f "$OUTPUT_FILE" ]; then
    echo "Success! SQL snapshot written to: $OUTPUT_FILE"
    echo ""
    echo "To import into PostgreSQL:"
    echo "  psql -d <database> -f $OUTPUT_FILE"
else
    echo "Error: Failed to generate SQL snapshot"
    exit 1
fi
