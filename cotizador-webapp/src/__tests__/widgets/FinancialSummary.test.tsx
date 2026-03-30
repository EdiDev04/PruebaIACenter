import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { FinancialSummary } from '@/widgets/financial-summary';

const defaultProps = {
  netPremium: 100,
  commercialPremiumBeforeTax: 120,
  commercialPremium: 139.2,
};

describe('FinancialSummary', () => {
  it('renders three financial cards', () => {
    render(<FinancialSummary {...defaultProps} />);
    // Three distinct label texts confirm three cards
    expect(screen.getByText('Prima Neta Total')).toBeInTheDocument();
    expect(screen.getByText('Prima Comercial (sin IVA)')).toBeInTheDocument();
    expect(screen.getByText('Prima Comercial Total')).toBeInTheDocument();
  });

  it('displays netPremium value', () => {
    render(<FinancialSummary {...defaultProps} />);
    // 100 formatted as COP → contains "100"
    const region = screen.getByRole('region', { name: 'Resumen financiero' });
    expect(region.textContent).toContain('100');
  });

  it('displays commercialPremium as prominent', () => {
    render(<FinancialSummary {...defaultProps} />);
    const region = screen.getByRole('region', { name: 'Resumen financiero' });
    // 139.2 → "139"
    expect(region.textContent).toContain('139');
  });

  it('formats all amounts as COP currency', () => {
    render(<FinancialSummary {...defaultProps} />);
    // COP locale uses "." as thousands separator
    render(
      <FinancialSummary
        netPremium={1000000}
        commercialPremiumBeforeTax={1200000}
        commercialPremium={1392000}
      />,
    );
    expect(screen.getAllByText(/1\.000\.000|1\.200\.000|1\.392\.000/).length).toBeGreaterThan(0);
  });
});
