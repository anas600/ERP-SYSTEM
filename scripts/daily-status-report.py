#!/usr/bin/env python3
"""
scripts/daily-status-report.py — DEC-067: Auto-generate daily status report.

Pulls metrics from:
1. Supabase PostgreSQL (table counts, row counts, audit)
2. GitHub API (PRs, issues, releases)

Generates: Markdown report to /tmp/daily-report-YYYY-MM-DD.md

Required env:
  SUPABASE_URL — PostgreSQL connection string
  GITHUB_TOKEN — GitHub PAT (read scope)
  GITHUB_REPO — owner/repo (default: anas600/ERP-SYSTEM)
  GITHUB_API — API URL (default: https://api.github.com)
Optional env:
  PREV_REPORT_FILE — path to yesterday's report (for trend)
  TELEGRAM_BOT_TOKEN, TELEGRAM_CHAT_ID — for short TG notification
  OUTPUT_DIR — where to save the report (default /tmp)
"""

import json
import os
import sys
import urllib.request
import urllib.error
from datetime import datetime, timezone, timedelta
from pathlib import Path

try:
    import psycopg2
    HAS_PSQL = True
except ImportError:
    HAS_PSQL = False

GITHUB_API = os.environ.get("GITHUB_API", "https://api.github.com")
GITHUB_REPO = os.environ.get("GITHUB_REPO", "anas600/ERP-SYSTEM")
OUTPUT_DIR = Path(os.environ.get("OUTPUT_DIR", "/tmp"))


def http_get(url, headers=None, timeout=15):
    """Make a GET request, return JSON or None on error."""
    req = urllib.request.Request(url, headers=headers or {})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return json.loads(resp.read())
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError) as e:
        print(f"WARN: GET {url} failed: {e}", file=sys.stderr)
        return None


def get_db_metrics(db_url):
    """Pull metrics from Supabase."""
    if not HAS_PSQL:
        return {"error": "psycopg2 not installed"}
    if not db_url:
        return {"error": "SUPABASE_URL not set"}

    metrics = {}
    try:
        conn = psycopg2.connect(db_url, connect_timeout=10)
        cur = conn.cursor()

        # Table count
        cur.execute("""
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'public'
        """)
        metrics["tables"] = cur.fetchone()[0]

        # Total rows (estimate)
        cur.execute("""
            SELECT SUM(n_live_tup) FROM pg_stat_user_tables
        """)
        metrics["total_rows_estimate"] = cur.fetchone()[0] or 0

        # Audit log entries
        cur.execute("SELECT COUNT(*) FROM audit_log")
        metrics["audit_log_total"] = cur.fetchone()[0]

        # Recent audit (last 24h)
        cur.execute("""
            SELECT COUNT(*) FROM audit_log
            WHERE created_at > NOW() - INTERVAL '24 hours'
        """)
        metrics["audit_log_24h"] = cur.fetchone()[0]

        # Active users (logged in last 7 days)
        cur.execute("""
            SELECT COUNT(DISTINCT user_id) FROM audit_log
            WHERE user_id IS NOT NULL
              AND created_at > NOW() - INTERVAL '7 days'
        """)
        metrics["active_users_7d"] = cur.fetchone()[0]

        # Total users
        cur.execute("SELECT COUNT(*) FROM users")
        metrics["users_total"] = cur.fetchone()[0]

        # Total tenants
        cur.execute("SELECT COUNT(*) FROM tenants")
        metrics["tenants_total"] = cur.fetchone()[0]

        # Total companies
        cur.execute("SELECT COUNT(*) FROM companies")
        metrics["companies_total"] = cur.fetchone()[0]

        # Soft-deleted records
        cur.execute("""
            SELECT
                (SELECT COUNT(*) FROM sales_invoices WHERE is_deleted = TRUE) +
                (SELECT COUNT(*) FROM payments WHERE is_deleted = TRUE) +
                (SELECT COUNT(*) FROM journal_entries WHERE is_deleted = TRUE) +
                (SELECT COUNT(*) FROM users WHERE is_deleted = TRUE)
        """)
        metrics["soft_deleted_total"] = cur.fetchone()[0]

        cur.close()
        conn.close()
    except Exception as e:
        metrics["error"] = str(e)
    return metrics


