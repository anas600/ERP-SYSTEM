// Sprint 64 Wave 3A (DEC-226) — Component tests for SubStatement.
//
// **Test infrastructure status (2026-08-27):** The FE test stack (Jest + RTL)
// is **not yet installed** in this repository — `package.json` has no
// `test` script and `node_modules` has neither `jest` nor
// `@testing-library/react`. The Worker contract claimed Sprint 63 Wave 3A
// set this up; that setup was not committed.
//
// These tests are written against the standard RTL API so they will pass
// once the orchestrator installs the missing pieces. The dependency blocks
// are documented in the Wave 3A report.
//
// Required to run (per the Sprint 63 Wave 3A plan that never landed):
//   npm install --save-dev jest @testing-library/react \
//     @testing-library/jest-dom jest-environment-jsdom ts-jest
//   + add a `test` script + jest.config.js + jest.setup.js + tsconfig test types
//
// Once installed: `npm test -- SubStatement.test`

import { render, screen } from '@testing-library/react';
import { SubStatement } from '@/components/subcontractor/SubStatement';
import type { SubStatement as SubStatementModel } from '@/lib/api-types';

const baseStatement: SubStatementModel = {
  subContractId: '00000000-0000-0000-0000-000000000001',
  subcontractorName: 'مقاول الكهرباء',
  subcontractorCode: 'ELEC-001',
  contractNumber: 'SC-001',
  scopeOfWork: 'أعمال الكهرباء',
  contractValue: 50_000,
  totalBilledGross: 30_000,
  totalRetentionWithheld: 3_000,
  totalRetentionReleased: 0,
  totalPaid: 13_500,
  outstandingBalance: 16_500,
  workCompletedToDate: 60,
  billingCount: 2,
  firstBillingDate: '2026-08-01T00:00:00Z',
  lastBillingDate: '2026-08-20T00:00:00Z',
  lastPaymentDate: '2026-08-22T00:00:00Z',
  status: 1,
  statusName: 'نشط',
  healthStatus: 'OK',
  healthStatusName: 'حالة جيدة',
};

describe('SubStatement', () => {
  // === 1. Health = OK ===

  it('SubStatement_ShowsHealthStatusBadge_ForOK', () => {
    render(<SubStatement statement={{ ...baseStatement, healthStatus: 'OK', healthStatusName: 'حالة جيدة' }} />);
    const badge = screen.getByTestId('health-badge');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveTextContent('حالة جيدة');
    expect(badge).toHaveAttribute('data-testid', 'health-badge');
  });

  // === 2. Health = OVERDUE ===

  it('SubStatement_ShowsHealthStatusBadge_ForOverdue', () => {
    render(
      <SubStatement
        statement={{
          ...baseStatement,
          healthStatus: 'OVERDUE',
          healthStatusName: 'متأخر السداد',
          outstandingBalance: 25_000,
        }}
      />
    );
    const badge = screen.getByTestId('health-badge');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveTextContent('متأخر السداد');
  });

  // === 3. Health = SETTLED ===

  it('SubStatement_ShowsHealthStatusBadge_ForSettled', () => {
    render(
      <SubStatement
        statement={{
          ...baseStatement,
          healthStatus: 'SETTLED',
          healthStatusName: 'مسوّى',
          outstandingBalance: 0,
          totalPaid: 30_000,
        }}
      />
    );
    const badge = screen.getByTestId('health-badge');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveTextContent('مسوّى');
  });

  // === 4. All financial fields are rendered ===

  it('SubStatement_DisplaysAllFinancialFields', () => {
    render(<SubStatement statement={baseStatement} />);

    // Header — contract number + subcontractor name
    expect(screen.getByText(/SC-001/)).toBeInTheDocument();
    expect(screen.getByText(/مقاول الكهرباء/)).toBeInTheDocument();

    // Stat cell labels
    expect(screen.getByText('قيمة العقد')).toBeInTheDocument();
    expect(screen.getByText('إجمالي المستخلصات')).toBeInTheDocument();
    expect(screen.getByText('إجمالي المدفوع')).toBeInTheDocument();
    expect(screen.getByText('الرصيد المستحق')).toBeInTheDocument();

    // Retention summary
    expect(screen.getByText('الاحتجاز المحتجز')).toBeInTheDocument();
    expect(screen.getByText('الاحتجاز المُحرّر')).toBeInTheDocument();
    expect(screen.getByText('نسبة الإنجاز')).toBeInTheDocument();

    // Numeric values (formatted as currency "30,000.00 LYD")
    // 30,000 → "30,000.00 LYD" (contract value + total billed)
    expect(screen.getAllByText(/30,000\.00 LYD/).length).toBeGreaterThanOrEqual(1);
    // 13,500 → "13,500.00 LYD" (total paid)
    expect(screen.getAllByText(/13,500\.00 LYD/).length).toBeGreaterThanOrEqual(1);
  });
});
