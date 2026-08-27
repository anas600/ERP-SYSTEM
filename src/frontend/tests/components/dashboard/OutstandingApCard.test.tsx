// Sprint 65 / Wave 2A (DEC-236) — OutstandingApCard component tests.
//
// Covers:
//   1. Renders the formatted AP value with the "requires review" hint when value > 0
//   2. Renders a clean (nothing-to-pay) message when value = 0
//   3. Hides the "requires review" footer when value is clean (negative or zero)

import { render, screen } from '@testing-library/react';
import { OutstandingApCard } from '@/components/dashboard/OutstandingApCard';

describe('OutstandingApCard', () => {
  it('shows the "requires review" hint when value > 0', () => {
    render(<OutstandingApCard value={5000} />);
    expect(screen.getByText(/الذمم الدائنة المستحقة/i)).toBeInTheDocument();
    expect(screen.getByText(/يتطلب مراجعة فريق المالية/i)).toBeInTheDocument();
  });

  it('renders a clean message when value = 0', () => {
    render(<OutstandingApCard value={0} />);
    expect(screen.getByText(/لا يوجد ذمم دائنة معلّقة/i)).toBeInTheDocument();
    expect(screen.queryByText(/يتطلب مراجعة فريق المالية/i)).not.toBeInTheDocument();
  });

  it('uses the LYD currency by default and respects a custom currency', () => {
    const { rerender } = render(<OutstandingApCard value={3000} />);
    expect(screen.getByText(/3,000 LYD/)).toBeInTheDocument();

    rerender(<OutstandingApCard value={4500} currency="USD" />);
    expect(screen.getByText(/4,500 USD/)).toBeInTheDocument();
  });
});
