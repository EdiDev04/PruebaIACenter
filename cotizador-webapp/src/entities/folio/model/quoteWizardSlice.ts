import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface WizardState {
  activeFolio: string | null;
  currentStep: number;
  folioVersion: number;
}

const initialState: WizardState = {
  activeFolio: null,
  currentStep: 0,
  folioVersion: 0,
};

export const quoteWizardSlice = createSlice({
  name: 'quoteWizard',
  initialState,
  reducers: {
    initWizard: (
      state,
      action: PayloadAction<{ activeFolio: string; currentStep: number }>
    ) => {
      state.activeFolio = action.payload.activeFolio;
      state.currentStep = action.payload.currentStep;
    },
    setFolioVersion: (state, action: PayloadAction<number>) => {
      state.folioVersion = action.payload;
    },
    setCurrentStep: (state, action: PayloadAction<number>) => {
      state.currentStep = action.payload;
    },
    resetWizard: (state) => {
      state.activeFolio = null;
      state.currentStep = 0;
      state.folioVersion = 0;
    },
  },
});

export const { initWizard, setFolioVersion, setCurrentStep, resetWizard } =
  quoteWizardSlice.actions;
