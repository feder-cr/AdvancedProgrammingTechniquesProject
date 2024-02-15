namespace TAP22_23.AuctionSite.Testing {
    [TestFixture]
    public class HostFactoryNullConnectionStringTests : AuctionSiteTests {
        /// <summary>
        ///     Verify that CreateHost on a null connection string
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void CreateHost_NullArg_Throws() {
            Assert.That(() => TheHostFactory.CreateHost(null!), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that LoadHost on a null connection string
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void LoadHost_NullConnectionString_Throws() {
            Assert.That(() => TheHostFactory.LoadHost(null!, TheAlarmClockFactory), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }

        /// <summary>
        ///     Verify that LoadHost on a null clock factory
        ///     throws AuctionSiteArgumentNullException
        /// </summary>
        [Test]
        public void LoadHoast_NullClockFactory_Throws() {
            Assert.That(() => TheHostFactory.LoadHost(TheConnectionString, null!), Throws.TypeOf<AuctionSiteArgumentNullException>());
        }
    }

    [TestFixture]
    public class HostFactoryBadConnectionTests : AuctionSiteTests {
        /// <summary>
        ///     Verify that CreateHost on a connection string with wrong Data Source
        ///     throws AuctionSiteUnavailableDbException
        /// </summary>
        [Test]
        public void CreateHost_BadConnectionString_Throws() {
            Assert.That(() => TheHostFactory.CreateHost("Data Source=pippo;Initial Catalog=pluto;Integrated Security=True;Connection Timeout=2"), Throws.TypeOf<AuctionSiteUnavailableDbException>());
        }

        /// <summary>
        ///     Verify that LoadHost on a connection string with wrong Data Source
        ///     throws AuctionSiteUnavailableDbException
        /// </summary>
        [Test]
        public void LoadHost_BadConnectionString_Throws() {
            Assert.That(() => TheHostFactory.LoadHost("Data Source=pippo;Initial Catalog=pluto;Integrated Security=True;Connection Timeout=2", TheAlarmClockFactory),
                Throws.TypeOf<AuctionSiteUnavailableDbException>());
        }
    }
}