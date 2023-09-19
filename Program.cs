using CalculadoraMinimal.Domain;
using CalculadoraMinimal.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(c =>
{
    c.AllowAnyHeader();
    c.AllowAnyMethod();
    c.AllowAnyOrigin();

});

app.UseHttpsRedirection();

//.Services.AddCors();

app.UseCors(builder => builder
.AllowAnyOrigin()
.AllowAnyMethod()
.AllowAnyHeader()
);

app.MapPost("/productTax", async (Product product) =>
{
    try
    {
        string apiUrl = "https://economia.awesomeapi.com.br/json/daily/USD-BRL/?start_date=" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "&end_date=" + DateTime.Now.ToString("yyyyMMdd");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    if (product.Currency == Currency.Real)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        decimal exchangeRate = ExtractExchangeRate(json);

                        decimal priceInDollars = Convert.ToDecimal(product.Price) / exchangeRate;
                        decimal deliveryCostInDollars = Convert.ToDecimal(product.DeliveryPrice) / exchangeRate;
                        decimal totalCost = (priceInDollars + deliveryCostInDollars);

                        var taxProduct = new TaxProduct
                        {
                            Tax = totalCost < 50 ? (totalCost * 0.17M) - totalCost : (totalCost * 0.17M) + (totalCost * 0.60M) - totalCost,
                            TotalWithTax = totalCost < 50 ? totalCost * 0.17M : totalCost * 0.60M
                        };

                        return Results.Ok(taxProduct); // Retorna um resultado HTTP 200 OK com o JSON
                    }
                    else if (product.Currency == Currency.Dolar)
                    {
                        decimal totalCost = Convert.ToDecimal(product.Price + product.DeliveryPrice);

                        var taxProduct = new TaxProduct
                        {
                            Tax = totalCost < 50 ? (totalCost * 0.17M) - totalCost : (totalCost * 0.17M) + (totalCost * 0.60M) - totalCost,
                            TotalWithTax = totalCost < 50 ? totalCost * 0.17M : totalCost * 0.60M
                        };

                        return Results.Ok(taxProduct); // Retorna um resultado HTTP 200 OK com o JSON
                    }
                }

                // Se nenhuma condição for atendida ou houver um erro de status HTTP, retorne um JSON de erro.
                return Results.BadRequest(new { error = "Erro ao calcular o imposto." });
            }
            catch (HttpRequestException ex)
            {
                // Trate a exceção, registre-a ou retorne um JSON de erro.
                return Results.StatusCode(500);
            }
        }
    }
    catch (Exception ex)
    {
        // Trate a exceção, registre-a ou retorne um JSON de erro.
        return Results.StatusCode(500);
    }
})
.WithName("API")
.WithOpenApi();

static decimal ExtractExchangeRate(string json)
{
    int startIndex = json.IndexOf("\"high\":\"") + 8;
    int endIndex = json.IndexOf("\"", startIndex);
    string rateSubstring = json.Substring(startIndex, endIndex - startIndex).Replace(".", ",");
    decimal exchangeRate = decimal.Parse(rateSubstring);
    return exchangeRate;
}

app.Run();
