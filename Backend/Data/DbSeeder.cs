using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new AppUser { Email = "admin@grocery.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "Admin", LastName = "User", Role = "Admin" },
                new AppUser { Email = "manager@grocery.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"), FirstName = "Store", LastName = "Manager", Role = "StoreManager" },
                new AppUser { Email = "driver@grocery.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123"), FirstName = "Delivery", LastName = "Driver", Role = "DeliveryDriver" },
                new AppUser { Email = "customer@grocery.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"), FirstName = "John", LastName = "Doe", Role = "Customer" }
            );
            await db.SaveChangesAsync();
        }

        if (await db.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Fruits & Vegetables", Description = "Fresh produce", ImageUrl = "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400" },
            new() { Name = "Dairy & Eggs", Description = "Milk, cheese, eggs", ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400" },
            new() { Name = "Bakery", Description = "Bread and pastries", ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400" },
            new() { Name = "Beverages", Description = "Drinks and juices", ImageUrl = "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400" },
            new() { Name = "Snacks", Description = "Chips and snacks", ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400" },
            new() { Name = "Meat & Seafood", Description = "Fresh meat and fish", ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400" },
            new() { Name = "Frozen Foods", Description = "Frozen meals and veg", ImageUrl = "https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=400" },
            new() { Name = "Pantry", Description = "Canned and dry goods", ImageUrl = "https://images.unsplash.com/photo-1584473457406-6240486418e9?w=400" },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var fv = categories[0].Id; var de = categories[1].Id; var bk = categories[2].Id;
        var bv = categories[3].Id; var sn = categories[4].Id; var mt = categories[5].Id;
        var fz = categories[6].Id; var pa = categories[7].Id;

        db.Products.AddRange(
            // Fruits & Vegetables
            new Product { Name = "Banana", Description = "Fresh yellow bananas", Price = 60m, Sku = "FV001", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", CategoryId = fv, StockQuantity = 100, Unit = "dozen", Brand = "Fresh Farm", AverageRating = 4.5 },
            new Product { Name = "Apple", Description = "Crisp red apples - Kashmir", Price = 180m, Sku = "FV002", ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400", CategoryId = fv, StockQuantity = 80, Unit = "kg", Brand = "Kashmir Orchards", AverageRating = 4.7 },
            new Product { Name = "Tomatoes", Description = "Fresh red tomatoes", Price = 40m, Sku = "FV003", ImageUrl = "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400", CategoryId = fv, StockQuantity = 70, Unit = "kg", Brand = "Farm Fresh", AverageRating = 4.3 },
            new Product { Name = "Spinach", Description = "Fresh spinach leaves", Price = 25m, Sku = "FV004", ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400", CategoryId = fv, StockQuantity = 60, Unit = "250g", Brand = "Organic India", AverageRating = 4.6 },
            new Product { Name = "Onions", Description = "Red onions - Nashik", Price = 35m, Sku = "FV005", ImageUrl = "https://images.unsplash.com/photo-1508747703725-719777637510?w=400", CategoryId = fv, StockQuantity = 120, Unit = "kg", Brand = "Nashik Fresh", AverageRating = 4.1 },
            new Product { Name = "Potatoes", Description = "Fresh potatoes", Price = 30m, Sku = "FV006", ImageUrl = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400", CategoryId = fv, StockQuantity = 150, Unit = "kg", Brand = "Farm Direct", AverageRating = 4.2 },
            new Product { Name = "Mango", Description = "Alphonso mangoes", Price = 250m, Sku = "FV007", ImageUrl = "https://images.unsplash.com/photo-1605027990121-3b2c6c16b5ee?w=400", CategoryId = fv, StockQuantity = 40, Unit = "kg", Brand = "Ratnagiri Fresh", AverageRating = 4.9 },
            new Product { Name = "Carrots", Description = "Fresh orange carrots", Price = 50m, Sku = "FV008", ImageUrl = "https://images.unsplash.com/photo-1445282768818-728615cc910a?w=400", CategoryId = fv, StockQuantity = 85, Unit = "kg", Brand = "Farm Direct", AverageRating = 4.3 },

            // Dairy & Eggs
            new Product { Name = "Amul Milk", Description = "Full cream milk 1L", Price = 65m, Sku = "DE001", ImageUrl = "https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400", CategoryId = de, StockQuantity = 60, Unit = "1L", Brand = "Amul", AverageRating = 4.4 },
            new Product { Name = "Farm Eggs", Description = "Fresh brown eggs", Price = 120m, Sku = "DE002", ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", CategoryId = de, StockQuantity = 50, Unit = "30 eggs", Brand = "Keggfarms", AverageRating = 4.8 },
            new Product { Name = "Amul Butter", Description = "Salted butter 100g", Price = 55m, Sku = "DE003", ImageUrl = "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400", CategoryId = de, StockQuantity = 35, Unit = "100g", Brand = "Amul", AverageRating = 4.7 },
            new Product { Name = "Amul Paneer", Description = "Fresh paneer 200g", Price = 85m, Sku = "DE004", ImageUrl = "https://images.unsplash.com/photo-1631452180539-96aca7d48617?w=400", CategoryId = de, StockQuantity = 40, Unit = "200g", Brand = "Amul", AverageRating = 4.6 },
            new Product { Name = "Amul Curd", Description = "Fresh curd 400g", Price = 45m, Sku = "DE005", ImageUrl = "https://images.unsplash.com/photo-1571212515416-fef01fc43637?w=400", CategoryId = de, StockQuantity = 45, Unit = "400g", Brand = "Amul", AverageRating = 4.6 },
            new Product { Name = "Amul Ghee", Description = "Pure ghee 500ml", Price = 280m, Sku = "DE006", ImageUrl = "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400", CategoryId = de, StockQuantity = 25, Unit = "500ml", Brand = "Amul", AverageRating = 4.8 },

            // Bakery
            new Product { Name = "Britannia Bread", Description = "Whole wheat bread 400g", Price = 35m, Sku = "BK001", ImageUrl = "https://images.unsplash.com/photo-1586444248902-2f64eddc13df?w=400", CategoryId = bk, StockQuantity = 30, Unit = "400g", Brand = "Britannia", AverageRating = 4.9 },
            new Product { Name = "Croissant", Description = "Buttery croissant", Price = 25m, Sku = "BK002", ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400", CategoryId = bk, StockQuantity = 40, Unit = "each", Brand = "Monginis", AverageRating = 4.7 },
            new Product { Name = "Brown Bread", Description = "Healthy brown bread 400g", Price = 40m, Sku = "BK003", ImageUrl = "https://images.unsplash.com/photo-1586444248902-2f64eddc13df?w=400", CategoryId = bk, StockQuantity = 35, Unit = "400g", Brand = "Harvest Gold", AverageRating = 4.5 },
            new Product { Name = "Pav Bread", Description = "Mumbai pav 6 pieces", Price = 20m, Sku = "BK004", ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400", CategoryId = bk, StockQuantity = 60, Unit = "6 pieces", Brand = "Local Bakery", AverageRating = 4.2 },
            new Product { Name = "Cake Rusk", Description = "Tea rusk 200g", Price = 35m, Sku = "BK005", ImageUrl = "https://images.unsplash.com/photo-1499636136210-6f4ee915583e?w=400", CategoryId = bk, StockQuantity = 40, Unit = "200g", Brand = "Britannia", AverageRating = 4.5 },

            // Beverages
            new Product { Name = "Real Orange Juice", Description = "Fresh orange juice 1L", Price = 85m, Sku = "BV001", ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400", CategoryId = bv, StockQuantity = 45, Unit = "1L", Brand = "Real", AverageRating = 4.5 },
            new Product { Name = "Bisleri Water", Description = "Mineral water 1L", Price = 20m, Sku = "BV002", ImageUrl = "https://images.unsplash.com/photo-1548839140-29a749e1cf4d?w=400", CategoryId = bv, StockQuantity = 90, Unit = "1L", Brand = "Bisleri", AverageRating = 4.3 },
            new Product { Name = "Tata Tea Gold", Description = "Premium tea 250g", Price = 180m, Sku = "BV003", ImageUrl = "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400", CategoryId = bv, StockQuantity = 55, Unit = "250g", Brand = "Tata Tea", AverageRating = 4.6 },
            new Product { Name = "Nescafe Coffee", Description = "Instant coffee 100g", Price = 220m, Sku = "BV004", ImageUrl = "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400", CategoryId = bv, StockQuantity = 30, Unit = "100g", Brand = "Nescafe", AverageRating = 4.8 },
            new Product { Name = "Coca Cola", Description = "Soft drink 600ml", Price = 40m, Sku = "BV005", ImageUrl = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=400", CategoryId = bv, StockQuantity = 100, Unit = "600ml", Brand = "Coca Cola", AverageRating = 4.4 },
            new Product { Name = "Tropicana Juice", Description = "Mixed fruit juice 1L", Price = 120m, Sku = "BV006", ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400", CategoryId = bv, StockQuantity = 50, Unit = "1L", Brand = "Tropicana", AverageRating = 4.6 },

            // Snacks
            new Product { Name = "Lays Classic", Description = "Potato chips 52g", Price = 20m, Sku = "SN001", ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400", CategoryId = sn, StockQuantity = 100, Unit = "52g", Brand = "Lays", AverageRating = 4.3 },
            new Product { Name = "Kurkure Masala Munch", Description = "Corn puffs 85g", Price = 20m, Sku = "SN002", ImageUrl = "https://images.unsplash.com/photo-1613919113640-25732ec5e61f?w=400", CategoryId = sn, StockQuantity = 90, Unit = "85g", Brand = "Kurkure", AverageRating = 4.4 },
            new Product { Name = "Haldiram Bhujia", Description = "Spicy noodles 200g", Price = 60m, Sku = "SN003", ImageUrl = "https://images.unsplash.com/photo-1606491956689-2ea866880c84?w=400", CategoryId = sn, StockQuantity = 70, Unit = "200g", Brand = "Haldiram", AverageRating = 4.6 },
            new Product { Name = "Parle-G Biscuits", Description = "Glucose biscuits 799g", Price = 50m, Sku = "SN004", ImageUrl = "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?w=400", CategoryId = sn, StockQuantity = 80, Unit = "799g", Brand = "Parle", AverageRating = 4.7 },
            new Product { Name = "Cadbury Dairy Milk", Description = "Milk chocolate 55g", Price = 35m, Sku = "SN005", ImageUrl = "https://images.unsplash.com/photo-1511381939415-e44015466834?w=400", CategoryId = sn, StockQuantity = 120, Unit = "55g", Brand = "Cadbury", AverageRating = 4.8 },
            new Product { Name = "Maggi Noodles", Description = "Instant noodles 70g", Price = 14m, Sku = "SN006", ImageUrl = "https://images.unsplash.com/photo-1617093727343-374698b1b08d?w=400", CategoryId = sn, StockQuantity = 150, Unit = "70g", Brand = "Maggi", AverageRating = 4.5 },

            // Meat & Seafood
            new Product { Name = "Chicken Breast", Description = "Fresh boneless chicken 500g", Price = 220m, Sku = "MT001", ImageUrl = "https://images.unsplash.com/photo-1604503468506-a8da13d11d36?w=400", CategoryId = mt, StockQuantity = 40, Unit = "500g", Brand = "Suguna", AverageRating = 4.5 },
            new Product { Name = "Mutton", Description = "Fresh goat mutton 500g", Price = 450m, Sku = "MT002", ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400", CategoryId = mt, StockQuantity = 25, Unit = "500g", Brand = "Fresh Meat", AverageRating = 4.6 },
            new Product { Name = "Rohu Fish", Description = "Fresh rohu fish 500g", Price = 180m, Sku = "MT003", ImageUrl = "https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=400", CategoryId = mt, StockQuantity = 30, Unit = "500g", Brand = "Ocean Fresh", AverageRating = 4.4 },
            new Product { Name = "Prawns", Description = "Fresh tiger prawns 250g", Price = 350m, Sku = "MT004", ImageUrl = "https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=400", CategoryId = mt, StockQuantity = 20, Unit = "250g", Brand = "Ocean Fresh", AverageRating = 4.7 },
            new Product { Name = "Chicken Eggs", Description = "White eggs 12 pack", Price = 80m, Sku = "MT005", ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", CategoryId = mt, StockQuantity = 60, Unit = "12 eggs", Brand = "Keggfarms", AverageRating = 4.5 },

            // Frozen Foods
            new Product { Name = "McCain Fries", Description = "Crispy french fries 400g", Price = 130m, Sku = "FZ001", ImageUrl = "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400", CategoryId = fz, StockQuantity = 50, Unit = "400g", Brand = "McCain", AverageRating = 4.5 },
            new Product { Name = "Safal Peas", Description = "Frozen green peas 500g", Price = 65m, Sku = "FZ002", ImageUrl = "https://images.unsplash.com/photo-1587735243615-c03f25aaff15?w=400", CategoryId = fz, StockQuantity = 60, Unit = "500g", Brand = "Safal", AverageRating = 4.3 },
            new Product { Name = "Frozen Corn", Description = "Sweet corn kernels 500g", Price = 75m, Sku = "FZ003", ImageUrl = "https://images.unsplash.com/photo-1551754655-cd27e38d2076?w=400", CategoryId = fz, StockQuantity = 45, Unit = "500g", Brand = "Safal", AverageRating = 4.4 },
            new Product { Name = "Aloo Paratha", Description = "Frozen stuffed paratha 4 pieces", Price = 75m, Sku = "FZ004", ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400", CategoryId = fz, StockQuantity = 35, Unit = "4 pieces", Brand = "Safal", AverageRating = 4.5 },
            new Product { Name = "Frozen Pizza", Description = "Margherita pizza 250g", Price = 180m, Sku = "FZ005", ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400", CategoryId = fz, StockQuantity = 25, Unit = "250g", Brand = "Dr. Oetker", AverageRating = 4.3 },

            // Pantry
            new Product { Name = "Tata Salt", Description = "Iodized salt 1kg", Price = 20m, Sku = "PA001", ImageUrl = "https://images.unsplash.com/photo-1518110925495-5fe2fda0442c?w=400", CategoryId = pa, StockQuantity = 100, Unit = "1kg", Brand = "Tata", AverageRating = 4.5 },
            new Product { Name = "Fortune Sunflower Oil", Description = "Refined sunflower oil 1L", Price = 140m, Sku = "PA002", ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400", CategoryId = pa, StockQuantity = 60, Unit = "1L", Brand = "Fortune", AverageRating = 4.4 },
            new Product { Name = "India Gate Basmati Rice", Description = "Premium basmati rice 1kg", Price = 120m, Sku = "PA003", ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400", CategoryId = pa, StockQuantity = 80, Unit = "1kg", Brand = "India Gate", AverageRating = 4.7 },
            new Product { Name = "Aashirvaad Atta", Description = "Whole wheat flour 5kg", Price = 280m, Sku = "PA004", ImageUrl = "https://images.unsplash.com/photo-1574323347407-f5e1ad6d020b?w=400", CategoryId = pa, StockQuantity = 50, Unit = "5kg", Brand = "Aashirvaad", AverageRating = 4.6 },
            new Product { Name = "MDH Garam Masala", Description = "Spice blend 100g", Price = 85m, Sku = "PA005", ImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400", CategoryId = pa, StockQuantity = 70, Unit = "100g", Brand = "MDH", AverageRating = 4.6 },
            new Product { Name = "Kissan Tomato Ketchup", Description = "Tomato ketchup 500g", Price = 95m, Sku = "PA006", ImageUrl = "https://images.unsplash.com/photo-1558818498-28c1e002b655?w=400", CategoryId = pa, StockQuantity = 55, Unit = "500g", Brand = "Kissan", AverageRating = 4.5 }
        );
        await db.SaveChangesAsync();

        if (await db.Coupons.AnyAsync()) return;

        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10m, MinOrderAmount = 200m, ExpiresAt = DateTime.UtcNow.AddMonths(6), IsActive = true, UsageLimit = 500 },
            new Coupon { Code = "SAVE50", DiscountType = "Fixed", DiscountValue = 50m, MinOrderAmount = 500m, ExpiresAt = DateTime.UtcNow.AddMonths(3), IsActive = true, UsageLimit = 200 },
            new Coupon { Code = "FRESH20", DiscountType = "Percentage", DiscountValue = 20m, MinOrderAmount = 300m, ExpiresAt = DateTime.UtcNow.AddMonths(2), IsActive = true, UsageLimit = 100 },
            new Coupon { Code = "FLAT100", DiscountType = "Fixed", DiscountValue = 100m, MinOrderAmount = 800m, ExpiresAt = DateTime.UtcNow.AddMonths(1), IsActive = true, UsageLimit = 50 },
            new Coupon { Code = "NEWUSER15", DiscountType = "Percentage", DiscountValue = 15m, MinOrderAmount = 250m, ExpiresAt = DateTime.UtcNow.AddMonths(12), IsActive = true, UsageLimit = 1000 }
        );
        await db.SaveChangesAsync();
    }
}
