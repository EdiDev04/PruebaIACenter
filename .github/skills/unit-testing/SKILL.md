---
name: unit-testing
description: Genera tests unitarios para la solución .NET (xUnit + Moq). Lee la spec y el código implementado. Requiere spec APPROVED e implementación completa.
argument-hint: "<nombre-feature> [usecase|controller|adapter|all]"
---

# Unit Testing — .NET 8 / xUnit + Moq

## Definition of Done

- [ ] Cobertura ≥ 80% en lógica de negocio (quality gate bloqueante)
- [ ] Tests 100% aislados — sin MongoDB real, sin HTTP externo (siempre mocks)
- [ ] Happy path + error path + edge cases cubiertos
- [ ] Los cambios no rompen contratos existentes del módulo

## Prerequisito — Lee en paralelo

```
.github/specs/<feature>.spec.md
código implementado en src/ReenviarDocElectronicos.{Domain,Infrastructure}/
.github/instructions/tests.instructions.md   (stack, convenciones, fixtures)
```

## Stack

| Componente | Tecnología |
|---|---|
| Framework | xUnit 2.x |
| Mocking | Moq 4.x |
| Cobertura | coverlet.collector |
| Target | .NET 8 |

## Estructura de archivos

```
src/ReenviarDocElectronicos.UnitTest/
  UseCase/       <FeatureName>UseCaseTest.cs
  Controllers/   <FeatureName>ControllerTest.cs
  Adapters/      <FeatureName>RepositoryTest.cs
  Mocks/         *.json   ← datos complejos para pruebas
```

## Convenciones

- Clase: `public class <Subject>Test`
- Método: `Method_Should_DoSomething_WhenCondition`
- SUT como propiedad `get =>` → instancia fresca por cada `[Fact]`
- Mocks declarados `private readonly Mock<IInterface> _mockX = new();`
- Comentarios `// Arrange` `// Act` `// Assert` siempre presentes

---

## Patrón — Use Case

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
        _settings = new Settings { /* props requeridas */ };

        // Act
        await Sut.Execute(/* params */);

        // Assert
        _mockRepository.Verify(x => x.Save(It.Is<MyDto>(d => d.Field == "expected")), Times.Once);
    }

    [Fact]
    public async Task Execute_Should_ThrowArgumentException_WhenRepositoryFails()
    {
        // Arrange
        _settings = new Settings { /* props requeridas */ };
        _mockRepository.Setup(x => x.Save(It.IsAny<MyDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => Sut.Execute(/* params */));
    }
}
```

## Patrón — Controller

```csharp
public class MyControllerTest
{
    private readonly Mock<IMyUseCase> _mockUseCase = new();
    private readonly Mock<ILogger<MyController>> _mockLogger = new();
    private ISettings _settings;

    private MyController Sut
    {
        get => new(_mockLogger.Object, _mockUseCase.Object, _settings);
        set { }
    }

    [Fact]
    public async Task Post_Should_Return200_WhenRequestIsValid()
    {
        // Arrange
        _settings = new Settings { /* props requeridas */ };
        var request = new MyRequest { /* valores válidos */ };

        // Act
        var response = (ObjectResult)await Sut.Post(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }
}
```

---

## Patrón — Adapter/Repository con MongoDB

### Setup del constructor

```csharp
public class MyRepositoryTest
{
    private readonly Mock<IMongoClient> _mockMongoClient = new();
    private readonly Mock<ISettings> _mockSettings = new();
    private readonly Mock<IMongoDatabase> _mockDatabase = new();
    private readonly Mock<IMongoCollection<MyDocument>> _mockCollection = new();
    private readonly Mock<IAsyncCursor<MyDocument>> _mockCursor = new();

    public MyRepositoryTest()
    {
        _mockSettings.Setup(x => x.MongoSettings).Returns(new MongoConnection
        {
            MongoDbName = "testdb",
            MongoCollectionName = "testcollection"
        });

        _mockMongoClient
            .Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(_mockDatabase.Object);

        _mockDatabase
            .Setup(x => x.GetCollection<MyDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_mockCollection.Object);
    }

