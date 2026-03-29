import { configureStore } from '@reduxjs/toolkit';
import { quoteWizardSlice } from '@/entities/folio';

export const store = configureStore({
  reducer: {
    quoteWizard: quoteWizardSlice.reducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
