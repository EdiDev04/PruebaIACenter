---
applyTo: "src/projects/**/*.{ts,html,scss}"
---

> **Scope**: Se aplica al frontend Angular de `genesis-gestorfactel-webapp`. Arquitectura Micro Frontend (Module Federation). UI SAP Fiori con Fundamental NGX.

# Instrucciones para Archivos de Frontend (Angular 13)

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Framework | Angular 13 + TypeScript 4.4 |
| UI library | `@fundamental-ngx/core` + `@fundamental-ngx/platform` (SAP Fiori 3) |
| Estilos | SCSS por componente + tokens SAP Fiori (`@sap-theming/theming-base-content`) |
| Auth (identidad) | Keycloak (`keycloak-angular` + `keycloak-js 16`) |
| Autorización | CASL (`@casl/ability` + `@casl/angular`) |
| HTTP | `HttpClient` de `@angular/common/http` |
| Formularios | Reactive Forms (`FormBuilder`, `FormGroup`) + Template-driven según complejidad |
| Internacionalización | `@ngx-translate/core` + `@ngx-translate/http-loader` |
| Observables | RxJS 7.5 (`Observable`, `lastValueFrom`, operadores pipeable) |
| Gráficos | `@swimlane/ngx-charts` |
| Utilidades | `lodash-es`, `file-saver`, `xlsx` |
| Build | `ngx-build-plus` + Webpack personalizado + Module Federation |
| Tests | Karma + Jasmine; cobertura con `karma-coverage` |
| Linting | TSLint 6 |

## Convenciones Obligatorias

- **Estilos**: SIEMPRE SCSS en archivo separado por componente (`styleUrls: ['./component.scss']`). NUNCA estilos inline ni CSS globales. Usar tokens de SAP Fiori para colores, espaciado y tipografía.
- **Nombres de archivos**: `kebab-case` para todos los archivos. Sufijos obligatorios: `.component.ts`, `.service.ts`, `.module.ts`, `.model.ts`, `.guard.ts`, `.pipe.ts`, `.spec.ts`.
- **Nombres de clases**: PascalCase para clases, interfaces y tipos. Sufijo obligatorio en la clase (ej. `HomeComponent`, `AuthService`, `AuthGuardService`).
- **Configuración runtime**: SIEMPRE leer de `ConfigService` (que carga `assets/app.config.json`). NUNCA hardcodear URLs ni IDs de aplicación.
- **Variables de entorno build-time**: usar `environment.ts` / `environment.qa.ts` / `environment.prod.ts` — SOLO para valores que cambian por entorno (ej. `url_assets`). No exponer secretos.
- **Encapsulación**: usar `ViewEncapsulation.None` únicamente cuando sea estrictamente necesario para estilos de Fundamental NGX que no penetran Shadow DOM.

## Estructura de Archivos

```
src/
  app/
    core/
      models/         ← interfaces y modelos de dominio (.model.ts)
      services/       ← servicios compartidos (HTTP, utilidades)
    facturacionelec/  ← módulo de feature (lazy load)
      <feature>/
        <feature>.component.ts
        <feature>.component.html
        <feature>.component.scss
        <feature>.component.spec.ts
    services/
      auth.service.ts         ← autorización por recurso (lee sessionStorage)
      auth-guard.service.ts   ← CanActivate basado en AuthService
      config.service.ts       ← carga app.config.json (APP_INITIALIZER)
      app.ability.ts          ← definición de reglas CASL
    pipes/
    shared/
      shared.module.ts
      components/
  assets/
    app.config.json           ← configuración runtime (URLs de APIs)
    i18n/
      es.json
      en.json
  environments/
    environment.ts
    environment.qa.ts
    environment.prod.ts
```

## Llamadas a la API Backend

Usar siempre `HttpClient` (NUNCA `fetch` ni Axios). Las llamadas van en servicios dentro de `core/services/` o del módulo de feature correspondiente, NUNCA directamente en componentes.

