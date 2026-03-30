import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useDispatch } from 'react-redux';
import { useCoverageOptionsQuery, coverageOptionsFormSchema } from '@/entities/coverage-options';
import type { CoverageOptionsFormValues, UpdateCoverageOptionsRequest } from '@/entities/coverage-options';
import { useGuaranteesQuery, GuaranteeCheckboxGroup } from '@/entities/guarantee';
import type { GuaranteeDto } from '@/entities/guarantee';
import { useLocationsQuery } from '@/entities/location';
import { setCurrentStep } from '@/entities/folio';
import { useSaveCoverageOptions, SAVE_COVERAGE_OPTIONS_STRINGS } from '@/features/save-coverage-options';
import { Modal, Button } from '@/shared/ui';
import type { ToastMessage } from '@/shared/ui';
import styles from './CoverageOptionsForm.module.css';

const CATEGORY_ORDER: GuaranteeDto['category'][] = ['fire', 'cat', 'additional', 'special'];

interface Props {
  readonly folio: string;
  readonly formId: string;
  readonly onSaveStart?: () => void;
  readonly onSaveEnd?: () => void;
  readonly onToastMessage: (message: string, type: ToastMessage['type']) => void;
  readonly onNavigateNext: () => void;
}

interface PendingUncheck {
  key: string;
  name: string;
  count: number;
}

