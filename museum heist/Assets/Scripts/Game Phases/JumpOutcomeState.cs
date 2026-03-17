public static class JumpOutcomeState
{
    public enum Outcome { None, Good, Bad }
    public static Outcome Current = Outcome.None;

    public static void Reset() => Current = Outcome.None;
}
