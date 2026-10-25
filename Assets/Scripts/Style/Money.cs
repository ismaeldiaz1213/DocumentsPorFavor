// Formats dollars the same way everywhere: $40, -$15.
public static class Money
{
    public static string Format(int dollars) => dollars < 0 ? $"-${-dollars}" : $"${dollars}";
}
