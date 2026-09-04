using UnityEngine.XR.ARSubsystems;

namespace FriLens
{
    /// <summary>
    /// Turns a tracking failure into something the person holding the phone can act on.
    ///
    /// ARCore's own guidance is that an app should not show the failure reason, it should show
    /// the remedy: excessive motion means "move the device more slowly", insufficient features
    /// means "point it somewhere with more texture". The enum name alone leaves the tester
    /// guessing, and a tester who guesses wrong keeps walking and ruins the run.
    ///
    /// This matters more here than in a normal AR app. Every one of these states ends in a
    /// relocalisation, and a relocalisation is the metre-scale jump that shows up in the log as
    /// the overlay leaping sideways. Telling the tester how to avoid the state is how the run
    /// produces drift data instead of jump data.
    /// </summary>
    public static class TrackingAdvice
    {
        /// <summary>
        /// A short instruction for the tester, or an empty string when there is nothing useful
        /// to say — either tracking is fine, or the cause is not something a person can fix by
        /// changing how they hold the phone.
        /// </summary>
        public static string For(NotTrackingReason reason)
        {
            return reason switch
            {
                NotTrackingReason.ExcessiveMotion => "move the phone more slowly",
                NotTrackingReason.InsufficientFeatures => "point at a wall with more detail",
                // A hand over the lens reports InsufficientLight, exactly like a dark corridor
                // does — the first field run produced three of these from a deliberately covered
                // camera. Telling somebody to find more light while their palm is on the lens
                // sends them the wrong way, so the line has to cover both causes.
                NotTrackingReason.InsufficientLight => "camera sees nothing — uncover it, or find light",
                NotTrackingReason.CameraUnavailable => "another app is using the camera",
                NotTrackingReason.Relocalizing => "hold still, finding its place again",
                NotTrackingReason.Initializing => "hold still while it starts",
                _ => ""
            };
        }

        /// <summary>
        /// Whether the reason is the tester's to fix. Used to decide between an instruction and
        /// a plain statement of fact: telling somebody to hold still when the camera has been
        /// taken by another app wastes the one line of screen this gets.
        /// </summary>
        public static bool IsActionable(NotTrackingReason reason)
        {
            return reason is NotTrackingReason.ExcessiveMotion
                or NotTrackingReason.InsufficientFeatures
                or NotTrackingReason.InsufficientLight;
        }
    }
}
