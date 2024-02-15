using Microsoft.Data.SqlClient;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace TAP22_23.AuctionSite.Logic
{
    public class Host : IHost
    {
        public string ConnectionString { get; set; }
        public IAlarmClockFactory AlarmClockFactory { get; set; }

        public Host(string connectionString, IAlarmClockFactory alarmClockFactory)
        {
            ConnectionString = connectionString;
            AlarmClockFactory = alarmClockFactory;
        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos()
        {
            List<SiteEntity> sites;

            try
            {
                using (var c = new MyDb(ConnectionString))
                {
                    sites = c.SiteEntities.ToList();
                }
            }
            catch (SqlException e)
            {
                throw new AuctionSiteUnavailableDbException(" DB not responding", e);
            }

            foreach (var site in sites)
            {
                yield return (site.Name, site.Timezone);
            }
        }

        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement)
        {
            if(name == null) throw new AuctionSiteArgumentNullException("Site name cannot be null");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("Name Invalid length");
            if (timezone < DomainConstraints.MinTimeZone || timezone > DomainConstraints.MaxTimeZone) throw new AuctionSiteArgumentOutOfRangeException("Invalid timezone value");
            if (minimumBidIncrement <= 0) throw new AuctionSiteArgumentOutOfRangeException("Invalid minimumbidincrement value");
            if (sessionExpirationTimeInSeconds <= 0) throw new AuctionSiteArgumentOutOfRangeException("Invalid sessionExpirationTimeInSeconds value");

            try
            {
                using (var c = new MyDb(ConnectionString))
                {
                    c.SiteEntities.Add(new SiteEntity(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement));
                    try
                    {
                        c.SaveChanges();
                    }
                    catch (AuctionSiteNameAlreadyInUseException e)
                    {
                        throw new AuctionSiteNameAlreadyInUseException(name, "This name site alredy exist", e);
                    }
                }
            }
            catch (SqlException e)
            {
                throw new AuctionSiteUnavailableDbException(" DB not responding", e);
            }
            catch (AuctionSiteNameAlreadyInUseException e)
            {
                throw new AuctionSiteNameAlreadyInUseException(name, "This name site alredy exist", e);
            }


        }

        public ISite LoadSite(string name)
        {
            if (name == null) throw new AuctionSiteArgumentNullException("Site name cannot be null");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("Name Invalid length");
            SiteEntity? site;
            using (var c = new MyDb(ConnectionString))
            {
                try
                {
                    site = c.SiteEntities.SingleOrDefault(s => s.Name == name);
                }
                catch (SqlException e)
                {
                    throw new AuctionSiteUnavailableDbException("DataBase not responding", e);

                }
                if (site == null) throw new AuctionSiteInexistentNameException($"This site not exist");
                return new Site(AlarmClockFactory.InstantiateAlarmClock(site.Timezone), ConnectionString, site);
            }
        }
    }
}
