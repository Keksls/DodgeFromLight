namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Captures the current pressed state of movement inputs shown by the tutorial HUD.
    /// </summary>
    public readonly struct SoloMovementInputState
    {
        public bool Forward { get; }
        public bool Left { get; }
        public bool Backward { get; }
        public bool Right { get; }
        public bool Jump { get; }

        /// <summary>
        /// Creates one immutable movement-input snapshot.
        /// </summary>
        public SoloMovementInputState(
            bool forward,
            bool left,
            bool backward,
            bool right,
            bool jump)
        {
            // The runtime bridge reads input while the presenter only consumes values.
            Forward = forward;
            Left = left;
            Backward = backward;
            Right = right;
            Jump = jump;
        }
    }
}
