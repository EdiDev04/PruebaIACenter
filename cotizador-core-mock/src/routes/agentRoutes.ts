import { Router, Request, Response } from 'express';
import agentsData from '../fixtures/agents.json';
import { Agent, ApiResponse, ApiError } from '../types';

const router = Router();
const agents = agentsData as Agent[];

// GET /v1/agents?code=<code>
router.get('/', (req: Request, res: Response) => {
  const { code } = req.query;

  if (code !== undefined) {
    const agent = agents.find((a) => a.code === code);
    if (!agent) {
      const error: ApiError = {
        type: 'agentNotFound',
        message: 'Agente no encontrado',
      };
      res.status(404).json(error);
      return;
    }
    const response: ApiResponse<Agent> = { data: agent };
    res.status(200).json(response);
    return;
  }

  const response: ApiResponse<Agent[]> = { data: agents };
  res.status(200).json(response);
});

export default router;
