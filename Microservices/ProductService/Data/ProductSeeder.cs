using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

public static class ProductSeeder
{
    public static async Task SeedAsync(ProductDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Products.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Fruits & Vegetables" },
            new() { Id = Guid.NewGuid(), Name = "Dairy & Eggs" },
            new() { Id = Guid.NewGuid(), Name = "Bakery" },
            new() { Id = Guid.NewGuid(), Name = "Beverages" },
            new() { Id = Guid.NewGuid(), Name = "Snacks" },
            new() { Id = Guid.NewGuid(), Name = "Meat & Seafood" },
            new() { Id = Guid.NewGuid(), Name = "Frozen Foods" },
            new() { Id = Guid.NewGuid(), Name = "Pantry" },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var cat = categories.ToDictionary(c => c.Name, c => c.Id);

        var products = new List<Product>
        {
            new() { Name="Aashirvaad Atta", Description="Whole wheat flour 5kg", Price=280, Sku="PA004", ImageUrl="https://redrosemart.com/cdn/shop/files/AASHIRVAADATTAWITHMULTIGRAINS5KG2023919597.png?v=1704275014&width=1920", CategoryId=cat["Pantry"], StockQuantity=50, Brand="Aashirvaad", Unit="5kg", AverageRating=4.6 },
            new() { Name="Amul Butter", Description="Salted butter 100g", Price=55, Sku="DE003", ImageUrl="https://www.dhirajbakers.com/Sites/1/Images/products/big/18/10050/amul-butter_30151.jpg", CategoryId=cat["Dairy & Eggs"], StockQuantity=35, Brand="Amul", Unit="100g", AverageRating=4.7 },
            new() { Name="Amul Milk", Description="Full cream milk 1L", Price=65, Sku="DE001", ImageUrl="https://www.jiomart.com/images/product/original/590002686/amul-gold-full-cream-milk-1-l-pouch-product-images-o590002686-p590049228-0-202409131647.jpg", CategoryId=cat["Dairy & Eggs"], StockQuantity=60, Brand="Amul", Unit="1L", AverageRating=4.4 },
            new() { Name="Amul Ghee", Description="Pure ghee 500ml", Price=280, Sku="DE006", ImageUrl="https://www.kiranapoorti.com/image/cache/catalog/ghee/amul%20500ml-320x320w.jpeg", CategoryId=cat["Dairy & Eggs"], StockQuantity=25, Brand="Amul", Unit="500ml", AverageRating=4.8 },
            new() { Name="Farm Eggs", Description="Fresh brown eggs", Price=120, Sku="DE002", ImageUrl="https://thehomesteadingrd.com/wp-content/uploads/2023/12/How-to-store-fresh-eggs.jpg", CategoryId=cat["Dairy & Eggs"], StockQuantity=50, Brand="Keggfarms", Unit="30 eggs", AverageRating=4.8 },
            new() { Name="Amul Paneer", Description="Fresh paneer 200g", Price=85, Sku="DE004", ImageUrl="https://cdn.grofers.com/da/cms-assets/cms/product/98064289-82ca-42ea-a64e-f9465af3c391.jpg", CategoryId=cat["Dairy & Eggs"], StockQuantity=40, Brand="Amul", Unit="200g", AverageRating=4.6 },
            new() { Name="Amul Curd", Description="Fresh curd 400g", Price=45, Sku="DE005", ImageUrl="https://cdn.grofers.com/da/cms-assets/cms/product/2107cdc3-8d54-41fb-a7ee-89d8573b9f06.jpg", CategoryId=cat["Dairy & Eggs"], StockQuantity=45, Brand="Amul", Unit="400g", AverageRating=4.6 },
            new() { Name="Britannia Bread", Description="Whole wheat bread 400g", Price=35, Sku="BK001", ImageUrl="https://media.britannia.co.in/large_White_Breads_2234_Milk_Slice_Bread_400g_T_and_T_3_D_FOP_1400x1400_390e2e0310.jpg", CategoryId=cat["Bakery"], StockQuantity=30, Brand="Britannia", Unit="400g", AverageRating=4.9 },
            new() { Name="Croissant", Description="Buttery croissant", Price=25, Sku="BK002", ImageUrl="https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400", CategoryId=cat["Bakery"], StockQuantity=40, Brand="Monginis", Unit="each", AverageRating=4.7 },
            new() { Name="Brown Bread", Description="Healthy brown bread 400g", Price=40, Sku="BK003", ImageUrl="https://images.unsplash.com/photo-1586444248902-2f64eddc13df?w=400", CategoryId=cat["Bakery"], StockQuantity=35, Brand="Harvest Gold", Unit="400g", AverageRating=4.5 },
            new() { Name="Real Orange Juice", Description="Fresh orange juice 1L", Price=85, Sku="BV001", ImageUrl="https://m.media-amazon.com/images/I/71k1gi24UtL.jpg", CategoryId=cat["Beverages"], StockQuantity=45, Brand="Real", Unit="1L", AverageRating=4.5 },
            new() { Name="Nescafe Coffee", Description="Instant coffee 100g", Price=220, Sku="BV004", ImageUrl="https://ik.imagekit.io/wlfr/wellness/images/products/262991-1.jpg", CategoryId=cat["Beverages"], StockQuantity=30, Brand="Nescafe", Unit="100g", AverageRating=4.8 },
            new() { Name="Tata Tea Gold", Description="Premium tea 250g", Price=180, Sku="BV003", ImageUrl="https://aapkabazar.co/_next/image?url=https%3A%2F%2Fimage.aapkabazar.co%2Fproduct%2F555%2F1696666478057.png%3Ftype%3Dpng&w=1080&q=75", CategoryId=cat["Beverages"], StockQuantity=55, Brand="Tata Tea", Unit="250g", AverageRating=4.6 },
            new() { Name="Lays Classic", Description="Potato chips 52g", Price=20, Sku="SN001", ImageUrl="https://jgcj.jayagrocer.com/cdn/shop/files/169273-1-1.jpg?v=1753762917", CategoryId=cat["Snacks"], StockQuantity=100, Brand="Lays", Unit="52g", AverageRating=4.3 },
            new() { Name="Cadbury Dairy Milk", Description="Milk chocolate 55g", Price=35, Sku="SN005", ImageUrl="https://www.shysha.in/wp-content/uploads/2021/10/P-1484.jpg", CategoryId=cat["Snacks"], StockQuantity=120, Brand="Cadbury", Unit="55g", AverageRating=4.8 },
            new() { Name="Parle-G Biscuits", Description="Glucose biscuits 799g", Price=50, Sku="SN004", ImageUrl="https://baazwsh.com/cdn/shop/products/glucose-biscuit-799g-parle-g-baazwsh-174862.jpg?v=1643043854", CategoryId=cat["Snacks"], StockQuantity=80, Brand="Parle", Unit="799g", AverageRating=4.7 },
            new() { Name="Maggi Noodles", Description="Instant noodles 70g", Price=14, Sku="SN006", ImageUrl="https://regalplus.com/cdn/shop/files/magginoodle.jpg?v=1736527271&width=1080", CategoryId=cat["Snacks"], StockQuantity=150, Brand="Maggi", Unit="70g", AverageRating=4.5 },
            new() { Name="Chicken Breast", Description="Fresh boneless chicken 500g", Price=220, Sku="MT001", ImageUrl="https://www.greenchickchop.in/cdn/shop/files/ChickenBreastBoneless.webp?v=1682572347", CategoryId=cat["Meat & Seafood"], StockQuantity=40, Brand="Suguna", Unit="500g", AverageRating=4.5, DiscountPercent=8 },
            new() { Name="Mutton", Description="Fresh goat mutton 500g", Price=450, Sku="MT002", ImageUrl="https://lh5.googleusercontent.com/proxy/fm5qZsQ1nm8HztsfW92BSx3XQKt1tl2miFs7fXnbRmwE5bmIR3ADNRXrG0h6fpcNpkggOK684pAHz0bkpBZTvZLVj8bIwot0-h0aDsU", CategoryId=cat["Meat & Seafood"], StockQuantity=25, Brand="Fresh Meat", Unit="500g", AverageRating=4.6 },
            new() { Name="Prawns", Description="Fresh tiger prawns 250g", Price=350, Sku="MT004", ImageUrl="https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=400", CategoryId=cat["Meat & Seafood"], StockQuantity=20, Brand="Ocean Fresh", Unit="250g", AverageRating=4.7 },
            new() { Name="McCain Fries", Description="Crispy french fries 400g", Price=130, Sku="FZ001", ImageUrl="https://maplesfood.com/wp-content/uploads/2020/11/mccain-french-fries.jpg", CategoryId=cat["Frozen Foods"], StockQuantity=50, Brand="McCain", Unit="400g", AverageRating=4.5, DiscountPercent=20 },
            new() { Name="Safal Peas", Description="Frozen green peas 500g", Price=65, Sku="FZ002", ImageUrl="https://bachatkart.in/image/cache/catalog/Alpino/safal%20green%20matar-700x700.jpg", CategoryId=cat["Frozen Foods"], StockQuantity=60, Brand="Safal", Unit="500g", AverageRating=4.3, DiscountPercent=12 },
            new() { Name="Aloo Paratha", Description="Frozen stuffed paratha 4 pieces", Price=75, Sku="FZ004", ImageUrl="https://palatesdesire.com/wp-content/uploads/2018/08/Aloo_paratha@palates_desire-scaled.jpg", CategoryId=cat["Frozen Foods"], StockQuantity=35, Brand="Safal", Unit="4 pieces", AverageRating=4.5 },
            new() { Name="Mango", Description="Alphonso mangoes", Price=250, Sku="FV007", ImageUrl="https://www.paperandtea.com/cdn/shop/articles/Mango_6fb74c95-c9b0-4559-88e8-f542e6d6b18d.jpg?v=1769533193&width=1024", CategoryId=cat["Fruits & Vegetables"], StockQuantity=40, Brand="Ratnagiri Fresh", Unit="kg", AverageRating=4.9 },
            new() { Name="Apple", Description="Crisp red apples - Kashmir", Price=180, Sku="FV002", ImageUrl="https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=80, Brand="Kashmir Orchards", Unit="kg", AverageRating=4.7 },
            new() { Name="Banana", Description="Fresh yellow bananas", Price=60, Sku="FV001", ImageUrl="https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=100, Brand="Fresh Farm", Unit="dozen", AverageRating=4.5 },
            new() { Name="Tomatoes", Description="Fresh red tomatoes", Price=40, Sku="FV003", ImageUrl="https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=70, Brand="Farm Fresh", Unit="kg", AverageRating=4.3 },
            new() { Name="Tata Salt", Description="Iodized salt 1kg", Price=20, Sku="PA001", ImageUrl="https://dmaxpro.in/wp-content/uploads/2023/08/tata-salt-1kg.png", CategoryId=cat["Pantry"], StockQuantity=100, Brand="Tata", Unit="1kg", AverageRating=4.5 },
            new() { Name="Fortune Sunflower Oil", Description="Refined sunflower oil 1L", Price=140, Sku="PA002", ImageUrl="https://gropharm.in/wp-content/uploads/2021/04/fortune.jpg", CategoryId=cat["Pantry"], StockQuantity=60, Brand="Fortune", Unit="1L", AverageRating=4.4, DiscountPercent=15 },
            new() { Name="India Gate Basmati Rice", Description="Premium basmati rice 1kg", Price=120, Sku="PA003", ImageUrl="https://m.media-amazon.com/images/I/71s30bA7zeL.jpg", CategoryId=cat["Pantry"], StockQuantity=80, Brand="India Gate", Unit="1kg", AverageRating=4.7, DiscountPercent=10 },
            new() { Name="Tropicana Juice", Description="Mixed fruit juice 1L", Price=120, Sku="BV006", ImageUrl="https://cdn.grofers.com/da/cms-assets/cms/product/5ddc9f79-a8ba-4182-b505-1748df04e7c3.jpg", CategoryId=cat["Beverages"], StockQuantity=50, Brand="Tropicana", Unit="1L", AverageRating=4.6, DiscountPercent=10 },
            new() { Name="Haldiram Bhujia", Description="Spicy noodles 200g", Price=60, Sku="SN003", ImageUrl="https://www.haldirams.com/media/catalog/product/cache/71134970afb779eb7860339989626b7e/a/l/aloo_bhujia12344.jpg", CategoryId=cat["Snacks"], StockQuantity=70, Brand="Haldiram", Unit="200g", AverageRating=4.6, DiscountPercent=15 },
            new() { Name="Kurkure Masala Munch", Description="Corn puffs 85g", Price=20, Sku="SN002", ImageUrl="https://cdn.grofers.com/da/cms-assets/cms/product/c909b249-b8f5-4422-93fb-a0c4567e78dd.jpg", CategoryId=cat["Snacks"], StockQuantity=90, Brand="Kurkure", Unit="85g", AverageRating=4.4 },
            new() { Name="Bisleri Water", Description="Mineral water 1L", Price=20, Sku="BV002", ImageUrl="https://5.imimg.com/data5/SELLER/Default/2024/5/417356491/HN/SK/SL/126647970/1l-bisleri-bottle.jpg", CategoryId=cat["Beverages"], StockQuantity=90, Brand="Bisleri", Unit="1L", AverageRating=4.3 },
            new() { Name="Coca Cola", Description="Soft drink 600ml", Price=40, Sku="BV005", ImageUrl="https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=400", CategoryId=cat["Beverages"], StockQuantity=100, Brand="Coca Cola", Unit="600ml", AverageRating=4.4 },
            new() { Name="Pav Bread", Description="Mumbai pav 6 pieces", Price=20, Sku="BK004", ImageUrl="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTc0gdNKu03NK0wQjn1j-Sy83fBGtQWF85XqQ&s", CategoryId=cat["Bakery"], StockQuantity=60, Brand="Local Bakery", Unit="6 pieces", AverageRating=4.2 },
            new() { Name="Cake Rusk", Description="Tea rusk 200g", Price=35, Sku="BK005", ImageUrl="https://www.yummyoyummy.com/wp-content/uploads/2016/01/Cake-rusk-1.jpg", CategoryId=cat["Bakery"], StockQuantity=40, Brand="Britannia", Unit="200g", AverageRating=4.5 },
            new() { Name="Chicken Eggs", Description="White eggs 12 pack", Price=80, Sku="MT005", ImageUrl="https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", CategoryId=cat["Meat & Seafood"], StockQuantity=60, Brand="Keggfarms", Unit="12 eggs", AverageRating=4.5 },
            new() { Name="Rohu Fish", Description="Fresh rohu fish 500g", Price=180, Sku="MT003", ImageUrl="https://siamcanadian.com/wp-content/uploads/2022/03/Rohu_B.jpg", CategoryId=cat["Meat & Seafood"], StockQuantity=30, Brand="Ocean Fresh", Unit="500g", AverageRating=4.4 },
            new() { Name="Frozen Pizza", Description="Margherita pizza 250g", Price=180, Sku="FZ005", ImageUrl="https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400", CategoryId=cat["Frozen Foods"], StockQuantity=25, Brand="Dr. Oetker", Unit="250g", AverageRating=4.3 },
            new() { Name="Frozen Corn", Description="Sweet corn kernels 500g", Price=75, Sku="FZ003", ImageUrl="https://images.unsplash.com/photo-1551754655-cd27e38d2076?w=400", CategoryId=cat["Frozen Foods"], StockQuantity=45, Brand="Safal", Unit="500g", AverageRating=4.4 },
            new() { Name="Onions", Description="Red onions - Nashik", Price=35, Sku="FV005", ImageUrl="https://images.unsplash.com/photo-1508747703725-719777637510?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=120, Brand="Nashik Fresh", Unit="kg", AverageRating=4.1 },
            new() { Name="Potatoes", Description="Fresh potatoes", Price=30, Sku="FV006", ImageUrl="https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=150, Brand="Farm Direct", Unit="kg", AverageRating=4.2 },
            new() { Name="Spinach", Description="Fresh spinach leaves", Price=25, Sku="FV004", ImageUrl="https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=60, Brand="Organic India", Unit="250g", AverageRating=4.6 },
            new() { Name="Carrots", Description="Fresh orange carrots", Price=50, Sku="FV008", ImageUrl="https://images.unsplash.com/photo-1445282768818-728615cc910a?w=400", CategoryId=cat["Fruits & Vegetables"], StockQuantity=85, Brand="Farm Direct", Unit="kg", AverageRating=4.3 },
            new() { Name="MDH Garam Masala", Description="Spice blend 100g", Price=85, Sku="PA005", ImageUrl="https://m.media-amazon.com/images/I/51Rv14rB3UL.jpg", CategoryId=cat["Pantry"], StockQuantity=70, Brand="MDH", Unit="100g", AverageRating=4.6 },
            new() { Name="Kissan Tomato Ketchup", Description="Tomato ketchup 500g", Price=95, Sku="PA006", ImageUrl="https://bisarga.com/wp-content/uploads/2021/06/KISSAN-TOMATO-KETCHUP-pouch-1-kg.jpg", CategoryId=cat["Pantry"], StockQuantity=55, Brand="Kissan", Unit="500g", AverageRating=4.5 },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
