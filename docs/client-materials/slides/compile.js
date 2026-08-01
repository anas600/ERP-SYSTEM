// ERP-SYSTEM Client Demo Slides
// Sprint 20 - Demo 2
// 8 slides for the client meeting

const pptxgen = require('pptxgenjs');
const pres = new pptxgen();

pres.layout = 'LAYOUT_16x9';
pres.title = 'ERP-SYSTEM Client Demo - Sprint 20';
pres.author = 'Mavis (Muhammad mode) - Strategic Advisor';

// Theme - professional, clean, Libyan-friendly
const theme = {
  primary: '0B3D91',    // deep blue
  secondary: '4A5568',  // gray text
  accent: 'E5A823',     // gold accent
  light: 'F4F6F8',      // light gray background
  bg: 'FFFFFF'          // white
};

// Helper: add page number badge
function addPageNumber(slide, num) {
  slide.addText(String(num) + ' / 8', {
    x: 9.0, y: 5.25, w: 0.9, h: 0.3,
    fontSize: 10, color: theme.secondary, fontFace: 'Arial',
    align: 'right', valign: 'middle', margin: 0
  });
}

// Helper: add bottom-right brand badge
function addBrandBadge(slide) {
  slide.addText('ERP-SYSTEM  |  Sprint 20 - Demo 2', {
    x: 0.3, y: 5.25, w: 6, h: 0.3,
    fontSize: 10, color: theme.secondary, fontFace: 'Arial',
    align: 'left', valign: 'middle', margin: 0
  });
}

// Helper: add title strip (top of content slide)
function addTitleStrip(slide, titleText) {
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 10, h: 0.85,
    fill: { color: theme.primary }, line: { type: 'none' }
  });
  slide.addText(titleText, {
    x: 0.5, y: 0.1, w: 9, h: 0.65,
    fontSize: 26, bold: true, color: 'FFFFFF', fontFace: 'Arial',
    align: 'left', valign: 'middle', margin: 0
  });
  // Accent bar
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0.85, w: 10, h: 0.05,
    fill: { color: theme.accent }, line: { type: 'none' }
  });
}

