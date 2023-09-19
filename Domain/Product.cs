using CalculadoraMinimal.Domain.Enums;

namespace CalculadoraMinimal.Domain
{
    public class Product
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? DeliveryPrice { get; set; }
        public Currency Currency { get; set; }
        
    }
}
