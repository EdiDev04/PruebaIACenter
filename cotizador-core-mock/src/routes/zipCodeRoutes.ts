import { Router, Request, Response } from 'express';
import zipCodesData from '../fixtures/zipCodes.json';
import { ZipCodeData, ApiResponse, ApiError } from '../types';

const router = Router();
const zipCodes = zipCodesData as ZipCodeData[];

// GET /v1/zip-codes/:zipCode
router.get('/:zipCode', (req: Request, res: Response) => {
  const { zipCode } = req.params;
  const found = zipCodes.find((z) => z.zipCode === zipCode);

  if (!found) {
    const error: ApiError = {
      type: 'zipCodeNotFound',
      message: 'Código postal no encontrado',
    };
    res.status(404).json(error);
    return;
  }

  const response: ApiResponse<ZipCodeData> = { data: found };
  res.status(200).json(response);
});

// POST /v1/zip-codes/validate
router.post('/validate', (req: Request, res: Response) => {
  const { zipCode } = req.body as { zipCode?: unknown };

  if (!zipCode || typeof zipCode !== 'string') {
    const error: ApiError = {
      type: 'validationError',
      message: "El campo 'zipCode' es obligatorio",
    };
    res.status(400).json(error);
    return;
  }

  const found = zipCodes.find((z) => z.zipCode === zipCode);
  const response: ApiResponse<{ valid: boolean; zipCode: string }> = {
    data: { valid: !!found, zipCode },
  };
  res.status(200).json(response);
});

export default router;
