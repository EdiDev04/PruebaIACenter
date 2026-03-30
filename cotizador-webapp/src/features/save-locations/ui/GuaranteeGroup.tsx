import { useState, useId } from 'react';
import type { Control, UseFormSetValue } from 'react-hook-form';
import { useWatch } from 'react-hook-form';
import type { LocationFormValues } from '@/entities/location';
import type { GuaranteeGroup as GuaranteeGroupType } from '@/entities/location/model/guaranteeCatalog';
import styles from './GuaranteeGroup.module.css';

interface Props {
  readonly group: GuaranteeGroupType;
  readonly control: Control<LocationFormValues>;
  readonly setValue: UseFormSetValue<LocationFormValues>;
  readonly defaultOpen?: boolean;
}

export function GuaranteeGroup({ group, control, setValue, defaultOpen = false }: Props) {
  const [open, setOpen] = useState(defaultOpen);
  const baseId = useId();

  const guarantees = useWatch({ control, name: 'guarantees' }) ?? [];

  function getItem(key: string) {
    return guarantees.find((g) => g.guaranteeKey === key);
  }

  function handleCheck(key: string, checked: boolean) {
    const current = guarantees.filter((g) => g.guaranteeKey !== key);
    if (checked) {
      setValue('guarantees', [...current, { guaranteeKey: key, insuredAmount: 0 }], {
        shouldValidate: true,
      });
    } else {
      setValue('guarantees', current, { shouldValidate: true });
    }
  }

  function handleAmount(key: string, raw: string) {
    const amount = Number.parseFloat(raw.replaceAll(',', '')) || 0;
    const current = guarantees.map((g) =>
      g.guaranteeKey === key ? { ...g, insuredAmount: amount } : g,
    );
    setValue('guarantees', current, { shouldValidate: true });
  }

  const selectedCount = group.items.filter((item) => getItem(item.guaranteeKey)).length;

  return (
    <div className={styles.group}>
      <button
        type="button"
        className={`${styles.header} ${open ? styles.headerOpen : ''}`}
        aria-expanded={open}
        aria-controls={`${baseId}-items`}
        onClick={() => setOpen((prev) => !prev)}
      >
        <span>{group.label}</span>
        <span className={styles.headerRight}>
          {!open && (
            <span className={styles.counter}>
              {selectedCount} de {group.items.length} seleccionadas
            </span>
          )}
          <span
            className={`material-symbols-outlined ${styles.chevron} ${open ? styles.chevronOpen : ''}`}
            aria-hidden="true"
          >
            expand_more
          </span>
        </span>
      </button>

      {open && (
        <div id={`${baseId}-items`} className={styles.items}>
          {group.items.map((item) => {
            const checked = !!getItem(item.guaranteeKey);
            const amount = getItem(item.guaranteeKey)?.insuredAmount ?? 0;
            const needsAmount = item.requiresInsuredAmount && checked;
            const amountMissing = needsAmount && amount === 0;
            const checkId = `${baseId}-${item.guaranteeKey}`;
            const amountId = `${baseId}-${item.guaranteeKey}-amount`;

            return (
              <div
                key={item.guaranteeKey}
                className={`${styles.item} ${checked ? styles.itemSelected : ''}`}
              >
                <input
                  type="checkbox"
                  id={checkId}
                  className={styles.checkbox}
                  checked={checked}
                  onChange={(e) => handleCheck(item.guaranteeKey, e.target.checked)}
                  aria-label={item.label}
                />
                <label htmlFor={checkId} className={styles.itemLabel}>
                  {item.label}
                  {item.recommended && (
                    <span className={styles.recommendedBadge}>Recomendado</span>
                  )}
                </label>
                {item.requiresInsuredAmount && checked && (
                  <div>
                    <div className={styles.amountWrapper}>
                      <span className={styles.currencyPrefix}>$</span>
                      <input
                        id={amountId}
                        type="number"
                        min="0"
                        step="1000"
                        value={amount === 0 ? '' : amount}
                        onChange={(e) => handleAmount(item.guaranteeKey, e.target.value)}
                        className={`${styles.amountInput} ${amountMissing ? styles.amountError : ''}`}
                        aria-label={`Suma asegurada para ${item.label}`}
                        aria-describedby={amountMissing ? `${amountId}-msg` : undefined}
                        placeholder="0"
                      />
                    </div>
                    {amountMissing && (
                      <p id={`${amountId}-msg`} className={styles.amountMsg}>
                        Suma asegurada requerida
                      </p>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
