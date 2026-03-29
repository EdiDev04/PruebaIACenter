import { Router, Request, Response } from 'express';
import { FolioResponse, ApiResponse } from '../types';

const router = Router();

let counter: number = parseInt(process.env.FOLIO_START ?? '1', 10);

// GET /v1/folios/next
router.get('/next', (_req: Request, res: Response) => {
  const year = new Date().getFullYear();
  const paddedCounter = String(counter).padStart(5, '0');
  const folioNumber = `DAN-${year}-${paddedCounter}`;
  counter += 1;

  const folioResponse: FolioResponse = { folioNumber };
  const response: ApiResponse<FolioResponse> = { data: folioResponse };
  res.status(200).json(response);
});

export default router;
