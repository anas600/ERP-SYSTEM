// Fix useToast import in all files that import it from @/components/ui
const fs = require('fs');
const path = require('path');

const files = [
  'admin/users/[id]/page.tsx',
  'admin/users/page.tsx',
  'admin/users/new/page.tsx',
  'admin/posting-rules/[id]/edit/page.tsx',
  'procurement/vendors/[id]/edit/page.tsx',
  'inventory/items/[id]/edit/page.tsx',
  'hr/attendance/page.tsx',
  'finance/sales-invoices/[id]/page.tsx',
  'finance/customers/[id]/edit/page.tsx',
  'finance/customers/new/page.tsx',
  'hr/employees/[id]/edit/page.tsx',
];

const root = 'src/frontend/app/(authenticated)';
let fixed = 0, skipped = 0;

for (const f of files) {
  const full = path.join(root, f);
  if (!fs.existsSync(full)) { console.log(`MISS: ${full}`); skipped++; continue; }
  let content = fs.readFileSync(full, 'utf-8');
  const before = content;

  // Pattern 1: `useToast,` or `, useToast` inside the @/components/ui import
  // Remove useToast from the @/components/ui imports
  content = content.replace(/^(\s*useToast,)\s*$/gm, '');
  content = content.replace(/^(\s*,\s*useToast)\s*$/gm, '');
  content = content.replace(/^(\s*useToast)\s*$/gm, '');
  // Inside the import block: `useToast,` at start, `, useToast` in middle, `, useToast` at end
  // Try the multi-line block replacement
  const m = content.match(/import\s*\{([^}]*)\}\s*from\s*'@\/components\/ui';/s);
  if (m) {
    const names = m[1].split(',').map(s => s.trim()).filter(s => s && s !== 'useToast');
    if (names.length === 0) {
      // Remove the whole import
      content = content.replace(/import\s*\{[^}]*\}\s*from\s*'@\/components\/ui';\s*\n/s, '');
    } else {
      const replacement = `import { ${names.join(', ')} } from '@/components/ui';`;
      content = content.replace(m[0], replacement);
    }
  }

  // Add the new useToast import after the @/components/ui import (or after first import)
  if (!content.includes("from '@/lib/useToast'")) {
    const lastImport = content.lastIndexOf('import ');
    if (lastImport >= 0) {
      // Find the end of that import line
      const afterLast = content.indexOf('\n', lastImport);
      content = content.slice(0, afterLast + 1) +
        "import { useToast } from '@/lib/useToast';\n" +
        content.slice(afterLast + 1);
    }
  }

  if (content !== before) {
    fs.writeFileSync(full, content);
    console.log(`✅ Fixed: ${f}`);
    fixed++;
  } else {
    console.log(`— Skipped (no change): ${f}`);
    skipped++;
  }
}
console.log(`\nFixed: ${fixed} | Skipped: ${skipped}`);
