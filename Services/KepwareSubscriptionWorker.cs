using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DRB_HMI_3D.Data;
using DRB_HMI_3D.Hubs;
using DRB_HMI_3D.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace DRB_HMI_3D.Services
{
    public class KepwareSubscriptionWorker : BackgroundService
    {
        private readonly string _endpointUrl = "opc.tcp://192.168.41.30:49320";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HmiRealtimeStore _store;
        private readonly IHubContext<HmiRealtimeHub> _hubContext;
        private readonly ILogger<KepwareSubscriptionWorker> _logger;

        private readonly ConcurrentDictionary<string, TagValueState> _latestValues = new(StringComparer.OrdinalIgnoreCase);

        private ApplicationConfiguration _config;
        private Session _session;
        private Subscription _subscription;

        private List<MachineConfig> _machineConfigs = new();
        private string _configSignature = "";

        private DateTime _lastConfigLoadUtc = DateTime.MinValue;
        private DateTime _lastPushUtc = DateTime.MinValue;

        private readonly TimeSpan _configReloadInterval = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _pushHeartbeatInterval = TimeSpan.FromSeconds(1);

        private int _hasChange = 0;

        public KepwareSubscriptionWorker(
            IServiceScopeFactory scopeFactory,
            HmiRealtimeStore store,
            IHubContext<HmiRealtimeHub> hubContext,
            ILogger<KepwareSubscriptionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _store = store;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConfigSessionAndSubscriptionAsync(stoppingToken);
                    await PushRealtimeIfNeededAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kepware subscription worker error");
                    ResetOpc();
                    Interlocked.Exchange(ref _hasChange, 1);
                }

                await Task.Delay(100, stoppingToken);
            }
        }

        private async Task EnsureConfigSessionAndSubscriptionAsync(CancellationToken stoppingToken)
        {
            var shouldReloadConfig =
                _machineConfigs.Count == 0 ||
                DateTime.UtcNow - _lastConfigLoadUtc >= _configReloadInterval;

            var configChanged = false;

            if (shouldReloadConfig)
            {
                configChanged = await ReloadConfigAsync(stoppingToken);
            }

            await EnsureSessionAsync();

            var needRebuildSubscription =
                _subscription == null ||
                _session == null ||
                !_session.Connected ||
                configChanged;

            if (needRebuildSubscription)
            {
                RebuildSubscription();
                Interlocked.Exchange(ref _hasChange, 1);
            }
        }

        private async Task<bool> ReloadConfigAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var workshops = await context.Workshops
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.PressGroups)
                    .ThenInclude(g => g.PressItems)
                        .ThenInclude(p => p.Tags)
                .OrderBy(w => w.Id)
                .ToListAsync(stoppingToken);

            var result = new List<MachineConfig>();

            foreach (var workshop in workshops)
            {
                foreach (var group in SafeGroups(workshop))
                {
                    foreach (var press in SafePresses(group))
                    {
                        var machine = new MachineConfig
                        {
                            WorkshopId = workshop.Id,
                            PressId = press.Id,
                            Active = press.Active,
                            MachineNumber = GetMachineNumber(press),
                            Tags = new List<TagConfig>()
                        };

                        foreach (var tag in SafeTags(press))
                        {
                            if (string.IsNullOrWhiteSpace(tag.KepwareAddress))
                            {
                                continue;
                            }

                            var kind = GetTagKind(tag.Name);

                            if (string.IsNullOrWhiteSpace(kind))
                            {
                                continue;
                            }

                            machine.Tags.Add(new TagConfig
                            {
                                Name = tag.Name ?? "",
                                Address = tag.KepwareAddress.Trim(),
                                Kind = kind,
                                DivideBy10 = ShouldDivideBy10(tag.Name)
                            });
                        }

                        result.Add(machine);
                    }
                }
            }

            result = result
                .OrderBy(x => x.MachineNumber)
                .ThenBy(x => x.PressId)
                .ToList();

            var newSignature = BuildConfigSignature(result);
            var changed = newSignature != _configSignature;

            _machineConfigs = result;
            _configSignature = newSignature;
            _lastConfigLoadUtc = DateTime.UtcNow;

            return changed;
        }

        private static string BuildConfigSignature(List<MachineConfig> machines)
        {
            var sb = new StringBuilder();

            foreach (var machine in machines.OrderBy(x => x.WorkshopId).ThenBy(x => x.PressId))
            {
                sb.Append(machine.WorkshopId);
                sb.Append("|");
                sb.Append(machine.PressId);
                sb.Append("|");
                sb.Append(machine.Active ? "1" : "0");
                sb.Append("|");

                foreach (var tag in machine.Tags.OrderBy(x => x.Address))
                {
                    sb.Append(tag.Kind);
                    sb.Append(":");
                    sb.Append(tag.Address);
                    sb.Append(";");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private async Task EnsureSessionAsync()
        {
            if (_session != null && _session.Connected)
            {
                return;
            }

            ResetOpc();

            _config = new ApplicationConfiguration
            {
                ApplicationName = "DRB_HMI_3D",
                ApplicationUri = "urn:localhost:DRB_HMI_3D",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPCFoundation/CertificateStores/MachineDefault",
                        SubjectName = "DRB_HMI_3D"
                    },

                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPCFoundation/CertificateStores/UA Applications"
                    },

                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPCFoundation/CertificateStores/UA Certificate Authorities"
                    },

                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPCFoundation/CertificateStores/RejectedCertificates"
                    },

                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },

                TransportConfigurations = new TransportConfigurationCollection(),

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 5000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 600000,
                    SecurityTokenLifetime = 3600000
                },

                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 120000,
                    MinSubscriptionLifetime = 10000
                }
            };

            await _config.Validate(ApplicationType.Client);

            _config.CertificateValidator.CertificateValidation += (sender, e) =>
            {
                e.Accept = true;
            };

            var endpointDescription = CoreClientUtils.SelectEndpoint(
                _config,
                _endpointUrl,
                false
            );

            var endpointConfiguration = EndpointConfiguration.Create(_config);

            var endpoint = new ConfiguredEndpoint(
                null,
                endpointDescription,
                endpointConfiguration
            );

            _session = await Session.Create(
                _config,
                endpoint,
                false,
                "DRB_HMI_3D_SUBSCRIPTION_SESSION",
                120000,
                null,
                null
            );
        }

        private void RebuildSubscription()
        {
            if (_session == null || !_session.Connected)
            {
                return;
            }

            try
            {
                if (_subscription != null)
                {
                    _subscription.Delete(true);
                    _session.RemoveSubscription(_subscription);
                }
            }
            catch
            {
            }

            _subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 250,
                KeepAliveCount = 10,
                LifetimeCount = 60,
                MaxNotificationsPerPublish = 5000,
                PublishingEnabled = true,
                Priority = 100
            };

            _session.AddSubscription(_subscription);
            _subscription.Create();

            var addresses = _machineConfigs
                .SelectMany(x => x.Tags)
                .Where(x => !string.IsNullOrWhiteSpace(x.Address))
                .Select(x => x.Address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = new List<MonitoredItem>();

            foreach (var address in addresses)
            {
                try
                {
                    var item = new MonitoredItem(_subscription.DefaultItem)
                    {
                        StartNodeId = NodeId.Parse(address),
                        AttributeId = Attributes.Value,
                        DisplayName = address,
                        SamplingInterval = 250,
                        QueueSize = 1,
                        DiscardOldest = true,
                        Handle = address
                    };

                    item.Notification += OnMonitoredItemNotification;
                    items.Add(item);
                }
                catch
                {
                    _latestValues.AddOrUpdate(
                        address,
                        new TagValueState
                        {
                            Good = false,
                            Value = null,
                            UpdatedAt = DateTime.Now
                        },
                        (_, oldValue) =>
                        {
                            oldValue.Good = false;
                            oldValue.Value = null;
                            oldValue.UpdatedAt = DateTime.Now;
                            return oldValue;
                        });
                }
            }

            if (items.Count > 0)
            {
                _subscription.AddItems(items);
                _subscription.ApplyChanges();
            }
        }

        private void OnMonitoredItemNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            var address = monitoredItem.Handle as string;

            if (string.IsNullOrWhiteSpace(address))
            {
                address = monitoredItem.DisplayName;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            foreach (var value in monitoredItem.DequeueValues())
            {
                var good = value != null && !StatusCode.IsBad(value.StatusCode);
                var raw = good ? value.Value : null;

                _latestValues.AddOrUpdate(
                    address,
                    new TagValueState
                    {
                        Good = good,
                        Value = raw,
                        UpdatedAt = DateTime.Now
                    },
                    (_, oldValue) =>
                    {
                        oldValue.Good = good;
                        oldValue.Value = raw;
                        oldValue.UpdatedAt = DateTime.Now;
                        return oldValue;
                    });
            }

            Interlocked.Exchange(ref _hasChange, 1);
        }

        private async Task PushRealtimeIfNeededAsync(CancellationToken stoppingToken)
        {
            var hasChange = Interlocked.Exchange(ref _hasChange, 0) == 1;
            var needHeartbeat = DateTime.UtcNow - _lastPushUtc >= _pushHeartbeatInterval;

            if (!hasChange && !needHeartbeat)
            {
                return;
            }

            _lastPushUtc = DateTime.UtcNow;

            foreach (var group in _machineConfigs.GroupBy(x => x.WorkshopId))
            {
                var workshopId = group.Key;
                var data = BuildWorkshopData(group.ToList(), _latestValues);

                _store.SetWorkshopData(workshopId, data);

                await _hubContext.Clients
                    .Group(HmiRealtimeHub.GroupName(workshopId))
                    .SendAsync("RealtimeUpdate", data, stoppingToken);
            }
        }

        private static WorkshopRealtimeDto BuildWorkshopData(
            List<MachineConfig> machines,
            ConcurrentDictionary<string, TagValueState> latestValues)
        {
            var result = new WorkshopRealtimeDto
            {
                Success = true,
                ServerTime = DateTime.Now.ToString("HH:mm:ss"),
                Machines = new List<MachineRealtimeDto>()
            };

            var running = 0;
            var warning = 0;
            var error = 0;
            var stop = 0;
            var summaryReadOk = true;

            foreach (var machine in machines)
            {
                var statusTag = GetFirstTag(machine, "STATUS");
                var timeTag = GetFirstTag(machine, "TIME");
                var pressureTag = GetFirstTag(machine, "PRESSURE");
                var tempTopTag = GetFirstTag(machine, "TEMP_TOP");
                var tempMidTag = GetFirstTag(machine, "TEMP_MID");
                var tempBottomTag = GetFirstTag(machine, "TEMP_BOTTOM");

                var status = GetTagText(statusTag, latestValues, out var statusReadOk);
                var time = GetTagText(timeTag, latestValues, out var timeReadOk);
                var pressure = GetTagText(pressureTag, latestValues, out var pressureReadOk);
                var tempTop = GetTagText(tempTopTag, latestValues, out var tempTopReadOk);
                var tempMid = GetTagText(tempMidTag, latestValues, out var tempMidReadOk);
                var tempBottom = GetTagText(tempBottomTag, latestValues, out var tempBottomReadOk);

                var allowStatusUpdate = statusReadOk || !machine.Active;

                var hasLimitError =
                    IsOutsideLimit(pressure, 196m, 10m) ||
                    IsOutsideLimit(tempTop, 150m, 5m) ||
                    IsOutsideLimit(tempMid, 150m, 5m) ||
                    IsOutsideLimit(tempBottom, 150m, 5m);

                var statusCss = allowStatusUpdate
                    ? GetStatusCss(machine.Active, status, hasLimitError)
                    : "drb-hmi-stop-card";

                result.Machines.Add(new MachineRealtimeDto
                {
                    Id = machine.PressId,

                    StatusReadOk = allowStatusUpdate,
                    StatusValue = status,
                    StatusCss = statusCss,

                    TimeReadOk = timeReadOk,
                    Time = time,

                    PressureReadOk = pressureReadOk,
                    Pressure = pressure,

                    TempTopReadOk = tempTopReadOk,
                    TempTop = tempTop,

                    TempMidReadOk = tempMidReadOk,
                    TempMid = tempMid,

                    TempBottomReadOk = tempBottomReadOk,
                    TempBottom = tempBottom
                });

                if (!allowStatusUpdate)
                {
                    summaryReadOk = false;
                    stop++;
                    continue;
                }

                if (statusCss == "drb-hmi-running")
                {
                    running++;
                }
                else if (statusCss == "drb-hmi-warning-card")
                {
                    warning++;
                }
                else if (statusCss == "drb-hmi-error-card")
                {
                    error++;
                }
                else
                {
                    stop++;
                }
            }

            var total = machines.Count;

            result.Summary = new SummaryRealtimeDto
            {
                ReadOk = summaryReadOk,
                Total = total,
                Running = running,
                Warning = warning,
                Error = error,
                Stop = stop,
                RunningPercent = Percent(running, total),
                WarningPercent = Percent(warning, total),
                ErrorPercent = Percent(error, total),
                StopPercent = Percent(stop, total),
                EfficiencyPercent = Percent(running, total)
            };

            return result;
        }

        private static TagConfig GetFirstTag(MachineConfig machine, string kind)
        {
            return machine.Tags.FirstOrDefault(x => x.Kind == kind);
        }

        private static string GetTagText(
            TagConfig tag,
            ConcurrentDictionary<string, TagValueState> latestValues,
            out bool readOk)
        {
            readOk = false;

            if (tag == null || string.IsNullOrWhiteSpace(tag.Address))
            {
                return "--";
            }

            if (!latestValues.TryGetValue(tag.Address, out var state))
            {
                return "--";
            }

            if (state == null || !state.Good || state.Value == null)
            {
                return "--";
            }

            readOk = true;

            var rawText = Convert.ToString(state.Value, CultureInfo.InvariantCulture) ?? "";

            if (tag.DivideBy10 &&
                decimal.TryParse(rawText, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            {
                return (number / 10).ToString("0.0", CultureInfo.InvariantCulture);
            }

            return string.IsNullOrWhiteSpace(rawText) ? "--" : rawText;
        }

        private static string GetStatusCss(bool active, string value, bool hasLimitError)
        {
            if (!active)
            {
                return "drb-hmi-stop-card";
            }

            value = (value ?? "").Trim().ToUpper();

            if (value == "--" || value == "")
            {
                return "drb-hmi-stop-card";
            }

            if (value == "0" || value == "FALSE" || value == "STOP" || value == "DUNG" || value == "DỪNG")
            {
                return "drb-hmi-stop-card";
            }

            if (value == "2" || value == "LOI" || value == "LỖI" || value == "ERROR")
            {
                return "drb-hmi-error-card";
            }

            if (hasLimitError)
            {
                return "drb-hmi-error-card";
            }

            if (value == "3" || value == "CANH BAO" || value == "CẢNH BÁO" || value == "WARNING")
            {
                return "drb-hmi-warning-card";
            }

            return "drb-hmi-running";
        }

        private static string GetTagKind(string tagName)
        {
            var name = Normalize(tagName);

            if (name.Contains("START/STOP") || name.Contains("TRANG THAI") || name.Contains("TRẠNG THÁI") || name.Contains("STATUS"))
            {
                return "STATUS";
            }

            if (name.Contains("THOI GIAN LUU HOA") || name.Contains("THỜI GIAN LƯU HÓA") || name.Contains("TIME"))
            {
                return "TIME";
            }

            if (name.Contains("AP LUC") || name.Contains("ÁP LỰC") || name.Contains("PRESSURE"))
            {
                return "PRESSURE";
            }

            if (name.Contains("NHIET DO MAM TREN") || name.Contains("NHIỆT ĐỘ MÂM TRÊN") || name.Contains("TEMP TOP"))
            {
                return "TEMP_TOP";
            }

            if (name.Contains("NHIET DO MAM GIUA") || name.Contains("NHIỆT ĐỘ MÂM GIỮA") || name.Contains("TEMP MID"))
            {
                return "TEMP_MID";
            }

            if (name.Contains("NHIET DO MAM DUOI") || name.Contains("NHIỆT ĐỘ MÂM DƯỚI") || name.Contains("TEMP BOTTOM"))
            {
                return "TEMP_BOTTOM";
            }

            return "";
        }

        private static bool ShouldDivideBy10(string tagName)
        {
            var name = Normalize(tagName);

            return name.Contains("AP LUC") ||
                   name.Contains("ÁP LỰC") ||
                   name.Contains("PRESSURE") ||
                   name.Contains("NHIET DO") ||
                   name.Contains("NHIỆT ĐỘ") ||
                   name.Contains("TEMP");
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "" : text.Trim().ToUpper();
        }

        private static bool IsOutsideLimit(string value, decimal setValue, decimal tolerance)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "--")
            {
                return false;
            }

            value = value.Trim().Replace(",", ".");

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            var min = setValue - tolerance;
            var max = setValue + tolerance;

            return number < min || number > max;
        }

        private static string Percent(int value, int total)
        {
            if (total <= 0)
            {
                return "0%";
            }

            return ((double)value / total * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static int GetMachineNumber(PressItem press)
        {
            if (press == null)
            {
                return int.MaxValue;
            }

            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(press.Name))
            {
                candidates.Add(press.Name.Trim());
            }

            if (!string.IsNullOrWhiteSpace(press.KepwareTag))
            {
                candidates.Add(press.KepwareTag.Trim());
            }

            foreach (var text in candidates)
            {
                var match = Regex.Match(text, @"CTL\s*\.?\s*(\d+)", RegexOptions.IgnoreCase);

                if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                {
                    return number;
                }
            }

            foreach (var text in candidates)
            {
                var matches = Regex.Matches(text, @"\d+");

                if (matches.Count > 0)
                {
                    var lastNumber = matches[matches.Count - 1].Value;

                    if (int.TryParse(lastNumber, out var number))
                    {
                        return number;
                    }
                }
            }

            return int.MaxValue;
        }

        private static IEnumerable<PressGroup> SafeGroups(Workshop workshop)
        {
            return workshop?.PressGroups ?? Enumerable.Empty<PressGroup>();
        }

        private static IEnumerable<PressItem> SafePresses(PressGroup group)
        {
            return group?.PressItems ?? Enumerable.Empty<PressItem>();
        }

        private static IEnumerable<PressTag> SafeTags(PressItem press)
        {
            return press?.Tags ?? Enumerable.Empty<PressTag>();
        }

        private void ResetOpc()
        {
            try
            {
                if (_subscription != null)
                {
                    _subscription.Delete(true);
                }
            }
            catch
            {
            }

            try
            {
                if (_session != null)
                {
                    _session.Close();
                    _session.Dispose();
                }
            }
            catch
            {
            }

            _subscription = null;
            _session = null;
        }

        private class MachineConfig
        {
            public int WorkshopId { get; set; }
            public int PressId { get; set; }
            public bool Active { get; set; }
            public int MachineNumber { get; set; }
            public List<TagConfig> Tags { get; set; } = new();
        }

        private class TagConfig
        {
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Kind { get; set; } = "";
            public bool DivideBy10 { get; set; }
        }

        private class TagValueState
        {
            public bool Good { get; set; }
            public object Value { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.Now;
        }
    }
}