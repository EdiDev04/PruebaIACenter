import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuoteStateQuery } from '@/entities/quote-state';
import { FinancialSummary } from '@/widgets/financial-summary';
import { LocationBreakdown } from '@/widgets/location-breakdown';
import { IncompleteAlerts } from '@/widgets/incomplete-alerts';
import { CalculateButton } from '@/features/calculate-quote';
import styles from './ResultsPage.module.css';

const STRINGS = {
  title: 'Resultados de la Cotización',
  recalculate: 'Recalcular',
  emptyStateReady: 'Ejecute el cálculo para ver los resultados de su cotización',
  emptyStateReadyDesc: 'ubicaciones listas para calcular.',
  emptyStateNotReady: 'No hay ubicaciones calculables',
  emptyStateNotReadyDesc: 'Complete al menos una ubicación con todos los datos requeridos para poder calcular.',
  goToLocations: 'Ir a ubicaciones →',
  sectionBreakdown: 'Desglose por ubicación',
};

export function ResultsPage() {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const folio = folioNumber ?? '';

  const { data: quoteState, isLoading, error } = useQuoteStateQuery(folio);
  const [calcError, setCalcError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className={styles.page}>
        <div className={styles.skeleton} aria-busy="true" aria-label="Cargando resultados..." />
      </div>
    );
  }

  if (error || !quoteState) {
    return (
      <div className={styles.page}>
        <div className={styles.errorBanner} role="alert">
          Error al cargar los resultados. Intente recargar la página.
        </div>
      </div>
    );
  }

  const { calculationResult, readyForCalculation, version, locations } = quoteState;

  const handleCalcSuccess = () => {
    setCalcError(null);
  };

  const handleCalcError = (message: string) => {
    setCalcError(message);
  };

  if (!calculationResult) {
    return (
      <div className={styles.page}>
        <div className={styles.emptyState}>
          {readyForCalculation ? (
            <>
              <span className={styles.emptyIcon} aria-hidden="true">🧮</span>
              <h2 className={styles.emptyTitle}>{STRINGS.emptyStateReady}</h2>
              <p className={styles.emptyDesc}>
                Tiene {locations.calculable} {STRINGS.emptyStateReadyDesc}
              </p>
              <CalculateButton
                folio={folio}
                version={version}
                onSuccess={handleCalcSuccess}
                onError={handleCalcError}
              />
            </>
          ) : (
            <>
              <span className={styles.emptyIcon} aria-hidden="true">⚠️</span>
              <h2 className={styles.emptyTitle}>{STRINGS.emptyStateNotReady}</h2>
              <p className={styles.emptyDesc}>{STRINGS.emptyStateNotReadyDesc}</p>
              <button
                type="button"
                className={styles.linkButton}
                onClick={() => navigate(`/quotes/${folio}/locations`)}
              >
                {STRINGS.goToLocations}
              </button>
            </>
          )}
          {calcError && (
            <div className={styles.errorBanner} role="alert">{calcError}</div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div className={styles.intro}>
          <h1 className={styles.title}>{STRINGS.title}</h1>
        </div>
        <CalculateButton
          folio={folio}
          version={version}
          onSuccess={handleCalcSuccess}
          onError={handleCalcError}
          buttonLabel={STRINGS.recalculate}
          variant="secondary"
        />
      </div>

      {calcError && (
        <div className={styles.errorBanner} role="alert">{calcError}</div>
      )}

      <FinancialSummary
        netPremium={calculationResult.netPremium}
        commercialPremiumBeforeTax={calculationResult.commercialPremiumBeforeTax}
        commercialPremium={calculationResult.commercialPremium}
      />

      <section>
        <h2 className={styles.sectionTitle}>{STRINGS.sectionBreakdown}</h2>
        <LocationBreakdown premiumsByLocation={calculationResult.premiumsByLocation} />
      </section>

      {locations.alerts.length > 0 && (
        <IncompleteAlerts
          alerts={locations.alerts}
          onEditLocation={() => navigate(`/quotes/${folio}/locations`)}
        />
      )}
    </div>
  );
}
