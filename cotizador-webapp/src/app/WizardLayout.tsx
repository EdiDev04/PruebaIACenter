import { Outlet, useParams } from 'react-router-dom';
import { useQuoteStateQuery } from '@/entities/quote-state';
import { ProgressBar } from '@/widgets/progress-bar';
import { LocationAlerts } from '@/widgets/location-alerts';
import { WizardHeader } from '@/widgets/WizardHeader';
import styles from './WizardLayout.module.css';

export function WizardLayout() {
  const { folioNumber = '' } = useParams<{ folioNumber: string }>();
  const { data: quoteState } = useQuoteStateQuery(folioNumber);

  const showAlerts =
    !!quoteState && quoteState.locations.alerts.length > 0;

  return (
    <div className={styles.layout}>
      <WizardHeader />
      {quoteState && (
        <ProgressBar progress={quoteState.progress} />
      )}
      {quoteState && (
        <div className={quoteState.readyForCalculation ? styles.bannerReady : styles.bannerPending}>
          {quoteState.readyForCalculation
            ? `${quoteState.locations.calculable} ubicación(es) lista(s) para calcular`
            : quoteState.locations.total === 0
              ? 'Agregue al menos una ubicación completa para poder calcular'
              : 'Complete los datos de al menos una ubicación para poder calcular'}
          {quoteState.locations.incomplete > 0 && quoteState.readyForCalculation && (
            <span className={styles.bannerIncomplete}>
              {' · '}{quoteState.locations.incomplete} con datos pendientes
            </span>
          )}
        </div>
      )}
      {showAlerts && quoteState && (
        <div className={styles.alertsContainer}>
          <LocationAlerts
            alerts={quoteState.locations.alerts}
            folio={folioNumber}
            calculable={quoteState.locations.calculable}
            total={quoteState.locations.total}
          />
        </div>
      )}
      <main className={styles.content}>
        <Outlet />
      </main>
    </div>
  );
}
