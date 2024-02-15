using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using TAP22_23.AuctionSite.Interface;



namespace TAP22_23
{
    public class MyDb : TapDbContext
    {
        public MyDb(string connectionString) : base(new DbContextOptionsBuilder<MyDb>().UseSqlServer(connectionString).Options) { }


        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (SqlException e)
            {
                throw new AuctionSiteUnavailableDbException("Db not work", e);
            }
            catch (DbUpdateException e)
            {
                var sqlException = e.InnerException as SqlException;

                if(sqlException.Number == 2601 || sqlException.Number == 2627)
                {
                    throw new AuctionSiteNameAlreadyInUseException(null, $"{sqlException.Message}", e);
                }
                throw new AuctionSiteInvalidOperationException($"{sqlException.Message}", e);

            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //site
            var site = modelBuilder.Entity<SiteEntity>();
            site.HasIndex(u => u.Name).IsUnique();
            site.Property(b => b.Name).HasMaxLength(DomainConstraints.MaxSiteName);

            site.HasMany(site => site.RegisteredUser)
                .WithOne(user => user.SiteEntity)
                .HasForeignKey(user => user.SiteEntityId)
                .OnDelete(DeleteBehavior.ClientCascade);

            //user 
            var user = modelBuilder.Entity<UserEntity>();
            user.Property(b => b.Username).HasMaxLength(DomainConstraints.MaxUserName);
            user.HasIndex(p => new { p.Username, p.SiteEntityId }).IsUnique();
            user.HasMany(user => user.Sessions)
                .WithOne(session => session.UserEntity)
                .HasForeignKey(session => session.UserId);

            user.HasMany(user => user.OwnAuctions)
                .WithOne(auction => auction.Owner!)
                .HasForeignKey(auction => auction.OwnerId)
                .OnDelete(DeleteBehavior.ClientCascade);

            user.HasMany(user => user.AuctionsBid)
                .WithOne(auction => auction.CurrentWinner)
                .HasForeignKey(auction => auction.CurrentWinnerId)
                .OnDelete(DeleteBehavior.SetNull); 


            //auction
            var auction = modelBuilder.Entity<AuctionEntity>();
            auction.HasOne(auction => auction.SiteEntity)
                .WithMany(site => site.Auctions)
                .HasForeignKey(auction => auction.SiteEntityId)
                .OnDelete(DeleteBehavior.NoAction);


            //session
            modelBuilder.Entity<SessionEntity>().HasOne(session => session.SiteEntity)
                .WithMany(site => site.Sessions)
                .HasForeignKey(site => site.SiteId)
                .OnDelete(DeleteBehavior.NoAction);

        }

        public DbSet<UserEntity> UserEntities { get; set; }
        public DbSet<SiteEntity> SiteEntities { get; set; }
        public DbSet<AuctionEntity> AuctionEntities { get; set; }
        public DbSet<SessionEntity> SessionEntities { get; set; }
    }

    public class UserEntity
    {
        public UserEntity(string username, string password, int siteEntityId)
        {
            Username = username;
            Password = password;
            SiteEntityId = siteEntityId;
            AuctionsBid = new List<AuctionEntity>();
            OwnAuctions = new List<AuctionEntity>();
            Sessions = new List<SessionEntity>();
        }

        public int UserEntityId { get; set; }

        [MinLength(DomainConstraints.MinUserName)]
        public string Username { get; set; }

        [MinLength(DomainConstraints.MinUserPassword)]
        public string Password { get; set; }
        public ICollection<SessionEntity> Sessions { get; set; }//NOT SURE
        public ICollection<AuctionEntity> OwnAuctions { get; set; }
        public ICollection<AuctionEntity> AuctionsBid { get; set; }
      
        public SiteEntity? SiteEntity { get; set; }
        public int SiteEntityId { get; set; }
    }

    public class SiteEntity
    {
        public SiteEntity(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement)
        {
            Name = name;
            Timezone = timezone;
            SessionExpirationTimeInSeconds = sessionExpirationTimeInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
            RegisteredUser = new List<UserEntity>();
            Auctions = new List<AuctionEntity>();
            Sessions = new List<SessionEntity>();
        }

        public int SiteEntityId { get; set; }

        [MinLength(DomainConstraints.MinSiteName)]
        public string Name { get; set; }

        [Range(DomainConstraints.MinTimeZone, DomainConstraints.MaxTimeZone)]
        public int Timezone  { get; set; }

        [Range(1, int.MaxValue)]
        public int SessionExpirationTimeInSeconds { get; set; }
        
        [Range(0.0, double.MaxValue)]
        public double MinimumBidIncrement { get; set; }


        public ICollection<UserEntity> RegisteredUser { get; set; }
        public ICollection<AuctionEntity> Auctions { get; set; }
        public ICollection<SessionEntity> Sessions { get; set; }

    }

    public class AuctionEntity
    {
        public AuctionEntity() { }

        public AuctionEntity(string description, DateTime endsOn,double startingPrice, int ownerId, int siteId)
        {
            Description = description;
            EndsOn = endsOn;
            StartingPrice = startingPrice;
            CurrentMaxOffer = startingPrice;
            CurrentPrice = startingPrice;
            OwnerId = ownerId;
            SiteEntityId = siteId;
        }


        public int AuctionEntityId { get; set; }
        public string Description { get; set; }
        public DateTime EndsOn { get; set; }
        public double StartingPrice { get; set; }
        public double CurrentPrice { get; set; }
        public double CurrentMaxOffer { get; set; }

        public UserEntity? CurrentWinner { get; set; }
        public int? CurrentWinnerId { get; set; }

        public UserEntity? Owner { get; set; }
        public int OwnerId { get; set; }

        public SiteEntity? SiteEntity { get; set; }
        public int SiteEntityId { get; set; }

  
    }

    public class SessionEntity
    {
        public SessionEntity(string sessionEntityId, DateTime validUntil, int userId, int siteId)
        {
            SessionEntityId = sessionEntityId;
            ValidUntil = validUntil;
            UserId = userId;
            SiteId = siteId;
        }


        public string SessionEntityId { get; set; }
        public DateTime ValidUntil { get; set; }


        public UserEntity? UserEntity { get; set; }
        public int UserId { get; set; }


        public SiteEntity? SiteEntity { get; set; }
        public int SiteId { get; set; }
    }
}