import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { ZipCodeDto } from '@/entities/zip-code';
import type { LocationFormValues, LocationDto } from '@/entities/location';
import { locationFormSchema, useLocationsQuery } from '@/entities/location';
import { useBusinessLinesQuery } from '@/entities/business-line';
import { DEFAULT_SELECTED_GUARANTEES } from '@/entities/location/model/guaranteeCatalog';
import { Button } from '@/shared/ui';
import { useSaveLocations } from '../model/useSaveLocations';
import { LocationFormStep1 } from './LocationFormStep1';
import { LocationFormStep2 } from './LocationFormStep2';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';
import styles from './LocationForm.module.css';

interface Props {
  readonly folio: string;
  readonly locationIndex?: number;
  readonly initialData?: Partial<LocationFormValues>;
  readonly onSuccess: () => void;
  readonly onCancel: () => void;
}

function buildDefaultValues(initialData?: Partial<LocationFormValues>): LocationFormValues {
  if (initialData) {
    return {
      locationName: initialData.locationName ?? '',
      address: initialData.address ?? '',
      zipCode: initialData.zipCode ?? '',
      state: initialData.state,
      municipality: initialData.municipality,
      neighborhood: initialData.neighborhood,
      city: initialData.city,
      catZone: initialData.catZone,
      constructionType: initialData.constructionType,
      level: initialData.level,
      constructionYear: initialData.constructionYear,
      businessLine: initialData.businessLine,
      guarantees: initialData.guarantees ?? [],
    };
  }
  return {
    locationName: '',
    address: '',
    zipCode: '',
    state: undefined,
    municipality: undefined,
    neighborhood: undefined,
    city: undefined,
    catZone: undefined,
    constructionType: undefined,
    level: undefined,
    constructionYear: undefined,
    businessLine: undefined,
    guarantees: DEFAULT_SELECTED_GUARANTEES.map((key) => ({ guaranteeKey: key, insuredAmount: 0 })),
  };
}

export function LocationForm({ folio, locationIndex, initialData, onSuccess, onCancel }: Props) {
  const [step, setStep] = useState<1 | 2>(1);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const isEditing = locationIndex !== undefined;

  const { data: locationsData } = useLocationsQuery(folio);
  const { data: businessLines = [] } = useBusinessLinesQuery();

  const form = useForm<LocationFormValues>({
    resolver: zodResolver(locationFormSchema),
    defaultValues: buildDefaultValues(initialData),
    mode: 'onBlur',
  });

  const saveLocations = useSaveLocations({
    folio,
    onSuccess,
    onError: setErrorMessage,
  });

  async function handleNextStep() {
    const valid = await form.trigger([
      'locationName',
      'address',
      'zipCode',
      'constructionType',
      'level',
      'constructionYear',
    ]);
    if (valid) setStep(2);
  }

  function handleSubmit(values: LocationFormValues) {
    if (!locationsData) return;

    const { locations, version } = locationsData;

    const existingLocation = isEditing ? locations.find((l) => l.index === locationIndex) : undefined;

    // Enrich businessLine with code/riskLevel from catalog if GET returned empty strings
    let resolvedBusinessLine = values.businessLine ?? null;
    if (resolvedBusinessLine && (!resolvedBusinessLine.code || !resolvedBusinessLine.riskLevel)) {
      const match = businessLines.find((bl) => bl.fireKey === resolvedBusinessLine!.fireKey);
      if (match) {
        resolvedBusinessLine = { code: match.code, description: match.description, fireKey: match.fireKey, riskLevel: match.riskLevel };
      }
    }

    const locationPayload: LocationDto = {
      index: isEditing ? (locationIndex ?? 1) : locations.length + 1,
      locationName: values.locationName,
      address: values.address,
      zipCode: values.zipCode ?? '',
      state: values.state ?? '',
      municipality: values.municipality ?? '',
      neighborhood: values.neighborhood ?? '',
      city: values.city ?? '',
      catZone: values.catZone ?? '',
      constructionType: values.constructionType ?? '',
      level: values.level ?? 0,
      constructionYear: values.constructionYear ?? 0,
      locationBusinessLine: resolvedBusinessLine,
      guarantees: values.guarantees ?? [],
      blockingAlerts: existingLocation?.blockingAlerts ?? [],
      validationStatus: existingLocation?.validationStatus ?? 'incomplete',
    };

    let updatedList: LocationDto[];
    if (isEditing) {
      updatedList = locations.map((loc) =>
        loc.index === locationIndex ? locationPayload : loc,
      );
    } else {
      updatedList = [...locations, locationPayload];
    }

    saveLocations.mutate({ locations: updatedList, version });
  }

  const title = isEditing ? S.titleEdit : S.titleNew;

  return (
    <dialog className={styles.overlay} aria-label={title} open>
      <div className={styles.drawer}>
        <header className={styles.header}>
          <div className={styles.titleBlock}>
            <h2 className={styles.title}>{title}</h2>
            <div className={styles.steps} aria-label="Pasos del formulario">
              <span className={`${styles.stepDot} ${step === 1 ? styles.stepDotActive : styles.stepDotDone}`} />
              <span className={`${styles.stepConnector}`} />
              <span className={`${styles.stepDot} ${step === 2 ? styles.stepDotActive : ''}`} />
            </div>
            <p className={styles.stepLabel}>
              Paso {step} de 2 — {step === 1 ? S.step1Label : S.step2Label}
            </p>
          </div>
          <button className={styles.closeBtn} onClick={onCancel} aria-label="Cerrar formulario">
            <span className="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        </header>

        <div className={styles.body}>
          <form
            id="location-form"
            noValidate
            onSubmit={form.handleSubmit(handleSubmit)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !(e.target instanceof HTMLTextAreaElement)) {
                e.preventDefault();
              }
            }}
          >
            {step === 1 && (
              <LocationFormStep1
                form={form}
                onZipResolved={(_data: ZipCodeDto) => { /* fields already set via setValue */ }}
              />
            )}
            {step === 2 && (
              <LocationFormStep2 form={form} businessLines={businessLines} />
            )}
          </form>

          {errorMessage && (
            <p className={styles.errorBanner} role="alert">{errorMessage}</p>
          )}
        </div>

        <footer className={styles.footer}>
          {step === 1 ? (
            <>
              <Button variant="ghost" type="button" onClick={onCancel}>
                {S.btnCancel}
              </Button>
              <Button variant="primary" type="button" onClick={handleNextStep}>
                {S.btnNext}
              </Button>
            </>
          ) : (
            <>
              <Button variant="ghost" type="button" onClick={() => setStep(1)}>
                {S.btnBack}
              </Button>
              <Button
                variant="primary"
                type="button"
                onClick={form.handleSubmit(handleSubmit, (errors) => {
                  const firstError = Object.values(errors).flatMap((e) =>
                    Array.isArray(e) ? e.map((i) => i?.insuredAmount?.message ?? i?.guaranteeKey?.message) : [e?.message],
                  ).find(Boolean);
                  setErrorMessage(firstError ?? 'Por favor verifica los datos del formulario');
                })}
                isLoading={saveLocations.isPending}
                loadingText="Guardando..."
              >
                {S.btnSave}
              </Button>
            </>
          )}
        </footer>
      </div>
    </dialog>
  );
}
