using System.Reflection;

namespace TAP22_23.AuctionSite.Testing {
    public static class Configuration {
        public static string ImplementationAssembly { get; private set; } = "";
        public static string ConnectionString { get; private set; } = "";

        public static void SetUp() {
            using (var configFile = File.OpenText(@"..\..\..\..\OriginalReferences\TestConfig.txt"))
            {
                ImplementationAssembly = configFile.ReadLine()!;
                ConnectionString = configFile.ReadLine()!;
            }
        }
 
    }

    public class TestAlarm : IAlarm {
        public event Action? RingingEvent;

        public void Dispose() { GC.SuppressFinalize(this); }

        internal void ExecuteActions() {
            RingingEvent?.Invoke();
        }
    }

    public class TestAlarmClock : IAlarmClock {
        private readonly TestAlarm _theAlarm = new();
        public readonly List<int> InstantiateAlarmCallData = new();

        public TestAlarmClock(int timezone) {
            Now = DateTime.UtcNow.AddHours(timezone);
            Timezone = timezone;
        }

        public int Timezone { get; }
        public DateTime Now { get; set; }

        public IAlarm InstantiateAlarm(int frequencyInMs) {
            InstantiateAlarmCallData.Add(frequencyInMs);
            return _theAlarm;
        }

        public void AddHours(double h) {
            Now = Now.AddHours(h);
        }

        public void AddSeconds(double s) {
            Now = Now.AddSeconds(s);
        }

        public void AddMinutes(double m) {
            Now = Now.AddMinutes(m);
        }

        public void RunRingingEvent() {
            _theAlarm.ExecuteActions();
        }
    }

    public class TestAlarmClockFactory : IAlarmClockFactory {
        private readonly Dictionary<int, TestAlarmClock> Clocks = new();

        public IAlarmClock InstantiateAlarmClock(int timezone) {
            if (Clocks.TryGetValue(timezone, out var clock))
                return clock;
            clock = new TestAlarmClock(timezone);
            Clocks[timezone] = clock;
            return clock;
        }
    }

    [TestFixture]
    public abstract class AuctionSiteTests {
        public static IHostFactory TheHostFactory;
        public static string TheConnectionString;
        public static void AuctionSiteTestsSetUp() {
            Configuration.SetUp();
            var implementationAssembly = Assembly.LoadFrom(Configuration.ImplementationAssembly);
            var hostFactoryType = implementationAssembly.GetTypes().Single(t => typeof(IHostFactory).IsAssignableFrom(t));
            TheHostFactory = (Activator.CreateInstance(hostFactoryType) as IHostFactory)!;
            TheConnectionString = Configuration.ConnectionString;
        }
        [OneTimeSetUp]
        public void AuctionSiteTestsInitialize() {
            AuctionSiteTestsSetUp();
        }

        protected IHost TheHost = null!;
        protected readonly TestAlarmClockFactory TheAlarmClockFactory = new();
        protected TestAlarmClock? TheClock;

        [SetUp]
        public void SetupHost() {
            TheHostFactory.CreateHost(TheConnectionString);
            TheHost = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory);
        }

        protected ISite LoadSiteFromName(string siteName, int timezone) {
            TheClock = (TestAlarmClock?)TheAlarmClockFactory.InstantiateAlarmClock(timezone);
            return TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).LoadSite(siteName);
        }
        protected ISite CreateAndLoadEmptySite(int timeZone, string siteName, int sessionExpirationTimeInSeconds, double minimumBidIncrement, out TestAlarmClock alarmClock) {
            TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).CreateSite(siteName, timeZone, sessionExpirationTimeInSeconds, minimumBidIncrement);
            alarmClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
            return TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).LoadSite(siteName);
        }
        protected ISite CreateAndLoadSite(int timeZone, string siteName, int sessionExpirationTimeInSeconds,
                                          double minimumBidIncrement, out TestAlarmClock alarmClock,
                                          List<string>? userNameList = null, string password = "puffo") {
            var newSite = CreateAndLoadEmptySite(timeZone, siteName, sessionExpirationTimeInSeconds, minimumBidIncrement, out alarmClock);
            if (null != userNameList)
                foreach (var user in userNameList)
                    newSite.CreateUser(user, password);
            return TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).LoadSite(siteName);
        }

        protected ISite CreateAndLoadSite(int timeZone, string siteName, int sessionExpirationTimeInSeconds,
                                          double minimumBidIncrement, List<string> userNameList,
                                          List<string> loggedUserNameList, int delayBetweenLoginInSeconds,
                                          out List<ISession> sessionList, out TestAlarmClock alarmClock,
                                          string password = "puffo") {
            //Pre: loggedUserNameList non empty and included in userNameList
            var newSite = CreateAndLoadEmptySite(timeZone, siteName, sessionExpirationTimeInSeconds, minimumBidIncrement, out alarmClock);
            foreach (var user in userNameList)
                newSite.CreateUser(user, password);
            sessionList = new List<ISession>();
            var howManySessions = loggedUserNameList.Count;
            sessionList.Add(newSite.Login(loggedUserNameList[0], password)!);
            for (var i = 1; i < howManySessions; i++) {
                alarmClock.AddSeconds(delayBetweenLoginInSeconds);
                sessionList.Add(newSite.Login(loggedUserNameList[i], password)!);
            }
            return TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).LoadSite(siteName);
        }

        protected static bool AreEquivalentSessions(ISession? session1, ISession? session2) {
            if (null == session2) {
                return session1 == null;
            }
            return CheckSessionValues(session1, session2.Id, session2.ValidUntil, session2.User);
        }

        protected static bool CheckSessionValues(ISession? session, string sessionId, DateTime validUntil, IUser user) {
            if (null == session)
                return false;
            return session.Id == sessionId && session.ValidUntil == validUntil && session.User.Equals(user);
        }

        protected bool CheckAuctionValues(IAuction auction, int auctionId, string sellerUsername, DateTime endsOn, string auctionDescription, double auctionCurrentPrice,
                                          string? currentWinnerUsername = null) {
            var currentWinner = auction.CurrentWinner();
            var currentWinnerIsCorrect = (currentWinnerUsername == null) ? currentWinner == null : currentWinner != null && currentWinner.Username == currentWinnerUsername;
            return auction.Id == auctionId && auction.Seller.Username == sellerUsername && SameDateTime(auction.EndsOn, endsOn) && auction.Description == auctionDescription &&
                   currentWinnerIsCorrect && Math.Abs(auction.CurrentPrice() - auctionCurrentPrice) < .001;
        }
        /// <summary>
        /// Equality up to seconds for dates
        /// Saving Date on DB may introduce approximations, so equality (as ticks) may not hold even for "same" date
        /// </summary>
        /// <param name="x">first date</param>
        /// <param name="y">second date</param>
        /// <returns>true iff date, hour, minute and second components are the same in both dates</returns>
        protected bool SameDateTime(DateTime x, DateTime y) {
            return x.Date == y.Date && x.Hour == y.Hour && x.Minute == y.Minute && x.Second == y.Second;
        }
    }
}