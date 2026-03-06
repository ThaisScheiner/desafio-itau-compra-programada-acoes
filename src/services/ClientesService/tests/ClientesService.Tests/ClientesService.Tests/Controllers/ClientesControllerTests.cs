using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Exceptions;
using ClientesService.Api.Controllers;
using ClientesService.Api.Controllers.Dto;
using ClientesService.Api.Controllers.Requests;
using ClientesService.Api.Domain.Entities;
using ClientesService.Api.Infrastructure.HttpClients.Dto;
using ClientesService.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ClientesService.Tests.Controllers;

public sealed class ClientesControllerTests
{
    [Fact]
    public async Task Adesao_DeveCriarClienteEContaGrafica_QuandoRequestForValido()
    {
        // Arrange
        var db = CreateDbContext();
        var httpClientFactory = CreateHttpClientFactory();

        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AdesaoRequest
        {
            Nome = "Joao da Silva",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 3000m
        };

        // Act
        var result = await controller.Adesao(request, CancellationToken.None);

        // Assert
        var created = result as CreatedResult;
        created.Should().NotBeNull();

        db.Clientes.Count().Should().Be(1);
        db.ContasGraficas.Count().Should().Be(1);

        var cliente = await db.Clientes.Include(x => x.ContaGrafica).FirstAsync();
        cliente.Nome.Should().Be("Joao da Silva");
        cliente.CPF.Should().Be("12345678901");
        cliente.Email.Should().Be("joao@email.com");
        cliente.ValorMensal.Should().Be(3000m);
        cliente.Ativo.Should().BeTrue();
        cliente.ContaGrafica.Should().NotBeNull();
        cliente.ContaGrafica!.NumeroConta.Should().StartWith("FLH-");
        cliente.ContaGrafica.Tipo.Should().Be("FILHOTE");
    }

    [Fact]
    public async Task Adesao_DeveLancarDomainException_QuandoCpfForInvalido()
    {
        // Arrange
        var db = CreateDbContext();
        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AdesaoRequest
        {
            Nome = "Joao",
            Cpf = "123",
            Email = "joao@email.com",
            ValorMensal = 1000m
        };

        // Act
        Func<Task> act = async () => await controller.Adesao(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("CPF_INVALIDO");
    }

    [Fact]
    public async Task Adesao_DeveLancarDomainException_QuandoValorMensalForMenorQue100()
    {
        // Arrange
        var db = CreateDbContext();
        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AdesaoRequest
        {
            Nome = "Joao",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 99m
        };

        // Act
        Func<Task> act = async () => await controller.Adesao(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("VALOR_MENSAL_INVALIDO");
    }

    [Fact]
    public async Task Adesao_DeveLancarDomainException_QuandoCpfJaExistir()
    {
        // Arrange
        var db = CreateDbContext();

        db.Clientes.Add(new Cliente
        {
            Nome = "Maria",
            CPF = "12345678901",
            Email = "maria@email.com",
            ValorMensal = 2000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AdesaoRequest
        {
            Nome = "Joao",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 3000m
        };

        // Act
        Func<Task> act = async () => await controller.Adesao(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("CLIENTE_CPF_DUPLICADO");
    }

    [Fact]
    public async Task Saida_DeveInativarCliente_QuandoClienteExistirEEstiverAtivo()
    {
        // Arrange
        var db = CreateDbContext();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Joao",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        // Act
        var result = await controller.Saida(1, CancellationToken.None);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();

        var cliente = await db.Clientes.FirstAsync();
        cliente.Ativo.Should().BeFalse();
        cliente.DataSaida.Should().NotBeNull();
    }

    [Fact]
    public async Task Saida_DeveLancarDomainException_QuandoClienteNaoExistir()
    {
        // Arrange
        var db = CreateDbContext();
        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        // Act
        Func<Task> act = async () => await controller.Saida(999, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("CLIENTE_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task AlterarValorMensal_DeveAtualizarValor_QuandoRequestForValido()
    {
        // Arrange
        var db = CreateDbContext();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Joao",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AlterarValorMensalRequest
        {
            NovoValorMensal = 6000m
        };

        // Act
        var result = await controller.AlterarValorMensal(1, request, CancellationToken.None);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();

        var cliente = await db.Clientes.FirstAsync();
        cliente.ValorMensal.Should().Be(6000m);
    }

    [Fact]
    public async Task AlterarValorMensal_DeveLancarDomainException_QuandoNovoValorForInvalido()
    {
        // Arrange
        var db = CreateDbContext();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Joao",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        var request = new AlterarValorMensalRequest
        {
            NovoValorMensal = 50m
        };

        // Act
        Func<Task> act = async () => await controller.AlterarValorMensal(1, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("VALOR_MENSAL_INVALIDO");
    }

    [Fact]
    public async Task Ativos_DeveRetornarSomenteClientesAtivos()
    {
        // Arrange
        var db = CreateDbContext();

        db.Clientes.AddRange(
            new Cliente
            {
                Id = 1,
                Nome = "Joao",
                CPF = "12345678901",
                Email = "joao@email.com",
                ValorMensal = 1000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            },
            new Cliente
            {
                Id = 2,
                Nome = "Maria",
                CPF = "98765432100",
                Email = "maria@email.com",
                ValorMensal = 2000m,
                Ativo = false,
                DataAdesao = DateTime.UtcNow,
                DataSaida = DateTime.UtcNow
            },
            new Cliente
            {
                Id = 3,
                Nome = "Pedro",
                CPF = "11122233344",
                Email = "pedro@email.com",
                ValorMensal = 1500m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var httpClientFactory = CreateHttpClientFactory();
        var controller = new ClientesController(db, httpClientFactory.Object);

        // Act
        var result = await controller.Ativos(CancellationToken.None);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();

        var json = JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("Joao");
        json.Should().Contain("Pedro");
        json.Should().NotContain("Maria");
    }

    private static ClientesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClientesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ClientesDbContext(options);
    }

    private static Mock<IHttpClientFactory> CreateHttpClientFactory()
    {
        var mock = new Mock<IHttpClientFactory>();

        var httpClient = new HttpClient(new FakeHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };

        mock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        return mock;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.ToLowerInvariant();
            var query = request.RequestUri!.Query.ToLowerInvariant();

            string json;

            if (path.Contains("/api/custodias/cliente/"))
            {
                json = """
                {
                  "clienteId": 1,
                  "posicoes": [
                    { "ticker": "PETR4", "quantidade": 10, "precoMedio": 30.00 },
                    { "ticker": "VALE3", "quantidade": 5, "precoMedio": 60.00 }
                  ]
                }
                """;
            }
            else if (path.Contains("/api/motor/aportes/"))
            {
                json = """
                {
                  "clienteId": 1,
                  "historicoAportes": [
                    { "data": "2026-03-05", "valor": 1000.00, "parcela": "1/3" },
                    { "data": "2026-03-15", "valor": 1000.00, "parcela": "2/3" }
                  ]
                }
                """;
            }
            else if (path.Contains("/api/cotacoes/fechamento/ultimo") && query.Contains("ticker=petr4"))
            {
                json = """
                {
                  "ticker": "PETR4",
                  "dataPregao": "2026-03-05T00:00:00Z",
                  "precoFechamento": 35.00
                }
                """;
            }
            else if (path.Contains("/api/cotacoes/fechamento/ultimo") && query.Contains("ticker=vale3"))
            {
                json = """
                {
                  "ticker": "VALE3",
                  "dataPregao": "2026-03-05T00:00:00Z",
                  "precoFechamento": 65.00
                }
                """;
            }
            else
            {
                json = "{}";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}