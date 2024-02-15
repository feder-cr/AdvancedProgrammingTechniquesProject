using System.Diagnostics;

namespace TAP22_23.AuctionSite.Testing {
    public class SiteTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes Site:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>working site</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>5</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>3600 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>3.5</description>
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
        public void SiteInitialize() {
            const string siteName = "working site";
            const int timeZone = 5;
            TheHost.CreateSite(siteName, timeZone, 3600, 3.5);
            Site = TheHost.LoadSite(siteName);
            TheClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
        }

        protected ISite Site = null!;

        private IEnumerable<IAuction> AddAuctions(DateTime endsOn, int auctionNumber) {
            Debug.Assert(auctionNumber > 0);
            var username = "pinco" + DateTime.Now.Ticks;
            Site.CreateUser(username, "pippo.123");
            var sellerSession = Site.Login(username, "pippo.123")!;
            var result = new List<IAuction>();
            for (var i = 0; i < auctionNumber; i++)
                result.Add(sellerSession.CreateAuction($"Auction {i} of {auctionNumber} ending on {endsOn}", endsOn, 7.7 * i + 11));
            return result;
        }

        /// <summary>
        ///     Verify that a call to ToyGetUsers on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void ToyGetUsers_OnDeletedObject_Throws() {
            Site.Delete();
            Assert.That(() => Site.ToyGetUsers(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that ToyGetUsers on a site without users
        ///     returns the empty sequence
        /// </summary>
        [Test]
        public void ToyGetUsers_ValidArg_ReturnsEmpty() {
            var users = Site.ToyGetUsers();
            Assert.That(users, Is.Empty);
        }

        /// <summary>
        ///     Verify that ToyGetUsers on a site with 5 users
        ///     returns 5 users
        /// </summary>
        [Test]
        public void ToyGetUsers_ValidArg_ReturnsEnumerableOf5() {
            var someUsers = new List<string> { "primo", "secondo", "terzo", "quarto", "quinto" };
            foreach (var user in someUsers)
                Site.CreateUser(user, "verySTRONGp@ssw0rd");

            var users = Site.ToyGetUsers().ToList();
            Assert.That(users, Has.Count.EqualTo(5));
        }

        /// <summary>
        ///     Verify that ToyGetUsers on a site with 5 users
        ///     returns users with the same names
        /// </summary>
        [Test]
        public void ToyGetUsers_ValidArg_ReturnsEnumerableOf5WithCorrectNames() {
            var someUsernames = new List<string> { "primo", "secondo", "terzo", "quarto", "quinto" };
            foreach (var user in someUsernames)
                Site.CreateUser(user, "verySTRONGp@ssw0rd");

            var usernames = Site.ToyGetUsers().Select(u => u.Username);
            Assert.That(someUsernames, Is.EquivalentTo(usernames));
        }

        /// <summary>
        ///     Verify that a call to ToyGetAuctions on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void ToyGetAuctions_OnDeletedObject_Throws() {
            Site.Delete();
            Assert.That(() => Site.ToyGetAuctions(true), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that ToyGetAuctions on a site without users
        ///     returns the empty sequence
        /// </summary>
        [Test]
        public void ToyGetAuctions_ValidArg_ReturnsEmpty1() {
            var auctions = Site.ToyGetAuctions(false);
            Assert.That(auctions, Is.Empty);
        }

        /// <summary>
        ///     Verify that ToyGetAuctions on a site with only expired auctions
        ///     returns the empty sequence if called with true
        /// </summary>
        [Test]
        public void ToyGetAuctions_ValidArg_ReturnsEmpty2() {
            AddAuctions(TheClock!.Now.AddDays(1), 7);
            TheClock.AddHours(25);
            var auctions = Site.ToyGetAuctions(true);
            Assert.That(auctions, Is.Empty);
        }

        /// <summary>
        ///     Verify that ToyGetAuctions on a site with two expired
        ///     and three still open auctions returns all five if called with false
        /// </summary>
        [Test]
        public void ToyGetAuctions_ValidArg_ReturnsEnumerableOf5() {
            var tomorrow = TheClock!.Now.AddDays(1);
            var expectedAuctions = AddAuctions(tomorrow, 2).Concat(AddAuctions(tomorrow.AddMinutes(5), 3));
            TheClock.Now = tomorrow.AddMinutes(1);
            var auctions = Site.ToyGetAuctions(false);
            Assert.That(auctions, Is.EquivalentTo(expectedAuctions));
        }

        /// <summary>
        ///     Verify that ToyGetAuctions on a site with five expired
        ///     and 3 still open auctions returns (only) the latter if called with true
        /// </summary>
        [Test]
        public void ToyGetAuctions_ValidArg_ReturnsEnumerableOf3Valid() {
            var tomorrow = TheClock!.Now.AddDays(1);
            AddAuctions(tomorrow, 5);
            var expectedAuctions = AddAuctions(tomorrow.AddDays(1), 3);
            TheClock.Now = tomorrow.AddHours(2);
            var auctions = Site.ToyGetAuctions(true);
            Assert.That(auctions, Is.EquivalentTo(expectedAuctions));
        }

        /// <summary>
        ///     Verify that a call to Login on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void Login_OnDeletedSite_Throws() {
            const string userName = "Vincenzo";
            const string password = "gdgajgfjgkgfakg";
            Site.CreateUser(userName, password);
            Site.Delete();
            Assert.That(() => Site.Login(userName, password), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that login on null username throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void Login_NullUsername_Throws() {
            Assert.That(() => Site.Login(null!, "puffetta"), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that login on null password throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void Login_NullPassword_Throws() {
            const string userName = "Agata";
            const string password = "nq3b457asf7";
            Site.CreateUser(userName, password);
            Assert.That(() => Site.Login(userName, null!), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that login on inexistent user return null session
        /// </summary>
        [Test]
        public void Login_InexistentUser_Returns_null() {
            var session = Site.Login("pinco", "ciao");
            Assert.That(session, Is.Null);
        }

        /// <summary>
        ///     Verify that login on an existing user with a wrong password returns a null session
        /// </summary>
        [Test]
        public void Login_WrongPassword_Returns_null() {
            const string username = "pinco";
            Site.CreateUser(username, "pippo.123");
            var session = Site.Login(username, "ciao");
            Assert.That(session, Is.Null);
        }

        /// <summary>
        ///     Verify that login on correct credentials return a non null session
        /// </summary>
        [Test]
        public void Login_ValidArg_ReturnsNonNullSession() {
            const string username = "pinco";
            const string password = "pippo.123";
            Site.CreateUser(username, password);
            var session = Site.Login(username, password);
            Assert.That(session, Is.Not.Null);
        }

        /// <summary>
        ///     Verify that login on correct credentials return a valid session
        ///     for the correct user
        /// </summary>
        [Test]
        public void Login_ValidArg_ReturnsUserSession() {
            const string username = "pinco";
            const string password = "pippo.123";
            Site.CreateUser(username, password);
            var session = Site.Login(username, password)!;
            Assert.That(session.User, Is.EqualTo(Site.ToyGetUsers().Single(u => u.Username == username)));
        }

        /// <summary>
        ///     Verify that login on correct credentials return a valid session
        ///     with a feasible expiration time
        /// </summary>
        [Test]
        public void Login_ValidArg_ReturnsSessionCorrectValidity() {
            const string username = "pinco";
            const string password = "pippo.123";
            Site.CreateUser(username, password);
            var session = Site.Login(username, password)!;
            var now = TheClock!.Now;
            Assert.That(session.ValidUntil, Is.InRange(now.AddSeconds(Site.SessionExpirationInSeconds - 5), now.AddSeconds(Site.SessionExpirationInSeconds + 5)));
        }

        /// <summary>
        ///     Verify that two calls to login for the same user return the same object
        /// </summary>
        [Test]
        public void Login_ValidArg_ReturnsOldSession() {
            const string username = "pinco";
            const string password = "pippo.123";
            Site.CreateUser(username, password);
            var expectedSessionId = Site.Login(username, password)!.Id;
            var sessionId = Site.Login(username, password)!.Id;
            Assert.That(sessionId, Is.EqualTo(expectedSessionId));
        }

        /// <summary>
        ///     Verify that a call to ToyGetSessions on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void ToyGetSessions_OnDeletedSite_Throws() {
            Site.Delete();
            Assert.That(() => Site.ToyGetSessions().ToList(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that ToyGetSessions does not return a logged-out session
        /// </summary>
        [Test]
        public void ToyGetSessions_Does_Not_Include_LoggedOut() {
            Site.CreateUser("pinco", "pippo.123");
            var session = Site.Login("pinco", "pippo.123")!;
            var sessionId = session.Id;
            session.Logout();
            var inexistentSession = Site.ToyGetSessions().SingleOrDefault(s => s.Id == sessionId);
            Assert.That(inexistentSession, Is.Null);
        }

        /// <summary>
        ///     Verify that ToyGetSessions does not return an expired session
        /// </summary>
        [Test]
        public void ToyGetSessions_DoesNot_Include_TimedOut_Sessions() {
            Site.CreateUser("pinco", "pippo.123");
            var session = Site.Login("pinco", "pippo.123")!;
            TheClock!.AddSeconds(Site.SessionExpirationInSeconds + 1);
            TheClock.RunRingingEvent();
            var inexistentSession = Site.ToyGetSessions().SingleOrDefault(s => s.Id == session.Id);
            Assert.That(inexistentSession, Is.Null);
        }

        /// <summary>
        ///     Verify that a known valid session is included in the result of ToyGetSession
        /// </summary>
        [Test]
        public void ToyGetSessions_ValidArg_ReturnsOneKnownSession() {
            var username = "pinco";
            var password = "pippo.123";
            Site.CreateUser(username, password);
            var expectedSession = Site.Login(username, password);
            if (null == expectedSession)
                Assert.Inconclusive($"user {username} should have been able to log in with password {password}");
            var session = Site.ToyGetSessions().Single(s => s.Id == expectedSession!.Id);
            Assert.That(AreEquivalentSessions(session, expectedSession));
        }

        /// <summary>
        ///     Verify that a group of valid sessions is included in the result of ToyGetSession
        /// </summary>
        [TestCase(3)]
        [TestCase(10)]
        public void ToyGetSessions_ValidArg_ReturnsKnownSessions(int sessionNumber) {
            var username = "nobody ";
            var password = "sempre uguale";
            var knownSessions = new ISession[sessionNumber];
            for (int i = 0; i < sessionNumber; i++) {
                Site.CreateUser(username + i, password);
                var aSession = Site.Login(username + i, password);
                if (null == aSession)
                    Assert.Inconclusive($"user {username} should have been able to log in with password {password}");
                knownSessions[i] = aSession!;
            }
            var sessions = Site.ToyGetSessions().ToList();
            Assert.Multiple(() => {
                for (int i = 0; i < sessionNumber; i++) {
                    var currentSession = sessions.FirstOrDefault(s => s.Id == knownSessions[i].Id);
                    Assert.That(AreEquivalentSessions(currentSession, knownSessions[i]));
                }
            });
        }

        /// <summary>
        ///     Verify that a call to CreateUser on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void CreateUser_OnDeletedObject_Throws() {
            Site.Delete();
            Assert.That(() => Site.CreateUser("new user", "shdhjajlhkahf"), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that CreateUser on null username throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void CreateUser_NullUsername_Throws() {
            Assert.That(() => Site.CreateUser(null!, "pincopallo"), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that CreateUser on null password throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void CreateUser_NullPassword_Throws() {
            Assert.That(() => Site.CreateUser("pincopallo", null!), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that CreateUser on a password shorter that
        ///     DomainConstraints.MinUserPassword throws AuctionSiteArgumentException
        /// </summary>
        [Test]
        public void CreateUser_TooShort_password_Throws() {
            Assert.That(() => Site.CreateUser("pincopallo", "boh"), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that CreateUser on a username shorter that
        ///     DomainConstraints.MinUserName throws AuctionSiteArgumentException
        /// </summary>
        [Test]
        public void CreateUser_TooShort_username_Throws() {
            Assert.That(() => Site.CreateUser("aa", "ma si'!"), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that CreateUser on a username longer that
        ///     DomainConstraints.MaxUserName throws AuctionSiteArgumentException
        /// </summary>
        [Test]
        public void CreateUser_TooLong_username_Throws() {
            Assert.That(() => Site.CreateUser("abcdefgh12345678abcdefgh12345678abcdefgh12345678abcdefgh12345678A", "vabenecosi'"), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that the second call to CreateUser on the username and password
        ///     throws AuctionSiteNameAlreadyInUseException
        /// </summary>
        [Test]
        public void CreateUser_Taken_Username_Throws() {
            Site.CreateUser("Giorgio", "corretta");
            Assert.That(() => Site.CreateUser("Giorgio", "corretta"), Throws.TypeOf<AuctionSiteNameAlreadyInUseException>());
        }


        /// <summary>
        ///     Verify that CreateUser on a username already taken (and different password)
        ///     throws AuctionSiteNameAlreadyInUseException
        /// </summary>
        [Test]
        public void CreateUser_Taken_Username_DifferentPassword_Throws() {
            Site.CreateUser("Giorgio", "corretta");
            Assert.That(() => Site.CreateUser("Giorgio", "ma si'!"), Throws.TypeOf<AuctionSiteNameAlreadyInUseException>());
        }

        /// <summary>
        ///     Verify that CreateUser adds a user with that name
        /// </summary>
        [Test]
        public void CreateUser_ValidArgs_CreateUserOkUsername() {
            const string username = "SonoNuovo";
            const string password = "ma si'!";
            Site.CreateUser(username, password);
            var user = Site.ToyGetUsers().SingleOrDefault(u => u.Username == username);
            Assert.That(user, Is.Not.Null);
        }

        /// <summary>
        ///  Verify that CreateUser with correct credentials may login
        ///  with those credentials
        /// </summary>
        [Test]
        public void CreateUser_ValidArgs_CreateUserOkCredentials() {
            const string username = "SonoNuovo";
            const string password = "ma si'!";
            Site.CreateUser(username, password);
            var session = Site.Login(username, password)!;
            Assert.That(session, Is.Not.Null);
            Assert.That(session.User.Username, Is.EqualTo(username));
        }

        /// <summary>
        ///     Verify that newly created users do not have sessions
        /// </summary>
        [Test]
        public void CreateUser_ValidArgs_CreateUser_Without_Session() {
            const string username = "pinco";
            const string password = "pippo.123";
            Site.CreateUser(username, password);
            var newUserSessions = Site.ToyGetSessions().Where(s => s.User.Username == username);
            Assert.That(newUserSessions, Is.Empty);
        }

        /// <summary>
        ///     Verify that a call to Delete on a deleted site throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void Delete_OnDeletedObject_Throws() {
            Site.Delete();
            Assert.That(() => Site.Delete(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that after deleting a site, its name is not known anymore to the Host
        /// </summary>
        [Test]
        public void Delete_ValidArg_DeletesThis() {
            Site.Delete();
            var survived = TheHostFactory.LoadHost(TheConnectionString, TheAlarmClockFactory).GetSiteInfos().Any(s => s.Name == Site.Name);
            Assert.That(!survived);
        }

        /// <summary>
        ///     Verify that property Name yield the site name
        /// </summary>
        [Test]
        public void Name_ReturnsSiteName() {
            const string name = "mi piace";
            TheHost.CreateSite(name, 0, 10, 2);
            var mySite = TheHost.LoadSite(name);
            Assert.That(mySite.Name, Is.EqualTo(name));
        }
    }
}