using Microsoft.AspNetCore.SignalR;

namespace CourtSyncPro.Hubs
{
    public class BookingHub : Hub
    {
        // Called by server → broadcasts to ALL connected browsers
        public async Task NotifySlotTaken(int slotId, int courtId, string courtName, string slotLabel)
        {
            await Clients.All.SendAsync("SlotTaken", slotId, courtId, courtName, slotLabel);
        }

        public async Task NotifySlotReleased(int slotId, int courtId)
        {
            await Clients.All.SendAsync("SlotAvailable", slotId, courtId);
        }
    }
}