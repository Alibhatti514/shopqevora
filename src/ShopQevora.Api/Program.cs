using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopQevora.Api;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
var builder=WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=65536);
var conn=builder.Configuration.GetConnectionString("Shop") ?? throw new Exception("Missing Shop connection string");
builder.Services.AddDbContext<ShopDb>(o=>o.UseMySql(conn,ServerVersion.AutoDetect(conn)));
builder.Services.AddIdentityCore<AppUser>(o=>{o.Password.RequiredLength=8;o.User.RequireUniqueEmail=true;}).AddEntityFrameworkStores<ShopDb>().AddDefaultTokenProviders();
builder.Services.AddSingleton<AuthSigning>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
// Accept pre-rebrand signed sessions for their remaining lifetime; newly issued tokens use ShopQevora.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<AuthSigning>((o,signing)=>{o.Events=new JwtBearerEvents{OnTokenValidated=async context=>{var stamp=context.Principal?.FindFirstValue("security_stamp");if(stamp is not null){var users=context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();var user=await users.GetUserAsync(context.Principal!);if(user is null||await users.GetSecurityStampAsync(user)!=stamp)context.Fail("Session is no longer valid. Sign in again.");}}};o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuers=new[]{"ShopQevora","ShopEase"},ValidAudiences=new[]{"ShopQevoraClient","ShopEaseClient"},IssuerSigningKey=signing.Key,ClockSkew=TimeSpan.FromSeconds(30)};});
builder.Services.AddScoped<GuestOrderService>();
builder.Services.AddDataProtection();builder.Services.AddSingleton(TimeProvider.System);builder.Services.AddSingleton<ContactFormTokens>();builder.Services.AddScoped<ContactInquiryService>();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;
 o.OnRejected=async(context,token)=>{context.HttpContext.Response.Headers.RetryAfter=(context.Lease.TryGetMetadata(MetadataName.RetryAfter,out var retry)?Math.Max(1,(int)Math.Ceiling(retry.TotalSeconds)):60).ToString(System.Globalization.CultureInfo.InvariantCulture);await Results.Json(new{message="Too many requests. Please wait before trying again."},statusCode:429).ExecuteAsync(context.HttpContext);};
 o.AddPolicy("contact-submit",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=Math.Clamp(builder.Configuration.GetValue<int?>("ContactRateLimit:PermitLimit")??5,1,1000),Window=TimeSpan.FromMinutes(10),QueueLimit=0}));
 o.AddPolicy("contact-challenge",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=Math.Clamp(builder.Configuration.GetValue<int?>("ContactRateLimit:ChallengeLimit")??30,1,1000),Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
o.AddPolicy("guest",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=Math.Clamp(builder.Configuration.GetValue<int?>("GuestRateLimit:PermitLimit")??10,1,1000),Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
builder.Services.AddAuthorization();builder.Services.AddCors(o=>o.AddPolicy("client",p=>p.WithOrigins(builder.Configuration["ClientOrigin"]??"http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();
_ = app.Services.GetRequiredService<AuthSigning>();
if(args.Contains("--audit-guest-schema")||args.Contains("--apply-guest-schema")){
 using var scope=app.Services.CreateScope();
 await GuestCheckoutSchema.AuditOrApply(scope.ServiceProvider.GetRequiredService<ShopDb>(),args.Contains("--apply-guest-schema"),Path.Combine(AppContext.BaseDirectory,"database","20261026_AddGuestCheckouts.sql"));
 return;
}
app.UseExceptionHandler(error=>error.Run(async context=>{var error=context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;var status=error is BadHttpRequestException requestError?requestError.StatusCode:500;await Results.Problem(status<500?"The request could not be read. Check the submitted fields and payload size.":"The store could not complete this request. Please retry; existing order and cart data is preserved.",statusCode:status).ExecuteAsync(context);}));
app.UseCors("client");app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.MapProfiles();app.MapContact();
app.MapGet("/api/health",()=>Results.Ok(new{status="ok",application="ShopQevora",instanceId=builder.Configuration["E2E:InstanceId"]}));
app.MapGet("/api/products",async(ShopDb db,string? search,string? category)=>{var q=db.Products.AsNoTracking().AsQueryable();if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Name.Contains(search));if(!string.IsNullOrWhiteSpace(category))q=q.Where(x=>x.Category==category);return await q.OrderBy(x=>x.ProductID).ToListAsync();});
app.MapGet("/api/products/{id:int}",async(ShopDb db,int id)=>await db.Products.FindAsync(id) is {} p?Results.Ok(p):Results.NotFound());
app.MapPost("/api/auth/register",async(RegisterRequest input,UserManager<AppUser> users)=>{if(!RequestValidation.Valid(input))return Results.BadRequest("Email and password required");var user=new AppUser{UserName=input.Email,Email=input.Email};var result=await users.CreateAsync(user,input.Password);return result.Succeeded?Results.Ok(new{message="Account created"}):Results.BadRequest(result.Errors.Select(e=>e.Description));});
app.MapPost("/api/auth/login",async(AuthRequest input,UserManager<AppUser> users,AuthSigning signing)=>{if(!RequestValidation.Valid(input))return Results.BadRequest("Enter a valid email and password.");var user=await users.FindByEmailAsync(input.Email);if(user==null||!await users.CheckPasswordAsync(user,input.Password))return Results.Unauthorized();var claims=new[]{new Claim(JwtRegisteredClaimNames.Sub,user.Id),new Claim(ClaimTypes.NameIdentifier,user.Id),new Claim(ClaimTypes.Email,user.Email!),new Claim("security_stamp",await users.GetSecurityStampAsync(user))};var token=new JwtSecurityToken("ShopQevora","ShopQevoraClient",claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:new SigningCredentials(signing.Key,SecurityAlgorithms.HmacSha256));return Results.Ok(new{token=new JwtSecurityTokenHandler().WriteToken(token),email=user.Email});});
app.MapPost("/api/guest-orders",async(GuestOrderRequest request,ShopDb db,GuestOrderService service)=>{
 if(!await GuestCheckoutSchema.Exists(db))return Results.Problem("Guest checkout is temporarily unavailable. The operator must apply the documented guest schema upgrade.",statusCode:503);
 try{return Results.Ok(await service.Place(request));}
 catch(CheckoutValidationException e){return Results.BadRequest(new{message=e.Message});}
 catch(CheckoutConflictException e){return Results.Conflict(new{message=e.Message});}
}).RequireRateLimiting("guest");
var secure=app.MapGroup("/api").RequireAuthorization();
secure.MapPost("/cart/merge",async(List<GuestLine> lines,ShopDb db,ClaimsPrincipal principal)=>{
 var errors=GuestOrderService.ValidateLines(lines).ToArray();if(errors.Length>0)return Results.BadRequest(errors);
 var ids=lines.Select(l=>l.ProductID).ToArray();if(await db.Products.CountAsync(p=>ids.Contains(p.ProductID))!=ids.Length)return Results.BadRequest(new{message="A guest product is no longer available. Your local bag is preserved."});
 var userId=principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
 var existing=await db.CartItems.Where(x=>x.UserId==userId&&ids.Contains(x.ProductID)).ToDictionaryAsync(x=>x.ProductID);
 foreach(var line in lines){if(existing.TryGetValue(line.ProductID,out var item))item.Quantity=Math.Max(item.Quantity,line.Quantity);else db.CartItems.Add(new CartItem{UserId=userId,ProductID=line.ProductID,Quantity=line.Quantity});}
 await db.SaveChangesAsync();return Results.NoContent();
});
secure.MapGet("/cart",async(ShopDb db,ClaimsPrincipal principal)=>{var cart=new Cart(db,principal.FindFirstValue(ClaimTypes.NameIdentifier)!);return Results.Ok(await cart.DisplayCartItems());});
secure.MapPost("/cart/{id:int}",async(int id,ShopDb db,ClaimsPrincipal principal)=>{var p=await db.Products.FindAsync(id);if(p==null)return Results.NotFound();await new Cart(db,principal.FindFirstValue(ClaimTypes.NameIdentifier)!).AddProduct(p);return Results.NoContent();});
secure.MapDelete("/cart/{id:int}",async(int id,ShopDb db,ClaimsPrincipal principal)=>{await new Cart(db,principal.FindFirstValue(ClaimTypes.NameIdentifier)!).RemoveProduct(id);return Results.NoContent();});
secure.MapPut("/cart/{id:int}",async(int id,QuantityRequest request,ShopDb db,ClaimsPrincipal principal)=>{if(request.Quantity is <1 or >99)return Results.BadRequest();var userId=principal.FindFirstValue(ClaimTypes.NameIdentifier)!;var item=await db.CartItems.SingleOrDefaultAsync(x=>x.UserId==userId&&x.ProductID==id);if(item==null)return Results.NotFound();item.Quantity=request.Quantity;await db.SaveChangesAsync();return Results.NoContent();});
secure.MapPost("/orders",async(ShopDb db,ClaimsPrincipal principal)=>{var userId=principal.FindFirstValue(ClaimTypes.NameIdentifier)!;await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);var items=await db.CartItems.Include(x=>x.Product).Where(x=>x.UserId==userId).ToListAsync();if(items.Count==0)return Results.BadRequest("Cart is empty");var order=new Order{UserId=userId,Total=OrderTotals.Calculate(items),Lines=items.Select(x=>new OrderLine{ProductName=x.Product.Name,UnitPrice=x.Product.Price,Quantity=x.Quantity}).ToList()};db.Orders.Add(order);db.CartItems.RemoveRange(items);await db.SaveChangesAsync();await transaction.CommitAsync();return Results.Ok(new{order.Id,order.Total});});
secure.MapGet("/orders",async(ShopDb db,ClaimsPrincipal principal)=>await db.Orders.AsNoTracking().Include(x=>x.Lines).Where(x=>x.UserId==principal.FindFirstValue(ClaimTypes.NameIdentifier)).OrderByDescending(x=>x.CreatedUtc).ToListAsync());
// Development-only schema initialization and sample products; never auto-seed production.
if(app.Environment.IsDevelopment()){
 using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDb>();await db.Database.EnsureCreatedAsync();
 await DemoCatalog.SeedAsync(db);
}
app.Run();
public partial class Program {}
