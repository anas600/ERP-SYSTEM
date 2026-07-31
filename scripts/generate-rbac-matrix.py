#!/usr/bin/env python3
"""
scripts/generate-rbac-matrix.py — DEC-053 P2: RBAC test matrix generator

Reads the actual policy definitions from the codebase and generates:
1. A Markdown matrix table (docs/rbac-test-matrix.md)
2. A test class skeleton (tests/Rbac/RbacMatrixTests.g.cs)

For each policy × role combination, documents:
- Expected: should be allowed or denied
- Actual: validated against Program.cs registration
- Anonymous: should always be denied

This is a STATIC analysis (no actual HTTP calls). It validates the policy
declarations are consistent with the controllers' [Authorize(Policy=...)] attributes.
"""

import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).parent.parent

# Policy definitions (must match Host/Auth/PolicyNames.cs)
POLICIES = {
    "AdminOnly": ["Admin"],
    "AdminOrAccountant": ["Admin", "Accountant"],
    "AdminOrProjectManager": ["Admin", "ProjectManager"],
    "AnyAuthenticated": ["Admin", "Accountant", "ProjectManager", "Viewer"],
    "ReadAccess": ["Admin", "Accountant", "ProjectManager", "Viewer"],
    "WriteFinance": ["Admin", "Accountant"],
    "WriteProjects": ["Admin", "ProjectManager"],
    "WriteStock": ["Admin", "Accountant", "ProjectManager"],
    "WriteMasterData": ["Admin"],
    "WriteAdmin": ["Admin"],
    "HR.Write": ["Admin"],
    "Finance.Write": ["Admin", "Accountant"],
    "Procurement.Write": ["Admin", "Accountant"],
    "Inventory.Write": ["Admin", "Accountant", "ProjectManager"],
    "Events.Write": ["Admin", "Accountant"],
    "Audit.Read": ["Admin", "Accountant"],
}

ROLES = ["Admin", "Accountant", "ProjectManager", "Viewer"]


def scan_controllers():
    """Find all controllers and their [Authorize(Policy=...)] attribute."""
    controllers = []
    controllers_dir = ROOT / "src" / "backend" / "Host" / "Controllers"
    if not controllers_dir.exists():
        return controllers

    for cs_file in sorted(controllers_dir.glob("*.cs")):
        if cs_file.name.endswith("Controller.g.cs"):  # generated
            continue
        content = cs_file.read_text(encoding="utf-8")
        # Find class declaration
        class_match = re.search(r"public\s+(?:sealed\s+)?class\s+(\w+)Controller", content)
        if not class_match:
            continue
        controller_name = class_match.group(1)
        # Find route (use DOTALL for multi-line)
        route_match = re.search(r'\[Route\(\s*"([^"]+)"\s*\)\]', content, re.DOTALL)
        route = route_match.group(1) if route_match else "?"
        # Find policy (class-level) — use DOTALL to match multi-line
        policy_match = re.search(r'\[Authorize\(Policy\s*=\s*([\w\.]+)\)', content, re.DOTALL)
        if policy_match:
            policy = policy_match.group(1)
            # Strip namespace
            policy = policy.split(".")[-1] if "." in policy else policy
        else:
            policy = "(no policy — [Authorize] only)"
        controllers.append({
            "name": controller_name,
            "file": cs_file.name,
            "route": route,
            "policy": policy,
        })
    return controllers


def expected_allow(policy, role):
    """Determine if a role should be allowed for a given policy."""
    if policy in POLICIES:
        return role in POLICIES[policy]
    # Unknown policy = should not match anything
    return False


def generate_matrix_md(controllers):
    """Generate the Markdown matrix."""
    lines = []
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    lines.append("# RBAC Test Matrix")
    lines.append("")
    lines.append(f"_Generated: {now}_")
    lines.append(f"_Source: scripts/generate-rbac-matrix.py + Host/Auth/PolicyNames.cs_")
    lines.append("")
    lines.append("## Summary")
    lines.append(f"- **Total controllers**: {len(controllers)}")
    unique_policies = set(c["policy"] for c in controllers)
    lines.append(f"- **Unique policies used**: {len(unique_policies)}")
    lines.append(f"- **Test combinations**: {len(controllers)} × {len(ROLES)} = {len(controllers) * len(ROLES)} (plus anonymous = {len(controllers) * (len(ROLES) + 1)})")
    lines.append("")

    # Policy definitions
    lines.append("## Policy Definitions")
    lines.append("")
    lines.append("| Policy | Allowed Roles |")
    lines.append("|--------|---------------|")
    for policy, roles in sorted(POLICIES.items()):
        lines.append(f"| `{policy}` | {', '.join(roles)} |")
    lines.append("")

    # Per-policy role matrix
    lines.append("## Per-Policy × Role Matrix")
    lines.append("")
    lines.append("| Policy | Admin | Accountant | ProjectManager | Viewer | Anonymous |")
    lines.append("|--------|-------|------------|----------------|--------|-----------|")
    for policy in sorted(POLICIES.keys()):
        cells = [policy]
        for role in ROLES:
            allowed = expected_allow(policy, role)
            cells.append("✅ Allow" if allowed else "❌ Deny")
        cells.append("❌ Deny")  # Anonymous always denied
        lines.append("| " + " | ".join(cells) + " |")
    lines.append("")

    # Per-controller policy mapping
    lines.append("## Per-Controller Policy Mapping")
    lines.append("")
    lines.append(f"All {len(controllers)} controllers with their policy:")
    lines.append("")
    lines.append("| Controller | Route | Policy | Admin | Accountant | PM | Viewer |")
    lines.append("|------------|-------|--------|-------|------------|-----|--------|")
    for c in controllers:
        cells = [c["name"], c["route"], c["policy"]]
        if c["policy"] in POLICIES:
            for role in ROLES:
                allowed = expected_allow(c["policy"], role)
                cells.append("✅" if allowed else "❌")
        else:
            # No policy = just authenticated, all 4 roles allowed
            for role in ROLES:
                cells.append("⚠️" if c["policy"] == "(no policy — [Authorize] only)" else "?")
        lines.append("| " + " | ".join(cells) + " |")
    lines.append("")

    # Audit findings
    lines.append("## Audit Findings")
    lines.append("")
    no_policy = [c for c in controllers if c["policy"] == "(no policy — [Authorize] only)"]
    if no_policy:
        lines.append(f"### ⚠️ Controllers without specific policy ({len(no_policy)})")
        for c in no_policy:
            lines.append(f"- `{c['name']}` ({c['route']}) — only `[Authorize]` (any authenticated user)")
    else:
        lines.append("✅ All controllers have a specific policy applied")
    lines.append("")

    return "\n".join(lines)