```typescript
// core/services/electronic-document/electronic-document.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from '../../../services/config.service';
import { ElectronicDocument } from '../../models/electronic-document/electronic-document.model';

@Injectable({ providedIn: 'root' })
export class ElectronicDocumentService {
  constructor(
    private http: HttpClient,
    private configService: ConfigService
  ) {}

  getDocuments(params: any): Observable<ElectronicDocument[]> {
    const url = this.configService.getConfig.api_baseUrl;
    return this.http.get<ElectronicDocument[]>(`${url}/documents`, { params });
  }

  rejectDocument(id: string, body: any): Observable<any> {
    const url = this.configService.getConfig.api_baseUrl;
    return this.http.post(`${url}/documents/${id}/reject`, body);
  }
}
```

El token de Keycloak es inyectado automáticamente por el interceptor de `keycloak-angular`. No añadir cabeceras `Authorization` manualmente en los servicios.

## Auth y Autorización

**Keycloak** gestiona la identidad. El flujo se inicializa en `APP_INITIALIZER` y el guard de Keycloak (`KeycloakAuthGuard`) protege las rutas.

**CASL** gestiona la autorización por recurso. Consumir siempre desde el `AppAbility` inyectado:

```typescript
// En un componente
import { AppAbility } from '../services/app.ability';

@Component({ ... })
export class MiComponent {
  constructor(private ability: AppAbility) {}

  canEdit(): boolean {
    return this.ability.can('update', 'MiRecurso');
  }
}
```

Para verificaciones más simples de recurso/acción usar `AuthService.isAuthorized(resource)`.

## Internacionalización (i18n)

Usar siempre `TranslateModule` y la pipe `| translate`. NUNCA texto literal en templates.

```html
<!-- template -->
<span>{{ 'FEATURE.LABEL_KEY' | translate }}</span>
<fd-button [label]="'COMMON.SAVE' | translate"></fd-button>
```

Las claves viven en `assets/i18n/es.json` y `assets/i18n/en.json` con estructura de namespacing por feature.

## Rutas (Angular Router)

Las rutas del módulo raíz se definen en `app.routes.ts` con lazy loading obligatorio para módulos de feature:

```typescript
// app.routes.ts
export const APP_ROUTES: Routes = [
  { path: '', redirectTo: 'gestorfactel', pathMatch: 'full' },
  {
    path: 'gestorfactel',
    loadChildren: () =>
      import('./facturacionelec/facturacionelec.module').then(m => m.gestorfactelModule),
  },
];
```

Las rutas internas del feature se definen en `<feature>.routes.ts`. Proteger rutas con `AuthGuardService` cuando se requiere control por recurso:

```typescript
{
  path: 'nueva-ruta',
  component: NuevaRutaComponent,
  canActivate: [AuthGuardService],
  data: { expectedResource: 'NOMBRE_RECURSO' },
}
```

## Componentes

- Un componente por archivo.
- No lógica de negocio en los componentes — delegar siempre a servicios.
- Usar `OnPush` change detection en componentes de listado / alto rendimiento cuando sea posible.
- Desuscribirse de Observables en `ngOnDestroy` (usar `takeUntil` con un `Subject` o `AsyncPipe` en template).
- Inputs/Outputs tipados explícitamente en TypeScript.

## Formularios

Preferir **Reactive Forms** para formularios complejos o con validaciones dinámicas. Template-driven solo para formularios simples de una o dos campos.

```typescript
this.form = this.fb.group({
  campo: [null, [Validators.required, Validators.maxLength(100)]],
});
```

## Module Federation

Este proyecto es un **Remote MFE**. Expone `./Component` y `./Module` en `remoteEntry.js`. Las dependencias compartidas (`@angular/core`, `@angular/common`, `@fundamental-ngx/core`, `keycloak-angular`) se declaran como `singleton: true` en `webpack.config.js`. No modificar la configuración de Module Federation sin validar impacto en el shell anfitrión.
