import { Request, Response, NextFunction } from 'express';

export function correlationIdMiddleware(
  req: Request,
  res: Response,
  next: NextFunction
): void {
  const headerName = 'X-Correlation-Id';
  const correlationId = req.headers[headerName.toLowerCase()] as string | undefined;
  const id = correlationId ?? crypto.randomUUID();
  res.setHeader(headerName, id);
  next();
}