// ============== SLIDE 1: COVER ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.primary };

  // Accent corner block
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 10, h: 0.4,
    fill: { color: theme.accent }, line: { type: 'none' }
  });

  // Main title
  slide.addText('ERP-SYSTEM', {
    x: 0.5, y: 1.2, w: 9, h: 1.2,
    fontSize: 72, bold: true, color: 'FFFFFF', fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0, charSpacing: 4
  });

  // Subtitle (Arabic)
  slide.addText('نظام إدارة موحّد للشركات الليبية', {
    x: 0.5, y: 2.5, w: 9, h: 0.7,
    fontSize: 28, color: 'FFFFFF', fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  // English subtitle
  slide.addText('Unified ERP for Libyan SMEs', {
    x: 0.5, y: 3.25, w: 9, h: 0.5,
    fontSize: 18, italic: true, color: theme.light, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  // Demo info card
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 2.5, y: 4.1, w: 5, h: 0.9,
    fill: { color: 'FFFFFF' }, line: { type: 'none' }
  });
  slide.addText([
    { text: 'Live demo:', options: { bold: true, color: theme.primary } },
    { text: '  http://localhost:3000', options: { color: theme.secondary } }
  ], {
    x: 2.5, y: 4.15, w: 5, h: 0.4,
    fontSize: 14, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });
  slide.addText([
    { text: 'Login:', options: { bold: true, color: theme.primary } },
    { text: '  admin@erp.local / ChangeMe1234!', options: { color: theme.secondary } }
  ], {
    x: 2.5, y: 4.55, w: 5, h: 0.4,
    fontSize: 12, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  // Footer
  slide.addText('Sprint 20 - Demo 2  |  2026-08-01', {
    x: 0.5, y: 5.3, w: 9, h: 0.3,
    fontSize: 11, color: theme.light, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });
}

// ============== SLIDE 2: THE PROBLEM ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, 'The Problem at Libyan SMEs');

  slide.addText('Most Libyan small-to-medium businesses run on a stack of disconnected tools. The result: chaos, lost time, and missed tax deadlines.', {
    x: 0.5, y: 1.1, w: 9, h: 0.7,
    fontSize: 16, color: theme.secondary, fontFace: 'Arial',
    align: 'left', valign: 'top', margin: 0
  });

  // Problem cards
  const problems = [
    { icon: '1', title: 'Excel + paper + WhatsApp', desc: 'Invoices, receipts, payroll scattered across files and chats. No single source of truth.' },
    { icon: '2', title: 'Tax invoices with no journal link', desc: 'VAT filed manually. Risk of mistakes. Auditor finds discrepancies every year.' },
    { icon: '3', title: 'Manual inventory counts', desc: 'Stock-outs and overstock. No low-stock alerts. Write-offs eat margin.' },
    { icon: '4', title: 'Payroll on a calculator', desc: 'EOS guessed. Overtime missed. Tax errors trigger penalties.' }
  ];

  problems.forEach((p, i) => {
    const x = 0.5 + (i % 2) * 4.7;
    const y = 2.0 + Math.floor(i / 2) * 1.55;
    // Card background
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 4.4, h: 1.4,
      fill: { color: theme.light }, line: { type: 'none' }
    });
    // Accent number circle
    slide.addShape(pres.shapes.OVAL, {
      x: x + 0.15, y: y + 0.15, w: 0.55, h: 0.55,
      fill: { color: theme.primary }, line: { type: 'none' }
    });
    slide.addText(p.icon, {
      x: x + 0.15, y: y + 0.15, w: 0.55, h: 0.55,
      fontSize: 22, bold: true, color: 'FFFFFF', fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Title
    slide.addText(p.title, {
      x: x + 0.85, y: y + 0.15, w: 3.4, h: 0.4,
      fontSize: 15, bold: true, color: theme.primary, fontFace: 'Arial',
      align: 'left', valign: 'middle', margin: 0
    });
    // Description
    slide.addText(p.desc, {
      x: x + 0.85, y: y + 0.6, w: 3.4, h: 0.7,
      fontSize: 11, color: theme.secondary, fontFace: 'Arial',
      align: 'left', valign: 'top', margin: 0
    });
  });

  addBrandBadge(slide);
  addPageNumber(slide, 2);
}

// ============== SLIDE 3: THE SOLUTION ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, 'The Solution: One Unified ERP');

  slide.addText('ERP-SYSTEM is a local-first, Libyan-built ERP that puts every business function in one place. Same data, same workflow, same UI.', {
    x: 0.5, y: 1.1, w: 9, h: 0.7,
    fontSize: 16, color: theme.secondary, fontFace: 'Arial',
    align: 'left', valign: 'top', margin: 0
  });

  // Solution pillars
  const pillars = [
    { title: 'Unified', desc: 'One database, one chart of accounts, one user. No double-entry.' },
    { title: 'Automated', desc: 'Every invoice posts a journal entry. Every GR updates stock. Every receipt allocates to invoices.' },
    { title: 'Auditable', desc: 'Every transaction has a source, a creator, a timestamp. Tax filing is one click.' },
    { title: 'Libyan', desc: 'LYD currency, Arabic labels, Libyan tax logic, local hosting.' }
  ];

  pillars.forEach((p, i) => {
    const x = 0.5 + (i % 2) * 4.7;
    const y = 2.0 + Math.floor(i / 2) * 1.55;
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 4.4, h: 1.4,
      fill: { color: 'FFFFFF' }, line: { color: theme.primary, width: 2 }
    });
    // Accent top bar
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 4.4, h: 0.1,
      fill: { color: theme.accent }, line: { type: 'none' }
    });
    slide.addText(p.title, {
      x: x + 0.2, y: y + 0.2, w: 4.0, h: 0.4,
      fontSize: 20, bold: true, color: theme.primary, fontFace: 'Arial',
      align: 'left', valign: 'middle', margin: 0
    });
    slide.addText(p.desc, {
      x: x + 0.2, y: y + 0.65, w: 4.0, h: 0.7,
      fontSize: 12, color: theme.secondary, fontFace: 'Arial',
      align: 'left', valign: 'top', margin: 0
    });
  });

  addBrandBadge(slide);
  addPageNumber(slide, 3);
}

