---
description: Principios de testing. Framework backend: xUnit + Moq + FluentAssertions (.NET). Framework frontend: Vitest + Testing Library (React).
paths:
  - "**/tests/**"
  - "**/__tests__/**"
  - "**/*.test.*"
  - "**/*.spec.*"
  - "**/*Test.cs"
  - "**/*Tests.cs"
---

# Reglas de Testing

## Referencia de Stack

| Capa | Framework | Herramientas |
|------|-----------|-------------|
| Backend (.NET) | xUnit 2.x | Moq 4.x + FluentAssertions + coverlet.collector |
| Frontend (React) | Vitest | Testing Library + jest-dom |

## Principios Universales (independiente del framework)

### Estructura AAA obligatoria

```
// Arrange — preparar datos, mocks y contexto
// Act     — ejecutar la acción bajo prueba
// Assert  — verificar el resultado esperado
```

### Pirámide de Testing

| Nivel | % recomendado | Qué cubre |
|-------|--------------|-----------|
| **Unitarios** | ~70% | Lógica de negocio aislada con mocks |
| **Integración** | ~20% | Flujos entre capas, endpoints HTTP |
| **E2E** | ~10% | Flujos críticos de usuario |

### Reglas de Oro del Testing

- **Independencia** — cada test se puede ejecutar solo, en cualquier orden
- **Aislamiento** — mockear SIEMPRE dependencias externas (DB, APIs, auth, tiempo)
- **Determinismo** — sin `Thread.Sleep`, sin dependencia de fechas reales, sin datos de producción
- **Cobertura mínima ≥ 80%** en lógica de negocio (quality gate bloqueante en CI)
- **Nombres descriptivos** — `Method_Should_DoSomething_WhenCondition` (backend) / `describe/it` claro (frontend)
- **Un assert lógico por test** — si necesitas varios, separar en tests distintos

### Por cada unidad cubrir

- ✅ Happy path — datos válidos, flujo exitoso
- ❌ Error path — excepción esperada, respuesta de error
- 🔲 Edge case — vacío, duplicado, límites, permisos

---

## Backend — xUnit + Moq + FluentAssertions

### Estructura de archivos

```
src/Cotizador.Tests/
  UseCase/
    <FeatureName>UseCaseTest.cs
  Controllers/
    <FeatureName>ControllerTest.cs
  Repositories/
    <FeatureName>RepositoryTest.cs
    Helpers/
      MongoRepositoryTestFixture.cs
      <Feature>TestData.cs
  Mocks/
    *.json
```

### Convenciones de Código (.NET)

- Clase de test: `public class <SubjectUnderTest>Test`
- Método: `[Fact]` o `[Theory]` con nombre `Method_Should_DoSomething_WhenCondition`
- Mocks declarados como `private readonly Mock<IInterface> _mockX = new();`
- Subject Under Test (SUT) instanciado via propiedad `get =>` para garantizar instancia fresca por test
- NUNCA campos `static` en la clase de test
- NUNCA compartir estado mutable entre `[Fact]` / `[Theory]`

### Patrón para Use Cases

```csharp
public class CalculateQuoteUseCaseTest
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<CalculateQuoteUseCase>> _mockLogger = new();

    private CalculateQuoteUseCase Sut =>
        new(_mockRepository.Object, _mockCoreOhsClient.Object, _mockLogger.Object);

    [Fact]
    public async Task Execute_Should_ReturnQuote_WhenValidDataProvided()
    {
        // Arrange
        var input = new QuoteRequest { /* valores válidos */ };
        _mockCoreOhsClient
            .Setup(x => x.GetRatesAsync(It.IsAny<string>()))
            .ReturnsAsync(new RateResponse { /* datos */ });

        // Act
        var result = await Sut.Execute(input);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.InsertAsync(It.IsAny<QuoteDocument>()), Times.Once);
    }

    [Fact]
    public async Task Execute_Should_ThrowCoreOhsUnavailableException_WhenClientFails()
    {
        // Arrange
        _mockCoreOhsClient
            .Setup(x => x.GetRatesAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<CoreOhsUnavailableException>(() =>
            Sut.Execute(new QuoteRequest()));
    }
}
```

### Patrón para Controllers

```csharp
public class QuoteControllerTest
{
    private readonly Mock<ICalculateQuoteUseCase> _mockUseCase = new();
    private readonly Mock<ILogger<QuoteController>> _mockLogger = new();

    private QuoteController Sut =>
        new(_mockUseCase.Object, _mockLogger.Object);

    [Fact]
    public async Task Post_Should_Return200_WhenRequestIsValid()
    {
        // Arrange
        var request = new QuoteRequest { /* valores válidos */ };
        _mockUseCase
            .Setup(x => x.Execute(It.IsAny<QuoteRequest>()))
            .ReturnsAsync(new QuoteResponse());

        // Act
        var result = (OkObjectResult)await Sut.Post(request);

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Post_Should_Return400_WhenModelStateIsInvalid()
    {
        // Arrange
        var controller = Sut;
        controller.ModelState.AddModelError("field", "required");

        // Act
        var result = (BadRequestObjectResult)await controller.Post(new QuoteRequest());

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
```

---

## Frontend — Vitest + Testing Library

### Convenciones (React / TypeScript)

- Archivos en `__tests__/` o co-localizados como `Component.test.tsx`
- Usar `describe` / `it` con nombres descriptivos en español o inglés consistente por feature
- Mockear servicios API con `vi.mock()` o MSW (Mock Service Worker)
- Preferir `userEvent` sobre `fireEvent` para interacciones de usuario
- NUNCA testear detalles de implementación — testear comportamiento visible al usuario

```typescript
describe('QuoteForm', () => {
  it('muestra error cuando el valor asegurado es negativo', async () => {
    // Arrange
    render(<QuoteForm />);
    const input = screen.getByLabelText(/valor asegurado/i);

    // Act
    await userEvent.type(input, '-100');
    await userEvent.click(screen.getByRole('button', { name: /calcular/i }));

    // Assert
    expect(screen.getByText(/el valor debe ser mayor a 0/i)).toBeInTheDocument();
  });
});
```

---

## Anti-patrones Prohibidos

- Tests que dependen del orden de ejecución
- Llamadas reales a servicios externos (DB, APIs, auth)
- `console.log` / `Console.WriteLine` permanentes en tests
- Lógica condicional dentro de un test (`if`/`else`)
- Datos de producción real en fixtures
- Mockear el SUT mismo (solo mockear sus dependencias)

## Estrategia de Regresión

- **Smoke suite** (`[Trait("Category", "Smoke")]` / `@smoke`): happy paths críticos → corre en cada PR
- **Regresión completa** (`[Trait("Category", "Regression")]` / `@regression`): todo → corre nightly o pre-release
- Un test marcado como crítico entra automáticamente al smoke suite
