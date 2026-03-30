import { useCallback } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAppSelector } from '@/app/hooks';
import { getGeneralInfo, putGeneralInfo } from '../api/generalInfoApi';
import { generalInfoFormSchema, type GeneralInfoFormData } from './generalInfoSchema';

export function useGeneralInfoForm(folioNumber: string) {
  const queryClient = useQueryClient();
  const folioVersion = useAppSelector((s) => s.quoteWizard.folioVersion);

  const { data: serverData, isLoading: isLoadingData, refetch } = useQuery({
    queryKey: ['general-info', folioNumber],
    queryFn: () => getGeneralInfo(folioNumber),
    enabled: !!folioNumber,
  });

  const form = useForm<GeneralInfoFormData>({
    resolver: zodResolver(generalInfoFormSchema),
    defaultValues: {
      name: '',
      taxId: '',
      email: '',
      phone: '',
      subscriberCode: '',
      officeName: '',
      agentCode: '',
      businessType: 'commercial',
      riskClassification: '',
    },
    values: serverData
      ? {
          name: serverData.data.insuredData.name ?? '',
          taxId: serverData.data.insuredData.taxId ?? '',
          email: serverData.data.insuredData.email ?? '',
          phone: serverData.data.insuredData.phone ?? '',
          subscriberCode: serverData.data.conductionData.subscriberCode ?? '',
          officeName: serverData.data.conductionData.officeName ?? '',
          agentCode: serverData.data.agentCode ?? '',
          businessType: (serverData.data.businessType as 'commercial' | 'industrial' | 'residential') ?? 'commercial',
          riskClassification: serverData.data.riskClassification ?? '',
        }
      : undefined,
  });

  // Prefer the version from the server response (survives page reloads).
  // Fall back to Redux only when the GET hasn't resolved yet.
  const resolvedVersion = serverData?.data.version ?? folioVersion;

  const mutation = useMutation({
    mutationFn: (data: GeneralInfoFormData) =>
      putGeneralInfo(folioNumber, { ...data, version: resolvedVersion }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['quote-state', folioNumber] });
    },
  });

  const reloadData = useCallback(() => refetch(), [refetch]);

  return { form, mutation, isLoadingData, reloadData };
}
