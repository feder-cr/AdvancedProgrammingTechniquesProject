using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace TAP22_23.AuctionSite.Logic
{
    public class Session : ISession
    {
        public string Id { get; }
        private readonly string connectionString;
        private readonly Site site;
        public IUser User { get; }

        public DateTime ValidUntil
        {
            get
            {
                using (var c = new MyDb(connectionString))
                {
                    var session = c.SessionEntities.FirstOrDefault(s => s.SessionEntityId == Id);
                    if (session == null) throw new AuctionSiteInvalidOperationException("Session not exist");
                    return session.ValidUntil;
                }
            }
        }

        public Session(string id, IUser user, Site site, string connectionString)
        {
            Id = id;
            User = user;
            this.site = site;
            this.connectionString = connectionString;
        }

        public Session(SessionEntity session, IUser user, Site site, string connectionString) : this(session.SessionEntityId, user, site, connectionString) { }


        public void ResetExpirationTime()
        {
            using (var context = new MyDb(connectionString))
            {
                var session = context.SessionEntities.FirstOrDefault(s => s.SessionEntityId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session not exist");
                session.ValidUntil = site.Now().AddSeconds(site.SessionExpirationInSeconds);
                context.SaveChanges();
            }
        }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice)
        {
            if (ValidUntil < site.Now()) throw new AuctionSiteInvalidOperationException("Session expired");
            if (description == null) throw new AuctionSiteArgumentNullException("description cannot be null");
            if (description.Length == 0) throw new AuctionSiteArgumentException("description cannot be empty");
            if (startingPrice < 0)
                throw new AuctionSiteArgumentOutOfRangeException("startingPrice cannot be a negative value");
            if (endsOn < site.Now())
                throw new AuctionSiteUnavailableTimeMachineException("endsOn cannot precede the current time.");
            using (var c = new MyDb(connectionString))
            {
                var auction = new AuctionEntity(description, endsOn, startingPrice, ((User)User).Id, site.Id);
                c.AuctionEntities.Add(auction);
                c.SaveChanges();
                auction = c.AuctionEntities.Include(a => a.Owner).First(a => a == auction);
                ResetExpirationTime();
                return new Auction(auction, site, connectionString);
            }
        }
        public override bool Equals(object? obj)
        {
            var other = obj as Session;
            if (other == null) return false;
            return other.Id == Id;
        }

        public void Logout()
        {
            using (var c = new MyDb(connectionString))
            {
                var session = c.SessionEntities.SingleOrDefault(s => s.SessionEntityId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session not exist");
                c.Remove(session);
                c.SaveChanges();
            }
        }
    }
}
