namespace ProductManager.Infrastructure.Seeding;

/// <summary>
/// The starter catalogue. Held as plain data separate from the seeding mechanics so the two can
/// change independently, and deliberately spread across the stock range — several products sit
/// at zero and several below their threshold — so every stock state is reachable in the UI the
/// moment the app first runs.
/// </summary>
internal static class SeedCatalogue
{
    internal sealed record SeedCategory(string Name, string Description);

    internal sealed record SeedProduct(
        string CategoryName,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        int LowStockThreshold);

    internal static IReadOnlyList<SeedCategory> Categories { get; } =
    [
        new("Audio", "Headphones, speakers and everything that makes a noise."),
        new("Computing", "Laptops, desktops and the parts that go inside them."),
        new("Mobile", "Phones, tablets and the accessories that outlive them."),
        new("Photography", "Cameras, lenses and lighting."),
        new("Peripherals", "Keyboards, mice, monitors and docks."),
        new("Home Office", "Desks, chairs and the rest of the working-from-home stack."),
    ];

    internal static IReadOnlyList<SeedProduct> Products { get; } =
    [
        new("Audio", "Aurora Over-Ear Headphones", "Closed-back wireless headphones with 40-hour battery life.", 249.99m, 42, 10),
        new("Audio", "Cadence Wireless Earbuds", "Compact earbuds with active noise cancellation.", 129.50m, 8, 10),
        new("Audio", "Resonance Bookshelf Speakers", "Pair of two-way bookshelf speakers in walnut.", 439.00m, 0, 5),
        new("Audio", "Podium USB Microphone", "Cardioid condenser microphone with desk stand.", 89.95m, 63, 15),

        new("Computing", "Meridian 14 Ultrabook", "14-inch magnesium chassis laptop, 16GB RAM, 512GB SSD.", 1249.00m, 17, 5),
        new("Computing", "Meridian 16 Workstation", "16-inch mobile workstation with discrete graphics.", 2199.00m, 4, 5),
        new("Computing", "Forge Mini Desktop", "Compact desktop for light development workloads.", 749.00m, 23, 8),
        new("Computing", "Vertex 2TB NVMe Drive", "PCIe 4.0 solid-state drive with heatsink.", 189.99m, 96, 20),
        new("Computing", "Vertex 32GB Memory Kit", "Dual-channel DDR5 memory kit, 6000MT/s.", 134.75m, 0, 12),

        new("Mobile", "Halo 7 Smartphone", "6.1-inch OLED handset with three-year update promise.", 899.00m, 31, 10),
        new("Mobile", "Halo 7 Pro Smartphone", "6.7-inch handset with periscope telephoto camera.", 1149.00m, 12, 10),
        new("Mobile", "Slate 11 Tablet", "11-inch tablet with detachable keyboard folio.", 649.00m, 19, 6),
        new("Mobile", "Tether 65W Charger", "Compact gallium-nitride charger with two USB-C ports.", 54.99m, 140, 25),
        new("Mobile", "Tether Braided Cable 2m", "USB-C to USB-C cable rated for 240W.", 19.99m, 3, 30),

        new("Photography", "Lumen X100 Mirrorless Body", "Full-frame mirrorless body with in-body stabilisation.", 1799.00m, 6, 3),
        new("Photography", "Lumen 35mm f/1.8 Lens", "Fast prime lens with weather sealing.", 549.00m, 14, 5),
        new("Photography", "Lumen 70-200mm f/2.8 Lens", "Telephoto zoom with dual linear motors.", 2099.00m, 0, 2),
        new("Photography", "Steadfast Carbon Tripod", "Four-section carbon-fibre tripod with ball head.", 329.00m, 27, 8),

        new("Peripherals", "Tactile 87 Mechanical Keyboard", "Tenkeyless board with hot-swappable switches.", 159.00m, 55, 15),
        new("Peripherals", "Glide Precision Mouse", "Lightweight wireless mouse with 8kHz polling.", 79.99m, 71, 20),
        new("Peripherals", "Panorama 34 Ultrawide Monitor", "34-inch curved ultrawide, 3440x1440 at 144Hz.", 899.00m, 9, 4),
        new("Peripherals", "Junction Thunderbolt Dock", "11-port dock with 96W power delivery.", 279.00m, 2, 6),

        new("Home Office", "Altitude Standing Desk", "Electric sit-stand desk with memory presets.", 649.00m, 11, 4),
        new("Home Office", "Posture Ergonomic Chair", "Mesh-back task chair with adjustable lumbar support.", 529.00m, 0, 4),
        new("Home Office", "Beam Monitor Light Bar", "Asymmetric screen bar with automatic brightness.", 109.00m, 38, 12),
    ];
}
