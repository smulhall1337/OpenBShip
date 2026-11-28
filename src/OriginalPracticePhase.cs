namespace FleetLab
{
    // Pinned RIP B9A8...6A569,410D4A..410D5A/original977. This is a counter,
    // not elapsed milliseconds. The source compares the complete16-bit word
    // with zero;all nonzero representations hold it. No full pause semantics.
    public sealed class OriginalPracticePhase
    {
        public uint Value {get;private set;}
        public OriginalPracticePhase(uint value){Value=value;}
        public OriginalPracticePhase Advance(short sourcePauseWord)
        {return new OriginalPracticePhase(sourcePauseWord==0?unchecked(Value+1):Value);}
    }
}
