namespace TAP22_23.AuctionSite.Testing {
    public class SessionTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes Site:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>site for session tests</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>-5</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>360 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>7</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description> User (with UserName = "My Dear Friend" and Pw = "f86d 78ds6^^^55") </description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>empty list</description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>Session for User</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void Initialize() {
            Site = CreateAndLoadSite(-5, "site for session tests", 360, 7, out TheClock);
            Site.CreateUser(UserName, Pw);
            User = Site.ToyGetUsers().Single(u => u.Username == UserName);
            var session = Site.Login(UserName, Pw);
            if (null == session)
                Assert.Inconclusive($"The user {UserName} should have been able to log in with password {Pw}");
            Session = session!;
        }

        protected ISite Site = null!;
        protected IUser User = null!;
        protected ISession Session = null!;
        protected const string UserName = "My Dear Friend";
        protected const string Pw = "f86d 78ds6^^^55";

        /// <summary>
        ///     Verify that CreateAuction on a deleted session
        ///     throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void CreateAuction_OnInvalidSession_Throws() {
            Session.Logout();
            Assert.That(() => Session.CreateAuction("a", TheClock!.Now, 10), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that CreateAuction on an expired session throws InvalidOperation
        /// </summary>
        [Test]
        public void CreateAuction_OnExpiredSession_Throws() {
            var previousValidUntil = Session.ValidUntil;
            TheClock!.Now = previousValidUntil.AddSeconds(1);
            Assert.That(() => Session.CreateAuction("asta", TheClock.Now.AddMinutes(5), 10), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that CreateAuction with a null auction description
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void CreateAuction_NullDescription_Throws() {
            Assert.That(() => Session.CreateAuction(null!, TheClock!.Now, 10), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that CreateAuction with an empty auction description
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateAuction_EmptyDescription_Throws() {
            Assert.That(() => Session.CreateAuction("", TheClock!.Now, 10), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that CreateAuction with a negative starting price
        ///     throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void CreateAuction_NegativeStartingPrice_Throws() {
            Assert.That(() => Session.CreateAuction("a", TheClock!.Now, -1), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that CreateAuction with an already passed ending date
        ///     throws AuctionSiteUnavailableTimeMachineException
        /// </summary>
        [Test]
        public void CreateAuction_InvalidEndsOnDate_Throws() {
            Assert.That(() => Session.CreateAuction("a", TheClock!.Now.AddHours(-24), 10), Throws.TypeOf<AuctionSiteUnavailableTimeMachineException>());
        }

        /// <summary>
        ///     Verify that two distinct auctions are created with distinct Ids
        /// </summary>
        [Test]
        public void CreateAuction_SecondAuction_ReturnsNewId() {
            var auction1Id = Session.CreateAuction("first auction", TheClock!.Now.AddHours(48), 1024).Id;
            var auction2Id = Session.CreateAuction("a", TheClock.Now.AddHours(24), 22).Id;
            Assert.That(auction1Id, Is.Not.EqualTo(auction2Id));
        }

        /// <summary>
        ///     Verify that a new auction gets a new Id
        ///     when other 50 auctions already exist
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_ReturnsNewId() {
            var randomGen = new Random();
            var usedAuctionIds = new List<int>();
            for (var i = 0; i < 50; i++) {
                var startingPrice = randomGen.NextDouble() * 100 + 1;
                var auction = Session.CreateAuction($"The {i}th auction for this session", DateTime.Now.AddDays(randomGen.Next(3650)), startingPrice);
                usedAuctionIds.Add(auction.Id);
            }
            var newAuctionId = Session.CreateAuction("a", TheClock!.Now.AddHours(24), 22).Id;
            Assert.That(usedAuctionIds, Does.Not.Contain(newAuctionId));
        }

        /// <summary>
        ///     Verify that CreateAuction increases the validity time of the session
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_UpdatesExpirationTime() {
            var previousValidUntil = Session.ValidUntil;
            TheClock!.Now = previousValidUntil.AddSeconds(-1);
            Session.CreateAuction("a", TheClock.Now.AddHours(24), 22);
            var validUntil = Session.ValidUntil;
            Assert.That(validUntil, Is.GreaterThan(previousValidUntil));
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     a non-null auction
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_ReturnsNonNullAuction() {
            var newAuction = Session.CreateAuction("a", TheClock!.Now.AddHours(24), 22);
            Assert.That(newAuction, Is.Not.Null);
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     an auction with the correct Seller
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_SellerOk() {
            var newAuction = Session.CreateAuction("a", TheClock!.Now.AddHours(24), 22);
            Assert.That(newAuction.Seller, Is.EqualTo(User));
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     an auction without current winner
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_NoCurrentWinner() {
            var newAuction = Session.CreateAuction("a", TheClock!.Now.AddHours(24), 22);
            Assert.That(newAuction.CurrentWinner(), Is.Null);
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     an auction with the correct starting price
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_StartingPrice() {
            const int startingPrice = 22;
            var newAuction = Session.CreateAuction("a", TheClock!.Now.AddHours(24), startingPrice);
            Assert.That(newAuction.CurrentPrice(), Is.EqualTo(startingPrice));
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     an auction with the correct ending time (up to seconds)
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_EndsOn() {
            var endsOn = TheClock!.Now.AddHours(24);
            var newAuction = Session.CreateAuction("a", endsOn, 22);
            Assert.That(SameDateTime(newAuction.EndsOn, endsOn));
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     an auction with the correct description
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_Description() {
            const string description = "a";
            var newAuction = Session.CreateAuction(description, TheClock!.Now.AddHours(24), 22);
            Assert.That(newAuction.Description, Is.EqualTo(description));
        }

        /// <summary>
        ///     Verify that CreateAuction on correct parameters creates
        ///     auctions with correct longish descriptions
        /// </summary>
        [Test]
        public void CreateAuction_NewAuction_LongDescription() {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 . , ; @ $ & ( ) ? ";
            var random = new Random();
            var descriptionLength = random.Next(100, 1001);
            var description = new string(Enumerable.Repeat(chars, descriptionLength).Select(s => s[random.Next(s.Length)]).ToArray());
            var newAuction = Session.CreateAuction(description, TheClock!.Now.AddHours(24), 22);
            Assert.That(newAuction.Description, Is.EqualTo(description));
        }


        /// <summary>
        ///     Verify that creating two auctions update the validity of the
        ///     session to the same value as a new login at the same time of
        ///     the second auction
        ///     (that is, CreateAuction reset validity time,
        ///     instead of increasing it of a fixed amount)
        /// </summary>
        [Test]
        public void CreateAuction_TwoInvocations_UpdatesExpirationTimeUpToMax() {
            var sessionExpirationTimeInSeconds = 600;
            var timeZone = 1;
            var userNameList = new List<string>() { "seller" };
            CreateAndLoadSite(timeZone, "pippo", sessionExpirationTimeInSeconds, 5, userNameList, userNameList, 0, out var sessionList, out TheClock, "user0");
            var session = sessionList.First();
            var initialExpiringTime = session.ValidUntil;
            TheClock.AddSeconds(5);
            // Seller creates a new auction with starting price 10
            session.CreateAuction("asta", TheClock.Now.AddMinutes(1), 10);
            TheClock.AddMinutes(5);
            // Seller creates another auction with starting price 10
            session.CreateAuction("asta2", TheClock.Now.AddMinutes(2), 10);
            var finalExpiringTime = session.ValidUntil;
            // session expiration time is between initialExpiringTime and initialExpiringTime+sessionExpirationTimeInSeconds+10
            // (tolerance of 10 seconds to be on the safe side)
            Assert.Multiple(() => {
                Assert.That(initialExpiringTime, Is.LessThan(finalExpiringTime));
                Assert.That(finalExpiringTime, Is.LessThanOrEqualTo(TheClock.Now.AddSeconds(sessionExpirationTimeInSeconds + 10)));
            });
        }
    }
}