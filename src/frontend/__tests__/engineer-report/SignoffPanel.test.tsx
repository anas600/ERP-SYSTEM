/**
 * Sprint 61 (DEC-194): SignoffPanel component unit test.
 *
 * One test:
 *  1. Disabled state when canSign=false (renders the disabled reason).
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { SignoffPanel } from '@/components/engineer-report/SignoffPanel';

describe('SignoffPanel (Sprint 61 DEC-194)', () => {
  it('renders the disabled state when canSign=false with a custom reason', () => {
    const onSign = jest.fn();
    render(
      <SignoffPanel
        canSign={false}
        disabledReason="التقرير في حالة مسودة — يجب إرساله أولاً."
        onSign={onSign}
      />
    );
    const block = screen.getByTestId('signoff-disabled');
    expect(block).toBeInTheDocument();
    expect(block).toHaveTextContent(/الاعتماد الإلكتروني/);
    expect(block).toHaveTextContent(/مسودة/);

    // The interactive panel should NOT be rendered
    expect(screen.queryByTestId('signoff-panel')).not.toBeInTheDocument();
    expect(screen.queryByTestId('signoff-decision-approve')).not.toBeInTheDocument();
  });
});
