using System.Threading.Tasks;
using DRB_HMI_3D.Services;
using Microsoft.AspNetCore.SignalR;

namespace DRB_HMI_3D.Hubs
{
    public class HmiRealtimeHub : Hub
    {
        private readonly HmiRealtimeStore _store;

        public HmiRealtimeHub(HmiRealtimeStore store)
        {
            _store = store;
        }

        public async Task JoinWorkshop(int workshopId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(workshopId));

            if (_store.TryGetWorkshopData(workshopId, out var data))
            {
                await Clients.Caller.SendAsync("RealtimeUpdate", data);
            }
        }

        public async Task LeaveWorkshop(int workshopId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(workshopId));
        }

        public static string GroupName(int workshopId)
        {
            return $"workshop-{workshopId}";
        }
    }
}