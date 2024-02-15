namespace TAP22_23.AuctionSite.Testing {
    public class AuctionTests : AuctionSiteTests {
        /// <summary>
        ///     Initializes Site:
        ///     <list type="table">
        ///         <item>
        ///             <term>name</term>
        ///             <description>SiteName = "site for auction tests"</description>
        ///         </item>
        ///         <item>
        ///             <term>time zone</term>
        ///             <description>-2</description>
        ///         </item>
        ///         <item>
        ///             <term>expiration time</term>
        ///             <description>300 seconds</description>
        ///         </item>
        ///         <item>
        ///             <term>minimum bid increment</term>
        ///             <description>7</description>
        ///         </item>
        ///         <item>
        ///             <term>users</term>
        ///             <description>Seller, Bidder1, Bidder2</description>
        ///         </item>
        ///         <item>
        ///             <term>auctions</term>
        ///             <description>
        ///                 TheAuction ("Beautiful object to be desired by everybody",
        ///                 starting price 5, ends in 7 days)
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>sessions</term>
        ///             <description>SellerSession, Bidder1Session, Bidder2Session</description>
        ///         </item>
        ///     </list>
        /// </summary>
        [SetUp]
        public void SiteUsersAuctionInitialize() {
            const int timeZone = -2;
            TheHost.CreateSite(SiteName, timeZone, 300, 7);
            TheClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
            Site = TheHost.LoadSite(SiteName);
            Seller = CreateAndLogUser("seller", out SellerSession, Site);
            Bidder1 = CreateAndLogUser("bidder1", out Bidder1Session, Site);
            Bidder2 = CreateAndLogUser("bidder2", out Bidder2Session, Site);
            TheAuction = SellerSession.CreateAuction("Beautiful object to be desired by everybody", TheClock.Now.AddDays(7), 5);
        }

        protected ISite Site = null!;

        protected IUser Seller = null!;
        protected ISession SellerSession = null!;

        protected IUser Bidder1 = null!;
        protected ISession Bidder1Session = null!;

        protected IUser Bidder2 = null!;
        protected ISession Bidder2Session = null!;

        protected IAuction TheAuction = null!;

        protected const string SiteName = "site for auction tests";

        protected IUser CreateAndLogUser(string username, out ISession session, ISite site) {
            site.CreateUser(username, username);
            var user = site.ToyGetUsers().SingleOrDefault(u => u.Username == username);
            if (null == user)
                Assert.Inconclusive($"user {username} has not been created");
            var login = site.Login(username, username);
            if (null == login)
                Assert.Inconclusive($"user {username} should log in with password {username}");
            session = login!;
            return user!;
        }

        /// <summary>
        ///     Verify that the CurrentWinner on an auction with no bids returns null
        /// </summary>
        [Test]
        public void CurrentWinner_NoBids_Null() {
            var currentWinner = TheAuction.CurrentWinner();
            Assert.That(currentWinner, Is.Null);
        }

        /// <summary>
        ///     Verify that the CurrentWinner on an auction which has received just
        ///     one bid returns the owner of the session used to make the bid
        /// </summary>
        [Test]
        public void Bid_SingleUserBids_Null() {
            TheAuction.Bid(Bidder1Session, 10);
            var winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder1));
        }

        /// <summary>
        ///     Verify that the CurrentWinner on an auction which has received
        ///     two bids returns the owner of the session used to make the highest bid
        /// </summary>
        [Test]
        public void Bid_TwoUsers() {
            TheAuction.Bid(Bidder1Session, 10);
            TheAuction.Bid(Bidder2Session, 20);
            var winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder2));
        }

        /// <summary>
        ///     Verify that the CurrentWinner on an auction which has received
        ///     two bids returns the owner of the session used to make the highest bid
        ///     also when the highest bidder is the first to bid
        /// </summary>
        [Test]
        public void Bid_TwoUsers_U1() {
            TheAuction.Bid(Bidder1Session, 100);
            TheAuction.Bid(Bidder2Session, 20);
            var winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder1));
        }


        /// <summary>
        ///     Verify that the CurrentPrice on an auction with no bids returns
        ///     the starting price of the auction
        /// </summary>
        [Test]
        public void CurrentPrice_NoBids_5() {
            Assert.That(TheAuction.CurrentPrice(), Is.EqualTo(5));
        }

        /// <summary>
        ///     Verify that the CurrentPrice on an auction with one bid returns
        ///     the starting price of the auction
        /// </summary>
        [Test]
        public void Bid_SingleUserBids_5() {
            TheAuction.Bid(Bidder1Session, 10);
            Assert.That(TheAuction.CurrentPrice(), Is.EqualTo(5));
        }

        /// <summary>
        ///     Verify that the CurrentPrice on an auction with two bids by the same user
        ///     returns the starting price of the auction
        /// </summary>
        [Test]
        public void Bid_SingleUserBidsTwice_10() {
            TwoBidsGetPrice(Bidder1Session, 10, Bidder1Session, 20, 5);
        }

        /// <summary>
        ///     Verify that the CurrentPrice on an auction with two bids by different users
        ///     differing more than the minimum bid increment of the auction site
        ///     returns the lower bid increased by the minimum bid increment
        /// </summary>
        [Test]
        public void Bid_TwoUsers_27() {
            TwoBidsGetPrice(Bidder1Session, 100, Bidder2Session, 20, 27);
        }

        /// <summary>
        ///     Verify that the CurrentPrice on an auction with two bids by different users
        ///     differing less than the minimum bid increment of the auction site
        ///     returns the highest bid
        /// </summary>
        [Test]
        public void Bid_TwoUsers_30() {
            TwoBidsGetPrice(Bidder2Session, 25, Bidder1Session, 30, 30);
        }

        /// <summary>
        ///     Make two offers and assert that the final CurrentPrice is <see cref="expectedCurrentPrice" />
        /// </summary>
        /// <param name="firstBidderSession">The session of the first bidder</param>
        /// <param name="firstOffer">The first bid</param>
        /// <param name="secondBidderSession">The session of the second bidder</param>
        /// <param name="secondOffer">The second bid</param>
        /// <param name="expectedCurrentPrice">The expected final price</param>
        private void TwoBidsGetPrice(ISession firstBidderSession, int firstOffer, ISession secondBidderSession, int secondOffer, int expectedCurrentPrice) {
            TheAuction.Bid(firstBidderSession, firstOffer);
            TheAuction.Bid(secondBidderSession, secondOffer);
            Assert.That(TheAuction.CurrentPrice(), Is.EqualTo(expectedCurrentPrice));
        }

        /// <summary>
        ///     Verify that bidding on an auction increases the validity time
        ///     of the bidder session
        /// </summary>
        [Test]
        public void Bid_ValidOffer_UpdatesSessionsValidUntil() {
            var validUntilBeforeBid = Bidder1Session.ValidUntil;
            TheClock!.AddSeconds(3 * 60);
            TheAuction.Bid(Bidder1Session, 30);
            var validUntilAfterBid = Bidder1Session.ValidUntil;
            Assert.That(validUntilBeforeBid, Is.LessThan(validUntilAfterBid));
        }

        /// <summary>
        ///     Verify that after deleting an auction, its id is not known anymore to the auction site
        /// </summary>
        [Test]
        public void Delete_ExistingAuction_Deletes() {
            var auctionId = TheAuction.Id;
            TheAuction.Delete();
            var deletedAuctionSurvives = Site.ToyGetAuctions(false).Any(a => a.Id == auctionId);
            Assert.That(!deletedAuctionSurvives);
        }

        /// <summary>
        ///     Verify that a call to Delete on a deleted auction throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void Delete_DeletedAuction_Throws() {
            TheAuction.Delete();
            Assert.That(() => TheAuction.Delete(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }

        /// <summary>
        ///     Verify that a call to Bid on a deleted auction throws AuctionSiteInvalidOperationException
        /// </summary>
        [Test]
        public void Bid_DeletedAuction_Throws() {
            TheAuction.Delete();
            Assert.That(() => TheAuction.Bid(Bidder1Session, 10), Throws.TypeOf<AuctionSiteInvalidOperationException>());
        }                                                                               

        /// <summary>
        ///     Verify that a call to Bid with a null session throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void Bid_NullSession_Throws() {
            Assert.That(() => TheAuction.Bid(null!, 44), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that a call to Bid with a negative bid throws AuctionSiteArgumentOutOfRangeException
        /// </summary>
        [Test]
        public void Bid_NegativeOffer_Throws() {
            Assert.That(() => TheAuction.Bid(Bidder1Session, -77), Throws.TypeOf<AuctionSiteArgumentOutOfRangeException>());
        }

        /// <summary>
        ///     Verify that a call to Bid with an expired session throws AuctionSiteArgumentException
        /// </summary>
        [Test]
        public void Bid_ExpiredSession_Throws() {
            var sessionExpirationTime = Bidder1Session.ValidUntil;
            TheClock!.Now = sessionExpirationTime.AddSeconds(1);
            Assert.That(() => TheAuction.Bid(Bidder1Session, 101), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that a call to Bid on a session done
        ///     after the owner logged out throws AuctionSiteArgumentException
        /// </summary>
        [Test]
        public void Bid_DeletedSession_Throws() {
            Bidder1Session.Logout();
            Assert.That(() => TheAuction.Bid(Bidder1Session, 101), Throws.TypeOf<AuctionSiteArgumentException>());
        }

        /// <summary>
        ///     Verify that a bid smaller than the starting price is not accepted
        /// </summary>
        [Test]
        public void Bid_NotEnoughMoney_False0() {
            var anotherAuction = SellerSession.CreateAuction("another auction", TheClock!.Now.AddDays(5), 100);
            var accepted = anotherAuction.Bid(Bidder2Session, 10);
            Assert.That(!accepted);
        }

        /// <summary>
        ///     Verify that a bid smaller than another bid by the same user
        ///     is not accepted
        /// </summary>
        [Test]
        public void Bid_NotEnoughMoney_False1() {
            TheAuction.Bid(Bidder2Session, 100);
            TheAuction.Bid(Bidder1Session, 20);
            var accepted = TheAuction.Bid(Bidder2Session, 50);
            Assert.That(!accepted);
        }

        /// <summary>
        ///     Verify that a bid higher than the current price is accepted
        ///     even if smaller than the higher standing bidding (by a different user)
        /// </summary>
        [Test]
        public void Bid_NotEnoughMoneyToWin_True() {
            var anotherAuction = SellerSession.CreateAuction("another auction", TheClock!.Now.AddDays(5), 10);
            anotherAuction.Bid(Bidder1Session, 100);
            var accepted = anotherAuction.Bid(Bidder2Session, 21);
            Assert.That(accepted);
        }

        /// <summary>
        ///     Verify that the first bid is accepted when equal to the starting price
        /// </summary>
        [Test]
        public void Bid_EnoughMoney_True1() {
            var anotherAuction = SellerSession.CreateAuction("another auction 1", TheClock!.Now.AddDays(5), 23);
            var accepted = anotherAuction.Bid(Bidder2Session, 23);
            Assert.That(accepted);
        }

        /// <summary>
        ///     Verify that the first bid is accepted when greater than the starting price
        /// </summary>
        [Test]
        public void Bid_EnoughMoney_True2() {
            var anotherAuction = SellerSession.CreateAuction("another auction 2", TheClock!.Now.AddDays(5), 23);
            var accepted = anotherAuction.Bid(Bidder2Session, 333);
            Assert.That(accepted);
        }

        /// <summary>
        ///     Verify that the second bid by the same user is accepted if it is greater than the first
        /// </summary>
        [Test]
        public void Bid_EnoughMoney_True3() {
            TheAuction.Bid(Bidder1Session, 10);
            var accepted = TheAuction.Bid(Bidder1Session, 333);
            Assert.That(accepted);
        }
    }
}