export function CoverageOptionsForm({
  folio,
  formId,
  onSaveStart,
  onSaveEnd,
  onToastMessage,
  onNavigateNext,
}: Props) {
  const dispatch = useDispatch();

  const {
    data: coverageOptionsData,
    isLoading: isLoadingCoverage,
  } = useCoverageOptionsQuery(folio);

  const { data: guaranteesData, isLoading: isLoadingGuarantees } = useGuaranteesQuery();
  const { data: locationsData } = useLocationsQuery(folio);

  const [pendingUncheck, setPendingUncheck] = useState<PendingUncheck | null>(null);

  const form = useForm<CoverageOptionsFormValues>({
    resolver: zodResolver(coverageOptionsFormSchema),
    defaultValues: {
      enabledGuarantees: [],
      deductiblePercentage: 0,
      coinsurancePercentage: 0,
    },
  });

  const { control, handleSubmit, register, reset, setValue, watch, formState: { errors } } = form;

  // Populate form when data arrives — convert decimals to percentages
  const [hasReset, setHasReset] = useState(false);
  if (coverageOptionsData && !hasReset) {
    reset({
      enabledGuarantees: coverageOptionsData.enabledGuarantees,
      deductiblePercentage: coverageOptionsData.deductiblePercentage * 100,
      coinsurancePercentage: coverageOptionsData.coinsurancePercentage * 100,
    });
    setHasReset(true);
  }

  const { mutate, isPending } = useSaveCoverageOptions({
    folio,
    onSuccess: () => {
      onSaveEnd?.();
      dispatch(setCurrentStep(4));
      onToastMessage(SAVE_COVERAGE_OPTIONS_STRINGS.toastSaved, 'success');
      onNavigateNext();
    },
    onError: (msg) => {
      onSaveEnd?.();
      onToastMessage(msg, 'error');
    },
  });

  const onSubmit = (values: CoverageOptionsFormValues) => {
    const request: UpdateCoverageOptionsRequest = {
      enabledGuarantees: values.enabledGuarantees,
      deductiblePercentage: values.deductiblePercentage / 100,
      coinsurancePercentage: values.coinsurancePercentage / 100,
      version: coverageOptionsData?.version ?? 1,
    };
    onSaveStart?.();
    mutate(request);
  };

  const enabledGuarantees = watch('enabledGuarantees');

  const handleToggle = (key: string, checked: boolean) => {
    if (checked) {
      setValue('enabledGuarantees', [...enabledGuarantees, key], { shouldValidate: true });
      return;
    }

    const guarantee = guaranteesData?.find((g) => g.key === key);
    const affectedCount =
      locationsData?.locations.filter((loc) =>
        loc.guarantees.some((g) => g.guaranteeKey === key),
      ).length ?? 0;

    if (affectedCount > 0) {
      setPendingUncheck({ key, name: guarantee?.name ?? key, count: affectedCount });
    } else {
      setValue(
        'enabledGuarantees',
        enabledGuarantees.filter((k) => k !== key),
        { shouldValidate: true },
      );
    }
  };

  const handleSelectAll = (category: string, keys: string[]) => {
    const merged = Array.from(new Set([...enabledGuarantees, ...keys]));
    setValue('enabledGuarantees', merged, { shouldValidate: true });
  };

  const confirmUncheck = () => {
    if (!pendingUncheck) return;
    setValue(
      'enabledGuarantees',
      enabledGuarantees.filter((k) => k !== pendingUncheck.key),
      { shouldValidate: true },
    );
    setPendingUncheck(null);
  };

  const isLoading = isLoadingCoverage || isLoadingGuarantees;

  if (isLoading) {
    return <div className={styles.loading} aria-busy="true">Cargando opciones de cobertura...</div>;
  }

  const guaranteesByCategory = CATEGORY_ORDER.reduce<Record<string, GuaranteeDto[]>>(
    (acc, cat) => {
      acc[cat] = (guaranteesData ?? []).filter((g) => g.category === cat);
      return acc;
    },
    {},
  );

  return (
    <>
      <form id={formId} onSubmit={handleSubmit(onSubmit)} className={styles.form} noValidate>
        {/* Parámetros Globales */}
        <section className={styles.section} aria-labelledby="global-params-title">
          <h2 id="global-params-title" className={styles.sectionTitle}>
            Parámetros Globales
          </h2>
          <div className={styles.globalParams}>
            <div className={styles.fieldGroup}>
              <label htmlFor="deductiblePercentage" className={styles.label}>
                Deducible (%)
              </label>
              <input
                id="deductiblePercentage"
                type="number"
                min={0}
                max={100}
                step={0.01}
                className={styles.input}
                aria-describedby={errors.deductiblePercentage ? 'deductible-error' : undefined}
                {...register('deductiblePercentage', { valueAsNumber: true })}
              />
              <span className={styles.helperText}>Porcentaje de deducible aplicable (0-100)</span>
              {errors.deductiblePercentage && (
                <span id="deductible-error" className={styles.error} role="alert">
                  {errors.deductiblePercentage.message}
                </span>
              )}
            </div>

            <div className={styles.fieldGroup}>
              <label htmlFor="coinsurancePercentage" className={styles.label}>
                Coaseguro (%)
              </label>
              <input
                id="coinsurancePercentage"
                type="number"
                min={0}
                max={100}
                step={0.01}
                className={styles.input}
                aria-describedby={errors.coinsurancePercentage ? 'coinsurance-error' : undefined}
                {...register('coinsurancePercentage', { valueAsNumber: true })}
              />
              <span className={styles.helperText}>Porcentaje de coaseguro aplicable (0-100)</span>
              {errors.coinsurancePercentage && (
                <span id="coinsurance-error" className={styles.error} role="alert">
                  {errors.coinsurancePercentage.message}
                </span>
              )}
            </div>
          </div>
        </section>

        {/* Garantías por categoría */}
        <section className={styles.section} aria-labelledby="guarantees-title">
          <h2 id="guarantees-title" className={styles.sectionTitle}>
            Garantías Habilitadas
          </h2>
          {errors.enabledGuarantees && (
            <span className={styles.error} role="alert">
              {errors.enabledGuarantees.message}
            </span>
          )}
          <Controller
            name="enabledGuarantees"
            control={control}
            render={({ field }) => (
              <div className={styles.guaranteeSections}>
                {CATEGORY_ORDER.map((cat) => {
                  const catGuarantees = guaranteesByCategory[cat] ?? [];
                  if (catGuarantees.length === 0) return null;
                  return (
                    <GuaranteeCheckboxGroup
                      key={cat}
                      category={cat}
                      guarantees={catGuarantees}
                      selectedKeys={field.value}
                      onToggle={handleToggle}
                      onSelectAll={handleSelectAll}
                      disabled={isPending}
                    />
                  );
                })}
              </div>
            )}
          />
        </section>
      </form>

      {/* Dialog de confirmación de deshabilitación */}
      <Modal
        isOpen={pendingUncheck !== null}
        title={SAVE_COVERAGE_OPTIONS_STRINGS.dialogTitle}
        onClose={() => setPendingUncheck(null)}
        footer={
          <>
            <Button
              variant="ghost"
              type="button"
              onClick={() => setPendingUncheck(null)}
            >
              {SAVE_COVERAGE_OPTIONS_STRINGS.dialogCancel}
            </Button>
            <Button
              variant="secondary"
              type="button"
              onClick={confirmUncheck}
            >
              {SAVE_COVERAGE_OPTIONS_STRINGS.dialogConfirm}
            </Button>
          </>
        }
      >
        <p>
          {pendingUncheck &&
            SAVE_COVERAGE_OPTIONS_STRINGS.dialogMessage(
              pendingUncheck.name,
              pendingUncheck.count,
            )}
        </p>
      </Modal>
    </>
  );
}
