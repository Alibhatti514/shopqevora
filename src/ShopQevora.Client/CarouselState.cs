namespace ShopQevora.Client;

// Rendering-independent state shared by the carousel and its unit tests.
public sealed class CarouselState(int count)
{
    public int Count { get; } = count > 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));
    public int Current { get; private set; }
    public bool Paused { get; set; }
    public bool ReducedMotion { get; set; }
    public bool CanAutoplay => !Paused && !ReducedMotion;
    public void Next() => Current = (Current + 1) % Count;
    public void Previous() => Current = (Current + Count - 1) % Count;
    public void GoTo(int index)
    {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
        Current = index;
    }
    public void Tick() { if (CanAutoplay) Next(); }
}

public sealed record HeroSlide(string Eyebrow, string Title, string Description, string Image, string Alt, string Button, string Category);
public static class HeroContent
{
    public static IReadOnlyList<HeroSlide> Slides { get; } = new HeroSlide[]
    {
        new("THE ART OF EVERYDAY LIVING", "Discover Your Everyday Favorites", "Thoughtfully selected pieces for modern living.", "everyday", "Sunlit contemporary living room with natural wood and soft neutral furnishings", "Shop the Collection", ""),
        new("DESIGNED TO KEEP UP", "Technology That Moves With You", "Discover refined essentials for a connected lifestyle.", "electronics", "Minimal workspace with a laptop, natural wood desktop and geometric accessories", "Explore Electronics", "Electronics"),
        new("A SPACE THAT FEELS LIKE YOU", "Make Every Space Feel Like Home", "Beautiful details for the places you love.", "home", "Warm living space filled with natural textures and thoughtfully arranged objects", "Shop Home & Living", "Home"),
        new("YOUR EVERYDAY, ELEVATED", "Find Your Signature Style", "Everyday essentials, elevated.", "fashion", "An editorial arrangement of neutral fashion and everyday accessories", "Explore Fashion", "Fashion"),
        new("THE FAVORITES EDIT", "Your Next Favorite Is Here", "Explore the pieces in our handpicked favorites edit.", "favorites", "A minimal dark lamp against a warm sage wall", "Shop Bestsellers", "favorites")
    };
}
