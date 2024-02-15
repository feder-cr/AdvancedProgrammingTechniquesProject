using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace TAP22_23.AuctionSite.Logic
{
    internal class User : IUser
    {
        public int Id { get; }
        public string Username { get;}

        private readonly Site site;
        private readonly string connectionString;
        public User(UserEntity user, Site site, string connectionString)
        {
            Username = user.Username;
            Id = user.UserEntityId;
            this.connectionString = connectionString;
            this.site = site;
        }



        public void Delete()
        {
            using (var c = new MyDb(connectionString))
            {

                var user = c.UserEntities.FirstOrDefault(u => u.UserEntityId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException("User not exist");

                var activeAuctionsWinner =
                    c.AuctionEntities.Where(a => a.CurrentWinner.UserEntityId == Id).Where(a => a.EndsOn > DateTime.Now);

                var activeAuctionsOwner =
                    c.AuctionEntities.Where(a => a.OwnerId == Id).Where(a => a.EndsOn > DateTime.Now);

                if (activeAuctionsWinner.Any() || activeAuctionsOwner.Any())
                {
                      throw new AuctionSiteInvalidOperationException(
                          "This user can't be deleted because they are the owner or winner of open auctions.");
                }

                var expiredAuctionsOwner =
                    c.AuctionEntities.Where(a => a.OwnerId == Id).Where(a => a.EndsOn < DateTime.Now);
                
                foreach (var auction in expiredAuctionsOwner)
                {
                    c.AuctionEntities.Remove(auction);
                }

                c.UserEntities.Remove(user);
                c.SaveChanges();
            }
        }


        public IEnumerable<IAuction> WonAuctions()
        {
            using (var c = new MyDb(connectionString))
            {
                var user = c.UserEntities.FirstOrDefault(u => u.UserEntityId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException("User deleted");
                var auctions = c.AuctionEntities.Include(a => a.Owner).Include(a=> a.CurrentWinner).Where(a => a.EndsOn < site.Now() && a.CurrentWinnerId == Id);
                return WonAuctionsNoEx(auctions.ToList());
            }
        }

        IEnumerable<IAuction> WonAuctionsNoEx(List<AuctionEntity> auctions)
        {
            foreach (var auction in auctions)
            {
                yield return new Auction(auction, site, connectionString);
            }
        }


        public override bool Equals(object? obj)
        {
            var item = obj as User;
            if (item == null) return false;
            return Id == item.Id;
        }


    }
}
