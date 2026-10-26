using System.Text.Json;
using Microsoft.JSInterop;
namespace ShopQevora.Client;

public sealed record GuestCartLine(int ProductID,int Quantity);
public sealed class GuestCartState
{
    readonly Dictionary<int,int> items=new();
    public string CheckoutKey {get;private set;}=Guid.NewGuid().ToString("N");
    public IReadOnlyList<GuestCartLine> Lines=>items.OrderBy(p=>p.Key).Select(p=>new GuestCartLine(p.Key,p.Value)).ToArray();
    public int Count=>items.Values.Sum();
    public void Restore(IEnumerable<GuestCartLine> lines,string? key)
    {
        items.Clear();foreach(var line in lines.Take(50))if(line is not null && line.ProductID>0 && line.Quantity is >=1 and <=99)items[line.ProductID]=Math.Max(items.GetValueOrDefault(line.ProductID),line.Quantity);
        if(Guid.TryParseExact(key,"N",out var parsed)&&parsed!=Guid.Empty)CheckoutKey=key!;
    }
    void Changed()=>CheckoutKey=Guid.NewGuid().ToString("N");
    public void Add(int id)
    {
        if(id<=0)throw new ArgumentOutOfRangeException(nameof(id));
        if(!items.ContainsKey(id)&&items.Count>=50)throw new InvalidOperationException("Your bag can contain up to 50 distinct products.");
        if(items.GetValueOrDefault(id)>=99)throw new InvalidOperationException("The maximum quantity for this item is 99.");
        items[id]=items.GetValueOrDefault(id)+1;Changed();
    }
    public void Set(int id,int quantity){if(quantity is <1 or >99)throw new ArgumentOutOfRangeException(nameof(quantity));if(!items.ContainsKey(id))throw new InvalidOperationException("Item not in your bag.");items[id]=quantity;Changed();}
    public void Remove(int id){if(items.Remove(id))Changed();}
    public void Clear(){items.Clear();Changed();}
}
public sealed class GuestCartStore(IJSRuntime js)
{
    const string Key="shopqevora_guest_cart_v1";
    public GuestCartState State {get;}=new();
    bool loaded;
    public async Task Load()
    {
        if(loaded)return;
        var json=await js.InvokeAsync<string?>("localStorage.getItem",Key);
        if(!string.IsNullOrWhiteSpace(json))try{var value=JsonSerializer.Deserialize<SavedCart>(json);if(value is not null)State.Restore(value.Lines??[],value.CheckoutKey);}catch(JsonException){ /* Ignore malformed browser data; never execute it. */ }
        loaded=true;
    }
    public async Task Save()=>await js.InvokeVoidAsync("localStorage.setItem",Key,JsonSerializer.Serialize(new SavedCart(State.Lines,State.CheckoutKey)));
    public async Task Clear(){State.Clear();await Save();}
    sealed record SavedCart(IReadOnlyList<GuestCartLine>? Lines,string CheckoutKey);
}
