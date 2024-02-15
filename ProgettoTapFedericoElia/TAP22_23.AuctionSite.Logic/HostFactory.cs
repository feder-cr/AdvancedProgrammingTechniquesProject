
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;
namespace TAP22_23.AuctionSite.Logic
{
    public class HostFactory : IHostFactory
    {
        public void CreateHost(string connectionString)
        {

            if (connectionString == null)
                throw new AuctionSiteArgumentNullException("connectionStrings cannot be null");

            try
            {
                using (var c = new MyDb(connectionString))
                {
                    c.Database.EnsureDeleted();
                    c.Database.EnsureCreated();
                }
            }
            catch (SqlException e)
            {
                throw new AuctionSiteUnavailableDbException("Unavailable DB", e);
            }

        }

        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory)
        {
            if (connectionString == null)
                throw new AuctionSiteArgumentNullException("connectionStrings cannot be null");
            if (alarmClockFactory == null)
                throw new AuctionSiteArgumentNullException("alarmClockFactory cannot be null");

            using (var c = new MyDb(connectionString))
            {
                try
                {
                    if (!c.Database.CanConnect()) throw new AuctionSiteUnavailableDbException();
                    return new Host(connectionString, alarmClockFactory);
                }
                catch (SqlException e)
                {
                    throw new AuctionSiteUnavailableDbException("Unavailable DB", e);
                }
            }
        }
    }
}