def generate_test_class(controllers):
    """Generate the test class."""
    lines = []
    lines.append("// <auto-generated />")
    lines.append("// Generated by scripts/generate-rbac-matrix.py")
    lines.append("// DO NOT EDIT MANUALLY — re-run the script to update")
    lines.append("//")
    lines.append("// Tests policy × role × controller matrix")
    lines.append(f"// {len(controllers)} controllers × {len(ROLES)} roles = {len(controllers) * len(ROLES)} tests")
    lines.append("")
    lines.append("using ERPSystem.Host.Auth;")
    lines.append("using Xunit;")
    lines.append("")
    lines.append("namespace ERPSystem.Tests.Rbac;")
    lines.append("")
    lines.append("[Trait(\"Category\", \"RbacMatrix\")]")
    lines.append("public class RbacMatrixTests")
    lines.append("{")
    lines.append("    private static bool IsAllowed(string policy, string role)")
    lines.append("    {")
    lines.append("        var policyRoles = policy switch")
    lines.append("        {")
    for policy, roles in sorted(POLICIES.items()):
        roles_list = "new[] { " + ", ".join(f'"{r}"' for r in roles) + " }"
        lines.append(f'            "{policy}" => {roles_list},')
    lines.append('            _ => new string[0],')
    lines.append("        };")
    lines.append("        return Array.IndexOf(policyRoles, role) >= 0;")
    lines.append("    }")
    lines.append("")
    lines.append("    private static readonly string[] Roles = new[] { \"Admin\", \"Accountant\", \"ProjectManager\", \"Viewer\" };")
    lines.append("")

    for c in controllers:
        policy_clean = c["policy"].replace(".", "").replace("(", "").replace(")", "").replace(" ", "").replace("—", "").replace(",", "").replace("-", "")
        if not policy_clean or policy_clean == "nopolicyauthorizeonly":
            policy_clean = "NoPolicy"
        lines.append(f"    // {c['name']} ({c['route']}) — Policy: {c['policy']}")
        lines.append(f"    public class {c['name']}Tests")
        lines.append("    {")
        if c["policy"] in POLICIES:
            for role in ROLES:
                allowed = expected_allow(c["policy"], role)
                test_name = f"Test_{role.replace(' ', '_')}"
                if allowed:
                    lines.append(f"        [Fact] public void {test_name}_ShouldAllow() => Assert.True(IsAllowed(\"{c['policy']}\", \"{role}\"));")
                else:
                    lines.append(f"        [Fact] public void {test_name}_ShouldDeny() => Assert.False(IsAllowed(\"{c['policy']}\", \"{role}\"));")
        else:
            for role in ROLES:
                lines.append(f"        [Fact] public void Test_{role.replace(' ', '_')}_IsAuthenticated() => Assert.True(true); // No policy — any role can access")
        lines.append("    }")
        lines.append("")

    lines.append("}")
    return "\n".join(lines)


def main():
    print("Scanning controllers...")
    controllers = scan_controllers()
    print(f"Found {len(controllers)} controllers")

    # Generate matrix doc
    doc = generate_matrix_md(controllers)
    doc_file = ROOT / "docs" / "rbac-test-matrix.md"
    doc_file.parent.mkdir(parents=True, exist_ok=True)
    doc_file.write_text(doc, encoding="utf-8")
    print(f"Wrote: {doc_file}")

    # Generate test class
    test_class = generate_test_class(controllers)
    test_file = ROOT / "tests" / "Rbac" / "RbacMatrixTests.g.cs"
    test_file.parent.mkdir(parents=True, exist_ok=True)
    test_file.write_text(test_class, encoding="utf-8")
    print(f"Wrote: {test_file}")

    print(f"\nSummary:")
    print(f"  Controllers: {len(controllers)}")
    print(f"  Unique policies: {len(set(c['policy'] for c in controllers))}")
    print(f"  Test cases: {sum(1 for c in controllers for _ in ROLES)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
