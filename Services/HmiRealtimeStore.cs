using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DRB_HMI_3D.Services
{
    public class HmiRealtimeStore
    {
        private readonly ConcurrentDictionary<int, WorkshopRealtimeDto> _data = new();

        public void SetWorkshopData(int workshopId, WorkshopRealtimeDto data)
        {
            _data.AddOrUpdate(workshopId, data, (_, __) => data);
        }

        public bool TryGetWorkshopData(int workshopId, out WorkshopRealtimeDto data)
        {
            return _data.TryGetValue(workshopId, out data);
        }
    }

    public class WorkshopRealtimeDto
    {
        public bool Success { get; set; } = true;
        public string ServerTime { get; set; } = "";
        public List<MachineRealtimeDto> Machines { get; set; } = new();
        public SummaryRealtimeDto Summary { get; set; } = new();
    }

    public class MachineRealtimeDto
    {
        public int Id { get; set; }

        public bool StatusReadOk { get; set; }
        public string StatusValue { get; set; } = "--";
        public string StatusCss { get; set; } = "drb-hmi-stop-card";

        public bool TimeReadOk { get; set; }
        public string Time { get; set; } = "--";

        public bool PressureReadOk { get; set; }
        public string Pressure { get; set; } = "--";

        public bool TempTopReadOk { get; set; }
        public string TempTop { get; set; } = "--";

        public bool TempMidReadOk { get; set; }
        public string TempMid { get; set; } = "--";

        public bool TempBottomReadOk { get; set; }
        public string TempBottom { get; set; } = "--";
    }

    public class SummaryRealtimeDto
    {
        public bool ReadOk { get; set; }
        public int Total { get; set; }
        public int Running { get; set; }
        public int Warning { get; set; }
        public int Error { get; set; }
        public int Stop { get; set; }

        public string RunningPercent { get; set; } = "0%";
        public string WarningPercent { get; set; } = "0%";
        public string ErrorPercent { get; set; } = "0%";
        public string StopPercent { get; set; } = "0%";
        public string EfficiencyPercent { get; set; } = "0%";
    }
}