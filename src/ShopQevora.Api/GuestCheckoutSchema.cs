using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
namespace ShopQevora.Api;

// Explicit, additive upgrade for the preexisting EnsureCreated database. No hidden DDL on startup.
public static class GuestCheckoutSchema
{
    private static async Task<string?> Scalar(DbConnection connection,string sql) { using var command=connection.CreateCommand();command.CommandText=sql;var value=await command.ExecuteScalarAsync();return value is null or DBNull?null:Convert.ToString(value); }
    public static async Task<bool> Exists(ShopDb db)
    {
        if(!db.Database.IsRelational())return true; // Test-only provider; production DI always uses MySQL.
        var connection=db.Database.GetDbConnection();var close=connection.State!=ConnectionState.Open;if(close)await connection.OpenAsync();
        try {return await Scalar(connection,"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='GuestCheckouts'")=="1";}
        finally{if(close)await connection.CloseAsync();}
    }
    public static async Task AuditOrApply(ShopDb db,bool apply,string sqlPath)
    {
        var connection=db.Database.GetDbConnection();await connection.OpenAsync();
        try {
            if(await Scalar(connection,"SELECT GET_LOCK('shopqevora_guest_schema',15)")!="1")throw new InvalidOperationException("Another schema upgrade is active.");
            if(await Scalar(connection,"SELECT ENGINE FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='Orders'")!="InnoDB")throw new InvalidOperationException("Expected existing InnoDB Orders. Stop and review schema manually.");
            var type=await Scalar(connection,"SELECT COLUMN_TYPE FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='Orders' AND column_name='Id'");
            if(type is not ("int" or "int(11)"))throw new InvalidOperationException("Orders.Id is not the expected signed int. No changes applied.");
            if(await Scalar(connection,"SELECT COUNT(*) FROM information_schema.key_column_usage WHERE table_schema=DATABASE() AND table_name='Orders' AND column_name='UserId' AND referenced_table_name IS NOT NULL")!="0")throw new InvalidOperationException("Orders.UserId has a foreign key. A separately reviewed migration is required; no changes applied.");
            if(await Exists(db)) {
                var columns=await Scalar(connection,"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='GuestCheckouts' AND column_name IN ('OrderId','Reference','CheckoutKeyHash','RequestHash','ContactJson')");
                if(columns!="5")throw new InvalidOperationException("Existing GuestCheckouts has an unexpected shape; no changes applied.");
                if(await Scalar(connection,"SELECT COUNT(DISTINCT index_name) FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='GuestCheckouts' AND non_unique=0 AND column_name IN ('Reference','CheckoutKeyHash')")!="2")throw new InvalidOperationException("GuestCheckouts is missing required unique indexes; no changes applied.");
                if(await Scalar(connection,"SELECT COUNT(*) FROM information_schema.key_column_usage WHERE table_schema=DATABASE() AND table_name='GuestCheckouts' AND column_name='OrderId' AND referenced_table_name='Orders' AND referenced_column_name='Id'")!="1")throw new InvalidOperationException("GuestCheckouts is missing its expected order relationship; no changes applied.");
                // Query the expected EF shape as a further read-only compatibility check.
                await db.GuestCheckouts.AsNoTracking().OrderBy(x=>x.OrderId).Take(1).ToListAsync();
                Console.WriteLine("Guest schema already present; no DDL or data changes applied.");return;
            }
            Console.WriteLine("Verified existing Orders schema. GuestCheckouts is missing.");
            if(!apply){Console.WriteLine("Review DATABASE_UPGRADE.md, back up privately, then run --apply-guest-schema.");return;}
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(sqlPath));
            Console.WriteLine("Created GuestCheckouts only. Existing tables and rows were not modified.");
        }
        finally {try{await Scalar(connection,"SELECT RELEASE_LOCK('shopqevora_guest_schema')");}finally{await connection.CloseAsync();}}
    }
}
