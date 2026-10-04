using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Masters;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Tests.Infrastructure;

internal sealed class TestDbContext : ImsDbContext
{
    public TestDbContext(DbContextOptions<ImsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
            {
                prop.SetDefaultValueSql(null);
                prop.SetColumnType(null);
            }
    }
}

internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    public ImsDbContext Db { get; }

    private TestDb()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<ImsDbContext>()
            .UseSqlite(_conn)
            .Options;
        Db = new TestDbContext(options);
        Db.Database.EnsureCreated();
    }

    public static TestDb Empty() => new();

    public static TestDb Seeded()
    {
        var t = new TestDb();
        var db = t.Db;
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Factories.Add(new Factory
        {
            FactoryCode = "FAC-01", FactoryName = "Main factory",
            Country = "Vietnam", IsActive = true, CreatedAt = now,
        });
        db.Factories.Add(new Factory
        {
            FactoryCode = "FAC-DEL", FactoryName = "Deleted factory",
            IsActive = true, IsDeleted = true, CreatedAt = now,
        });

        db.Zones.Add(new Zone
        {
            ZoneCode = "ZONE-A", ZoneName = "Zone A",
            FactoryCode = "FAC-01", IsActive = true, CreatedAt = now,
        });

        db.Locations.Add(new Location
        {
            LocationCode = "W1", LocationType = LocationType.Large, LargeCode = "W1",
            LocationName = "Warehouse 1", ZoneCode = "ZONE-A", IsActive = true, CreatedAt = now,
        });
        db.Locations.Add(new Location
        {
            LocationCode = "W1_ZA", LocationType = LocationType.Middle,
            LargeCode = "W1", MiddleCode = "ZA", LocationName = "Zone A aisle",
            ZoneCode = "ZONE-A", ParentLocationCode = "W1", IsActive = true, CreatedAt = now,
        });

        db.ItemGroups.Add(new ItemGroup
        {
            ItemGroupCode = "IG-01", ItemGroupName = "Group 1",
            Category = ItemGroupCategory.RawMaterials, IsActive = true, CreatedAt = now,
        });

        db.Roles.Add(new Role { RoleCode = "Admin", RoleName = "Admin", IsActive = true, CreatedAt = now });
        db.Languages.Add(new Language { LanguageCode = "vi", LanguageName = "Vietnamese", IsActive = true, CreatedAt = now });

        db.DefaultValues.Add(new DefaultValue
        {
            ValueType = "PackingType", ValueCode = "BOX",
            NameLang1 = "Box", NameLang2 = "箱", NameLang3 = "Thùng",
            IsActive = true, CreatedAt = now,
        });

        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
