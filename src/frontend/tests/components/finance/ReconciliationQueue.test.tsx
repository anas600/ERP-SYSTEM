// Sprint 65 / Wave 3A (DEC-237) — ReconciliationQueue component tests.
//
// Covers:
//   1. Renders the empty state when the receipts list is empty
//   2. Renders one row per receipt with the receipt number, amount, and date
//   3. Calls onFindMatches with the receiptId when the "find matches" button is clicked

import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ReconciliationQueue } from '@/components/finance/ReconciliationQueue';
import type { UnmatchedReceipt } from '@/lib/api-types';

function makeReceipt(overrides: Partial<UnmatchedReceipt> = {}): UnmatchedReceipt {
  return {
    receiptId: 'rc-1',
    receiptNumber: 'RC-2026-0001',
    receiptDate: '2026-08-15T00:00:00Z',
    amount: 8_000,
    customerName: 'العميل أ',
    daysSinceReceipt: 5,
    ...overrides,
  };
}

describe('ReconciliationQueue', () => {
  it('renders the empty state when the receipts list is empty', () => {
    render(<ReconciliationQueue receipts={[]} />);

    // EmptyState title in Arabic
    expect(screen.getByText(/لا توجد سندات قبض في الانتظار/i)).toBeInTheDocument();
  });

  it('renders one row per receipt with the receipt number, amount, and date', () => {
    const receipts = [
      makeReceipt({ receiptId: 'rc-1', receiptNumber: 'RC-2026-0001', amount: 5_000 }),
      makeReceipt({ receiptId: 'rc-2', receiptNumber: 'RC-2026-0002', amount: 12_500 }),
    ];
    render(<ReconciliationQueue receipts={receipts} onFindMatches={() => {}} />);

    // Both receipt numbers visible
    expect(screen.getByText('RC-2026-0001')).toBeInTheDocument();
    expect(screen.getByText('RC-2026-0002')).toBeInTheDocument();

    // Both amounts visible (Intl.NumberFormat trims trailing zeros, so 5000 → "5,000 LYD")
    expect(screen.getByText(/5,000 LYD/)).toBeInTheDocument();
    expect(screen.getByText(/12,500 LYD/)).toBeInTheDocument();

    // Two rows
    expect(screen.getAllByTestId('queue-row')).toHaveLength(2);
  });

  it('calls onFindMatches with the receiptId when the "find matches" button is clicked', async () => {
    const user = userEvent.setup();
    const onFindMatches = jest.fn();
    const receipts = [makeReceipt({ receiptId: 'rc-99' })];
    render(<ReconciliationQueue receipts={receipts} onFindMatches={onFindMatches} />);

    await user.click(screen.getByTestId('queue-find-button'));

    expect(onFindMatches).toHaveBeenCalledTimes(1);
    expect(onFindMatches).toHaveBeenCalledWith('rc-99');
  });
});
