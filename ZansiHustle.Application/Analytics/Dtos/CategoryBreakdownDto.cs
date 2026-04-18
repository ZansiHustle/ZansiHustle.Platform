namespace ZansiHustle.Application.Analytics.Dtos
{
    /// <summary>
    /// Per-seller-category revenue share. <see cref="Value"/> is the percentage
    /// (0-100, rounded to whole numbers) of paid revenue this category accounts
    /// for. <see cref="Revenue"/> is the absolute ZAR total.
    /// </summary>
    public class CategoryBreakdownDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Revenue { get; set; }
    }
}
