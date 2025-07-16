using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Inventory.Application.DTOs.Products
{
    public class CreateProductDto : IValidatableObject
    {
        /// <summary>
        /// Código único del producto (alfanumérico + guiones, en mayúsculas).
        /// </summary>
        [Required(ErrorMessage = "El SKU es obligatorio.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El SKU debe tener entre 3 y 20 caracteres.")]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "El SKU solo admite letras mayúsculas, números y guiones.")]
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Nombre amigable del producto.
        /// </summary>
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción opcional del producto.
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string? Description { get; set; }

        /// <summary>
        /// Precio de venta. No puede ser negativo.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Price { get; set; }

        /// <summary>
        /// Stock disponible en unidades. No puede ser negativo.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        /// <summary>
        /// Valoración media (0–5).
        /// </summary>
        [Range(0, 5, ErrorMessage = "El rating debe estar entre 0 y 5.")]
        public double Rating { get; set; } = 0;

        /// <summary>
        /// URL de la imagen principal.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Identificador de la categoría a la que pertenece el producto.
        /// </summary>
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Range(1, long.MaxValue, ErrorMessage = "CategoryId debe ser mayor que 0.")]
        public long CategoryId { get; set; }

        /// <summary>
        /// Validaciones de negocio adicionales que no se pueden expresar con atributos.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // SKU en mayúsculas (convención interna)
            if (!string.IsNullOrWhiteSpace(SKU) && SKU != SKU.ToUpperInvariant())
            {
                yield return new ValidationResult(
                    "El SKU debe estar en mayúsculas.", new[] { nameof(SKU) });
            }

            // Validar Rating: si hay stock 0, rating debería ser 0 (regla de negocio ejemplo)
            if (Stock == 0 && Rating > 0)
            {
                yield return new ValidationResult(
                    "No puede haber rating cuando el stock es 0.", new[] { nameof(Rating), nameof(Stock) });
            }

            // Validar que la URL sea HTTPS si existe
            if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                var isValidHttps = Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uriResult) &&
                                   uriResult!.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
                if (!isValidHttps)
                {
                    yield return new ValidationResult(
                        "La URL de la imagen debe usar HTTPS.", new[] { nameof(ImageUrl) });
                }
            }

            // Ejemplo de SKU más Price juntos (regla custom): precio mínimo depende del SKU
            // (Simple demo – remove or adapt to real business rule)
            if (Price == 0 && Regex.IsMatch(SKU, @"^PREMIUM-", RegexOptions.IgnoreCase))
            {
                yield return new ValidationResult(
                    "Los productos con SKU 'PREMIUM-' deben tener un precio mayor a 0.", new[] { nameof(Price) });
            }
        }
    }
}
