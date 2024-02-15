namespace TAP22_23.AuctionSite.Testing {
    public class UserTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes Site:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>site for user tests</description>
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
        ///             <description>username = "My Dear Friend", pw = "f86d 78ds6^^^55"</description>
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
        public void Initialize() {
            Site = CreateAndLoadEmptySite(-5, "site for user tests", 360, 7, out TheClock);
            Site.CreateUser(UserName, Pw);
            User = Site.ToyGetUsers().Single(u => u.Username == UserName);
        }

        protected ISite Site = null!;
        protected IUser User = null!;
        protected const string UserName = "My Dear Friend";
        protected const string Pw = "f86d 78ds6^^^55";

        /// <summary>
        ///     Verify that after deleting a user, their name is not known anymore to the site
        /// </summary>
        [Test]
        public void Delete_ExistingUser_Deletes() {
            User.Delete();
            var survived = Site.ToyGetUsers().Any(u => u.Username == UserName);
            Assert.That(!survived);
        }

        /// <summary>
        ///     Verify that a call to Delete on a deleted user throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void Delete_DeletedUser_Throws() {
            User.Delete();
            Assert.That(() => User.Delete(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that a newly created user has no won auctions
        /// </summary>
        [Test]
        public void WonAuctions_NewUser_NoAuctions() {
            var wonAuctions = User.WonAuctions();
            Assert.That(wonAuctions, Is.Empty);
        }

        /// <summary>
        ///     Verify that WonAuctions returns the won auctions of a user who has won some
        /// </summary>
        /// <param name="howManyAuctions"></param>
        [Test]
        public void WonAuctions_UserWithWonAuctions_NonEmpty([Random(1, 10, 1)] int howManyAuctions) {
            var userSession = Site.Login(UserName, Pw)!;
            const string sellerName = "very lucky seller";
            const string sellerPw = "seller's password";
            Site.CreateUser(sellerName, sellerPw);
            var sellerSession = Site.Login(sellerName, sellerPw)!;
            var randomGen = new Random();
            var auctions = new List<IAuction>();
            for (var i = 0; i < howManyAuctions; i++) {
                var startingPrice = randomGen.NextDouble() * 100 + 1;
                var auction = sellerSession.CreateAuction($"The {i}th auction for {sellerName}", TheClock!.Now.AddDays(randomGen.Next(3650)), startingPrice);
                auctions.Add(auction);
                auction.Bid(userSession, startingPrice * 2);
            }

            TheClock!.AddSeconds(3650 * 24 * 60 * 60 + 1);
            var wonAuctions = User.WonAuctions();
            Assert.That(auctions, Is.EquivalentTo(wonAuctions));
        }
    }
}