// ============== SLIDE 4: 13 FUNCTIONS ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, '13 Functions - Live Today');

  slide.addText('Every function is implemented, documented, and demo-ready. Click any of them in the demo.', {
    x: 0.5, y: 1.1, w: 9, h: 0.5,
    fontSize: 14, color: theme.secondary, fontFace: 'Arial',
    align: 'left', valign: 'top', margin: 0
  });

  // 13 functions in 2 columns
  const functions = [
    { code: 'AR', name: 'Customers', path: '/finance/customers' },
    { code: 'AR', name: 'Sales Invoices', path: '/finance/sales-invoices' },
    { code: 'AR', name: 'Receipts', path: '/finance/receipts' },
    { code: 'PUR', name: 'Vendors', path: '/procurement/vendors' },
    { code: 'PUR', name: 'Purchase Orders', path: '/procurement/purchase-orders' },
    { code: 'PUR', name: 'Goods Receipts', path: '/procurement/goods-receipts' },
    { code: 'PUR', name: 'Vendor Bills', path: '/procurement/bills' },
    { code: 'INV', name: 'Items', path: '/inventory/items' },
    { code: 'FIN', name: 'Chart of Accounts', path: '/finance/accounts' },
    { code: 'FIN', name: 'Journal Entries', path: '/finance/journal-entries' },
    { code: 'HR', name: 'Employees', path: '/hr/employees' },
    { code: 'PAY', name: 'Payroll Runs', path: '/hr/payroll' },
    { code: 'PRJ', name: 'Projects', path: '/projects' }
  ];

  functions.forEach((f, i) => {
    const x = 0.5 + (i % 2) * 4.7;
    const y = 1.75 + Math.floor(i / 2) * 0.5;
    // Module badge
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 0.6, h: 0.4,
      fill: { color: theme.primary }, line: { type: 'none' }
    });
    slide.addText(f.code, {
      x: x, y: y, w: 0.6, h: 0.4,
      fontSize: 11, bold: true, color: 'FFFFFF', fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Function name
    slide.addText(f.name, {
      x: x + 0.75, y: y, w: 2.5, h: 0.4,
      fontSize: 13, bold: true, color: '1A1A1A', fontFace: 'Arial',
      align: 'left', valign: 'middle', margin: 0
    });
    // Path
    slide.addText(f.path, {
      x: x + 0.75, y: y, w: 3.5, h: 0.4,
      fontSize: 10, color: theme.secondary, fontFace: 'Arial',
      align: 'right', valign: 'middle', margin: 0
    });
  });

  // Total bar
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0.5, y: 5.0, w: 9, h: 0.4,
    fill: { color: theme.accent }, line: { type: 'none' }
  });
  slide.addText('13 of 13 demo functions documented (Sprint 19 + 20) - 14 more in P2 backlog', {
    x: 0.5, y: 5.0, w: 9, h: 0.4,
    fontSize: 12, bold: true, color: theme.primary, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  addPageNumber(slide, 4);
}

// ============== SLIDE 5: DEMO FLOW ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, 'Demo Flow (15 minutes)');

  // Horizontal flow arrows
  const steps = [
    { n: '1', label: 'Login', detail: 'admin / pass' },
    { n: '2', label: 'Dashboard', detail: '4 KPIs at a glance' },
    { n: '3', label: 'New Customer', detail: '30 sec form' },
    { n: '4', label: 'Create Invoice', detail: 'auto journal' },
    { n: '5', label: 'Receive Money', detail: '1-click allocate' }
  ];

  const startX = 0.3;
  const stepW = 1.7;
  const gap = 0.3;

  steps.forEach((s, i) => {
    const x = startX + i * (stepW + gap);
    // Step circle
    slide.addShape(pres.shapes.OVAL, {
      x: x + 0.55, y: 1.5, w: 0.6, h: 0.6,
      fill: { color: theme.primary }, line: { type: 'none' }
    });
    slide.addText(s.n, {
      x: x + 0.55, y: 1.5, w: 0.6, h: 0.6,
      fontSize: 20, bold: true, color: 'FFFFFF', fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Label
    slide.addText(s.label, {
      x: x, y: 2.2, w: stepW, h: 0.4,
      fontSize: 14, bold: true, color: theme.primary, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Detail
    slide.addText(s.detail, {
      x: x, y: 2.6, w: stepW, h: 0.3,
      fontSize: 10, color: theme.secondary, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Arrow between steps
    if (i < steps.length - 1) {
      slide.addShape(pres.shapes.LINE, {
        x: x + stepW, y: 1.8, w: gap, h: 0,
        line: { color: theme.accent, width: 2, endArrowType: 'triangle' }
      });
    }
  });

  // Key wins panel
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0.5, y: 3.4, w: 9, h: 1.7,
    fill: { color: theme.light }, line: { type: 'none' }
  });
  slide.addText('What you will see in 15 minutes:', {
    x: 0.7, y: 3.5, w: 8.6, h: 0.4,
    fontSize: 14, bold: true, color: theme.primary, fontFace: 'Arial',
    align: 'left', valign: 'middle', margin: 0
  });
  slide.addText([
    { text: 'A complete sales cycle: customer creation -> invoice -> post -> payment -> balance update', options: { bullet: true, breakLine: true } },
    { text: 'Every action creates an automatic journal entry (Dr AR / Cr Sales) - no manual accounting', options: { bullet: true, breakLine: true } },
    { text: 'Customer statement + AR aging update in real time', options: { bullet: true, breakLine: true } },
    { text: 'The same workflow works for vendors, items, payroll, and 9 other functions', options: { bullet: true } }
  ], {
    x: 0.7, y: 3.9, w: 8.6, h: 1.1,
    fontSize: 12, color: theme.secondary, fontFace: 'Arial',
    align: 'left', valign: 'top', paraSpaceAfter: 4, margin: 0
  });

  addBrandBadge(slide);
  addPageNumber(slide, 5);
}

// ============== SLIDE 6: WHY IT MATTERS ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, 'Why It Matters - The Numbers');

  // 4 big stat cards
  const stats = [
    { num: '30 -> 2 min', label: 'Invoice prep time', desc: 'Data flows automatically from customer to invoice to journal entry' },
    { num: '13', label: 'Documented functions', desc: 'Every workflow has a bilingual guide: business purpose, API, UI, edge cases' },
    { num: '0', label: 'Manual reconciliations', desc: 'Every transaction posted creates a balanced journal entry - no manual reconciliation' },
    { num: '100%', label: 'Libyan', desc: 'LYD currency, Arabic labels, Libyan tax logic, local hosting - no external dependencies' }
  ];

  stats.forEach((s, i) => {
    const x = 0.5 + (i % 2) * 4.7;
    const y = 1.3 + Math.floor(i / 2) * 1.85;
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 4.4, h: 1.65,
      fill: { color: 'FFFFFF' }, line: { color: theme.primary, width: 1 }
    });
    slide.addText(s.num, {
      x: x, y: y + 0.1, w: 4.4, h: 0.65,
      fontSize: 32, bold: true, color: theme.accent, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    slide.addText(s.label, {
      x: x, y: y + 0.75, w: 4.4, h: 0.4,
      fontSize: 14, bold: true, color: theme.primary, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    slide.addText(s.desc, {
      x: x + 0.2, y: y + 1.15, w: 4.0, h: 0.45,
      fontSize: 10, color: theme.secondary, fontFace: 'Arial',
      align: 'center', valign: 'top', margin: 0
    });
  });

  addBrandBadge(slide);
  addPageNumber(slide, 6);
}

// ============== SLIDE 7: ARCHITECTURE ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.bg };
  addTitleStrip(slide, 'Architecture - Built to Last');

  // Stack diagram (3 layers)
  const layers = [
    { label: 'Frontend (Next.js 14)', detail: 'App Router, TypeScript, Tailwind, shadcn/ui - 87 pages', color: theme.primary },
    { label: 'Backend (C# / .NET 9)', detail: 'Dapper, FluentMigrator, JWT, RBAC, multi-company', color: theme.secondary },
    { label: 'Database (PostgreSQL 17)', detail: 'Fluent migrations, idempotent, no tenant_id - company_id only', color: theme.accent }
  ];

  layers.forEach((l, i) => {
    const y = 1.3 + i * 1.0;
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 1.0, y: y, w: 8.0, h: 0.85,
      fill: { color: l.color }, line: { type: 'none' }
    });
    slide.addText(l.label, {
      x: 1.2, y: y + 0.08, w: 7.6, h: 0.35,
      fontSize: 16, bold: true, color: 'FFFFFF', fontFace: 'Arial',
      align: 'left', valign: 'middle', margin: 0
    });
    slide.addText(l.detail, {
      x: 1.2, y: y + 0.43, w: 7.6, h: 0.35,
      fontSize: 12, color: 'FFFFFF', fontFace: 'Arial',
      align: 'left', valign: 'middle', margin: 0
    });
  });

  // Security + Modular callouts
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0.5, y: 4.5, w: 4.4, h: 0.85,
    fill: { color: theme.light }, line: { type: 'none' }
  });
  slide.addText([
    { text: 'Security: ', options: { bold: true, color: theme.primary } },
    { text: 'JWT + BCrypt + RBAC roles (Admin / Accountant / Sales / Viewer)', options: { color: theme.secondary } }
  ], {
    x: 0.6, y: 4.55, w: 4.2, h: 0.75,
    fontSize: 11, fontFace: 'Arial', align: 'left', valign: 'middle', margin: 0
  });

  slide.addShape(pres.shapes.RECTANGLE, {
    x: 5.1, y: 4.5, w: 4.4, h: 0.85,
    fill: { color: theme.light }, line: { type: 'none' }
  });
  slide.addText([
    { text: 'Modular: ', options: { bold: true, color: theme.primary } },
    { text: '16 modules - AccountsReceivable, Procurement, Inventory, Finance, HR, Payroll, Projects, etc.', options: { color: theme.secondary } }
  ], {
    x: 5.2, y: 4.55, w: 4.2, h: 0.75,
    fontSize: 11, fontFace: 'Arial', align: 'left', valign: 'middle', margin: 0
  });

  addBrandBadge(slide);
  addPageNumber(slide, 7);
}

