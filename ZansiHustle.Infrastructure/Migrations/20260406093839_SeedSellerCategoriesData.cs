using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSellerCategoriesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Generate GUIDs for categories
            var beautyId = new Guid("11111111-1111-1111-1111-111111111111");
            var foodId = new Guid("22222222-2222-2222-2222-222222222222");
            var fashionId = new Guid("33333333-3333-3333-3333-333333333333");
            var homeServicesId = new Guid("44444444-4444-4444-4444-444444444444");
            var repairsId = new Guid("55555555-5555-5555-5555-555555555555");
            var electronicsId = new Guid("66666666-6666-6666-6666-666666666666");
            var healthId = new Guid("77777777-7777-7777-7777-777777777777");
            var educationId = new Guid("88888888-8888-8888-8888-888888888888");
            var eventsId = new Guid("99999999-9999-9999-9999-999999999999");
            var photographyId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var transportId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var professionalId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var digitalId = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var retailId = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var homeLivingId = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
            var kidsId = new Guid("aaaaaaaa-1111-aaaa-1111-aaaaaaaa1111");
            var automotiveId = new Guid("bbbbbbbb-2222-bbbb-2222-bbbbbbbb2222");
            var printingId = new Guid("cccccccc-3333-cccc-3333-cccccccc3333");
            var agricultureId = new Guid("dddddddd-4444-dddd-4444-dddddddd4444");
            var petsId = new Guid("eeeeeeee-5555-eeee-5555-eeeeeeee5555");
            var travelId = new Guid("ffffffff-6666-ffff-6666-ffffffff6666");
            var financialId = new Guid("11111111-7777-1111-7777-111111117777");
            var constructionId = new Guid("22222222-8888-2222-8888-222222228888");
            var communityId = new Guid("33333333-9999-3333-9999-333333339999");
            var otherId = new Guid("44444444-0000-4444-0000-444444440000");

            // Insert Categories
            migrationBuilder.InsertData(
                table: "SellerCategories",
                columns: new[] { "Id", "Name", "Slug", "Description", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { beautyId, "Beauty & Personal Care", "beauty-personal-care", "Hair, nails, makeup, skincare, and beauty services", 0, true, DateTime.UtcNow },
                    { foodId, "Food & Dining", "food-dining", "Restaurants, catering, meal prep, and food vendors", 1, true, DateTime.UtcNow },
                    { fashionId, "Fashion & Apparel", "fashion-apparel", "Clothing, shoes, accessories, and tailoring", 2, true, DateTime.UtcNow },
                    { homeServicesId, "Home Services", "home-services", "Cleaning, gardening, handyman, and home maintenance", 3, true, DateTime.UtcNow },
                    { repairsId, "Repairs & Trades", "repairs-trades", "Electricians, plumbers, builders, and repair services", 4, true, DateTime.UtcNow },
                    { electronicsId, "Electronics & Tech", "electronics-tech", "Phones, laptops, gadgets, and tech services", 5, true, DateTime.UtcNow },
                    { healthId, "Health & Wellness", "health-wellness", "Fitness, nutrition, therapy, and wellness services", 6, true, DateTime.UtcNow },
                    { educationId, "Education & Tutoring", "education-tutoring", "Tutoring, coaching, lessons, and skills training", 7, true, DateTime.UtcNow },
                    { eventsId, "Events & Entertainment", "events-entertainment", "Event planning, entertainment, and party services", 8, true, DateTime.UtcNow },
                    { photographyId, "Photography & Videography", "photography-videography", "Professional photo and video services", 9, true, DateTime.UtcNow },
                    { transportId, "Transport & Delivery", "transport-delivery", "Moving, courier, and delivery services", 10, true, DateTime.UtcNow },
                    { professionalId, "Professional Services", "professional-services", "Legal, accounting, consulting, and business services", 11, true, DateTime.UtcNow },
                    { digitalId, "Digital & Creative Services", "digital-creative-services", "Design, marketing, content, and creative services", 12, true, DateTime.UtcNow },
                    { retailId, "Retail & General Goods", "retail-general-goods", "Retailers, resellers, and general merchandise", 13, true, DateTime.UtcNow },
                    { homeLivingId, "Home & Living", "home-living", "Furniture, decor, and home improvement", 14, true, DateTime.UtcNow },
                    { kidsId, "Kids & Family", "kids-family", "Children's products, childcare, and family services", 15, true, DateTime.UtcNow },
                    { automotiveId, "Automotive", "automotive", "Car services, repairs, and automotive products", 16, true, DateTime.UtcNow },
                    { printingId, "Printing & Branding", "printing-branding", "Print services and branding solutions", 17, true, DateTime.UtcNow },
                    { agricultureId, "Agriculture & Outdoor", "agriculture-outdoor", "Farming, produce, and outdoor services", 18, true, DateTime.UtcNow },
                    { petsId, "Pets", "pets", "Pet care, grooming, and pet products", 19, true, DateTime.UtcNow },
                    { travelId, "Travel & Accommodation", "travel-accommodation", "Lodging, tours, and travel services", 20, true, DateTime.UtcNow },
                    { financialId, "Financial Services", "financial-services", "Financial advice, insurance, and money services", 21, true, DateTime.UtcNow },
                    { constructionId, "Construction & Property", "construction-property", "Construction, real estate, and property services", 22, true, DateTime.UtcNow },
                    { communityId, "Community & Local Services", "community-local-services", "Community support, NGO, and local services", 23, true, DateTime.UtcNow },
                    { otherId, "Other", "other", "Miscellaneous categories that don't fit elsewhere", 999, true, DateTime.UtcNow }
                });

            // Insert Subcategories - Beauty & Personal Care
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), beautyId, "Barbers", "barbers", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Hair Stylists", "hair-stylists", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Braiders", "braiders", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Nail Technicians", "nail-technicians", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Makeup Artists", "makeup-artists", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Beauty Salons", "beauty-salons", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Lash Technicians", "lash-technicians", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Waxing Specialists", "waxing-specialists", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Skincare Specialists", "skincare-specialists", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Massage Therapists", "massage-therapists", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Mobile Beauty Services", "mobile-beauty-services", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Bridal Beauty Services", "bridal-beauty-services", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Men's Grooming", "mens-grooming", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Wig Installation", "wig-installation", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Wig Sales", "wig-sales", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Hair Product Sellers", "hair-product-sellers", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Beauty Product Sellers", "beauty-product-sellers", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Tattoo Artists", "tattoo-artists", 17, true, DateTime.UtcNow },
                    { Guid.NewGuid(), beautyId, "Piercing Services", "piercing-services", 18, true, DateTime.UtcNow }
                });

            // Food & Dining
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), foodId, "Restaurants", "restaurants", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Fast Food", "fast-food", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Takeaways", "takeaways", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Shisanyama", "shisanyama", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Catering", "catering", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Home Cooks", "home-cooks", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Meal Prep Services", "meal-prep-services", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Private Chefs", "private-chefs", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Bakers", "bakers", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Cake Makers", "cake-makers", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Dessert Sellers", "dessert-sellers", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Street Food Vendors", "street-food-vendors", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Snack Sellers", "snack-sellers", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Fruit & Veg Sellers", "fruit-veg-sellers", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Grocery Stores", "grocery-stores", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Spaza Shops", "spaza-shops", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Butcheries", "butcheries", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Coffee Vendors", "coffee-vendors", 17, true, DateTime.UtcNow },
                    { Guid.NewGuid(), foodId, "Beverage Sellers", "beverage-sellers", 18, true, DateTime.UtcNow }
                });

            // Fashion & Apparel
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), fashionId, "Clothing", "clothing", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Fashion Boutiques", "fashion-boutiques", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Streetwear Sellers", "streetwear-sellers", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Shoe Sellers", "shoe-sellers", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Sneaker Resellers", "sneaker-resellers", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Tailoring", "tailoring", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Dressmakers", "dressmakers", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Alterations", "alterations", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Traditional Wear", "traditional-wear", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Kids Clothing", "kids-clothing", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Women's Fashion", "womens-fashion", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Men's Fashion", "mens-fashion", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Uniform Suppliers", "uniform-suppliers", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Bags & Handbags", "bags-handbags", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Accessories", "accessories", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Jewellery", "jewellery", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Watch Sellers", "watch-sellers", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Fabric Sellers", "fabric-sellers", 17, true, DateTime.UtcNow },
                    { Guid.NewGuid(), fashionId, "Thrift / Pre-Owned Clothing", "thrift-clothing", 18, true, DateTime.UtcNow }
                });

            // Home Services
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), homeServicesId, "Cleaners", "cleaners", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Deep Cleaning", "deep-cleaning", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Laundry Services", "laundry-services", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Ironing Services", "ironing-services", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Home Organising", "home-organising", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Garden Services", "garden-services", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Landscaping", "landscaping", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Pest Control", "pest-control", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Pool Cleaning", "pool-cleaning", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Carpet Cleaning", "carpet-cleaning", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Window Cleaning", "window-cleaning", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Upholstery Cleaning", "upholstery-cleaning", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Waste Removal", "waste-removal", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Moving Help", "moving-help", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Handyman Services", "handyman-services", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Home Cooking Services", "home-cooking-services", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Elderly Assistance", "elderly-assistance", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeServicesId, "Home Care Services", "home-care-services", 17, true, DateTime.UtcNow }
                });

            // Repairs & Trades
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), repairsId, "Repair Services", "repair-services", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Electricians", "electricians", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Plumbers", "plumbers", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Builders", "builders", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Painters", "painters", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Tilers", "tilers", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Carpenters", "carpenters", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Welders", "welders", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Roofing Services", "roofing-services", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Ceiling Installers", "ceiling-installers", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Flooring Installers", "flooring-installers", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Appliance Repair", "appliance-repair", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "TV Repair", "tv-repair", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Phone Repair", "phone-repair", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Laptop Repair", "laptop-repair", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Fridge Repair", "fridge-repair", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Aircon Services", "aircon-services", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Generator Repair", "generator-repair", 17, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Borehole Services", "borehole-services", 18, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Gate Motor Repair", "gate-motor-repair", 19, true, DateTime.UtcNow },
                    { Guid.NewGuid(), repairsId, "Locksmiths", "locksmiths", 20, true, DateTime.UtcNow }
                });

            // Electronics & Tech
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), electronicsId, "Electronics", "electronics", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Phone Sellers", "phone-sellers", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Laptop Sellers", "laptop-sellers", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Computer Accessories", "computer-accessories", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Gaming Consoles", "gaming-consoles", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "TV & Audio", "tv-audio", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Camera Equipment", "camera-equipment", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Smart Devices", "smart-devices", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Phone Accessories", "phone-accessories", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Tech Repairs", "tech-repairs", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Software Setup", "software-setup", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "IT Support", "it-support", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "WiFi Installation", "wifi-installation", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "CCTV Installation", "cctv-installation", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Solar Installation", "solar-installation", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), electronicsId, "Data & Airtime Sellers", "data-airtime-sellers", 15, true, DateTime.UtcNow }
                });

            // Health & Wellness
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), healthId, "Health & Wellness", "health-wellness", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Personal Trainers", "personal-trainers", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Fitness Coaches", "fitness-coaches", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Gym Instructors", "gym-instructors", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Dieticians", "dieticians", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Nutrition Coaches", "nutrition-coaches", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Counsellors", "counsellors", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Therapists", "therapists", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Wellness Coaches", "wellness-coaches", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Yoga Instructors", "yoga-instructors", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Pilates Instructors", "pilates-instructors", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Mobile Clinics", "mobile-clinics", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Home Nursing", "home-nursing", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Caregivers", "caregivers", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Wellness Product Sellers", "wellness-product-sellers", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Supplement Retailers", "supplement-retailers", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), healthId, "Physiotherapy Services", "physiotherapy-services", 16, true, DateTime.UtcNow }
                });

            // Education & Tutoring
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), educationId, "Education & Tutoring", "education-tutoring", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Academic Tutors", "academic-tutors", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Homework Assistance", "homework-assistance", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Exam Prep Tutors", "exam-prep-tutors", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "University Tutors", "university-tutors", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Language Tutors", "language-tutors", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Coding Tutors", "coding-tutors", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Computer Lessons", "computer-lessons", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Driving Schools", "driving-schools", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Music Lessons", "music-lessons", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Art Lessons", "art-lessons", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Sports Coaching", "sports-coaching", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Skills Training", "skills-training", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Business Coaching", "business-coaching", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Early Childhood Learning", "early-childhood-learning", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), educationId, "Special Needs Support", "special-needs-support", 15, true, DateTime.UtcNow }
                });

            // Events & Entertainment
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), eventsId, "Events", "events", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Event Planners", "event-planners", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Wedding Planners", "wedding-planners", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Decor Services", "decor-services", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "MCs", "mcs", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "DJs", "djs", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Live Bands", "live-bands", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Performers", "performers", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Sound Hire", "sound-hire", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Lighting Hire", "lighting-hire", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Tent Hire", "tent-hire", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Chair & Table Hire", "chair-table-hire", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Bridal Services", "bridal-services", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Event Staffing", "event-staffing", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Kids Party Services", "kids-party-services", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Funeral Services", "funeral-services", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), eventsId, "Traditional Ceremony Services", "traditional-ceremony-services", 16, true, DateTime.UtcNow }
                });

            // Photography & Videography
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), photographyId, "Photography", "photography", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Videography", "videography", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Event Photography", "event-photography", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Wedding Photography", "wedding-photography", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Portrait Photography", "portrait-photography", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Product Photography", "product-photography", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Content Creation", "content-creation", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Drone Services", "drone-services", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Photo Editing", "photo-editing", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Video Editing", "video-editing", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Studio Photography", "studio-photography", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), photographyId, "Photo Booth Services", "photo-booth-services", 11, true, DateTime.UtcNow }
                });

            // Transport & Delivery
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), transportId, "Transport", "transport", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Delivery Services", "delivery-services", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Courier Services", "courier-services", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Moving Services", "moving-services", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Furniture Transport", "furniture-transport", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "School Transport", "school-transport", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Staff Transport", "staff-transport", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Airport Transfers", "airport-transfers", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Shuttle Services", "shuttle-services", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Bike Delivery", "bike-delivery", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Errand Running", "errand-running", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), transportId, "Ride Services", "ride-services", 11, true, DateTime.UtcNow }
                });

            // Professional Services
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), professionalId, "Legal Services", "legal-services", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Accounting", "accounting", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Bookkeeping", "bookkeeping", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Tax Services", "tax-services", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Business Registration", "business-registration", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Admin Support", "admin-support", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Virtual Assistance", "virtual-assistance", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Recruitment Services", "recruitment-services", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Consulting", "consulting", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "HR Services", "hr-services", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Company Secretarial Services", "company-secretarial-services", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Tender Assistance", "tender-assistance", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Proposal Writing", "proposal-writing", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), professionalId, "Research Services", "research-services", 13, true, DateTime.UtcNow }
                });

            // Digital & Creative Services
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), digitalId, "Graphic Design", "graphic-design", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Logo Design", "logo-design", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Brand Design", "brand-design", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Social Media Management", "social-media-management", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Content Writing", "content-writing", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Copywriting", "copywriting", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Web Design", "web-design", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Web Development", "web-development", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Mobile App Development", "mobile-app-development", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "UI/UX Design", "ui-ux-design", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "SEO Services", "seo-services", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Digital Marketing", "digital-marketing", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Paid Ads Management", "paid-ads-management", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Video Editing", "video-editing", 13, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Animation", "animation", 14, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Voice Over Services", "voice-over-services", 15, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Podcast Editing", "podcast-editing", 16, true, DateTime.UtcNow },
                    { Guid.NewGuid(), digitalId, "Presentation Design", "presentation-design", 17, true, DateTime.UtcNow }
                });

            // Retail & General Goods
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), retailId, "General Retail", "general-retail", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Marketplace Sellers", "marketplace-sellers", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Resellers", "resellers", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Wholesalers", "wholesalers", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Second-Hand Goods", "second-hand-goods", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Household Goods", "household-goods", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Stationery", "stationery", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Office Supplies", "office-supplies", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Party Supplies", "party-supplies", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Gift Shops", "gift-shops", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Toy Sellers", "toy-sellers", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Book Sellers", "book-sellers", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Seasonal Goods", "seasonal-goods", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), retailId, "Imported Goods", "imported-goods", 13, true, DateTime.UtcNow }
                });

            // Home & Living
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), homeLivingId, "Furniture", "furniture", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Home Decor", "home-decor", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Curtains & Blinds", "curtains-blinds", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Bedding", "bedding", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Kitchenware", "kitchenware", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Appliances", "appliances", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Interior Design", "interior-design", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Storage Solutions", "storage-solutions", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Lighting", "lighting", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Bathroom Supplies", "bathroom-supplies", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Garden Furniture", "garden-furniture", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "DIY Supplies", "diy-supplies", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Hardware", "hardware", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), homeLivingId, "Building Materials", "building-materials", 13, true, DateTime.UtcNow }
                });

            // Kids & Family
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), kidsId, "Kids Clothing", "kids-clothing", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Baby Products", "baby-products", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Toy Sellers", "toy-sellers", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Childcare Services", "childcare-services", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Nannies", "nannies", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Au Pairs", "au-pairs", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Kids Party Services", "kids-party-services", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Educational Toys", "educational-toys", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Baby Sitting", "baby-sitting", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Family Photography", "family-photography", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), kidsId, "Kids Tutoring", "kids-tutoring", 10, true, DateTime.UtcNow }
                });

            // Automotive
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), automotiveId, "Car Wash", "car-wash", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Mobile Car Wash", "mobile-car-wash", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Auto Repair", "auto-repair", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Mechanics", "mechanics", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Panel Beating", "panel-beating", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Tyre Services", "tyre-services", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Car Detailing", "car-detailing", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Spare Parts", "spare-parts", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Auto Electrical", "auto-electrical", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Battery Services", "battery-services", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Vehicle Diagnostics", "vehicle-diagnostics", 10, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Car Sound Installation", "car-sound-installation", 11, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Tracking Installation", "tracking-installation", 12, true, DateTime.UtcNow },
                    { Guid.NewGuid(), automotiveId, "Towing Services", "towing-services", 13, true, DateTime.UtcNow }
                });

            // Printing & Branding
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), printingId, "Printing", "printing", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Branding", "branding", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "T-Shirt Printing", "t-shirt-printing", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Signage", "signage", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Flyer Printing", "flyer-printing", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Business Cards", "business-cards", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Packaging Design", "packaging-design", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Label Printing", "label-printing", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Promotional Materials", "promotional-materials", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Embroidery", "embroidery", 9, true, DateTime.UtcNow },
                    { Guid.NewGuid(), printingId, "Stamp Making", "stamp-making", 10, true, DateTime.UtcNow }
                });

            // Agriculture & Outdoor
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), agricultureId, "Fresh Produce", "fresh-produce", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Plant Sellers", "plant-sellers", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Garden Supplies", "garden-supplies", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Farming Supplies", "farming-supplies", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Poultry Sellers", "poultry-sellers", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Livestock Sellers", "livestock-sellers", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Land Preparation", "land-preparation", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Irrigation Services", "irrigation-services", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), agricultureId, "Outdoor Maintenance", "outdoor-maintenance", 8, true, DateTime.UtcNow }
                });

            // Pets
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), petsId, "Pet Grooming", "pet-grooming", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Sitting", "pet-sitting", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Walking", "pet-walking", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Food Sellers", "pet-food-sellers", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Accessories", "pet-accessories", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Veterinary Support Services", "veterinary-support", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Training", "pet-training", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), petsId, "Pet Breeding", "pet-breeding", 7, true, DateTime.UtcNow }
                });

            // Travel & Accommodation
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), travelId, "Guest Houses", "guest-houses", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Short Stay Rentals", "short-stay-rentals", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Tour Guides", "tour-guides", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Travel Planning", "travel-planning", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Shuttle Services", "shuttle-services", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Holiday Activities", "holiday-activities", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), travelId, "Local Experiences", "local-experiences", 6, true, DateTime.UtcNow }
                });

            // Financial Services
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), financialId, "Insurance Advisors", "insurance-advisors", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Financial Advisors", "financial-advisors", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Loan Assistance", "loan-assistance", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Investment Education", "investment-education", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Forex Education", "forex-education", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Savings Groups", "savings-groups", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), financialId, "Payment Collection Services", "payment-collection-services", 6, true, DateTime.UtcNow }
                });

            // Construction & Property
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), constructionId, "Construction", "construction", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Renovations", "renovations", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Property Maintenance", "property-maintenance", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Real Estate Agents", "real-estate-agents", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Property Photography", "property-photography", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Property Cleaning", "property-cleaning", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Landscaping", "landscaping", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Architecture Services", "architecture-services", 7, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Quantity Surveying", "quantity-surveying", 8, true, DateTime.UtcNow },
                    { Guid.NewGuid(), constructionId, "Bricklaying", "bricklaying", 9, true, DateTime.UtcNow }
                });

            // Community & Local Services
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), communityId, "Community Support", "community-support", 0, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "NGO Services", "ngo-services", 1, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Church Services", "church-services", 2, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Funeral Support", "funeral-support", 3, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Local Announcements", "local-announcements", 4, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Volunteer Services", "volunteer-services", 5, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Security Services", "security-services", 6, true, DateTime.UtcNow },
                    { Guid.NewGuid(), communityId, "Watchman Services", "watchman-services", 7, true, DateTime.UtcNow }
                });

            // Other
            migrationBuilder.InsertData(
                table: "SellerSubcategories",
                columns: new[] { "Id", "SellerCategoryId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAtUtc" },
                values: new object[,]
                {
                    { Guid.NewGuid(), otherId, "Other", "other", 0, true, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete all seeded data
            migrationBuilder.Sql("DELETE FROM \"SellerSubcategories\"");
            migrationBuilder.Sql("DELETE FROM \"SellerCategories\"");
        }
    }
}
