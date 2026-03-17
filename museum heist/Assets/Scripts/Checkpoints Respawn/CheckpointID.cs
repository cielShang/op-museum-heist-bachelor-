public enum CheckpointID
{
    None,

    // Phase 1
    P1_Safe,        // after corridor
    P1_Risky,      // after jump landing

    // Phase 2 (depends on P1 choice, but both end before stairs)
    P2_DoneSafe,   // finished P2 after P1 safe
    P2_DoneRisky,  // finished P2 after P1 risky

    // Phase 3 safe entry positions
    P3_SafeLeft,
    P3_SafeRight
}
