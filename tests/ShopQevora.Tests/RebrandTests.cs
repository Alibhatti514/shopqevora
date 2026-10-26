using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;
namespace ShopQevora.Tests;
public class RebrandTests
{
    static string Root(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"ShopQevora.sln")))d=d.Parent;return d!.FullName;}
    [Fact]public void AllExistingPhotographyRemainsByteForByteUnchanged()
    {
        var hashes=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Root(),"output","preserved-photography-sha256.json")))!;Assert.Equal(48,hashes.Count);
        foreach(var (path,hash) in hashes)Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Root(),path)))).ToLowerInvariant());
    }
    [Fact]public void RenamedProjectsKeepSecretStoreAndVectorLogoContracts()
    {
        var root=Root();var project=XDocument.Load(Path.Combine(root,"src","ShopQevora.Api","ShopQevora.Api.csproj"));Assert.Equal("b6ae4ba0-2251-461a-b3cf-30f7daf43a6e",project.Descendants("UserSecretsId").Single().Value);
        foreach(var file in new[]{"logo.svg","logo-light.svg","monogram.svg","monogram-light.svg","favicon.svg"})
        {
            var svg=XDocument.Load(Path.Combine(root,"src","ShopQevora.Client","wwwroot",file));Assert.NotNull(svg.Root!.Attribute("viewBox"));Assert.DoesNotContain(svg.Descendants(),e=>e.Name.LocalName is "image" or "text");Assert.Contains("ShopQevora",svg.ToString());
        }
    }
}
