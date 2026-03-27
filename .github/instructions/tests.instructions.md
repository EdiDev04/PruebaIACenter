---
applyTo: "src/ReenviarDocElectronicos.UnitTest/**/*.cs"
---

> **Scope**: Se aplica al proyecto `ReenviarDocElectronicos.UnitTest` (.NET 8 / xUnit + Moq). Solo se testea **lógica de negocio y contratos**: Use Cases, Controllers y Adapters (Repositories). NUNCA infraestructura real ni configuración de DI.

# Instrucciones para Archivos de Pruebas Unitarias (C# / xUnit + Moq)

## Stack de Testing

| Componente | Tecnología |
|---|---|
| Framework | xUnit 2.x |
| Mocking | Moq 4.x |
| Cobertura | coverlet.collector |
| Target framework | .NET 9 |

## Principios

- **Independencia**: cada test es 100% autónomo — sin estado compartido entre `[Fact]` / `[Theory]`.
- **Aislamiento**: mockear SIEMPRE dependencias externas (MongoDB, RabbitMQ, `ILogger`, `ISettings`).
- **Claridad**: nombre del método sigue el patrón `Method_Should_DoSomething_WhenCondition`.
- **Patrón AAA**: comentarios `// Arrange` → `// Act` → `// Assert` siempre presentes.
- **Cobertura**: happy path + error path + edge cases por cada unidad.

## Estructura de archivos

Los tests viven en el proyecto `ReenviarDocElectronicos.UnitTest`, espejando la estructura de capas:

```
src/ReenviarDocElectronicos.UnitTest/
  UseCase/
    <FeatureName>UseCaseTest.cs
  Controllers/
    <FeatureName>ControllerTest.cs
  Adapters/
    <FeatureName>RepositoryTest.cs
    Helpers/
      MongoRepositoryTestFixture.cs   ← fixture compartida para MongoDB
      <Feature>TestData.cs            ← datos para [Theory]
  Mocks/
    *.json                            ← JSON de datos de prueba
```

## Convenciones de Código

- Clase de test: `public class <SubjectUnderTest>Test`.
- Método de test: `[Fact]` o `[Theory]` con nombre `Method_Should_DoSomething_WhenCondition`.
- Mocks declarados como `private readonly Mock<IInterface> _mockX = new();`.
- Subject Under Test (SUT) instanciado via propiedad `get =>` para garantizar instancia fresca por test.
- Nunca campos `static` en la clase de test.

## Patrón para Use Cases

```csharp
public class MyUseCaseTest
{
    private readonly Mock<IMyRepository> _mockRepository = new();
    private readonly Mock<ILogger<MyUseCase>> _mockLogger = new();
    private ISettings _settings;

    private MyUseCase Sut => new(_mockRepository.Object, _settings, _mockLogger.Object);

    [Fact]
    public async Task Execute_Should_SaveEntity_WhenValidDataProvided()
    {
        // Arrange
        _settings = new Settings { /* propiedades requeridas */ };

        // Act
        await Sut.Execute(/* parámetros */);

        // Assert
        _mockRepository.Verify(x => x.Save(It.Is<MyDto>(d => d.field == "expected")), Times.Once);
    }

    [Fact]
    public async Task Execute_Should_ThrowArgumentException_WhenRepositoryFails()
    {
        // Arrange
        _settings = new Settings { /* propiedades requeridas */ };
        _mockRepository
            .Setup(x => x.Save(It.IsAny<MyDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => Sut.Execute(/* parámetros */));
    }
}
```

## Patrón para Controllers

```csharp
public class MyControllerTest
{
    private readonly Mock<IMyUseCase> _mockUseCase = new();
    private readonly Mock<ILogger<MyController>> _mockLogger = new();
    private ISettings _settings;

    public MyController Sut
    {
        get => new(_mockLogger.Object, _mockUseCase.Object, _settings);
        set { }
    }

    [Fact]
    public async Task Post_Should_Return200_WhenRequestIsValid()
    {
        // Arrange
        _settings = new Settings { /* propiedades requeridas */ };
        var request = new MyRequest { /* valores válidos */ };

        // Act
        var response = (ObjectResult)await Sut.Post(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Should_Return400_WhenModelStateIsInvalid()
    {
        // Arrange
        _settings = new Settings { /* propiedades requeridas */ };
        var controller = Sut;
        controller.ModelState.AddModelError("field", "required");

        // Act
        var response = (ObjectResult)await controller.Post(new MyRequest());

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }
}
```

## Patrón para Adapters (Repositories) con MongoDB

Usar `MongoRepositoryTestFixture<TDocument>` para configurar el mock de `IMongoClient`:

```csharp
public class MyRepositoryTest
{
    [Fact]
    public async Task GetById_Should_ThrowArgumentException_WhenMongoFails()
    {
        // Arrange
        using var fixture = new MongoRepositoryTestFixture<MyDocument>();
        fixture.SetupFindAsyncThrows(fixture.collectionMock, new Exception("Connection error"));
        var repository = new MyRepository(fixture.mongoClientMock.Object, fixture.settings);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => repository.GetById("id"));
        Assert.Contains("Connection error", ex.Message);
    }

    [Theory]
    [MemberData(nameof(MyTestData.ValidData), MemberType = typeof(MyTestData))]
    public async Task Update_Should_CallUpdateOnce_WhenValidDataProvided(MyDto dto)
    {
        // Arrange
        using var fixture = new MongoRepositoryTestFixture<MyDocument>();
        fixture.SetupUpdateOneAsync(1, 1, null);
        var repository = new MyRepository(fixture.mongoClientMock.Object, fixture.settings);

        // Act
        await repository.Update(dto);

        // Assert
        fixture.collectionMock.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<MyDocument>>(),
            It.IsAny<UpdateDefinition<MyDocument>>(),
            null, default), Times.Once);
    }
}
```

## Tests parametrizados con `[Theory]`

Usar `TheoryData<T>` en una clase `static` separada en `Adapters/Helpers/`:

```csharp
public static class MyTestData
{
    public static TheoryData<MyDto> ValidData => new()
    {
        new MyDto { field = "value1" },
        new MyDto { field = "value2" },
    };
}
```

## Qué testear

| ✅ Testear | ❌ No testear |
|---|---|
| Use Cases (lógica de negocio) | `Program.cs` / wiring de DI |
| Controllers (HTTP status codes, delegación) | Modelos / DTOs sin lógica |
| Adapters / Repositories (MongoDB) | Constructores sin lógica |
| Manejo de excepciones (`ArgumentException`, timeouts) | Constantes y enums |

## Nunca hacer

- Tests que dependen del orden de ejecución.
- Llamadas reales a MongoDB, RabbitMQ o APIs externas.
- `Console.WriteLine` permanentes en tests.
- Lógica condicional (`if/else`) dentro de un `[Fact]` o `[Theory]`.
- Instanciar el SUT fuera de la propiedad `get =>` (rompe el aislamiento por test).
- Usar `Thread.Sleep` para sincronización (cero tests *flaky*).

---

> Para quality gates, pirámide de testing y nomenclatura Gherkin, ver `.github/docs/lineamientos/dev-guidelines.md` §7 y `.github/docs/lineamientos/qa-guidelines.md`.

## DoR / DoD de Automatización

**DoR**: caso ejecutado exitosamente en manual sin bugs críticos · datos identificados · ambiente estable · aprobación del equipo.  
**DoD**: código revisado por pares (pull request) · datos desacoplados del código · integrado al pipeline CI · trazabilidad hacia la HU.
- [ ] Con documentación y trazabilidad hacia la HU
