import { describe, it, expect } from 'vitest';
import {
  quoteWizardSlice,
  initWizard,
  setFolioVersion,
  setCurrentStep,
  resetWizard,
} from '@/entities/folio';

const reducer = quoteWizardSlice.reducer;

describe('quoteWizardSlice', () => {
  it('returns the correct initial state', () => {
    const state = reducer(undefined, { type: '@@INIT' });
    expect(state).toEqual({
      activeFolio: null,
      currentStep: 0,
      folioVersion: 0,
    });
  });

  it('initWizard sets activeFolio and currentStep', () => {
    const state = reducer(
      undefined,
      initWizard({ activeFolio: 'DAN-2026-00001', currentStep: 2 })
    );
    expect(state.activeFolio).toBe('DAN-2026-00001');
    expect(state.currentStep).toBe(2);
  });

  it('initWizard does not affect folioVersion', () => {
    const prevState = { activeFolio: null, currentStep: 0, folioVersion: 5 };
    const state = reducer(prevState, initWizard({ activeFolio: 'DAN-2026-00001', currentStep: 1 }));
    expect(state.folioVersion).toBe(5);
  });

  it('setFolioVersion updates folioVersion', () => {
    const state = reducer(undefined, setFolioVersion(7));
    expect(state.folioVersion).toBe(7);
  });

  it('setCurrentStep updates only the currentStep and leaves other fields intact', () => {
    const prevState = { activeFolio: 'DAN-2026-00001', currentStep: 0, folioVersion: 3 };
    const state = reducer(prevState, setCurrentStep(3));
    expect(state.currentStep).toBe(3);
    expect(state.activeFolio).toBe('DAN-2026-00001');
    expect(state.folioVersion).toBe(3);
  });

  it('resetWizard restores the initial state regardless of previous values', () => {
    const prevState = { activeFolio: 'DAN-2026-00001', currentStep: 4, folioVersion: 5 };
    const state = reducer(prevState, resetWizard());
    expect(state).toEqual({ activeFolio: null, currentStep: 0, folioVersion: 0 });
  });
});
