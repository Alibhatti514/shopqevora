using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace ShopQevora.Api;
public sealed class AuthSigning
{
 public SymmetricSecurityKey Key {get;}
 public AuthSigning(IConfiguration configuration)
 {
  var value=configuration["Jwt:Key"]??throw new InvalidOperationException("Configure the existing Jwt:Key User Secret.");
  if(Encoding.UTF8.GetByteCount(value)<32)throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
  Key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value));
 }
}
