import { Router, Request, Response } from 'express';
import businessLinesData from '../fixtures/businessLines.json';
import { BusinessLine, ApiResponse } from '../types';

const router = Router();
const businessLines = businessLinesData as BusinessLine[];

router.get('/', (_req: Request, res: Response) => {
  const response: ApiResponse<BusinessLine[]> = { data: businessLines };
  res.status(200).json(response);
});

export default router;
