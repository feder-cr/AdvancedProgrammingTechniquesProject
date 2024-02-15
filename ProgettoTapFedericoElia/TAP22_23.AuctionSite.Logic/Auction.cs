
using System;
using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace TAP22_23.AuctionSite.Logic
{
    internal class Auction : IAuction
    {
        public int Id { get; }
        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }
        private readonly string connectionString;
        private readonly Site site;

        public Auction(string connectionString, Site site, int id, IUser seller, string description, DateTime endsOn)
        {
            this.connectionString = connectionString;
            this.site = site;
            Id = id;
            Seller = seller;
            Description = description;
            EndsOn = endsOn;
        }

        public Auction(AuctionEntity auction, Site site, string connectionString) :
            this(connectionString, site, auction.AuctionEntityId, new User(auction.Owner!, site, connectionString), auction.Description, auction.EndsOn)
        { }

        public IUser? CurrentWinner()
        {
            using (var c = new MyDb(connectionString))
            {
                var auction = c.AuctionEntities.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (auction == null) { throw new AuctionSiteInvalidOperationException("Auction not exist"); }
                var currentWinner = c.AuctionEntities.Include(a => a.CurrentWinner).SingleOrDefault(a => a.AuctionEntityId == Id).CurrentWinner; ;
                    
                if (currentWinner == null)                         
                {
                    return null;
                }
                else return new User(currentWinner, site, connectionString);
            }
        }

        public double CurrentPrice()
        {
            using (var c = new MyDb(connectionString))
            {
                var auction = c.AuctionEntities.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (auction == null) { throw new AuctionSiteInvalidOperationException("Auction not exist"); }
                var currentPrice = c.AuctionEntities.FirstOrDefault(a => a.AuctionEntityId == Id);

                return currentPrice.CurrentPrice;
                    
            }
        }

        public void Delete()
        {

            using (var c = new MyDb(connectionString))
            {
                var auction = c.AuctionEntities.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (auction != null)
                {
                    c.Remove(auction);
                    c.SaveChanges();
                }
                else
                {
                    throw new AuctionSiteInvalidOperationException("Auction not exist");
                }
            }


        }
        public override bool Equals(object? obj)
        {
            var other = obj as Auction;
            if (other == null) return false;
            return other.Id == Id;
        }

        public bool Bid(ISession session, double offer)
        {
            using (var c = new MyDb(connectionString))
            {
                var auction = c.AuctionEntities.FirstOrDefault(a => a.AuctionEntityId == Id);
                if(auction == null) { throw new AuctionSiteInvalidOperationException("Auction not exist"); }
                if(session==null) throw new AuctionSiteArgumentNullException("Session expired");
                if (offer < 0) throw new AuctionSiteArgumentOutOfRangeException("Offer cannot be negative");

                var sessionE = c.SessionEntities.FirstOrDefault(a => a.SessionEntityId == session.Id);
                if (sessionE == null)
                {
                    throw new AuctionSiteArgumentException("Session expired");
                }
                else
                {
                    if ((sessionE.ValidUntil < site.Now()) || ((User)Seller).Id == sessionE.UserId)
                    {
                        throw new AuctionSiteArgumentException("Problem with a session");
                    }
                }



                ((Session)session).ResetExpirationTime();
                var currentwinner = CurrentWinner();
                var bidder = session.User;
                if (bidder.Equals(CurrentWinner()) && offer < auction.CurrentMaxOffer + site.MinimumBidIncrement)
                {
                    return false;
                }
                
                if (!bidder.Equals(CurrentWinner()) && offer < CurrentPrice())
                {
                    return false;
                }

                if (!bidder.Equals(CurrentWinner()) && CurrentWinner() != null &&
                    offer < CurrentPrice() + site.MinimumBidIncrement)
                {
                    return false;
                }

                if (bidder.Equals(CurrentWinner()) && offer > auction.CurrentMaxOffer)
                {
                    auction.CurrentMaxOffer = offer;
                    return true;
                }
                
                if (CurrentWinner() == null && offer > auction.CurrentMaxOffer)
                {
                    auction.CurrentMaxOffer = offer;
                    auction.CurrentWinnerId = ((User)bidder).Id;
                    c.SaveChanges();
                    return true;
                }
                else
                {
                    if (offer > auction.CurrentMaxOffer)
                    {
                        auction.CurrentPrice = Math.Min(auction.CurrentMaxOffer + site.MinimumBidIncrement, offer);
                        auction.CurrentMaxOffer = offer;
                        auction.CurrentWinnerId = ((User)bidder).Id;


                    }
                    else
                    {
                        auction.CurrentPrice = Math.Min(auction.CurrentMaxOffer, offer + site.MinimumBidIncrement);
                    }

                }
                c.SaveChanges();
                return true;
            }
        }
    }
}
