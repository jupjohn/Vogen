using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Vogen;

namespace UsingTypesGeneratedInTheSameProject;

/*
 * In this scenario, we want to use the System.Text.Json converters
 * for the value objects that were generated by Vogen in this project.
 *
 * We create a `Supplier` which contains value objects, we serialize it to a string,
 * and we deserialize back into an Order.
 *
 * We use the `SupplierGenerationContext` below and tell it about `Supplier`. It then goes through its properties
 * and builds a mapping of converters.
 *
 * **NOTE** - because the value objects WERE BUILT IN THIS PROJECT, they are not 'fully formed', so we need to
 * tell System.Text.Json to use the 'type factory' that Vogen generates (Infra.VogenTypesFactory) to get its hints about
 * mapping types to converters.
 */


[EfCoreConverter<Id>]
[EfCoreConverter<Name>]
[EfCoreConverter<Age>]
[EfCoreConverter<Department>]
[EfCoreConverter<HireDate>]
public partial class EfCoreConverters;

public static class EfCoreScenario
{
    public static void Run()
    {
        AddAndSaveItems(amount: 10);
        AddAndSaveItems(amount: 10);

        PrintItems();

        return;

        static void AddAndSaveItems(int amount)
        {
            using var context = new DbContext();

            for (int i = 0; i < amount; i++)
            {
                var entity = new EmployeeEntity
                {
                    Name = Name.From("Fred #" + i),
                    Age = Age.From(42 + i),
                    Department = Department.From("Quarry"),
                    HireDate = HireDate.From(new DateOnly(1066, 12, 13))
                };

                context.Entities.Add(entity);
            }

            context.SaveChanges();
        }

        static void PrintItems()
        {
            using var ctx = new DbContext();

            var entities = ctx.Entities.ToList();
            Console.WriteLine(string.Join(Environment.NewLine, entities.Select(e => $"ID: {e.Id.Value}, Name: {e.Name}, Age: {e.Age}")));
        }
    }
}

internal class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<EmployeeEntity> Entities { get; set; } = default!;

    // you can use this method explicitly when creating your entities, or use SomeIdValueGenerator as shown below
    // public int GetNextMyEntityId()
    // {
    //     var maxLocalId = SomeEntities.Local.Any() ? SomeEntities.Local.Max(e => e.Id.Value) : 0;
    //     var maxSavedId = SomeEntities.Any() ? SomeEntities.Max(e => e.Id.Value) : 0;
    //     return Math.Max(maxLocalId, maxSavedId) + 1;
    // }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<EmployeeEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(e => e.Id).HasValueGenerator<SomeIdValueGenerator>();
            b.Property(e => e.Id).HasVogenConversion();
            b.Property(e => e.Name).HasVogenConversion();
            b.Property(e => e.Department).HasVogenConversion();
            b.Property(e => e.HireDate).HasVogenConversion();
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("SomeDB");
    }
}

internal class SomeIdValueGenerator : ValueGenerator<Id>
{
    public override Id Next(EntityEntry entry)
    {
        var entities = ((DbContext)entry.Context).Entities;

        var next = Math.Max(MaxFrom(entities.Local), MaxFrom(entities)) + 1;

        return Id.From(next);

        static int MaxFrom(IEnumerable<EmployeeEntity> es)
        {
            return es.Any() ? es.Max(e => e.Id.Value) : 0;
        }
    }

    public override bool GeneratesTemporaryValues => false;
}