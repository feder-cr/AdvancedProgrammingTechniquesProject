namespace TAP22_23.AuctionSite.Testing {
    [TestFixture]
    public class HostBasicTests : AuctionSiteTests {
        /// <summary>
        ///     Verify that CreateSite on a null site name
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_NullName_Throws() {
            Assert.That(() => TheHost.CreateSite(null!, 2, 300, 4), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }


        /// <summary>
        ///     Verify that LoadSite on a null site name
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void LoadSite_NullName_Throws() {
            Assert.That(() => TheHost.LoadSite(null!), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }


        /// <summary>
        ///     Verify that GetSiteInfos on correct arguments
        ///     returns a not null (possibly empty) list
        /// </summary>
        [Test]
        public void GetSiteInfos_ValidArg_ReturnsNonnull() {
            var siteNamesList = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).GetSiteInfos();
            Assert.That(siteNamesList, Is.Not.Null);
        }

        /// <summary>
        ///     Verify that CreateSite on correct arguments
        ///     actually adds the new site (and nothing else)
        /// </summary>
        [Test]
        public void GetSiteInfos_JustCreatedSite_ReturnsTheNewSite() {
            const string siteName = "_boh_";
            TheHost.CreateSite(siteName, -3, 6000, 25);
            var siteNamesList = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).GetSiteInfos();
            Assert.That(siteNamesList, Is.EquivalentTo(new List<(string Name, int TimeZone)> { (siteName, -3) }));
        }

        /// <summary>
        ///     Verify that GetSiteInfos on correct arguments
        ///     returns an empty list when called before creating any site
        /// </summary>
        [Test]
        public void GetSiteInfos_OnEmptyDB_ReturnsEmpty() {
            var siteNamesList = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).GetSiteInfos();
            Assert.That(siteNamesList, Is.Empty);
        }

        private void GetSiteNames_DbWithNSites_ReturnsThoseSiteNames(List<string> expectedSiteNames) {
            foreach (var siteName in expectedSiteNames)
                TheHost.CreateSite(siteName, 1, 60, 1);

            var siteNamesList = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).GetSiteInfos().Select(i => i.Name).ToList();
            Assert.That(siteNamesList, Is.EquivalentTo(expectedSiteNames));
        }

        /// <summary>
        ///     Verify that GetSiteInfos on correct arguments
        ///     returns a list with the three names of the existing three sites
        /// </summary>
        [Test]
        public void GetSiteNames_DbWith3Sites_Returns3Names() {
            var expectedSiteNames = new List<string> { "A", "B", "C" };
            GetSiteNames_DbWithNSites_ReturnsThoseSiteNames(expectedSiteNames);
        }
        /// <summary>
        ///     Verify that GetSiteInfos on correct arguments
        ///     returns a list with the names and timezones of the existing sites
        /// </summary>
        [TestCase(7)]
        [TestCase(27)]
        public void GetSiteNames_DbWithNSites_ReturnsCorrectNamesAndTimezones(int howManySites) {
            const string basicName = "site #";
            var expectedSiteInfos = new List<(string name, int timezone)>();
            for (int i = 0; i < howManySites; i++) {
                var name = basicName + i;
                var timezone = i % (DomainConstraints.MaxTimeZone + 1);
                expectedSiteInfos.Add((name, timezone));
                TheHost.CreateSite(name, timezone, 120, i + 1);
            }

            var result = TheHost.GetSiteInfos();
            Assert.That(result, Is.EquivalentTo(expectedSiteInfos));
        }

        /// <summary>
        ///     Verify that GetSiteInfos on correct arguments
        ///     returns a list with the names of the existing sites
        ///     in the case of a randomly number n of sites with name s0, s1..sn
        /// </summary>
        [Test]
        public void GetSiteNames_DbWithRandomNumberOfSites_ReturnsThatManyNames([Random(0, 20, 1)] int howMany) {
            var expectedSiteNames = new List<string>();
            for (var i = 0; i < howMany; i++)
                expectedSiteNames.Add($"s{i}");

            GetSiteNames_DbWithNSites_ReturnsThoseSiteNames(expectedSiteNames);
        }

        /// <summary>
        ///     Verify that CreateSite on a otherwise correct parameters
        ///     but on a site name already in use by another site
        ///     throws AuctionSiteNameAlreadyInUseException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_NameTaken_Throws() {
            const string taken = "taken!!";
            TheHost.CreateSite(taken, 4, 1200, 0.2);
            Assert.That(() => TheHost.CreateSite(taken, 2, 120, 0.25), Throws.TypeOf<AuctionSiteNameAlreadyInUseException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a otherwise correct parameters
        ///     but on a site name already in use by another site
        ///     throws AuctionSiteNameAlreadyInUseException
        ///     even if all the parameter coincide with the values of the existing site
        /// </summary>
        [Test]
        public void CreateSiteOnDb_SameDataOfExistingSite_Throws() {
            const string taken = "taken!!";
            TheHost.CreateSite(taken, 4, 1200, 0.2); //first creation must be ok
            Assert.That(() => TheHost.CreateSite(taken, 4, 1200, 0.2), Throws.TypeOf<AuctionSiteNameAlreadyInUseException>());
        }

        /// <summary>
        ///     Verify that CreateSite on an empty site name
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_NameEmpty_Throws() {
            Assert.That(() => TheHost.CreateSite("", 0, 600, 0.01), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a timezone smaller than DomainConstraints.MinTimeZone
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_TimezoneTooSmall_Throws() {
            Assert.That(() => TheHost.CreateSite("troppo giusto", -13, 600, 0.01), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a timezone much larger than DomainConstraints.MaxTimeZone
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_FarTooLargeTimezone_Throws() {
            Assert.That(() => TheHost.CreateSite("troppo giusto", 1024, 600, 0.01), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a timezone slightly larger than DomainConstraints.MaxTimeZone
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_TooLargeTimezone_Throws() {
            Assert.That(() => TheHost.CreateSite("troppo giusto", 13, 600, 0.01), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a negative session expiration time
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_NegativeSessionExpirationTime_Throws() {
            Assert.That(() => TheHost.CreateSite("troppo giusto", 1, -10, 0.01), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that CreateSite on a negative minimum increment
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateSiteOnDb_NegativeMinimumIncrement_Throws() {
            Assert.That(() => TheHost.CreateSite("troppo giusto", 2, 60, -0.01), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that GetTheTimezoneOf on correct arguments returns the
        ///     timezone of the site
        /// </summary>
        [Test]
        public void GetTheTimezoneOf_ValidArg_Returns5() {
            const string siteName = "questo va";
            const int expectedTimezone = 5;
            TheHost.CreateSite(siteName, expectedTimezone, 673, 2.8);
            var timezone = TheHost.GetSiteInfos().Single(s => s.Name == siteName).TimeZone;
            Assert.That(timezone, Is.EqualTo(expectedTimezone));
        }

        /// <summary>
        ///     Verify that LoadSite
        ///     throws AuctionSiteInexistentNameException
        ///     if the name provided as argument is not the name of an existing site name
        /// </summary>
        [Test]
        public void LoadSite_InexistentSite_Throws() {
            Assert.That(() => TheHost.LoadSite("pippo"), Throws.TypeOf<AuctionSiteInexistentNameException>());
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite1() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(newSite.Name, Is.EqualTo(siteName));
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and with correct time zone
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite2() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(newSite.Timezone, Is.EqualTo(timeZone));
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and with correct session expiration time
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite3() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(newSite.SessionExpirationInSeconds, Is.EqualTo(sessionExpirationTimeInSeconds));
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and with correct minimum increment
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite4() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(newSite.MinimumBidIncrement, Is.EqualTo(minimumBidIncrement));
        }
        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and with correct clock
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite8() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(newSite.Now(), Is.EqualTo(TheClock.Now));
        }
        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and without users
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite5() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(!newSite.ToyGetUsers().Any());
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and without auctions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite6() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(!newSite.ToyGetAuctions(false).Any());
        }

        /// <summary>
        ///     Verify that LoadSite on correct arguments
        ///     actually creates a site with the given name and without sessions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsNewSite7() {
            const int timeZone = 3;
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 360;
            const double minimumBidIncrement = .5;
            var newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock);

            Assert.That(!newSite.ToyGetSessions().Any());
        }
    }

    public class HostLoadSiteWithUsersTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes newSite:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>pippo</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>3</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>360 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>.5</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description>"Alice", "Barbara", "Carlotta", "Dalila"</description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void SetupSite() {
            userList = new List<string> { "Alice", "Barbara", "Carlotta", "Dalila" };
            newSite = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                out TheClock,
                userList);
        }

        private const int timeZone = 3;
        private const string siteName = "pippo";
        private const int sessionExpirationTimeInSeconds = 360;
        private const double minimumBidIncrement = .5;
        private List<string> userList = null!;
        private ISite newSite = null!;

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite name
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers1() {
            Assert.That(newSite.Name, Is.EqualTo(siteName));
        }


        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite time zone
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers2() {
            Assert.That(newSite.Timezone, Is.EqualTo(timeZone));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite expiration time
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers3() {
            Assert.That(newSite.SessionExpirationInSeconds, Is.EqualTo(sessionExpirationTimeInSeconds));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite minimum bid increment
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers4() {
            Assert.That(newSite.MinimumBidIncrement, Is.EqualTo(minimumBidIncrement));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite users
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers5() {
            Assert.That(newSite.ToyGetUsers().Select(u => u.Username), Is.EquivalentTo(userList));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite auctions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers6() {
            Assert.That(newSite.ToyGetAuctions(false), Is.EquivalentTo(new List<IAuction>()));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite sessions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithUsers7() {
            Assert.That(newSite.ToyGetSessions(), Is.EquivalentTo(new List<ISession>()));
        }
    }

    public class HostLoadSiteWithSessionsTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes newSite:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>pippo</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>3</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>7200 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>.5</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description>"Alice", "Barbara", "Carlotta", "Dalila"</description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>aliceSession for Alice and barbaraSession for Barbara</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void SetUpSiteWithSessions() {
            userList = new List<string> { alice, barbara, "Carlotta", "Dalila" };
            loggedUserList = new List<string> { barbara, alice };
            newSite = CreateAndLoadSite(timeZone, siteName, sessionExpirationTimeInSeconds, minimumBidIncrement, userList, loggedUserList, 1800, out expectedSessionList, out TheClock);
            sessionList = newSite.ToyGetSessions().ToList();
            aliceSession = sessionList.Single(s => s.User.Username == alice);
            barbaraSession = sessionList.Single(s => s.User.Username == barbara);
        }

        private const int timeZone = 3;
        private const string siteName = "pippo";
        private const int sessionExpirationTimeInSeconds = 7200;
        private const double minimumBidIncrement = .5;
        private const string alice = "Alice";
        private const string barbara = "Barbara";
        private List<string> userList = null!;
        private List<string> loggedUserList = null!;
        private List<ISession>? expectedSessionList;
        private ISite newSite = null!;
        private List<ISession> sessionList = null!;
        private ISession aliceSession = null!;
        private ISession barbaraSession = null!;


        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite name
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions0() {
            Assert.That(newSite.Name, Is.EqualTo(siteName));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite time zone
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions2() {
            Assert.That(newSite.Timezone, Is.EqualTo(timeZone));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite expiration time
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions3() {
            Assert.That(newSite.SessionExpirationInSeconds, Is.EqualTo(sessionExpirationTimeInSeconds));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite minimum bid increment
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions4() {
            Assert.That(newSite.MinimumBidIncrement, Is.EqualTo(minimumBidIncrement));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite users
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions5() {
            var expectedUserList = newSite.ToyGetUsers().Select(u => u.Username);
            Assert.That(expectedUserList, Is.EquivalentTo(userList));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite auctions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions6() {
            var expectedAuctions = newSite.ToyGetAuctions(false);
            Assert.That(expectedAuctions, Is.Empty);
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite sessions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsSiteWithSessions7() {
            Assert.That(sessionList, Is.EquivalentTo(expectedSessionList));
            Assert.That(aliceSession.ValidUntil, Is.GreaterThan(TheClock!.Now.AddHours(1)));
            Assert.That(barbaraSession.ValidUntil, Is.GreaterThan(TheClock.Now.AddMinutes(30)));
        }
    }

    public class HostCreateEmptySiteTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes CreatedSite:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>troppo giusto</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>2</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>60 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>.01</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description>empty list</description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void CreateAndLoadSite() {
            TheHost.CreateSite(SiteName, TimeZone, SessionExpirationTimeInSeconds, MinimumBidIncrement);
            TheClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(TimeZone);
            CreatedSite = TheHost.LoadSite(SiteName);
        }

        private const string SiteName = "troppo giusto";
        private const double MinimumBidIncrement = 0.01;
        private const int TimeZone = 2;
        private const int SessionExpirationTimeInSeconds = 60;
        private ISite CreatedSite { get; set; } = null!;

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite name
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite1() {
            Assert.That(CreatedSite.Name, Is.EqualTo(SiteName));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite time zone
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite2() {
            Assert.That(CreatedSite.Timezone, Is.EqualTo(TimeZone));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. Now
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite3() {
            Assert.That(CreatedSite.Now(), Is.EqualTo(TheClock!.Now));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite expiration time
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite4() {
            Assert.That(CreatedSite.SessionExpirationInSeconds, Is.EqualTo(SessionExpirationTimeInSeconds));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite minimum bid increment
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite5() {
            Assert.That(CreatedSite.MinimumBidIncrement, Is.EqualTo(MinimumBidIncrement));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite sessions
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite6() {
            Assert.That(CreatedSite.ToyGetSessions(), Is.Empty);
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite auctions
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite7() {
            Assert.That(CreatedSite.ToyGetAuctions(false), Is.Empty);
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. CreatedSite users
        /// </summary>
        [Test]
        public void CreateSiteOnDb_ValidArg_CreatesSite8() {
            Assert.That(CreatedSite.ToyGetUsers(), Is.Empty);
        }
    }

    public class HostLoadSiteFullSiteTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes newSite:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>pippo</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>3</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>7200 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>.5</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description>"Alice", "Barbara", "Carlotta", "Dalila"</description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>
        ///                 barbaraAuction(barbaraDescription, winner=Carlotta,currentPrice=8,25,
        ///                 endsOn=7/12/2035,maxProposal=9,27)
        ///                 and aliceAuction(aliceDescription, winner=null,currentPrice=0,25, endsOn=16/12/2035,maxProposal=0)
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>aliceSession for Alice and barbaraSession for Barbara</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void Setup() {
            var newSite1 = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                userList,
                userList,
                0,
                out var expectedSessionList1,
                out TheClock,
                password);
            var aliceExpectedSession = expectedSessionList1.Single(s => s.User.Username == alice);
            var barbaraExpectedSession = expectedSessionList1.Single(s => s.User.Username == barbara);
            var carlottaExpectedSession = expectedSessionList1.Single(s => s.User.Username == carlotta);
            var dalilaExpectedSession = expectedSessionList1.Single(s => s.User.Username == dalila);
            barbaraAuctionEndsOn = new DateTime(2035, 12, 7);
            barbaraAuction = barbaraExpectedSession.CreateAuction(barbaraAuctionDescription, barbaraAuctionEndsOn, barbaraStartingPrice);
            if (!barbaraAuction.Bid(dalilaExpectedSession, 7.75))
                Assert.Inconclusive("Dalila's bid should have been accepted");
            dalilaExpectedSession.Logout();
            if (!barbaraAuction.Bid(carlottaExpectedSession, 9.27))
                Assert.Inconclusive("Carlotta's bid should have been accepted");
            carlottaExpectedSession.Logout();
            aliceAuctionEndsOn = TheClock.Now;
            aliceAuction = aliceExpectedSession.CreateAuction(aliceAuctionDescription, aliceAuctionEndsOn, aliceStartingPrice);
            expectedAuctionList = new List<IAuction> { barbaraAuction, aliceAuction };
            TheClock.AddHours(23);
            barbaraExpectedSession = newSite1.Login(barbara, password)!;
            var barbaraExpectedTimeOut = TheClock.Now.AddSeconds(sessionExpirationTimeInSeconds);
            if (!SameDateTime(barbaraExpectedSession.ValidUntil, barbaraExpectedTimeOut))
                Assert.Inconclusive("Wrong setup: Barbara's session has not the expected validity");
            TheClock.AddMinutes(30);
            aliceExpectedSession = newSite1.Login(alice, password)!;
            var aliceExpectedTimeOut = TheClock.Now.AddSeconds(sessionExpirationTimeInSeconds);
            if (!SameDateTime(aliceExpectedSession.ValidUntil, aliceExpectedTimeOut))
                Assert.Inconclusive("Wrong setup: Alice's session has not the expected validity");
            TheClock.AddMinutes(30);
            expectedSessionList1 = new List<ISession> { barbaraExpectedSession, aliceExpectedSession };
            var yesterday = TheClock.Now.AddDays(-1);
            if (!SameDateTime(yesterday.Date, aliceAuctionEndsOn.Date))
                Assert.Inconclusive("Wrong setup: current time is not day after Alice's auction ends");
            expectedSessionList = expectedSessionList1;
        }

        private const int timeZone = 3;
        private const string siteName = "pippo";
        private const int sessionExpirationTimeInSeconds = 7200;
        private const double minimumBidIncrement = .5;
        private const string alice = "Alice";
        private const string barbara = "Barbara";
        private const string carlotta = "Carlotta";
        private const string dalila = "Dalila";
        private readonly List<string> userList = new() { dalila, carlotta, barbara, alice };
        private const string password = "puffo_blu55";
        private List<IAuction> expectedAuctionList = null!;
        private List<ISession> expectedSessionList = null!;
        private IAuction aliceAuction = null!;
        private DateTime aliceAuctionEndsOn;
        private const string aliceAuctionDescription = "fa schifo: non lo comprate";
        private const double aliceStartingPrice = .25;
        private IAuction barbaraAuction = null!;
        private const string barbaraAuctionDescription = "non lo venderemo mai";
        private DateTime barbaraAuctionEndsOn;
        private const double barbaraStartingPrice = 3.75;
        private ISite? newSite;

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite name
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite1() {
            newSite = TheHost.LoadSite(siteName);
            Assert.That(newSite.Name, Is.EqualTo(siteName));
        }


        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite time zone
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite2() {
            newSite = TheHost.LoadSite(siteName);

            Assert.That(newSite.Timezone, Is.EqualTo(timeZone));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite expiration time
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite3() {
            newSite = TheHost.LoadSite(siteName);

            Assert.That(newSite.SessionExpirationInSeconds, Is.EqualTo(sessionExpirationTimeInSeconds));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite minimum bid increment
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite4() {
            newSite = TheHost.LoadSite(siteName);

            Assert.That(newSite.MinimumBidIncrement, Is.EqualTo(minimumBidIncrement));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite Now
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite5() {
            newSite = TheHost.LoadSite(siteName);
            var expectedClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
            expectedClock.Now = new DateTime(1955, 4, 27);
            Assert.That(newSite.Now(), Is.EqualTo(expectedClock.Now));
        }

        /// <summary>
        ///     Verify that the the alarm has been initialized with the correct frequency
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_InstantiateAlarm() {
            newSite = TheHost.LoadSite(siteName);
            var expectedClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
            Assert.That(expectedClock.InstantiateAlarmCallData, Contains.Item(5 * 60 * 1000));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite users
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite6() {
            newSite = TheHost.LoadSite(siteName);
            var usernames = newSite.ToyGetUsers().Select(u => u.Username);
            Assert.That(usernames, Is.EquivalentTo(userList));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite sessions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite7() {
            newSite = TheHost.LoadSite(siteName);
            var sessionList = newSite.ToyGetSessions().ToList();
            Assert.That(sessionList, Is.EquivalentTo(expectedSessionList));
            var aliceSession = sessionList.Single(s => s.User.Username == alice);
            Assert.That(aliceSession.ValidUntil, Is.GreaterThanOrEqualTo(TheClock!.Now.AddHours(1).AddMinutes(30)));
            var barbaraSession = sessionList.Single(s => s.User.Username == barbara);
            Assert.That(barbaraSession.ValidUntil, Is.GreaterThanOrEqualTo(TheClock.Now.AddHours(1)));
        }

        /// <summary>
        ///     Verify that the setup is correct w.r.t. newSite auctions
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_ReturnsFullSite8() {
            newSite = TheHost.LoadSite(siteName);
            var auctionList = newSite.ToyGetAuctions(false).ToList();
            Assert.That(auctionList.Count, Is.EqualTo(2));
            var barbaraAuction = auctionList.Single(a => a.Seller.Username == barbara);
            Assert.That(CheckAuctionValues(barbaraAuction,
                expectedAuctionList.Single(a => a.Seller.Username == barbara).Id,
                barbara,
                barbaraAuctionEndsOn,
                barbaraAuctionDescription,
                8.25,
                carlotta));
            var aliceAuction = auctionList.Single(a => a.Seller.Username == alice);
            Assert.That(CheckAuctionValues(aliceAuction,
                expectedAuctionList.Single(a => a.Seller.Username == alice).Id,
                alice,
                aliceAuctionEndsOn,
                aliceAuctionDescription,
                .25));
        }

        /// <summary>
        ///     Verify that the the alarm has been initialized with the correct frequency
        ///     on a few different timezone clocks
        /// </summary>
        [Test]
        public void LoadSite_ValidArg_InstantiateAlarmsCorrectTimezone() {
            CreateAndLoadEmptySite(-7, "site -7", 1000, 42.42, out var testAlarmClock1);
            CreateAndLoadEmptySite(5, "site 5", 1000, 42.42, out var testAlarmClock2);
            CreateAndLoadEmptySite(0, "site 0", 1000, 42.42, out var testAlarmClock3);
            Assert.Multiple(() => {
                Assert.That(testAlarmClock1.InstantiateAlarmCallData, Contains.Item(5 * 60 * 1000));
                Assert.That(testAlarmClock2.InstantiateAlarmCallData, Contains.Item(5 * 60 * 1000));
                Assert.That(testAlarmClock3.InstantiateAlarmCallData, Contains.Item(5 * 60 * 1000));
            });
        }
    }

    public class HostSessionCleanupTests : AuctionSiteTests {
        /// <summary>
        ///     Verify that after the alarm rings no expired session survives
        /// </summary>
        [Test]
        public void OnAlarmRaisedCleanupSessions() {
            const string siteName = "pippo";
            const int sessionExpirationTimeInSeconds = 10;
            const int minimumBidIncrement = 5;
            const int timeZone = 1;
            const string uniqueUser = "seller";
            var usernameList = new List<string> { uniqueUser };
            var site = CreateAndLoadSite(timeZone, siteName, sessionExpirationTimeInSeconds, minimumBidIncrement, usernameList, usernameList, 0, out var sessions, out TheClock);
            var initialExpiringTime = sessions.Single(s => s.User.Username == uniqueUser).ValidUntil;
            TheClock.Now = initialExpiringTime.AddSeconds(1);
            TheClock.RunRingingEvent();
            Assert.That(!site.ToyGetSessions().Any(), "not cleaned-up on ringing event");
        }

        /// <summary>
        ///     Verify that after the alarm rings only the not yet expired sessions survive
        /// </summary>
        [Test]
        public void OnAlarmRaisedCleanupOnlyExpiredSessions() {
            const string siteName = "un po' di fantasia";
            const int sessionExpirationTimeInSeconds = 15;
            const int minimumBidIncrement = 5;
            const int timeZone = 7;
            const string user1 = "first user"; //session expires now+15
            const string user2 = "second user"; //session expires now+18
            const string user3 = "third user"; //session expires now+21
            const string user4 = "fourth user"; //session expires now+24
            var usernameList = new List<string> { user1, user2, user3, user4 };
            var site = CreateAndLoadSite(timeZone,
                siteName,
                sessionExpirationTimeInSeconds,
                minimumBidIncrement,
                usernameList,
                usernameList,
                3,
                out var sessions,
                out TheClock);
            var expectedSessions = site.ToyGetSessions().Where(s => s.User.Username == user3 || s.User.Username == user4);
            var initialExpiringTime = sessions.Single(s => s.User.Username == user1).ValidUntil;//original now+15"
            TheClock.Now = initialExpiringTime.AddSeconds(5);
            TheClock.RunRingingEvent();
            var actualSession = site.ToyGetSessions();
            Assert.That(actualSession, Is.EquivalentTo(expectedSessions));
        }
    }
}