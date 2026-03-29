import { Router, Request, Response } from 'express';
import subscribersData from '../fixtures/subscribers.json';
import { Subscriber, ApiResponse } from '../types';

const router = Router();
const subscribers = subscribersData as Subscriber[];

router.get('/', (_req: Request, res: Response) => {
  const response: ApiResponse<Subscriber[]> = { data: subscribers };
  res.status(200).json(response);
});

export default router;
