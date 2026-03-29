#language: es
# ============================================================
# SPEC-004 — Gestión de Datos Generales: Endpoints Proxy de Catálogos
# Generado por: QA Agent
# Fecha: 2026-03-29
# Spec de referencia: .github/specs/general-info-management.spec.md §3.5
# Fixtures:
#   cotizador-core-mock/src/fixtures/subscribers.json
#   cotizador-core-mock/src/fixtures/agents.json
#   cotizador-core-mock/src/fixtures/riskClassification.json
# Tests unitarios existentes: CatalogControllerTests.cs (NO duplicar)
# ============================================================

Característica: Proxy de catálogos — Suscriptores, Agentes y Clasificación de Riesgo
  Como frontend del cotizador
  Quiero consultar catálogos a través de los endpoints proxy del backend
  Para que el formulario de datos generales (Step 1 del wizard) sea poblado con datos reales de core-mock
  Sin exponer directamente core-mock al cliente (RN-004-10)

  # ─────────────────────────────────────────────────────────────────────────────
  # Contexto compartido — credenciales y cabeceras
  # Auth: Basic dXNlcjpwYXNz  →  user:pass
  # ─────────────────────────────────────────────────────────────────────────────

  Antecedentes:
    Dado que el sistema tiene las credenciales básicas "user:pass" configuradas
    Y que core-mock está operativo y expone sus fixtures
    Y que los suscriptores disponibles en core-mock son:
      | code    | name                   | office            | active |
      | SUB-001 | María González López   | CDMX Central      | true   |
      | SUB-002 | Carlos Ramírez Torres  | Guadalajara Norte | true   |
      | SUB-003 | Ana Martínez Ruiz      | Monterrey Sur     | true   |
    Y que los agentes disponibles en core-mock son:
      | code    | name               | region    | active |
      | AGT-001 | Roberto Hernández  | Centro    | true   |
      | AGT-002 | Laura Sánchez      | Occidente | true   |
      | AGT-003 | Pedro Díaz         | Norte     | true   |
    Y que las clasificaciones de riesgo en core-mock son:
      | code        | description                                   | factor |
      | standard    | Riesgo estándar                               | 1.0    |
      | preferred   | Riesgo preferente — perfil de riesgo bajo     | 0.85   |
      | substandard | Riesgo subestándar — perfil de riesgo alto    | 1.25   |


  # ═══════════════════════════════════════════════════════════════════════════
  # BLOQUE 1 — GET /v1/subscribers
  # ═══════════════════════════════════════════════════════════════════════════

  @smoke @critico @proxy @suscriptores
  Escenario: Happy path — obtener catálogo de suscriptores
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/subscribers
    Entonces la respuesta tiene código HTTP 200
    Y el body sigue el envelope: { "data": [...] }
    Y "data" contiene exactamente 3 elementos
    Y el primer elemento contiene:
      | campo  | valor                 |
      | code   | SUB-001               |
      | name   | María González López  |
      | office | CDMX Central          |
      | active | true                  |
    Y el segundo elemento contiene:
      | campo  | valor                  |
      | code   | SUB-002                |
      | name   | Carlos Ramírez Torres  |
      | office | Guadalajara Norte      |
      | active | true                   |
    Y el tercer elemento contiene:
      | campo  | valor              |
      | code   | SUB-003            |
      | name   | Ana Martínez Ruiz  |
      | office | Monterrey Sur      |
      | active | true               |

  @seguridad @smoke @suscriptores
  Escenario: Sin autenticación — GET /v1/subscribers rechaza sin token
    Dado que NO envío la cabecera "Authorization"
    Cuando realizo GET /v1/subscribers
    Entonces la respuesta tiene código HTTP 401
    Y el body contiene:
      | campo   | valor                             |
      | type    | unauthorized                      |
      | message | Credenciales inválidas o ausentes |
      | field   | null                              |
    Y el endpoint NO llama a core-mock

  @seguridad @suscriptores
  Escenario: Credenciales incorrectas — GET /v1/subscribers rechaza
    Dado que envío la cabecera "Authorization: Basic aW52YWxpZDp3cm9uZw=="
    Y las credenciales "invalid:wrong" son inválidas
    Cuando realizo GET /v1/subscribers
    Entonces la respuesta tiene código HTTP 401
    Y el campo "type" del body es "unauthorized"

  @error-path @resiliencia @suscriptores
  Escenario: core-mock no disponible — GET /v1/subscribers retorna 503
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que core-mock NO está disponible (timeout o conexión rechazada)
    Cuando realizo GET /v1/subscribers
    Entonces la respuesta tiene código HTTP 503
    Y el body contiene:
      | campo   | valor                                                   |
      | type    | coreOhsUnavailable                                      |
      | message | Servicio de catálogos no disponible, intente más tarde  |
      | field   | null                                                    |
    Y el backend NO expone el stack trace ni detalles internos de la excepción

  @edge-case @suscriptores
  Escenario: core-mock disponible pero retorna lista vacía
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que core-mock retorna una lista vacía de suscriptores
    Cuando realizo GET /v1/subscribers
    Entonces la respuesta tiene código HTTP 200
    Y el body es { "data": [] }


  # ═══════════════════════════════════════════════════════════════════════════
  # BLOQUE 2 — GET /v1/agents?code={code}
  # ═══════════════════════════════════════════════════════════════════════════

  @smoke @critico @proxy @agentes
  Escenario: Agente existe — GET /v1/agents?code=AGT-001 retorna datos completos
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/agents?code=AGT-001
    Entonces la respuesta tiene código HTTP 200
    Y el body contiene:
      | campo          | valor              |
      | data.code      | AGT-001            |
      | data.name      | Roberto Hernández  |
      | data.region    | Centro             |
      | data.active    | true               |

  @smoke @critico @proxy @agentes
  Escenario: Agente existe con código AGT-002 — segundo agente del catálogo
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/agents?code=AGT-002
    Entonces la respuesta tiene código HTTP 200
    Y "data.code" es "AGT-002"
    Y "data.name" es "Laura Sánchez"
    Y "data.region" es "Occidente"

  @error-path @agentes
  Escenario: Agente no existe — GET /v1/agents?code=AGT-999 retorna 404 en español
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que el agente "AGT-999" NO existe en core-mock
    Cuando realizo GET /v1/agents?code=AGT-999
    Entonces la respuesta tiene código HTTP 404
    Y el body contiene:
      | campo   | valor                                             |
      | type    | agentNotFound                                     |
      | message | El agente AGT-999 no está registrado en el catálogo |
      | field   | null                                              |
    Y el mensaje está en español (RN-004-09)

  @error-path @validacion @agentes
  Escenario: Formato inválido — GET /v1/agents?code=INVALID retorna 400 sin llamar a core-mock
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/agents?code=INVALID
    Entonces la respuesta tiene código HTTP 400
    Y el body contiene:
      | campo   | valor                     |
      | type    | validationError           |
      | message | Código de agente inválido |
      | field   | code                      |
    Y el backend NO realiza ninguna llamada HTTP hacia core-mock

  @error-path @validacion @agentes
  Esquema del escenario: Formatos de código de agente inválidos rechazados con 400
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/agents?code=<codigo>
    Entonces la respuesta tiene código HTTP 400
    Y el campo "type" del body es "validationError"
    Y el campo "field" del body es "code"
    Ejemplos:
      | codigo      | descripcion                          |
      | AGT001      | Sin guion                            |
      | AGT-01      | Solo 2 dígitos (requiere 3)          |
      | AGT-0001    | 4 dígitos (requiere exactamente 3)   |
      | agt-001     | Minúsculas                           |
      | AGT-00A     | Caracteres no numéricos              |
      | AGT-        | Sin dígitos                          |
      |             | Cadena vacía                         |

  @error-path @validacion @agentes
  Escenario: Sin parámetro code — GET /v1/agents sin query param retorna 400
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/agents (sin el parámetro code)
    Entonces la respuesta tiene código HTTP 400
    Y el campo "type" del body es "validationError"
    Y el campo "field" del body es "code"

  @seguridad @agentes
  Escenario: Sin autenticación — GET /v1/agents rechaza con 401
    Dado que NO envío la cabecera "Authorization"
    Cuando realizo GET /v1/agents?code=AGT-001
    Entonces la respuesta tiene código HTTP 401
    Y el campo "type" del body es "unauthorized"
    Y el endpoint NO llama a core-mock

  @error-path @resiliencia @agentes
  Escenario: core-mock no disponible — GET /v1/agents?code=AGT-001 retorna 503
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que core-mock NO está disponible
    Cuando realizo GET /v1/agents?code=AGT-001
    Entonces la respuesta tiene código HTTP 503
    Y el body contiene:
      | campo   | valor                                                   |
      | type    | coreOhsUnavailable                                      |
      | message | Servicio de catálogos no disponible, intente más tarde  |
      | field   | null                                                    |


  # ═══════════════════════════════════════════════════════════════════════════
  # BLOQUE 3 — GET /v1/catalogs/risk-classification
  # ═══════════════════════════════════════════════════════════════════════════

  @smoke @critico @proxy @clasificacion-riesgo
  Escenario: Happy path — obtener clasificaciones de riesgo
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/catalogs/risk-classification
    Entonces la respuesta tiene código HTTP 200
    Y el body sigue el envelope: { "data": [...] }
    Y "data" contiene exactamente 3 clasificaciones
    Y el elemento con code "standard" contiene:
      | campo       | valor            |
      | code        | standard         |
      | description | Riesgo estándar  |
      | factor      | 1.0              |
    Y el elemento con code "preferred" contiene:
      | campo       | valor                                         |
      | code        | preferred                                     |
      | description | Riesgo preferente — perfil de riesgo bajo     |
      | factor      | 0.85                                          |
    Y el elemento con code "substandard" contiene:
      | campo       | valor                                         |
      | code        | substandard                                   |
      | description | Riesgo subestándar — perfil de riesgo alto    |
      | factor      | 1.25                                          |

  @seguridad @clasificacion-riesgo
  Escenario: Sin autenticación — GET /v1/catalogs/risk-classification rechaza con 401
    Dado que NO envío la cabecera "Authorization"
    Cuando realizo GET /v1/catalogs/risk-classification
    Entonces la respuesta tiene código HTTP 401
    Y el body contiene:
      | campo   | valor                             |
      | type    | unauthorized                      |
      | message | Credenciales inválidas o ausentes |
      | field   | null                              |
    Y el endpoint NO llama a core-mock

  @error-path @resiliencia @clasificacion-riesgo
  Escenario: core-mock no disponible — GET /v1/catalogs/risk-classification retorna 503
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que core-mock NO está disponible (timeout o HTTP 5xx)
    Cuando realizo GET /v1/catalogs/risk-classification
    Entonces la respuesta tiene código HTTP 503
    Y el body contiene:
      | campo   | valor                                                   |
      | type    | coreOhsUnavailable                                      |
      | message | Servicio de catálogos no disponible, intente más tarde  |
      | field   | null                                                    |
    Y el error NO incluye información de la URL interna de core-mock

  @edge-case @clasificacion-riesgo
  Escenario: Verificar que los factores numéricos son de tipo decimal (no string)
    Dado que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando realizo GET /v1/catalogs/risk-classification
    Entonces la respuesta tiene código HTTP 200
    Y el campo "factor" de cada elemento es un número decimal (no string)
    Y el factor del elemento "preferred" es menor que 1.0
    Y el factor del elemento "substandard" es mayor que 1.0


  # ═══════════════════════════════════════════════════════════════════════════
  # BLOQUE 4 — Flujo End-to-End: Carga de catálogos → Guardar datos generales
  # (Cobertura del wizard Step 1 completo — HU-004-01 .. HU-004-05)
  # ═══════════════════════════════════════════════════════════════════════════

  @smoke @e2e @critico @wizard
  Escenario: E2E exitoso — cargar catálogos, llenar formulario y guardar datos generales
    # Fase 1 — Carga de catálogos (concurrente al abrir el formulario)
    Dado que el folio "DAN-2026-00001" existe con estado "draft" y version 1
    Y que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Cuando el frontend carga el formulario de datos generales (Step 1 del wizard)
    Entonces el sistema ejecuta en paralelo:
      | llamada                                  | resultado esperado    |
      | GET /v1/subscribers                      | 200 con 3 suscriptores |
      | GET /v1/catalogs/risk-classification     | 200 con 3 clasificaciones |
    Y el selector de suscriptor muestra las opciones:
      | código  | nombre visible              |
      | SUB-001 | María González López        |
      | SUB-002 | Carlos Ramírez Torres       |
      | SUB-003 | Ana Martínez Ruiz           |
    Y el selector de clasificación de riesgo muestra las opciones:
      | código      | descripción visible                              |
      | standard    | Riesgo estándar                                  |
      | preferred   | Riesgo preferente — perfil de riesgo bajo        |
      | substandard | Riesgo subestándar — perfil de riesgo alto       |

    # Fase 2 — El usuario busca un agente por código
    Cuando el usuario ingresa el código de agente "AGT-001" en el campo de búsqueda
    Y el frontend realiza GET /v1/agents?code=AGT-001
    Entonces la respuesta tiene código HTTP 200
    Y el campo de agente muestra "Roberto Hernández (Centro)"

    # Fase 3 — El usuario llena el formulario y guarda
    Cuando el usuario completa el formulario con:
      | campo                            | valor                          |
      | insuredData.name                 | Grupo Industrial SA de CV      |
      | insuredData.taxId                | GIN850101AAA                   |
      | insuredData.email                | contacto@grupoindustrial.com   |
      | insuredData.phone                | 5551234567                     |
      | conductionData.subscriberCode    | SUB-001                        |
      | conductionData.officeName        | CDMX Central                   |
      | agentCode                        | AGT-001                        |
      | businessType                     | commercial                     |
      | riskClassification               | standard                       |
      | version                          | 1                              |
    Y el frontend realiza PUT /v1/quotes/DAN-2026-00001/general-info con esos datos
    Entonces la respuesta tiene código HTTP 200
    Y la respuesta incluye version 2
    Y el estado del folio transiciona de "draft" a "in_progress"
    Y "metadata.lastWizardStep" es 1
    Y "metadata.updatedAt" fue actualizado

  @error-path @e2e @wizard
  Escenario: E2E — catálogos cargados pero agente no existe al guardar
    Dado que el folio "DAN-2026-00002" existe con estado "draft" y version 1
    Y que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que los catálogos de suscriptores y clasificación de riesgo cargaron exitosamente
    Cuando el usuario ingresa agentCode "AGT-999" que no existe en core-mock
    Y el frontend realiza GET /v1/agents?code=AGT-999
    Entonces la respuesta tiene código HTTP 404
    Y el formulario muestra el mensaje "El agente AGT-999 no está registrado en el catálogo"
    Y el botón "Guardar" permanece deshabilitado mientras el agente no sea válido

  @error-path @e2e @resiliencia @wizard
  Escenario: E2E — core-mock no disponible al cargar formulario (degradación parcial)
    Dado que el folio "DAN-2026-00003" existe con estado "draft" y version 1
    Y que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que core-mock NO está disponible
    Cuando el frontend intenta cargar los catálogos al abrir el formulario
    Entonces GET /v1/subscribers retorna 503
    Y GET /v1/catalogs/risk-classification retorna 503
    Y el frontend muestra el mensaje "Servicio de catálogos no disponible, intente más tarde"
    Y el formulario NO permite guardar hasta que los catálogos estén disponibles

  @error-path @e2e @versionado @wizard
  Escenario: E2E — conflicto de versión al guardar datos generales
    Dado que el folio "DAN-2026-00001" tiene version 4 en MongoDB
    Y que envío la cabecera "Authorization: Basic dXNlcjpwYXNz"
    Y que los catálogos cargaron exitosamente
    Cuando el usuario intenta guardar dados generales con version 3 (desactualizada)
    Y el frontend realiza PUT /v1/quotes/DAN-2026-00001/general-info con version 3
    Entonces la respuesta tiene código HTTP 409
    Y el body contiene:
      | campo   | valor                                                          |
      | type    | versionConflict                                                |
      | message | El folio fue modificado por otro proceso. Recargue para continuar |
      | field   | null                                                           |
    Y el frontend muestra alerta con botón "Recargar"
    Y los datos del folio NO fueron modificados en MongoDB


  # ═══════════════════════════════════════════════════════════════════════════
  # TABLA DE DATOS DE PRUEBA
  # ═══════════════════════════════════════════════════════════════════════════

  # Datos de prueba — Agentes válidos (fixture: agents.json)
  # | code    | name               | region    | active |
  # | AGT-001 | Roberto Hernández  | Centro    | true   |
  # | AGT-002 | Laura Sánchez      | Occidente | true   |
  # | AGT-003 | Pedro Díaz         | Norte     | true   |

  # Datos de prueba — Suscriptores válidos (fixture: subscribers.json)
  # | code    | name                   | office            | active |
  # | SUB-001 | María González López   | CDMX Central      | true   |
  # | SUB-002 | Carlos Ramírez Torres  | Guadalajara Norte | true   |
  # | SUB-003 | Ana Martínez Ruiz      | Monterrey Sur     | true   |

  # Datos de prueba — Clasificaciones de riesgo (fixture: riskClassification.json)
  # | code        | description                                | factor |
  # | standard    | Riesgo estándar                            | 1.0    |
  # | preferred   | Riesgo preferente — perfil de riesgo bajo  | 0.85   |
  # | substandard | Riesgo subestándar — perfil de riesgo alto | 1.25   |

  # Datos de prueba — Códigos de agente inválidos
  # | codigo   | razón                                |
  # | AGT001   | Falta el guion separador             |
  # | AGT-01   | Solo 2 dígitos numéricos             |
  # | AGT-0001 | 4 dígitos en lugar de 3              |
  # | agt-001  | Prefijo en minúsculas                |
  # | 001-AGT  | Prefijo y número intercambiados      |
  # | AGT-999  | Formato válido, agente inexistente   |

  # Base64 Credentials:
  # user:pass → dXNlcjpwYXNz
  # invalid:wrong → aW52YWxpZDp3cm9uZw==