    private MyRepository CreateRepository() =>
        new(_mockMongoClient.Object, _mockSettings.Object);
}
```

### Mockear FindAsync (lectura)

```csharp
// Con datos
_mockCursor.SetupSequence(x => x.Current).Returns(documentList);
_mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(true)
    .ReturnsAsync(false);

_mockCollection.Setup(x => x.FindAsync(
    It.IsAny<FilterDefinition<MyDocument>>(),
    It.IsAny<FindOptions<MyDocument, MyDocument>>(),
    It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockCursor.Object);

// Sin resultados — mismo patrón con lista vacía en Current
_mockCursor.SetupSequence(x => x.Current).Returns(new List<MyDocument>());
```

### Mockear escritura

```csharp
// InsertOneAsync
_mockCollection.Setup(x => x.InsertOneAsync(
    It.IsAny<MyDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

// UpdateOneAsync / UpdateManyAsync
var updateResult = new UpdateResult.Acknowledged(1, 1, null);
_mockCollection.Setup(x => x.UpdateOneAsync(
    It.IsAny<FilterDefinition<MyDocument>>(), It.IsAny<UpdateDefinition<MyDocument>>(),
    It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(updateResult);

// DeleteOneAsync — con validación de filtro
_mockCollection.Setup(x => x.DeleteOneAsync(
    It.IsAny<FilterDefinition<MyDocument>>(), It.IsAny<CancellationToken>()))
    .Callback<FilterDefinition<MyDocument>, CancellationToken>((filter, _) =>
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<MyDocument>();
        var rendered = filter.Render(new RenderArgs<MyDocument>(serializer, BsonSerializer.SerializerRegistry));
        Assert.Equal(expectedId, rendered["_id"].AsObjectId);
    })
    .Returns(Task.CompletedTask);
```

### Verificar llamadas

```csharp
_mockCollection.Verify(x => x.FindAsync(
    It.IsAny<FilterDefinition<MyDocument>>(),
    It.IsAny<FindOptions<MyDocument, MyDocument>>(),
    It.IsAny<CancellationToken>()), Times.Once);

_mockCollection.Verify(x => x.DeleteManyAsync(
    It.IsAny<FilterDefinition<MyDocument>>(),
    It.IsAny<CancellationToken>()), Times.Never);

_mockPublisher.Verify(x => x.PublishAsync(
    It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<CancellationToken>()), Times.Exactly(3));
```

---

## Datos de prueba

| Caso | Estrategia |
|---|---|
| Objetos simples (< 5 props) | Crear directamente en el test |
| Objetos complejos / anidados | Cargar desde `Mocks/<Entity>.json` |

```csharp
// Carga desde JSON (objetos complejos)
var json = File.ReadAllText("Mocks/InvoiceDian.json");
var settings = new JsonSerializerSettings { ContractResolver = InvoiceDataContractResolver.Instance };
var docs = JsonConvert.DeserializeObject<List<DocumentInvoiceDian>>(json, settings);
docs[0].Id = new ObjectId("64274f13fa588ffc3eae02f4");
```

Configurar los JSON para que se copien al output:
```xml
<ItemGroup>
  <None Update="Mocks\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Qué testear

| ✅ Testear | ❌ No testear |
|---|---|
| Use Cases (lógica de negocio) | `Program.cs` / wiring de DI |
| Controllers (HTTP status codes) | Modelos / DTOs sin lógica |
| Adapters / Repositories (MongoDB) | Constructores sin lógica |

## Restricciones

- Solo modificar archivos en `ReenviarDocElectronicos.UnitTest/`. Nunca tocar código fuente.
- `Find` y `Project` son métodos de extensión — mockear `FindAsync` en su lugar.
- No mockear tipos internos sellados de MongoDB (ej. `ConnectionId`); lanzar la excepción directamente en el setup.
