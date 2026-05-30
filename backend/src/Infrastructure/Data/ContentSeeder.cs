using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public static class ContentSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedServicesAsync(db);
        await SeedPriceExamplesAsync(db);
        await SeedArticlesAsync(db);
    }

    private static async Task SeedServicesAsync(AppDbContext db)
    {
        if (await db.Services.AnyAsync()) return;   // только в пустую таблицу
        db.Services.AddRange(
            new Service { Title="Строительство", Slug="stroitelstvo", IconName="home", SortOrder=1, IsPublished=true,
                ShortDescription="Дома, беседки, сараи и подобное — по нашему или вашему проекту.",
                Content="<p>Строим частные дома, беседки, сараи и хозпостройки. Можем работать по вашему проекту или спроектировать сами.</p>", Tag="stroitelstvo" },
            new Service { Title="Ремонт и пристройки", Slug="remont-pristroyki", IconName="hammer", SortOrder=2, IsPublished=true,
                ShortDescription="Веранды, пристройки, расширение, фасады.",
                Content="<p>Ремонт частных построек, веранды и пристройки, расширение, фасадные работы.</p>", Tag="remont" },
            new Service { Title="Замена фундамента", Slug="zamena-fundamenta", IconName="layers", SortOrder=3, IsPublished=true,
                ShortDescription="Подведём новый надёжный фундамент под существующую постройку.",
                Content="<p>Меняем фундамент под существующими постройками.</p>", Tag="foundation" },
            new Service { Title="Передвижка построек", Slug="peredvizhka", IconName="move", SortOrder=4, IsPublished=true,
                ShortDescription="Аккуратно: бытовки и небольшие строения.",
                Content="<p>Передвигаем небольшие строения и бытовки.</p>", Tag="peredvizhka" },
            new Service { Title="Под ключ", Slug="pod-klyuch", IconName="key", SortOrder=5, IsPublished=true,
                ShortDescription="Закупим материалы по своей цене и привезём.",
                Content="<p>Берём закупку и доставку материалов на себя — по своей цене.</p>", Tag="pod-klyuch" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedPriceExamplesAsync(AppDbContext db)
    {
        if (await db.ServicePrices.AnyAsync()) return;   // боевые цены не трогаем
        db.ServicePrices.AddRange(
            new ServicePrice { Title="Замена фундамента дачного дома 4×5 с верандой", SortOrder=1, Price=340000, Duration="2 дня",
                Description="С бетонных блоков на винтовые сваи и швеллеры.", ArticleSlug="zamena-fundamenta-svai", Tag="foundation" },
            new ServicePrice { Title="Утеплённая каркасная пристройка 3×7", SortOrder=2, Price=750000, Duration="1 неделя",
                Description="Сэндвич-панели, 2 окна.", Tag="remont" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedArticlesAsync(AppDbContext db)
    {
        if (await db.Articles.AnyAsync()) return;   // боевые статьи не трогаем
        db.Articles.Add(new Article {
            Title="Замена фундамента на винтовые сваи и швеллеры",
            Slug="zamena-fundamenta-svai",
            Summary="Как заменили фундамент дачного дома 4×5 с бетонных блоков на сваи и швеллеры.",
            Content="<p>Дачный дом 4×5 с верандой стоял на бетонных блоках. Предложили винтовые сваи по периметру и швеллеры. Сделали за два дня.</p>",
            Tags="foundation", IsPublished=true, PublishedAt=DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}
