# 🏢 ERP-SYSTEM — Elevator Pitch (Libyan SME Edition)

> **One-page introduction for the client meeting.** Designed to be read in 60 seconds.
> **Print:** A4 landscape or portrait. **Audience:** Business owner / CEO / CFO.

---

## 🇱🇾 بالعربية

### ERP-SYSTEM — نظام إدارة موحّد للشركات الليبية

**ما هو؟** نظام ERP محلي مبني للشركات الليبية. يدير الفواتير، المخزون، الموظفين، الرواتب، والتقارير المالية في مكان واحد.

**لماذا يهمّك؟**

| المشكلة عند الشركات الليبية | الحل في ERP-SYSTEM |
|---|---|
| إكسل + ورق + برامج متفرقة | نظام واحد متكامل |
| فواتير ضريبية بدون ربط بالقيود | كل فاتورة = قيد محاسبي تلقائي |
| جرد المخزون يدوي | تنبيهات تلقائية عند الحد الأدنى |
| رواتب محسوبة بالآلة الحاسبة | دورة رواتب بضغطة واحدة + EOS |
| ضريبة القيمة المضافة تُحسب يدوياً | تقرير VAT جاهز لكل ربع سنة |
| حسابات على ورق | دليل حسابات منظّم بـ 5 أنواع |

**الأرقام المهمة:**

- 💰 **تقليل وقت إعداد الفواتير** من 30 دقيقة → 2 دقيقة (البيانات تتدفق تلقائياً)
- 📊 **13 وظيفة موثّقة** جاهزة الآن: عملاء، موردين، أصناف، فواتير، أوامر شراء، استلامات، فواتير موردين، سندات قبض، دليل حسابات، قيود يومية، موظفين، رواتب، مشاريع
- 🌍 **ليبي 100%** — العملة دينار ليبي (LYD)، المصطلحات بالعربي، خوادم محلية
- 🔐 **آمن** — JWT + BCrypt + صلاحيات RBAC (Admin / Accountant / Sales / Viewer)
- 📱 **متصفح** — يفتح من أي جهاز، لا يحتاج تثبيت

**متى تبدأ؟** اليوم. الـ demo يعمل على `http://localhost:3000` بحساب: `admin@erp.local` / `ChangeMe1234!`.

---

## 🇬🇧 In English

### ERP-SYSTEM — A Unified ERP for Libyan SMEs

**What is it?** A local-first ERP built for Libyan small-to-medium businesses. It manages invoices, inventory, employees, payroll, and financial reports in one place.

**Why does it matter to you?**

| The problem at Libyan SMEs | The solution in ERP-SYSTEM |
|---|---|
| Excel + paper + disconnected tools | One unified system |
| Tax invoices with no journal link | Every invoice = automatic journal entry |
| Manual inventory counts | Auto low-stock notifications |
| Payroll calculated on a calculator | One-click payroll run + EOS |
| VAT calculated by hand | Quarterly VAT report, ready to file |
| Accounts on paper | Structured 5-type chart of accounts |

**The numbers that matter:**

- 💰 **Reduce invoice prep time** from 30 min → 2 min (data flows automatically)
- 📊 **13 documented functions** live today: customers, vendors, items, sales invoices, purchase orders, goods receipts, vendor bills, AR receipts, chart of accounts, journal entries, employees, payroll, projects
- 🌍 **100% Libyan** — Dinar (LYD) currency, Arabic terms, local hosting
- 🔐 **Secure** — JWT + BCrypt + RBAC roles (Admin / Accountant / Sales / Viewer)
- 📱 **Browser-based** — opens on any device, no install needed

**When can you start?** Today. The demo is live at `http://localhost:3000` with login: `admin@erp.local` / `ChangeMe1234!`.

---

## 🎯 Three questions the client will ask (and the answers)

1. **"Does it run in Libya?"** — Yes. Hosted locally, no external dependencies, LYD currency, Libyan tax logic.
2. **"How long to deploy?"** — A clean install runs in 15-20 minutes. Production setup is 1-2 days including data migration.
3. **"What if I need a feature you don't have?"** — The codebase is documented (13 workflow docs) and the architecture is modular (`Modules/AccountsReceivable`, `Modules/Procurement`, etc.) — new features slot in cleanly.

---

## 📞 Next step

- **Read the workflow docs** in `docs/workflows/` for any function in detail
- **Schedule a 30-min walkthrough** to see the demo live
- **Pilot program:** 30-day trial with your real data, then decide

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2)._
_Prepared by: Mavis (Muhammad mode) — Strategic Advisor, ERP-SYSTEM project._