// ============== SLIDE 8: NEXT STEPS ==============
{
  const slide = pres.addSlide();
  slide.background = { color: theme.primary };

  // Accent corner
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 10, h: 0.4,
    fill: { color: theme.accent }, line: { type: 'none' }
  });

  // Title
  slide.addText('Next Steps', {
    x: 0.5, y: 0.8, w: 9, h: 0.9,
    fontSize: 48, bold: true, color: 'FFFFFF', fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  // 3 call-to-action cards
  const ctas = [
    { num: '01', title: 'Browse the docs', desc: 'Read all 13 workflow documents in docs/workflows/ - bilingual, detailed, ready for your team' },
    { num: '02', title: 'Schedule a 30-min walkthrough', desc: 'We go through the demo together, answer your questions, focus on what matters to your business' },
    { num: '03', title: 'Pilot program (30 days)', desc: 'We deploy with your real data, train your team, gather feedback - then decide on a 12-month plan' }
  ];

  ctas.forEach((c, i) => {
    const x = 0.5 + i * 3.1;
    const y = 2.1;
    // Card background
    slide.addShape(pres.shapes.RECTANGLE, {
      x: x, y: y, w: 2.9, h: 2.6,
      fill: { color: 'FFFFFF' }, line: { type: 'none' }
    });
    // Number
    slide.addText(c.num, {
      x: x, y: y + 0.2, w: 2.9, h: 0.6,
      fontSize: 36, bold: true, color: theme.accent, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Title
    slide.addText(c.title, {
      x: x + 0.15, y: y + 0.85, w: 2.6, h: 0.6,
      fontSize: 16, bold: true, color: theme.primary, fontFace: 'Arial',
      align: 'center', valign: 'middle', margin: 0
    });
    // Description
    slide.addText(c.desc, {
      x: x + 0.2, y: y + 1.5, w: 2.5, h: 1.0,
      fontSize: 11, color: theme.secondary, fontFace: 'Arial',
      align: 'center', valign: 'top', margin: 0
    });
  });

  // Contact line
  slide.addText('Ready when you are. |  admin@erp.local  |  Sprint 20 - Demo 2', {
    x: 0.5, y: 5.0, w: 9, h: 0.4,
    fontSize: 14, color: theme.light, fontFace: 'Arial',
    align: 'center', valign: 'middle', margin: 0
  });

  addPageNumber(slide, 8);
}

// ============== WRITE FILE ==============

pres.writeFile({ fileName: './erp-demo-slides.pptx' })
  .then((fn) => console.log('OK:', fn))
  .catch((e) => { console.error('FAIL:', e); process.exit(1); });
