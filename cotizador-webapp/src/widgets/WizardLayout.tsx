import { Outlet } from 'react-router-dom';
import { WizardHeader } from './WizardHeader';
import styles from './WizardLayout.module.css';

export function WizardLayout() {
  return (
    <div className={styles.layout}>
      <WizardHeader />
      <main className={styles.content}>
        <Outlet />
      </main>
    </div>
  );
}
