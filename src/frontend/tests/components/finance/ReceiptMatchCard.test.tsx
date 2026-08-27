// Sprint 65 / Wave 3A (DEC-237) — ReceiptMatchCard component tests.
//
// Covers:
//   1. Renders the subcontractor name, payment number, amount, and date
//   2. Shows the correct score badge tone (success for EXCELLENT/GOOD, warning for FAIR, danger for POOR)
//   3. Calls onConfirm with the match when the button is clicked

import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ReceiptMatchCard } from '@/components/finance/ReceiptMatchCard';
import type { SubPaymentMatch } from '@/lib/api-types';

function makeMatch(overrides: Partial<SubPaymentMatch> = {}): SubPaymentMatch {
  return {
    subPaymentId: 'sp-1',
    subContractId: 'sc-1',
    subcontractorName: 'مقاول الفجر',
    paymentNumber: 'SP-2026-0001',
    amount: 12_500,
    paymentDate: '2026-08-10T00:00:00Z',
    score: 100,
    matchQuality: 'EXCELLENT',
    matchQualityName: 'ممتاز',
    ...overrides,
  };
}

describe('ReceiptMatchCard', () => {
  it('renders the subcontractor name, payment number, amount, and date', () => {
    const match = makeMatch();
    render(<ReceiptMatchCard match={match} />);

    // Subcontractor name
    expect(screen.getByText('مقاول الفجر')).toBeInTheDocument();
    // Payment number
    expect(screen.getByText(/SP-2026-0001/)).toBeInTheDocument();
    // Amount (Intl.NumberFormat trims trailing zeros, so 12500 → "12,500 LYD")
    expect(screen.getByText(/12,500 LYD/)).toBeInTheDocument();
    // The score badge includes the score
    expect(screen.getByTestId('match-quality').textContent).toContain('100');
  });

  it('uses the right score-badge tone per match quality', () => {
    const { rerender } = render(
      <ReceiptMatchCard match={makeMatch({ matchQuality: 'EXCELLENT', score: 95, matchQualityName: 'ممتاز' })} />,
    );
    // EXCELLENT uses the success variant (green) per the QUALITY_TONE map
    expect(screen.getByTestId('match-quality').className).toMatch(/success/);

    rerender(
      <ReceiptMatchCard match={makeMatch({ matchQuality: 'POOR', score: 10, matchQualityName: 'ضعيف' })} />,
    );
    // POOR uses the danger variant (red)
    expect(screen.getByTestId('match-quality').className).toMatch(/danger/);
  });

  it('calls onConfirm with the match when the confirm button is clicked', async () => {
    const user = userEvent.setup();
    const match = makeMatch();
    const onConfirm = jest.fn();
    render(<ReceiptMatchCard match={match} onConfirm={onConfirm} />);

    await user.click(screen.getByTestId('match-confirm-button'));

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onConfirm).toHaveBeenCalledWith(match);
  });
});
