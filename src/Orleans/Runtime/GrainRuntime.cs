using System;
using Orleans.Streams;
using Orleans.Timers;

namespace Orleans.Runtime
{
    internal class GrainRuntime : IGrainRuntime
    {
        public GrainRuntime(Guid serviceId, SiloAddress siloAddress, IGrainFactory grainFactory, ITimerRegistry timerRegistry, IReminderRegistry reminderRegistry, IStreamProviderManager streamProviderManager, IServiceProvider serviceProvider)
        {
            ServiceId = serviceId;
            SiloAddress = siloAddress;
            GrainFactory = grainFactory;
            TimerRegistry = timerRegistry;
            ReminderRegistry = reminderRegistry;
            StreamProviderManager = streamProviderManager;
            ServiceProvider = serviceProvider;
        }

        public Guid ServiceId { get; private set; }

        public SiloAddress SiloAddress { get; private set; }

        public string SiloIdentity { get { return SiloAddress.ToLongString(); } }

        public IGrainFactory GrainFactory { get; private set; }
        
        public ITimerRegistry TimerRegistry { get; private set; }
        
        public IReminderRegistry ReminderRegistry { get; private set; }
        
        public IStreamProviderManager StreamProviderManager { get; private set;}
        public IServiceProvider ServiceProvider { get; private set; }

        public Logger GetLogger(string loggerName)
        {
            return LogManager.GetLogger(loggerName, LoggerType.Grain);
        }

        public void DeactivateOnIdle(Grain grain)
        {
            RuntimeClient.Current.DeactivateOnIdle(grain.Data.ActivationId);
        }

        public void DelayDeactivation(Grain grain, TimeSpan timeSpan)
        {
            grain.Data.DelayDeactivation(timeSpan);
        }
    }
}