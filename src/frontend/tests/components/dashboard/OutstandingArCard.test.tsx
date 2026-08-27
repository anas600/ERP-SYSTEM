// Sprint 65 / Wave 2A (DEC-236) — OutstandingArCard component tests.
//
// Covers:
//   1. Renders the formatted AR value with the "requires collection" hint when value > 0
//   2. Renders a clean (no-attention) message when value = 0
//   3. Passes the currency prop through to the formatter (defaults to LYD)

import { render, screen } from '@testing-library/react';
import { OutstandingArCard } from '@/components/dashboard/OutstandingArCard';

describe('OutstandingArCard', () => {
  it('shows the "requires collection" hint when value > 0', () => {
    render(<OutstandingArCard value={12500} />);
    // The label is part of the StatCard — assert it shows the AR label
    expect(screen.getByText(/الذمم المدينة المستحقة/i)).toBeInTheDocument();
    // The footer hint should be the "attention" copy
    expect(screen.getByText(/يتطلب متابعة التحصيل/i)).toBeInTheDocument();
  });

  it('renders a clean message when value = 0', () => {
    render(<OutstandingArCard value={0} />);
    // When clean, the footer hint is hidden and the subtitle is "nothing due"
    expect(screen.getByText(/لا يوجد ذمم مدينة مستحقة/i)).toBeInTheDocument();
    expect(screen.queryByText(/يتطلب متابعة التحصيل/i)).not.toBeInTheDocument();
  });

  it('uses the LYD currency by default and respects a custom currency', () => {
    const { rerender } = render(<OutstandingArCard value={1000} />);
    // formatMoney uses LYD by default
    expect(screen.getByText(/1,000 LYD/)).toBeInTheDocument();

    rerender(<OutstandingArCard value={2000} currency="USD" />);
    expect(screen.getByText(/2,000 USD/)).toBeInTheDocument();
  });
});
