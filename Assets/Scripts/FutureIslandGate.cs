using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class FutureIslandGate : MonoBehaviour
    {
        [SerializeField] private string destinationIsland = "Future Island";
        [SerializeField] private string ownerName = "Teammate";
        [SerializeField] private bool connected;

        public string DestinationIsland { get { return destinationIsland; } }
        public string OwnerName { get { return ownerName; } }
        public bool Connected { get { return connected; } }

        public void Configure(string destination, string owner, bool isConnected)
        {
            destinationIsland = destination;
            ownerName = owner;
            connected = isConnected;
        }

        public string InteractionHint()
        {
            if (connected)
            {
                return destinationIsland + " link is connected and ready for team integration.";
            }

            return destinationIsland + " link reserved for " + ownerName +
                   ". The safety seal remains active until that island is merged.";
        }

        public void Interact(ArcaneGameManager manager)
        {
            if (manager != null)
            {
                manager.SetFeedback(InteractionHint());
            }
            ArcaneAudio.Play2D(connected ? SoundId.UiSelect : SoundId.UiError);
        }
    }
}
