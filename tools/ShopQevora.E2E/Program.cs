using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using ShopQevora.Api;

var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var statePath=Path.Combine(root,".e2e","runtime.json");
var configuration=new ConfigurationBuilder().SetBasePath(Path.Combine(root,"src/ShopQevora.Api")).AddJsonFile("appsettings.json").AddUserSecrets(typeof(ShopDb).Assembly).AddEnvironmentVariables().Build();
var source=configuration.GetConnectionString("Shop")??throw new InvalidOperationException("Existing MySQL configuration is required.");
try {
 if(args.FirstOrDefault()=="upgrade-contact"){await ShopQevora.Maintenance.ContactUpgrade.Apply(source,Path.Combine(root,"database/20261026_AddContactInquiries.sql"));return;}
 if(args.FirstOrDefault()=="upgrade-profiles") {
  if(OperatingSystem.IsWindows())throw new InvalidOperationException("Use a reviewed private backup and manual profile upgrade on Windows.");
  var cs=new MySqlConnectionStringBuilder(source);if(cs.Server is not ("127.0.0.1" or "localhost"))throw new InvalidOperationException("Only local upgrades are supported.");
  await using var db= new MySqlConnection(source);await db.OpenAsync();
  await using var inspect=db.CreateCommand();inspect.CommandText="SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='CustomerProfiles'";
  if(Convert.ToInt32(await inspect.ExecuteScalarAsync())>0){Console.WriteLine("CustomerProfiles already exists; no schema changes made.");return;}
  var backupDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".local/share/ShopQevora/backups");Directory.CreateDirectory(backupDir);if(!OperatingSystem.IsWindows())File.SetUnixFileMode(backupDir,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  var backup=Path.Combine(backupDir,"before-profiles-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".sql");
  using(var output=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
   if(!OperatingSystem.IsWindows())File.SetUnixFileMode(backup,UnixFileMode.UserRead|UnixFileMode.UserWrite);
   var start=new System.Diagnostics.ProcessStartInfo("mysqldump"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
   foreach(var argument in new[]{"--host="+cs.Server,"--port="+cs.Port,"--user="+cs.UserID,"--single-transaction","--no-tablespaces","--skip-lock-tables",cs.Database})start.ArgumentList.Add(argument);
   start.Environment["MYSQL_PWD"]=cs.Password;
   using var dump=System.Diagnostics.Process.Start(start)!;var errors=dump.StandardError.ReadToEndAsync();await dump.StandardOutput.BaseStream.CopyToAsync(output);await dump.WaitForExitAsync();await errors;if(dump.ExitCode!=0||output.Length<1000)throw new InvalidOperationException("Backup failed. No schema changes applied; inspect MySQL backup grants privately.");
  }
  inspect.CommandText="SELECT COLLATION_NAME FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='AspNetUsers' AND column_name='Id'";var collation=Convert.ToString(await inspect.ExecuteScalarAsync());if(collation is null||!Regex.IsMatch(collation,"^utf8mb4_[a-z0-9_]+$"))throw new InvalidOperationException("Unexpected Identity key collation; review manually.");
  var sql=await File.ReadAllTextAsync(Path.Combine(root,"database/20261026_AddCustomerProfiles.sql"));sql=sql.Replace("CHARACTER SET utf8mb4 NOT NULL","CHARACTER SET utf8mb4 COLLATE "+collation+" NOT NULL");
  inspect.CommandText=sql;await inspect.ExecuteNonQueryAsync();Console.WriteLine("Private database backup saved outside the repository. Added CustomerProfiles only; existing records unchanged.");return;
 }
 if(args.FirstOrDefault()=="seed-catalog") {await using var db=new ShopDb(new DbContextOptionsBuilder<ShopDb>().UseMySql(source,new MySqlServerVersion(new Version(8,4,0))).Options);var before=await db.Products.CountAsync();await DemoCatalog.SeedAsync(db);await DemoCatalog.SeedAsync(db);Console.WriteLine($"Catalog: {before} before, {await db.Products.CountAsync()} after two idempotent seed passes. Existing product IDs and prices preserved.");return;}
 if(args.FirstOrDefault()=="accept-catalog-extension") {
  var staleState=Read();if(staleState.OwnerPid>0){try{if(!System.Diagnostics.Process.GetProcessById(staleState.OwnerPid).HasExited)throw new InvalidOperationException("A fixture run is active.");}catch(ArgumentException){}}
  await using var db=Database(staleState);var original=JsonSerializer.Deserialize<List<Product>>(await File.ReadAllTextAsync(Path.Combine(root,"output/database-image-audit.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var current=await db.Products.AsNoTracking().OrderBy(x=>x.ProductID).ToListAsync();foreach(var p in original){var match=current.SingleOrDefault(x=>x.ProductID==p.ProductID);if(match is null||match.Name!=p.Name||match.Price!=p.Price||match.Category!=p.Category||match.Description!=p.Description||match.ImageUrl!=p.ImageUrl)throw new InvalidOperationException("An original product differs; no baseline updated.");}
  if(current.Count!=36||current.Any(p=>!original.Any(o=>o.ProductID==p.ProductID)&&!DemoCatalog.Products.Any(d=>d.Name==p.Name&&d.Price==p.Price&&d.ImageUrl==p.ImageUrl)))throw new InvalidOperationException("Unexpected catalog extension.");
  if(await db.Users.CountAsync()!=staleState.Users||await db.Orders.CountAsync()!=staleState.Orders||await db.CartItems.CountAsync()!=staleState.CartItems)throw new InvalidOperationException("Fixture counts differ; review before updating the baseline.");
  staleState.ProductsHash=await ProductHash(db);Write(staleState);Console.WriteLine("Verified all original product records unchanged and exactly 16 deliberate additions. Updated stale fixture catalog fingerprint only.");return;
 }
 if(args.FirstOrDefault()=="cleanup") {if(File.Exists(statePath))await Cleanup(Read());return;}
 if(args.FirstOrDefault()!="prepare")throw new InvalidOperationException("Use prepare or cleanup.");
 if(File.Exists(statePath)&&!Read().Cleaned)await Cleanup(Read());
 var builder=new MySqlConnectionStringBuilder(source);
 if(builder.Server is not ("127.0.0.1" or "localhost" or "::1"))throw new InvalidOperationException("E2E fixtures are restricted to local MySQL. No remote data was touched.");
 await using var connection=new MySqlConnection(source);await connection.OpenAsync();
 var dedicated=false;
 await using(var command=connection.CreateCommand()){
  command.CommandText="CREATE DATABASE IF NOT EXISTS `ShopQevora_E2E` CHARACTER SET utf8mb4";
  try{await command.ExecuteNonQueryAsync();builder.Database="ShopQevora_E2E";dedicated=true;}
  catch(MySqlException ex)when(ex.Number is 1044 or 1142){ }
 }
 var state=new RuntimeState{OwnerPid=int.TryParse(Environment.GetEnvironmentVariable("SHOPQEVORA_E2E_OWNER_PID"),out var owner)?owner:0,RunId=Guid.NewGuid().ToString("N"),Mode=dedicated?"dedicated":"isolated-fixtures",ConnectionString=builder.ConnectionString,SigningKey=Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))};
 await using(var db=Database(state)){
  if(dedicated){await db.Database.EnsureCreatedAsync();await DemoCatalog.SeedAsync(db);}
  if(!await GuestCheckoutSchema.Exists(db))throw new InvalidOperationException("Apply the reviewed guest schema upgrade before running E2E. No upgrade was applied automatically.");
  if(!await ContactEndpoints.SchemaExists(db))throw new InvalidOperationException("Apply the explicit backed-up contact schema upgrade before running E2E.");
  state.Inquiries=await db.ContactInquiries.CountAsync();
  state.ProductsHash=await ProductHash(db);
  state.Users=await db.Users.CountAsync();state.Orders=await db.Orders.CountAsync();state.CartItems=await db.CartItems.CountAsync();
 }
 Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);Write(state);
 Console.WriteLine($"MySQL ready; {state.Mode} prepared. Existing catalog and customer records are outside the fixture scope.");
}
catch(Exception ex)when(ex is MySqlException){Console.Error.WriteLine("MySQL fixture setup/cleanup failed. Credentials were not displayed. Review local MySQL availability and grants.");Environment.ExitCode=1;}

RuntimeState Read()=>JsonSerializer.Deserialize<RuntimeState>(File.ReadAllText(statePath))!;
void Write(RuntimeState state){File.WriteAllText(statePath,JsonSerializer.Serialize(state));if(!OperatingSystem.IsWindows())File.SetUnixFileMode(statePath,UnixFileMode.UserRead|UnixFileMode.UserWrite);}
ShopDb Database(RuntimeState state)=>new(new DbContextOptionsBuilder<ShopDb>().UseMySql(state.ConnectionString,new MySqlServerVersion(new Version(8,4,0))).Options);
async Task<string> ProductHash(ShopDb db)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(await db.Products.AsNoTracking().OrderBy(x=>x.ProductID).ToListAsync()))));
async Task Cleanup(RuntimeState state){
 if(state.Cleaned){Console.WriteLine("Fixture run is already clean; no data changes applied.");return;}
 if(state.OwnerPid>0&&state.OwnerPid.ToString()!=Environment.GetEnvironmentVariable("SHOPQEVORA_E2E_OWNER_PID")){try{var active=System.Diagnostics.Process.GetProcessById(state.OwnerPid);if(!active.HasExited)throw new InvalidOperationException("Another E2E run is active; cleanup refused.");}catch(ArgumentException){}}
 if(!Regex.IsMatch(state.RunId,"^[a-f0-9]{32}$"))throw new InvalidOperationException("Invalid run scope; cleanup refused.");
 var prefix="e2e-"+state.RunId+"-";
 await using var db=Database(state);
 var users=await db.Users.Where(u=>u.Email!=null&&u.Email.StartsWith(prefix)&&u.Email.EndsWith("@example.test")).ToListAsync();
 if(users.Any(u=>u.UserName!=u.Email))throw new InvalidOperationException("Unexpected user fixture shape; cleanup refused.");
 var guests=await db.GuestCheckouts.Where(g=>g.ContactJson.Contains(prefix)).ToListAsync();
 foreach(var guest in guests){var contact=JsonSerializer.Deserialize<GuestContact>(guest.ContactJson);if(contact is null||!contact.Email.StartsWith(prefix,StringComparison.Ordinal)||!contact.Email.EndsWith("@example.test",StringComparison.Ordinal))throw new InvalidOperationException("Guest fixture marker mismatch; cleanup refused.");}
 var userIds=users.Select(u=>u.Id).ToArray();var guestIds=guests.Select(g=>g.OrderId).ToArray();
 var orders=await db.Orders.Include(o=>o.Lines).Where(o=>userIds.Contains(o.UserId)||guestIds.Contains(o.Id)).ToListAsync();
 if(orders.Any(o=>guestIds.Contains(o.Id)&&o.UserId!="guest:"+guests.Single(g=>g.OrderId==o.Id).Reference))throw new InvalidOperationException("Guest owner mismatch; cleanup refused.");
 var inquiries=await db.ContactInquiries.Where(x=>x.Email.StartsWith(prefix)&&x.Email.EndsWith("@example.test")).ToListAsync();
 if(inquiries.Any(x=>x.UserId is not null&&!userIds.Contains(x.UserId)))throw new InvalidOperationException("Contact fixture owner mismatch; cleanup refused.");
 await using var transaction=await db.Database.BeginTransactionAsync();
 db.ContactInquiries.RemoveRange(inquiries);
 db.GuestCheckouts.RemoveRange(guests);await db.SaveChangesAsync();
 db.OrderLines.RemoveRange(orders.SelectMany(o=>o.Lines));db.Orders.RemoveRange(orders);
 db.CartItems.RemoveRange(await db.CartItems.Where(c=>userIds.Contains(c.UserId)).ToListAsync());db.Users.RemoveRange(users);await db.SaveChangesAsync();await transaction.CommitAsync();
 if(await ProductHash(db)!=state.ProductsHash)throw new InvalidOperationException("Catalog changed during E2E; investigate without resetting any data.");
 state.Cleaned=true;state.ConnectionString="";state.SigningKey="";Write(state);
 var unchanged=state.Users==await db.Users.CountAsync()&&state.Orders==await db.Orders.CountAsync()&&state.CartItems==await db.CartItems.CountAsync()&&state.Inquiries==await db.ContactInquiries.CountAsync();
 Console.WriteLine($"Removed only this run's {users.Count} disposable users, {orders.Count} orders and {inquiries.Count} inquiries. Catalog unchanged. Baseline customer/cart/order counts {(unchanged?"restored":"changed externally; no unrelated rows were removed")}.");
}
sealed class RuntimeState{
 public int OwnerPid{get;set;}
 public string RunId{get;set;}="";public string Mode{get;set;}="";public string ConnectionString{get;set;}="";public string SigningKey{get;set;}="";public string ProductsHash{get;set;}="";public int Users{get;set;}public int Orders{get;set;}public int CartItems{get;set;}public int Inquiries{get;set;}public bool Cleaned{get;set;}
}