def get_github_metrics(github_token):
    """Pull metrics from GitHub API."""
    if not github_token:
        return {"error": "GITHUB_TOKEN not set"}

    headers = {
        "Authorization": f"token {github_token}",
        "Accept": "application/vnd.github+json",
    }

    metrics = {}

    # Open issues
    issues = http_get(
        f"{GITHUB_API}/repos/{GITHUB_REPO}/issues?state=open&per_page=100",
        headers,
    )
    if issues is not None:
        # Filter out PRs (they show up in issues endpoint)
        prs_in_issues = [i for i in issues if "pull_request" in i]
        metrics["open_issues"] = len(issues) - len(prs_in_issues)
        metrics["open_prs"] = len(prs_in_issues)

    # Recent PRs (last 24h merged)
    since = (datetime.now(timezone.utc) - timedelta(days=1)).isoformat()
    recent_prs = http_get(
        f"{GITHUB_API}/repos/{GITHUB_REPO}/pulls?state=closed&sort=updated&direction=desc&per_page=20&since={since}",
        headers,
    )
    if recent_prs is not None:
        merged = [p for p in recent_prs if p.get("merged_at")]
        metrics["prs_merged_24h"] = len(merged)
        metrics["recent_prs"] = [
            {"number": p["number"], "title": p["title"], "merged_at": p["merged_at"]}
            for p in merged[:5]
        ]

    # Total commits on default branch
    commits = http_get(
        f"{GITHUB_API}/repos/{GITHUB_REPO}/commits?per_page=1",
        headers,
    )
    if commits is not None and len(commits) > 0:
        metrics["latest_commit"] = {
            "sha": commits[0]["sha"][:7],
            "message": commits[0]["commit"]["message"].split("\n")[0][:80],
            "date": commits[0]["commit"]["committer"]["date"],
        }

    return metrics


def find_dl_count():
    """Count defense layer numbers mentioned in merged PR titles + commits."""
    # Heuristic: count PRs (each PR ~3-5 DLs)
    # Actual count tracked in /workspace/.mavis but we don't have direct access
    return "see CHANGELOG"  # Placeholder


def count_decs():
    """Count DECs in docs/dec-* folders."""
    docs_dir = Path(__file__).parent.parent / "docs"
    if not docs_dir.exists():
        return 0
    dec_count = 0
    for d in docs_dir.iterdir():
        if d.is_dir() and (d.name.startswith("dec-") or d.name.startswith("DEC-")):
            for f in d.iterdir():
                if f.is_file() and (f.name.startswith("DEC-") or "README" in f.name):
                    dec_count += 1
    return dec_count


