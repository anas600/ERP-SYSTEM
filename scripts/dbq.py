"""dbq — minimal psql-style query runner for ERP-SYSTEM.
Usage:
    py -3 scripts/dbq.py "SELECT 1"
    py -3 scripts/dbq.py -c "SELECT count(*) FROM users"
    py -3 scripts/dbq.py --table users    # show table schema
    py -3 scripts/dbq.py --tables         # list all tables
    py -3 scripts/dbq.py --diag           # show connection info + DB version
"""
import os, sys, argparse, urllib.parse, psycopg2, psycopg2.extras

CONN_ENV = "NEON_URL"

def normalize_conn(raw: str) -> str:
    """Accept either .NET-style 'Host=...;Port=...;Database=...;User Id=...;password=...;...'
    or URI-style 'postgresql://...'. Returns a URI string psycopg2 understands."""
    if raw.startswith(("postgresql://", "postgres://")):
        return raw
    # .NET-style parse
    parts = {}
    for chunk in raw.split(";"):
        if "=" not in chunk: continue
        k, v = chunk.split("=", 1)
        parts[k.strip().lower()] = v.strip()
    user = parts.get("user id") or parts.get("user") or parts.get("uid")
    pwd  = parts.get("password") or parts.get("pwd")
    host = parts.get("host") or parts.get("server")
    port = parts.get("port", "5432")
    db   = parts.get("database") or parts.get("db")
    sslm = (parts.get("ssl mode") or "prefer").lower().replace(" ", "")
    if not all([user, host, db]):
        raise ValueError(f"missing required .NET conn parts: {list(parts)}")
    q = {"sslmode": sslm} if sslm and sslm != "prefer" else {}
    return f"postgresql://{user}:{urllib.parse.quote(pwd or '')}@{host}:{port}/{db}?{urllib.parse.urlencode(q)}"

def main():
    ap = argparse.ArgumentParser(description="ERP-SYSTEM DB query runner (psql-style)")
    ap.add_argument("sql", nargs="*", help="SQL to run (or omit for --diag/--tables/--table)")
    ap.add_argument("-c", "--command", help="SQL to run (single string)")
    ap.add_argument("--table", help="Show schema for a table")
    ap.add_argument("--tables", action="store_true", help="List all tables")
    ap.add_argument("--diag", action="store_true", help="Show connection + version info")
    ap.add_argument("--json", action="store_true", help="Output as JSON (default: text table)")
    ap.add_argument("--no-header", action="store_true", help="Hide column headers")
    args = ap.parse_args()

    raw = os.environ.get(CONN_ENV) or os.environ.get("SUPABASE_DB_CONN")
    if not raw:
        print(f"ERROR: set {CONN_ENV} env var (Supabase/Neon connection string).", file=sys.stderr)
        sys.exit(2)
    try:
        url = normalize_conn(raw)
    except Exception as e:
        print(f"ERROR: bad conn string: {e}", file=sys.stderr)
        sys.exit(2)

    try:
        conn = psycopg2.connect(url, connect_timeout=10)
    except Exception as e:
        print(f"CONNECT FAIL: {type(e).__name__}: {e}", file=sys.stderr)
        sys.exit(3)
    try:
        cur = conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)

        if args.diag:
            cur.execute("SELECT version() AS v, current_database() AS db, current_user AS u, inet_server_addr() AS host, inet_server_port() AS port, now() AS server_time")
            row = cur.fetchone()
            for k, v in row.items():
                print(f"{k:14}: {v}")
            return

        if args.tables:
            cur.execute("""SELECT table_schema, table_name, pg_size_pretty(pg_total_relation_size(quote_ident(table_schema)||'.'||quote_ident(table_name))) AS size
                           FROM information_schema.tables
                           WHERE table_schema NOT IN ('pg_catalog','information_schema','mt_events')
                           ORDER BY table_schema, table_name""")
            for r in cur.fetchall(): print(f"  {r['table_schema']:15} {r['table_name']:35} {r['size']}")
            return

        if args.table:
            cur.execute("""SELECT column_name, data_type, is_nullable, column_default
                           FROM information_schema.columns
                           WHERE table_name = %s ORDER BY ordinal_position""", (args.table,))
            rows = cur.fetchall()
            if not rows:
                print(f"ERROR: table '{args.table}' not found", file=sys.stderr)
                sys.exit(4)
            for r in rows:
                print(f"  {r['column_name']:30} {r['data_type']:25} {'NULL' if r['is_nullable']=='YES' else 'NOT NULL':8} {r['column_default'] or ''}")
            return

        # Run SQL
        sql = args.command or " ".join(args.sql) if (args.command or args.sql) else None
        if not sql:
            ap.print_help()
            return
        cur.execute(sql)
        if cur.description is None:
            # no result set
            conn.commit()
            print(f"OK (no result). Rowcount: {cur.rowcount}")
            return
        rows = cur.fetchall()
        if args.json:
            import json
            print(json.dumps([dict(r) for r in rows], default=str, indent=2))
            return
        if not rows:
            print("(0 rows)")
            return
        cols = [d.name for d in cur.description]
        widths = [max(len(c), max((len(str(r[c])) for r in rows), default=0)) for c in cols]
        if not args.no_header:
            print(" | ".join(c.ljust(w) for c, w in zip(cols, widths)))
            print("-+-".join("-" * w for w in widths))
        for r in rows:
            print(" | ".join(str(r[c]).ljust(w) for c, w in zip(cols, widths)))
        print(f"\n({len(rows)} row{'s' if len(rows)!=1 else ''})")
    finally:
        conn.close()

if __name__ == "__main__":
    main()
