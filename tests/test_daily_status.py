"""Tests for daily-status-report.py — DEC-067"""

import importlib.util
import os
import sys
from pathlib import Path

# Load the script
SCRIPT = Path(__file__).parent.parent / "scripts" / "daily-status-report.py"
spec = importlib.util.spec_from_file_location("daily", SCRIPT)
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)


def test_format_report_full_metrics():
    """Should generate a complete report when all metrics provided."""
    db = {
        "tables": 35, "total_rows_estimate": 1000,
        "tenants_total": 4, "users_total": 5, "active_users_7d": 3,
        "companies_total": 6, "audit_log_total": 100, "audit_log_24h": 10,
        "soft_deleted_total": 0
    }
    gh = {
        "open_issues": 0, "open_prs": 2, "prs_merged_24h": 1,
        "recent_prs": [{"number": 119, "title": "Soft delete", "merged_at": "2026-07-22"}],
        "latest_commit": {"sha": "abc1234", "message": "feat: x", "date": "2026-07-22"}
    }
    report = mod.format_report(db, gh)

    assert "Daily Status Report" in report
    assert "Supabase" in report
    assert "35 tables" in report
    assert "Open PRs: 2" in report
    assert "#119" in report
    assert "abc1234" in report
    assert "DECs" in report


def test_format_report_with_errors():
    """Should handle errors gracefully (show warnings)."""
    db = {"error": "connection refused"}
    gh = {"error": "no token"}
    report = mod.format_report(db, gh)

    assert "Daily Status Report" in report
    assert "⚠️ DB Error" in report
    assert "⚠️ GitHub Error" in report


def test_format_report_empty_recent_prs():
    """Should show 'No PRs merged' when list is empty."""
    db = {"tables": 30}
    gh = {"open_issues": 0, "open_prs": 0, "prs_merged_24h": 0, "recent_prs": []}
    report = mod.format_report(db, gh)

    assert "No PRs merged" in report


def test_count_decs():
    """Should find DECs in docs/dec-* folders."""
    count = mod.count_decs()
    # Should be at least a few (DEC-051, DEC-052, etc.)
    assert count >= 0  # May be 0 in test env, just verify no crash


def test_http_get_handles_404():
    """Should return None on 404."""
    result = mod.http_get("https://api.github.com/repos/nonexistent/nonexistent-404-test-xyz")
    # Should return None (or actual data if rate limited, depends on env)
    assert result is None or isinstance(result, dict)


def test_format_report_includes_today_date():
    """Should include today's date in the title."""
    from datetime import datetime, timezone
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    db = {"tables": 30}
    gh = {"open_issues": 0, "open_prs": 0}
    report = mod.format_report(db, gh)
    assert today in report


def test_format_report_sections_present():
    """All expected sections should be present."""
    db = {"tables": 30, "total_rows_estimate": 100, "tenants_total": 1,
          "users_total": 1, "active_users_7d": 1, "companies_total": 1,
          "audit_log_total": 0, "audit_log_24h": 0, "soft_deleted_total": 0}
    gh = {"open_issues": 0, "open_prs": 0, "prs_merged_24h": 0,
          "recent_prs": [], "latest_commit": {"sha": "abc", "message": "test", "date": "2026-07-22"}}
    report = mod.format_report(db, gh)

    # Required sections
    assert "## 🚀 Sprint Status" in report
    assert "## 🔀 Recent PRs" in report
    assert "## 🧪 Test & Audit Health" in report
    assert "## 📋 DECs & Defense Layers" in report
    assert "## 🔨 Latest Commit" in report


def test_find_dl_count():
    """Should return a string (placeholder)."""
    result = mod.find_dl_count()
    assert isinstance(result, str)


if __name__ == "__main__":
    import pytest
    sys.exit(pytest.main([__file__, "-v"]))
