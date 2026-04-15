using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Static seed content for UAT/dev. Mirrors the shape of the mobile app's
    /// previously-deleted <c>mocks/seed.data.ts</c> — same realistic SA
    /// marketplace flavour, but structured for the real backend entities.
    /// </summary>
    internal static class UatSeedContent
    {
        public const string UatPassword = "UatPass1!";

        // ─── Categories (GUIDs match SeedSellerCategoriesData migration) ─────
        public static class Categories
        {
            public static readonly Guid Beauty         = new("11111111-1111-1111-1111-111111111111");
            public static readonly Guid Food           = new("22222222-2222-2222-2222-222222222222");
            public static readonly Guid Fashion        = new("33333333-3333-3333-3333-333333333333");
            public static readonly Guid HomeServices   = new("44444444-4444-4444-4444-444444444444");
            public static readonly Guid Repairs        = new("55555555-5555-5555-5555-555555555555");
            public static readonly Guid Electronics    = new("66666666-6666-6666-6666-666666666666");
            public static readonly Guid Health         = new("77777777-7777-7777-7777-777777777777");
            public static readonly Guid Photography    = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            public static readonly Guid Retail         = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            public static readonly Guid HomeLiving     = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
            public static readonly Guid Automotive     = new("bbbbbbbb-2222-bbbb-2222-bbbbbbbb2222");
        }

        // ─── Sellers ─────────────────────────────────────────────────────────
        public sealed record SeedSeller(
            string Email,
            string FirstName,
            string LastName,
            string Phone,
            string ShopName,
            string ShopDescription,
            Guid CategoryId,
            string City,
            string Province,
            string LogoUrl);

        public static readonly IReadOnlyList<SeedSeller> Sellers = new List<SeedSeller>
        {
            new("seller1@zh.com", "Thandi", "Mokoena", "+27821110001",
                "Urban Threads",
                "Streetwear, sneakers, and accessories for the modern hustler.",
                Categories.Fashion, "Sandton", "Gauteng",
                "https://images.unsplash.com/photo-1441984904996-e0b6ba687e04?w=240&q=80"),

            new("seller2@zh.com", "Sipho", "Ndlovu", "+27821110002",
                "Zansi Tech Hub",
                "Certified pre-loved phones, laptops, and gadgets.",
                Categories.Electronics, "Pretoria", "Gauteng",
                "https://images.unsplash.com/photo-1512054502232-10a0a035d672?w=240&q=80"),

            new("seller3@zh.com", "Naledi", "Khumalo", "+27821110003",
                "Glow Beauty SA",
                "Natural hair care, makeup and skincare — proudly SA.",
                Categories.Beauty, "Johannesburg", "Gauteng",
                "https://images.unsplash.com/photo-1522335789203-aaa3de9de3a5?w=240&q=80"),

            new("seller4@zh.com", "Kabelo", "Dlamini", "+27821110004",
                "FixIt Pros",
                "Trusted home repair and maintenance across Gauteng.",
                Categories.HomeServices, "Pretoria East", "Gauteng",
                "https://images.unsplash.com/photo-1581578731548-c64695cc6952?w=240&q=80"),

            new("seller5@zh.com", "Lerato", "Mthembu", "+27821110005",
                "Fresh Basket",
                "Organic produce, free-range eggs and local artisan goods.",
                Categories.Food, "Midrand", "Gauteng",
                "https://images.unsplash.com/photo-1542838132-92c53300491e?w=240&q=80"),

            new("seller6@zh.com", "Bongani", "Zulu", "+27821110006",
                "Motor Mate",
                "Car care essentials, accessories and at-home services.",
                Categories.Automotive, "Durban", "KwaZulu-Natal",
                "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=240&q=80"),

            new("seller7@zh.com", "Palesa", "Motaung", "+27821110007",
                "Palesa Photography",
                "Weddings, events and family portraits across SA.",
                Categories.Photography, "Cape Town", "Western Cape",
                "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?w=240&q=80"),

            new("seller8@zh.com", "Tshepo", "Molefe", "+27821110008",
                "Home Haven",
                "Handwoven homeware, decor and handmade furniture.",
                Categories.HomeLiving, "Bloemfontein", "Free State",
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=240&q=80"),

            new("seller9@zh.com", "Nomsa", "Sithole", "+27821110009",
                "KasiBites",
                "Authentic township flavours — delivered fresh.",
                Categories.Food, "Soweto", "Gauteng",
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=240&q=80"),

            new("seller10@zh.com", "Ayanda", "Nkosi", "+27821110010",
                "Hustle Gym",
                "Fitness gear and personal training for hustlers.",
                Categories.Health, "Centurion", "Gauteng",
                "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=240&q=80")
        };

        // ─── Buyers ──────────────────────────────────────────────────────────
        public sealed record SeedBuyer(string Email, string FirstName, string LastName, string Phone);

        public static readonly IReadOnlyList<SeedBuyer> Buyers = new List<SeedBuyer>
        {
            new("buyer1@zh.com",  "Busisiwe", "Dube",      "+27832220001"),
            new("buyer2@zh.com",  "Lindiwe",  "Maseko",    "+27832220002"),
            new("buyer3@zh.com",  "Thabo",    "Ngwenya",   "+27832220003"),
            new("buyer4@zh.com",  "Zanele",   "Khoza",     "+27832220004"),
            new("buyer5@zh.com",  "Mbali",    "Radebe",    "+27832220005"),
            new("buyer6@zh.com",  "Sizwe",    "Mthethwa",  "+27832220006"),
            new("buyer7@zh.com",  "Refilwe",  "Gumede",    "+27832220007"),
            new("buyer8@zh.com",  "Xolani",   "Mhlongo",   "+27832220008"),
            new("buyer9@zh.com",  "Karabo",   "Motaung",   "+27832220009"),
            new("buyer10@zh.com", "Dineo",    "Mahlangu",  "+27832220010")
        };

        // ─── Products (shopIndex is 1-based, mapping to Sellers[shopIndex-1]) ─
        public sealed record SeedProduct(
            int ShopIndex,
            string Title,
            string Description,
            decimal Price,
            int Stock,
            ListingCondition Condition,
            Guid CategoryId,
            IReadOnlyList<string> Images);

        public static readonly IReadOnlyList<SeedProduct> Products = new List<SeedProduct>
        {
            // Urban Threads (seller1 / Fashion)
            new(1, "Handmade Leather Bag",      "Full-grain oiled leather crossbody, hand-stitched in Joburg.",        850m, 3,  ListingCondition.New,     Categories.Fashion,     new[]{ "https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=600" }),
            new(1, "Custom Print T-Shirt",      "180gsm cotton, screen-printed with local art.",                        220m, 25, ListingCondition.New,     Categories.Fashion,     new[]{ "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600" }),
            new(1, "Ankara Print Dress",        "Midi dress with bold Ankara print. Available S-XL.",                   650m, 12, ListingCondition.New,     Categories.Fashion,     new[]{ "https://images.unsplash.com/photo-1539008835657-9e8e9680c956?w=600" }),
            new(1, "Limited Edition Sneakers",  "Low-top sneakers, boxed and numbered. Sizes 6-11.",                   1250m, 8,  ListingCondition.New,     Categories.Fashion,     new[]{ "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600" }),
            new(1, "Denim Jacket",              "Relaxed-fit denim jacket, stone-washed.",                              750m, 10, ListingCondition.New,     Categories.Fashion,     new[]{ "https://images.unsplash.com/photo-1516257984-b1b4d707412e?w=600" }),

            // Zansi Tech Hub (seller2 / Electronics)
            new(2, "iPhone 13 (Pre-loved)",     "Like-new condition. 128GB, battery health 92%, original box.",       12500m, 2,  ListingCondition.LikeNew, Categories.Electronics, new[]{ "https://images.unsplash.com/photo-1632661674596-df8be070a5c5?w=600" }),
            new(2, "PlayStation 5 + 2 Pads",    "Slim edition, used for 3 months, includes 2 controllers.",           14000m, 1,  ListingCondition.LikeNew, Categories.Electronics, new[]{ "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=600" }),
            new(2, "Samsung Galaxy Tab A8",     "Sealed box. 64GB storage, 10.5\" display.",                           6800m, 4,  ListingCondition.New,     Categories.Electronics, new[]{ "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600" }),
            new(2, "Wireless Earbuds",          "Active noise cancellation, 6hr playback.",                             450m, 30, ListingCondition.New,     Categories.Electronics, new[]{ "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=600" }),
            new(2, "Mechanical Keyboard",       "Hot-swappable switches, RGB, TKL layout.",                           1200m, 6,  ListingCondition.New,     Categories.Electronics, new[]{ "https://images.unsplash.com/photo-1586298611998-eeba8fcdef0c?w=600" }),

            // Glow Beauty SA (seller3 / Beauty)
            new(3, "Natural Hair Care Kit",     "Shampoo + conditioner + leave-in, sulfate-free.",                      350m, 20, ListingCondition.New,     Categories.Beauty,      new[]{ "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=600" }),
            new(3, "Makeup Starter Set",        "Foundation, concealer, powder, mascara.",                              890m, 8,  ListingCondition.New,     Categories.Beauty,      new[]{ "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600" }),
            new(3, "Nail Polish Bundle",        "8 trending shades, long-wear formula.",                                180m, 35, ListingCondition.New,     Categories.Beauty,      new[]{ "https://images.unsplash.com/photo-1604654894610-df63bc536371?w=600" }),
            new(3, "Fragrance Oil Sampler",     "6 signature scents, 10ml each.",                                       420m, 15, ListingCondition.New,     Categories.Beauty,      new[]{ "https://images.unsplash.com/photo-1541643600914-78b084683601?w=600" }),

            // Home Haven (seller8 / HomeLiving)
            new(8, "Handwoven Throw Blanket",   "100% wool, locally woven. 150x200cm.",                                 850m, 6,  ListingCondition.New,     Categories.HomeLiving,  new[]{ "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=600" }),
            new(8, "Ceramic Plant Pots (3)",    "Matte terracotta, drainage included.",                                 290m, 18, ListingCondition.New,     Categories.HomeLiving,  new[]{ "https://images.unsplash.com/photo-1485955900006-10f4d324d411?w=600" }),
            new(8, "Wall Art Canvas",           "Abstract SA landscape, 60x90cm, framed.",                              550m, 9,  ListingCondition.New,     Categories.HomeLiving,  new[]{ "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?w=600" }),
            new(8, "LED Desk Lamp",             "Adjustable colour temp, USB charging port.",                           340m, 12, ListingCondition.New,     Categories.HomeLiving,  new[]{ "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=600" }),

            // Fresh Basket (seller5 / Food)
            new(5, "Organic Veggie Box",        "Weekly seasonal veg from Midrand farms. Serves 3-4.",                  280m, 40, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1542838132-92c53300491e?w=600" }),
            new(5, "Free-Range Eggs (30)",      "Pasture-raised, brown and white mix.",                                 110m, 50, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1587486913049-53fc88980cfc?w=600" }),
            new(5, "Homemade Biltong 500g",     "Premium beef, air-dried with spices.",                                 260m, 20, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1544025162-d76694265947?w=600" }),
            new(5, "Artisan Sourdough",         "Hand-folded, long-ferment loaf.",                                       85m, 15, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=600" }),

            // KasiBites (seller9 / Food)
            new(9, "Kota Meal Deal (x4)",       "Classic bread roll kota, choose your fillings. Pack of 4.",            220m, 30, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600" }),
            new(9, "Dried Boerewors",           "Traditional recipe, vac-packed.",                                      180m, 25, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1544025162-d76694265947?w=600" }),
            new(9, "Mageu Pack (6)",            "Traditional maize drink, 6 x 500ml.",                                   72m, 40, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1625938146568-1d2c6fd90dcd?w=600" }),
            new(9, "Spaza Essentials Combo",    "Maize meal, oil, sugar, tea — family pack.",                           450m, 18, ListingCondition.New,     Categories.Food,        new[]{ "https://images.unsplash.com/photo-1542838132-92c53300491e?w=600" }),

            // Motor Mate (seller6 / Automotive)
            new(6, "Car Battery Booster",       "12V jump starter, 2000A peak, USB output.",                            950m, 7,  ListingCondition.New,     Categories.Automotive,  new[]{ "https://images.unsplash.com/photo-1487754180451-c456f719a1fc?w=600" }),
            new(6, "Dashboard Camera",          "1080p dashcam with loop recording.",                                  1200m, 5,  ListingCondition.New,     Categories.Automotive,  new[]{ "https://images.unsplash.com/photo-1544636331-e26879cd4d9b?w=600" }),
            new(6, "Floor Mats Set",            "Heavy-duty rubber floor mats. Universal fit.",                         380m, 20, ListingCondition.New,     Categories.Automotive,  new[]{ "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=600" }),
            new(6, "Wheel Covers (4)",          "15\" alloy-look wheel covers, set of 4.",                              450m, 12, ListingCondition.New,     Categories.Automotive,  new[]{ "https://images.unsplash.com/photo-1600661653561-629509216228?w=600" }),

            // Hustle Gym (seller10 / Health)
            new(10, "Resistance Bands Set",     "5 resistance levels, door anchor, carry bag.",                         220m, 35, ListingCondition.New,     Categories.Health,      new[]{ "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=600" }),
            new(10, "Yoga Mat Premium",         "6mm TPE, non-slip, carry strap.",                                      390m, 20, ListingCondition.New,     Categories.Health,      new[]{ "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=600" }),
            new(10, "Adjustable Dumbbells",     "Pair, 2-20kg range. Space-saving.",                                   1800m, 4,  ListingCondition.New,     Categories.Health,      new[]{ "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=600" }),
            new(10, "Skipping Rope",            "Weighted handles, adjustable length.",                                  95m, 60, ListingCondition.New,     Categories.Health,      new[]{ "https://images.unsplash.com/photo-1622454655738-f0f3c32c5d87?w=600" })
        };

        // ─── Services ────────────────────────────────────────────────────────
        public sealed record SeedService(
            int ShopIndex,
            string Title,
            string Description,
            decimal Price,
            PricingModel PricingModel,
            Guid CategoryId,
            string? ServiceArea,
            IReadOnlyList<string> Availability,
            IReadOnlyList<string> BookingMethods,
            IReadOnlyList<string> Images);

        public static readonly IReadOnlyList<SeedService> Services = new List<SeedService>
        {
            // FixIt Pros (seller4 / HomeServices)
            new(4, "House Deep Clean",             "Full 2-3 bed house deep clean (kitchen, bathrooms, floors).", 450m, PricingModel.Fixed,       Categories.HomeServices, "Greater Johannesburg", new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp", "Phone Call" }, new[]{ "https://images.unsplash.com/photo-1581578731548-c64695cc6952?w=600" }),
            new(4, "Plumbing Emergency Callout",   "Same-day plumbing call-out for blockages and leaks.",         350m, PricingModel.Hourly,      Categories.Repairs,      "Pretoria East",        new[]{ "24/7" },                  new[]{ "Phone Call" },             new[]{ "https://images.unsplash.com/photo-1607472586893-edb57bdc0e39?w=600" }),
            new(4, "Electrical Fault Diagnosis",   "Certified electrician, fault-finding and minor repairs.",     400m, PricingModel.Hourly,      Categories.Repairs,      "Greater Pretoria",     new[]{ "Weekdays" },              new[]{ "Phone Call", "WhatsApp" }, new[]{ "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=600" }),
            new(4, "Garden Maintenance",           "Lawn, hedges, beds. Monthly maintenance available.",          300m, PricingModel.Fixed,       Categories.HomeServices, "Sandton & Midrand",    new[]{ "Weekends" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=600" }),
            new(4, "Appliance Repair",             "Fridge, washing machine, tumble dryer diagnostics.",          0m,   PricingModel.Quote,       Categories.Repairs,      "Pretoria",             new[]{ "Weekdays" },              new[]{ "Phone Call" },             new[]{ "https://images.unsplash.com/photo-1581578731548-c64695cc6952?w=600" }),
            new(4, "Interior Painting",            "Room painting (prep + 2 coats). Per room.",                   0m,   PricingModel.Negotiable,  Categories.HomeServices, "Gauteng",              new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1562259949-e8e7689d7828?w=600" }),

            // Palesa Photography (seller7 / Photography)
            new(7, "Wedding Photography",          "Full-day wedding coverage, 400+ edited images.",              3500m, PricingModel.Fixed,       Categories.Photography,  "Cape Town & WC",       new[]{ "Weekends" },              new[]{ "Email", "WhatsApp" },      new[]{ "https://images.unsplash.com/photo-1519741497674-611481863552?w=600" }),
            new(7, "Family Portrait Session",      "1hr on-location shoot, 20 retouched images.",                  800m, PricingModel.Fixed,       Categories.Photography,  "Western Cape",         new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1511895426328-dc8714191300?w=600" }),
            new(7, "Event Coverage",               "Corporate events, parties, launches.",                         450m, PricingModel.Hourly,      Categories.Photography,  "Cape Town",            new[]{ "Weekdays", "Weekends" }, new[]{ "Email" },                  new[]{ "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=600" }),
            new(7, "Graduation Photoshoot",        "2hr session on campus. Includes gown prep.",                   650m, PricingModel.Fixed,       Categories.Photography,  "Stellenbosch / UCT",   new[]{ "Weekends" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=600" }),
            new(7, "Instagram Content Shoot",      "Lifestyle content for creators and brands.",                   800m, PricingModel.Fixed,       Categories.Photography,  "Cape Town",            new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1556761175-4b46a572b786?w=600" }),

            // Urban Threads (seller1 / Fashion services)
            new(1, "Custom Tailoring",             "Made-to-measure garments. Fitting + delivery.",                300m, PricingModel.Fixed,       Categories.Fashion,      "Johannesburg",         new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?w=600" }),
            new(1, "Clothing Alterations",         "Hems, resizing, repairs. Fast turnaround.",                    150m, PricingModel.Hourly,      Categories.Fashion,      "Sandton",              new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?w=600" }),

            // Zansi Tech Hub (seller2 / Electronics services)
            new(2, "Phone Screen Repair",          "Most brands, same-day turnaround.",                            0m,   PricingModel.Quote,       Categories.Electronics,  "Pretoria",             new[]{ "Weekdays", "Weekends" }, new[]{ "Phone Call", "WhatsApp" }, new[]{ "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600" }),
            new(2, "Laptop Diagnostic",            "Drop-off diagnostic with written report.",                     250m, PricingModel.Fixed,       Categories.Electronics,  "Pretoria",             new[]{ "Weekdays" },              new[]{ "Email" },                  new[]{ "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=600" }),
            new(2, "Data Recovery",                "HDD / SSD / USB data recovery. No-recovery-no-fee.",           0m,   PricingModel.Quote,       Categories.Electronics,  "Nationwide",           new[]{ "Weekdays" },              new[]{ "Email" },                  new[]{ "https://images.unsplash.com/photo-1580894732444-8ecded7900cd?w=600" }),

            // Glow Beauty SA (seller3 / Beauty services)
            new(3, "Makeup Application",           "Professional makeup for events and dates.",                    450m, PricingModel.Fixed,       Categories.Beauty,       "Joburg Northern Subs", new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600" }),
            new(3, "Bridal Makeup Trial",          "Trial session before your big day.",                           600m, PricingModel.Fixed,       Categories.Beauty,       "Johannesburg",         new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1522337660859-02fbefca4702?w=600" }),
            new(3, "Lash Extensions",              "Classic or hybrid lashes, full set.",                          550m, PricingModel.Fixed,       Categories.Beauty,       "Sandton",              new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1522337094846-8a818192de1f?w=600" }),
            new(3, "Nail Art Session",             "Gel nails with custom art.",                                   250m, PricingModel.Fixed,       Categories.Beauty,       "Sandton",              new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1604654894610-df63bc536371?w=600" }),

            // Home Haven (seller8 / Home & Living services)
            new(8, "Interior Consultation",        "1hr home visit + mood board.",                                 500m, PricingModel.Hourly,      Categories.HomeLiving,   "Free State",           new[]{ "Weekdays" },              new[]{ "Email" },                  new[]{ "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=600" }),
            new(8, "Furniture Assembly",           "Flat-pack assembly with tools and care.",                      280m, PricingModel.Fixed,       Categories.HomeLiving,   "Bloemfontein",         new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1533090481720-856c6e3c1fdc?w=600" }),

            // Motor Mate (seller6 / Automotive services)
            new(6, "Mobile Car Wash",              "At-home wash + interior vacuum.",                              180m, PricingModel.Fixed,       Categories.Automotive,   "Durban North",         new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=600" }),
            new(6, "Oil Change at Home",           "Filter + oil, genuine parts, 30min.",                          420m, PricingModel.Fixed,       Categories.Automotive,   "Durban Metro",         new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=600" }),
            new(6, "Battery Replacement",          "We come to you with the right battery.",                       0m,   PricingModel.Quote,       Categories.Automotive,   "Durban Metro",         new[]{ "24/7" },                  new[]{ "Phone Call" },             new[]{ "https://images.unsplash.com/photo-1487754180451-c456f719a1fc?w=600" }),

            // Hustle Gym (seller10 / Health services)
            new(10, "Personal Training Session",   "1-on-1 training session, customised programme.",               350m, PricingModel.Hourly,      Categories.Health,       "Centurion",            new[]{ "Weekdays", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=600" }),
            new(10, "Group Fitness Class",         "HIIT/bootcamp, small group, 1hr.",                             120m, PricingModel.Fixed,       Categories.Health,       "Centurion Gym",        new[]{ "Evenings", "Weekends" }, new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=600" }),
            new(10, "Nutrition Coaching",          "Weekly check-ins, meal plan and accountability.",              0m,   PricingModel.Negotiable,  Categories.Health,       "Online & In-person",   new[]{ "Weekdays" },              new[]{ "Email", "WhatsApp" },      new[]{ "https://images.unsplash.com/photo-1490818387583-1baba5e638af?w=600" }),
            new(10, "Sports Massage",              "45min deep-tissue recovery.",                                  400m, PricingModel.Fixed,       Categories.Health,       "Centurion",            new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=600" }),
            new(10, "Kids Karate Class",           "Group class for ages 6-12, beginners welcome.",                150m, PricingModel.Fixed,       Categories.Health,       "Centurion",            new[]{ "Weekends" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1599058917212-d750089bc07e?w=600" }),

            // Fresh Basket (seller5 / Food service)
            new(5, "Weekly Veggie Box Delivery",   "Doorstep delivery every Friday.",                              100m, PricingModel.Fixed,       Categories.Food,         "Midrand",              new[]{ "Weekdays" },              new[]{ "WhatsApp" },               new[]{ "https://images.unsplash.com/photo-1542838132-92c53300491e?w=600" }),

            // KasiBites (seller9 / Food service)
            new(9, "Event Catering",               "Kasi-flavour catering for parties and events.",                0m,   PricingModel.Quote,       Categories.Food,         "Soweto & Joburg",      new[]{ "Weekends" },              new[]{ "Phone Call", "WhatsApp" }, new[]{ "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=600" })
        };

        // ─── Orders ──────────────────────────────────────────────────────────
        public sealed record SeedOrder(
            int BuyerIndex,
            int ShopIndex,
            int ListingIndex,        // 0-based index within the shop's listings (products + services combined, products first)
            bool IsService,          // selects the listing from Products or Services list
            int Quantity,
            OrderStatus Status,
            int CreatedDaysAgo,
            string? DeliveryAddress,
            string? Notes);

        /// <summary>
        /// 15 orders across all 5 statuses (3 each). Designed so:
        /// - buyer1 places orders with 5 different sellers
        /// - seller1 (Urban Threads) receives 4 orders from 4 different buyers
        /// - includes a mix of product and service orders
        /// </summary>
        public static readonly IReadOnlyList<SeedOrder> Orders = new List<SeedOrder>
        {
            // seller1 Urban Threads — 4 incoming orders, 4 different buyers
            new(1, 1, 0, false, 1, OrderStatus.Pending,    1,  "12 Jan Smuts Ave, Rosebank", "Please call on arrival."),
            new(2, 1, 1, false, 2, OrderStatus.Confirmed,  3,  "45 Main Rd, Sandton",        null),
            new(3, 1, 2, false, 1, OrderStatus.InProgress, 5,  "7 Oxford Rd, Melrose",       "Medium size, please."),
            new(4, 1, 3, false, 1, OrderStatus.Completed,  14, "33 Rivonia Rd, Morningside", null),

            // buyer1 placing orders with multiple sellers
            new(1, 2, 0, false, 1, OrderStatus.Cancelled,  10, null,                         "Changed my mind."),
            new(1, 3, 0, true,  1, OrderStatus.Pending,    2,  null,                         "Book for Saturday."),
            new(1, 4, 0, true,  1, OrderStatus.Confirmed,  4,  "12 Jan Smuts Ave, Rosebank", "House: 120sqm."),
            new(1, 5, 0, false, 2, OrderStatus.InProgress, 1,  "12 Jan Smuts Ave, Rosebank", "Leave at reception."),

            // Broader spread
            new(5, 4, 1, true,  1, OrderStatus.InProgress, 2,  "9 Main Rd, Arcadia",         "Leaking kitchen tap."),
            new(6, 7, 1, true,  1, OrderStatus.Completed,  20, null,                         "Family photos went great."),
            new(7, 6, 0, false, 1, OrderStatus.Cancelled,  6,  null,                         "Found a better deal."),
            new(8, 10, 0, true, 1, OrderStatus.Pending,    1,  null,                         "Preferred: Saturday morning."),
            new(9, 8, 0, false, 1, OrderStatus.Confirmed,  3,  "30 Church St, Bloemfontein", null),
            new(10, 9, 0, false, 1, OrderStatus.Completed, 18, "22 Vilakazi St, Soweto",     "Enjoyed the kota!"),
            new(2, 2, 1, false, 1, OrderStatus.Cancelled,  7,  null,                         "Out of stock mismatch.")
        };
    }
}
