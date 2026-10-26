using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using SkiaSharp;
namespace ShopQevora.Api;
public class CustomerProfile {
 [Key,MaxLength(255)]public string UserId{get;set;}="";
 [MaxLength(120)]public string FullName{get;set;}="";
 [MaxLength(60)]public string DisplayName{get;set;}="";
 [MaxLength(32)]public string Phone{get;set;}="";
 [MaxLength(2)]public string PhoneRegion{get;set;}="PK";
 [MaxLength(240)]public string Address{get;set;}="";
 [MaxLength(100)]public string City{get;set;}="";
 [MaxLength(100)]public string Region{get;set;}="";
 [MaxLength(20)]public string PostalCode{get;set;}="";
 [MaxLength(2)]public string Country{get;set;}="PK";
 public byte[]? Avatar{get;set;}
 [MaxLength(254)]public string? PendingEmail{get;set;}
}
public record ProfileInput([property:StringLength(120)]string FullName,[property:StringLength(60)]string DisplayName,[property:StringLength(32)]string Phone,[property:Required,StringLength(2,MinimumLength=2)]string PhoneRegion,[property:StringLength(240)]string Address,[property:StringLength(100)]string City,[property:StringLength(100)]string Region,[property:StringLength(20)]string PostalCode,[property:Required,StringLength(2,MinimumLength=2)]string Country);
public record PasswordInput(string CurrentPassword,string NewPassword);
public record EmailInput(string Email,string CurrentPassword);
public record AvatarInput(string ContentType,string Data);
public static class ProfileEndpoints {
 public static string? NormalizePhone(string? phone,string region){if(string.IsNullOrWhiteSpace(phone))return "";var utility=PhoneNumberUtil.GetInstance();try{var parsed=utility.Parse(phone,region);return utility.IsValidNumber(parsed)?utility.Format(parsed,PhoneNumberFormat.E164):null;}catch(NumberParseException){return null;}}
 public static void MapProfiles(this WebApplication app){
 var routes=app.MapGroup("/api/account").RequireAuthorization();
 routes.MapGet("/dialing-codes",()=>PhoneNumberUtil.GetInstance().GetSupportedRegions().Order().Select(r=>new{Region=r,Code=PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(r)}));
 routes.MapGet("/profile",async(ShopDb db,ClaimsPrincipal principal,UserManager<AppUser> users)=>{
  var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();var p=await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==user.Id)??new CustomerProfile{UserId=user.Id};
  return Results.Ok(new{p.FullName,p.DisplayName,p.Phone,p.PhoneRegion,p.Address,p.City,p.Region,p.PostalCode,p.Country,p.PendingEmail,Email=user.Email,EmailVerified=user.EmailConfirmed,Avatar=p.Avatar is null?null:"data:image/png;base64,"+Convert.ToBase64String(p.Avatar)});
 });
 routes.MapPut("/profile",async(ProfileInput input,ShopDb db,ClaimsPrincipal principal,UserManager<AppUser> users)=>{
  if(!RequestValidation.Valid(input))return Results.BadRequest(new{message="Check field lengths and country selections."});
  if(!PhoneNumberUtil.GetInstance().GetSupportedRegions().Contains(input.Country)||!PhoneNumberUtil.GetInstance().GetSupportedRegions().Contains(input.PhoneRegion))return Results.BadRequest(new{message="Choose a valid country."});
  if(new[]{input.FullName,input.DisplayName,input.Address,input.City,input.Region,input.PostalCode}.Any(value=>value is not null && (value.Contains('<')||value.Contains('>')||value.Contains('\0'))))return Results.BadRequest(new{message="Use plain text without HTML markup in profile fields."});
  var phone=NormalizePhone(input.Phone,input.PhoneRegion);if(phone is null)return Results.BadRequest(new{message="Enter a valid phone number for the selected dialing region. Numbers are not ownership-verified."});
  var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();var p=await Get(db,user.Id);
  p.FullName=input.FullName?.Trim()??"";p.DisplayName=input.DisplayName?.Trim()??"";p.Phone=phone;p.PhoneRegion=input.PhoneRegion;p.Address=input.Address?.Trim()??"";p.City=input.City?.Trim()??"";p.Region=input.Region?.Trim()??"";p.PostalCode=input.PostalCode?.Trim()??"";p.Country=input.Country;await db.SaveChangesAsync();return Results.NoContent();
 });
 routes.MapPost("/password",async(PasswordInput input,ClaimsPrincipal principal,UserManager<AppUser> users)=>{
  if(string.IsNullOrEmpty(input.CurrentPassword)||string.IsNullOrEmpty(input.NewPassword)||input.NewPassword.Length>128)return Results.BadRequest(new{message="Enter your current password and a new password of 8–128 characters."});var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();var result=await users.ChangePasswordAsync(user,input.CurrentPassword,input.NewPassword);return result.Succeeded?Results.Ok(new{message="Password changed. Sign in again on this device. Sessions issued by this version are invalidated. Older compatible JWTs expire within eight hours."}):Results.BadRequest(new{message=string.Join(" ",result.Errors.Select(x=>x.Description))});
 });
 routes.MapPost("/email",async(EmailInput input,ShopDb db,ClaimsPrincipal principal,UserManager<AppUser> users)=>{
  if(string.IsNullOrWhiteSpace(input.Email)||input.Email.Length>254||!new EmailAddressAttribute().IsValid(input.Email)||string.IsNullOrEmpty(input.CurrentPassword))return Results.BadRequest(new{message="Enter a valid email and current password."});var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();if(!await users.CheckPasswordAsync(user,input.CurrentPassword))return Results.BadRequest(new{message="The current password is incorrect."});var existing=await users.FindByEmailAsync(input.Email);if(existing is not null)return Results.BadRequest(new{message="That email is already in use."});var p=await Get(db,user.Id);p.PendingEmail=input.Email.Trim();await db.SaveChangesAsync();return Results.Ok(new{message="Request saved as pending. Email delivery is not configured; your sign-in email is unchanged and the new address is not verified."});
 });
 routes.MapPost("/avatar",async(AvatarInput input,ShopDb db,ClaimsPrincipal principal,UserManager<AppUser> users)=>{
  if(input.ContentType is not ("image/png" or "image/jpeg" or "image/webp")||input.Data is null||input.Data.Length>55000)return Results.BadRequest(new{message="Choose a PNG, JPEG or WebP image up to 40 KB and 2048 × 2048 pixels."});
  byte[] data;try{data=Convert.FromBase64String(input.Data);}catch(FormatException){return Results.BadRequest(new{message="Invalid image data."});}if(data.Length>40960)return Results.BadRequest(new{message="The image must be no larger than 40 KB."});
  using var codec=SKCodec.Create(new SKMemoryStream(data));
  var expected=input.ContentType switch{"image/png"=>SKEncodedImageFormat.Png,"image/jpeg"=>SKEncodedImageFormat.Jpeg,_=>SKEncodedImageFormat.Webp};
  if(codec is null||codec.EncodedFormat!=expected||codec.Info.Width>2048||codec.Info.Height>2048)return Results.BadRequest(new{message="Use a valid PNG, JPEG or WebP image no larger than 2048 × 2048 pixels."});
  using var decoded=new SKBitmap(codec.Info.Width,codec.Info.Height,SKColorType.Rgba8888,SKAlphaType.Premul);if(codec.GetPixels(decoded.Info,decoded.GetPixels())!=SKCodecResult.Success)return Results.BadRequest(new{message="The image is incomplete or corrupt."});
  using var resized=new SKBitmap(160,160,SKColorType.Rgba8888,SKAlphaType.Premul);using(var canvas=new SKCanvas(resized)){var side=Math.Min(decoded.Width,decoded.Height);var left=(decoded.Width-side)/2f;var top=(decoded.Height-side)/2f;canvas.DrawBitmap(decoded,new SKRect(left,top,left+side,top+side),new SKRect(0,0,160,160));}
  using var clean=SKImage.FromBitmap(resized);using var encoded=clean.Encode(SKEncodedImageFormat.Png,90);
  var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();var p=await Get(db,user.Id);p.Avatar=encoded.ToArray();await db.SaveChangesAsync();return Results.NoContent();

 });
 routes.MapDelete("/avatar",async(ShopDb db,ClaimsPrincipal principal,UserManager<AppUser> users)=>{var user=await users.GetUserAsync(principal);if(user is null)return Results.Unauthorized();var p=await db.CustomerProfiles.SingleOrDefaultAsync(x=>x.UserId==user.Id);if(p is not null){p.Avatar=null;await db.SaveChangesAsync();}return Results.NoContent();});
 }
 static async Task<CustomerProfile> Get(ShopDb db,string id){var p=await db.CustomerProfiles.SingleOrDefaultAsync(x=>x.UserId==id);if(p is null){p=new(){UserId=id};db.CustomerProfiles.Add(p);}return p;}
}
