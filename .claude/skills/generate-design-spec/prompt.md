---
name: generate-design-spec
description: Genera design specs y diseños UI usando Google Stitch para una feature del cotizador. Produce mockups, reglas visuales y artefactos que el frontend-developer consume como input obligatorio.
---

## Instrucciones

1. Recibe el nombre de la feature como argumento: `/generate-design-spec <nombre-feature>`
2. Delega al sub-agente `ux-designer`
3. El agente ejecuta el flujo completo:
   - Análisis Data-Driven del modelo de datos
   - Generación del design spec (`.github/design-specs/<feature>.design.md`)
   - Generación de pantallas en Google Stitch via MCP
   - Extracción de HTML/CSS de referencia
   - Validación contra principios de diseño conductual

## Requisitos

- El Stitch MCP debe estar configurado en `.claude/settings.json`
- Si no existe `.github/design-specs/DESIGN-SYSTEM.md`, el agente lo genera automáticamente
- La feature puede o no tener spec técnica previa — si existe, la consume como input adicional

## Output esperado

```
.github/design-specs/
├── DESIGN-SYSTEM.md               → Sistema de diseño (tokens, principios, accesibilidad)
├── DESIGN.md                      → Formato Stitch-compatible del design system
├── .stitch-config.json            → Project y screen IDs de Stitch
├── <feature>.design.md            → Design spec completo (6 secciones)
└── screens/<feature>/
    └── *.html                     → HTML/CSS de referencia generado por Stitch
```

## Uso

```bash
# Generar design spec para una pantalla
/generate-design-spec ubicaciones

# Generar design spec para todo el cotizador
/generate-design-spec cotizador-completo

# Regenerar una pantalla específica con refinamientos
/generate-design-spec ubicaciones --refine "Agregar estado de carga en el formulario"
```