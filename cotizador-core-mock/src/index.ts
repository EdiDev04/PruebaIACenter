import express from 'express';
import { correlationIdMiddleware } from './middleware/correlationId';
import subscriberRoutes from './routes/subscriberRoutes';
import agentRoutes from './routes/agentRoutes';
import businessLineRoutes from './routes/businessLineRoutes';
import zipCodeRoutes from './routes/zipCodeRoutes';
import folioRoutes from './routes/folioRoutes';
import catalogRoutes from './routes/catalogRoutes';
import tariffRoutes from './routes/tariffRoutes';

const app = express();
const port = parseInt(process.env.PORT ?? '3001', 10);

// Middleware
app.use(express.json());
app.use(correlationIdMiddleware);

// Routes
app.use('/v1/subscribers', subscriberRoutes);
app.use('/v1/agents', agentRoutes);
app.use('/v1/business-lines', businessLineRoutes);
app.use('/v1/zip-codes', zipCodeRoutes);
app.use('/v1/folios', folioRoutes);
app.use('/v1/catalogs', catalogRoutes);
app.use('/v1/tariffs', tariffRoutes);

// Health check
app.get('/health', (_req, res) => {
  res.status(200).json({ status: 'ok', service: 'cotizador-core-mock' });
});

app.listen(port, () => {
  console.log(`cotizador-core-mock running on port ${port}`);
});

export default app;
