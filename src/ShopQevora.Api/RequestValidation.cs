using System.ComponentModel.DataAnnotations;
namespace ShopQevora.Api;
public static class RequestValidation {
 public static bool Valid(object value) => Validator.TryValidateObject(value,new ValidationContext(value),new List<ValidationResult>(),true);
}
