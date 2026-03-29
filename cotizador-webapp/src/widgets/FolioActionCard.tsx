import { ReactNode } from 'react';
import styles from './FolioActionCard.module.css';

interface Props {
  title: string;
  description: string;
  icon: string;
  children: ReactNode;
}

export function FolioActionCard({ title, description, icon, children }: Props) {
  return (
    <div className={styles.card}>
      <div className={styles.top}>
        <div className={styles.iconWrapper}>
          <span className="material-symbols-outlined" aria-hidden="true">{icon}</span>
        </div>
        <h2 className={styles.title}>{title}</h2>
        <p className={styles.description}>{description}</p>
      </div>
      <div className={styles.actions}>{children}</div>
    </div>
  );
}