def format_report(db_metrics, gh_metrics):
    """Format the report as Markdown."""
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    lines = []

    lines.append(f"# 📊 Daily Status Report — {today}")
    lines.append("")
    lines.append(f"_Generated: {datetime.now(timezone.utc).isoformat()}_")
    lines.append("")

    # Sprint Status
    lines.append("## 🚀 Sprint Status")
    if "error" in db_metrics:
        lines.append(f"⚠️ DB Error: {db_metrics['error']}")
    else:
        lines.append(f"- **Database**: Supabase ({db_metrics.get('tables', '?')} tables, ~{db_metrics.get('total_rows_estimate', 0):,} rows)")
        lines.append(f"- **Tenants**: {db_metrics.get('tenants_total', 0)}")
        lines.append(f"- **Users**: {db_metrics.get('users_total', 0)} (active 7d: {db_metrics.get('active_users_7d', 0)})")
        lines.append(f"- **Companies**: {db_metrics.get('companies_total', 0)}")
    if "error" in gh_metrics:
        lines.append(f"⚠️ GitHub Error: {gh_metrics['error']}")
    else:
        lines.append(f"- **Open PRs**: {gh_metrics.get('open_prs', '?')}")
        lines.append(f"- **Open Issues**: {gh_metrics.get('open_issues', '?')}")
        lines.append(f"- **PRs merged (24h)**: {gh_metrics.get('prs_merged_24h', '?')}")
    lines.append("")

    # Recent PRs
    lines.append("## 🔀 Recent PRs (last 24h)")
    recent = gh_metrics.get("recent_prs", [])
    if recent:
        for p in recent:
            lines.append(f"- **#{p['number']}** {p['title']} (merged {p['merged_at'][:10]})")
    else:
        lines.append("_No PRs merged in last 24h_")
    lines.append("")

    # Test Health
    lines.append("## 🧪 Test & Audit Health")
    if "error" not in db_metrics:
        lines.append(f"- **Audit log entries**: {db_metrics.get('audit_log_total', 0):,} (last 24h: {db_metrics.get('audit_log_24h', 0):,})")
        lines.append(f"- **Soft-deleted records**: {db_metrics.get('soft_deleted_total', 0)}")
    lines.append("")

    # DECs + Defense Layers
    decs = count_decs()
    lines.append("## 📋 DECs & Defense Layers")
    lines.append(f"- **DECs documented**: {decs}")
    lines.append(f"- **Defense Layers**: {find_dl_count()} (run `git log --oneline | wc -l` for commit count)")
    lines.append("")

    # Latest commit
    if "latest_commit" in gh_metrics:
        lc = gh_metrics["latest_commit"]
        lines.append("## 🔨 Latest Commit")
        lines.append(f"- **`{lc['sha']}`** {lc['message']}")
        lines.append(f"  _{lc['date']}_")
        lines.append("")

    lines.append("---")
    lines.append("_Auto-generated by scripts/daily-status-report.py — DEC-067_")
    lines.append("")

    return "\n".join(lines)


def post_to_telegram(text):
    """Post a short summary to Telegram."""
    bot = os.environ.get("TELEGRAM_BOT_TOKEN")
    chat = os.environ.get("TELEGRAM_CHAT_ID")
    if not bot or not chat:
        print("INFO: Telegram not configured, skipping")
        return False
    url = f"https://api.telegram.org/bot{bot}/sendMessage"
    payload = json.dumps({"chat_id": chat, "text": text, "parse_mode": "Markdown"}).encode()
    req = urllib.request.Request(url, data=payload, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return resp.status == 200
    except Exception as e:
        print(f"WARN: Telegram post failed: {e}", file=sys.stderr)
        return False


def main():
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    output_file = OUTPUT_DIR / f"daily-report-{today}.md"

    print(f"Generating daily report for {today}...")

    db_metrics = get_db_metrics(os.environ.get("SUPABASE_URL", ""))
    gh_metrics = get_github_metrics(os.environ.get("GITHUB_TOKEN", ""))

    report = format_report(db_metrics, gh_metrics)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    output_file.write_text(report, encoding="utf-8")
    print(f"Report saved: {output_file}")
    print("---")
    print(report)

    # Telegram summary (short version)
    summary_lines = [
        f"📊 *Daily Report — {today}*",
    ]
    if "error" not in gh_metrics:
        summary_lines.append(f"• Open PRs: {gh_metrics.get('open_prs', '?')}")
        summary_lines.append(f"• Merged 24h: {gh_metrics.get('prs_merged_24h', '?')}")
    if "error" not in db_metrics:
        summary_lines.append(f"• Active users 7d: {db_metrics.get('active_users_7d', 0)}")
        summary_lines.append(f"• Audit log 24h: {db_metrics.get('audit_log_24h', 0):,}")
    if post_to_telegram("\n".join(summary_lines)):
        print("Telegram: posted summary")
    else:
        print("Telegram: skipped or failed")

    return 0


if __name__ == "__main__":
    sys.exit(main())
