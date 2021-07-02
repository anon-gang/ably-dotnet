namespace IO.Ably.Push
{
    /// <summary>
    /// Public Push state machine interface.
    /// </summary>
    public interface IPushStateMachine
    {
        /// <summary>
        /// Notifies the Push state machine about a new push registration token.
        /// </summary>
        /// <param name="tokenResult">token result.</param>
        void UpdateRegistrationToken(Result<string> tokenResult);
    }
}
