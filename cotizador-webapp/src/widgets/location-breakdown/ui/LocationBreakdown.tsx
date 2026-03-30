import { useState } from 'react';
import type { LocationPremiumDto } from '@/entities/quote-state';
import { formatCurrency } from '@/shared/lib/formatCurrency';
import { CoverageAccordion } from './CoverageAccordion';
import styles from './LocationBreakdown.module.css';

interface LocationBreakdownProps {
  premiumsByLocation: LocationPremiumDto[];
}

export function LocationBreakdown({ premiumsByLocation }: LocationBreakdownProps) {
  const calculable = premiumsByLocation.filter(p => p.validationStatus === 'calculable');

  const [expandedIndexes, setExpandedIndexes] = useState<Set<number>>(
    () => calculable.length === 1 ? new Set([calculable[0].locationIndex]) : new Set()
  );

  const toggleExpanded = (index: number) => {
    setExpandedIndexes(prev => {
      const next = new Set(prev);
      if (next.has(index)) {
        next.delete(index);
      } else {
        next.add(index);
      }
      return next;
    });
  };

  const totalNetPremium = calculable.reduce((sum, p) => sum + p.netPremium, 0);

  if (calculable.length === 0) {
    return (
      <div className={styles.empty}>
        No hay ubicaciones calculables para mostrar.
      </div>
    );
  }

  return (
    <section className={styles.container} aria-label="Desglose por ubicación" role="table">
      <div className={styles.tableHeader} role="row">
        <span role="columnheader">Ubicación</span>
        <span role="columnheader" className={styles.right}>Prima Neta</span>
        <span role="columnheader" className={styles.center}>Estado</span>
        <span role="columnheader" />
      </div>

      {calculable.map((location) => {
        const isExpanded = expandedIndexes.has(location.locationIndex);
        return (
          <div key={location.locationIndex} className={styles.rowWrapper}>
            <button
              type="button"
              className={styles.row}
              onClick={() => toggleExpanded(location.locationIndex)}
              aria-expanded={isExpanded}
              aria-controls={`coverage-${location.locationIndex}`}
            >
              <span className={styles.locationName}>
                <span className={styles.chevron} aria-hidden="true">
                  {isExpanded ? '▼' : '▶'}
                </span>
                {location.locationName}
              </span>
              <span className={styles.right}>{formatCurrency(location.netPremium)}</span>
              <span className={styles.center}>
                <span className={styles.badge}>Calculable</span>
              </span>
              <span />
            </button>

            {isExpanded && (
              <div id={`coverage-${location.locationIndex}`} role="region">
                <CoverageAccordion coveragePremiums={location.coveragePremiums} />
              </div>
            )}
          </div>
        );
      })}

      <div className={styles.totalRow} role="row">
        <span className={styles.totalLabel}>Total</span>
        <span className={`${styles.right} ${styles.totalAmount}`}>
          {formatCurrency(totalNetPremium)}
        </span>
        <span />
        <span />
      </div>
    </section>
  );
}
