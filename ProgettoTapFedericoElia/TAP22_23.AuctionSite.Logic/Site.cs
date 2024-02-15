using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace TAP22_23.AuctionSite.Logic
{
    public class Site : ISite
    {

        public int Id { get; }
        private readonly IAlarmClock alarmClock;
        private readonly string connectionString;
        public double MinimumBidIncrement { get; }

        public string Name { get; }
        public int Timezone { get; }

        public int SessionExpirationInSeconds { get; }
        private IAlarm alarm;


        public Site(IAlarmClock alarmClock, string connectionString, SiteEntity site)
        {
            this.alarmClock = alarmClock;
            this.connectionString = connectionString;
            Name = site.Name;
            Timezone = site.Timezone;
            SessionExpirationInSeconds = site.SessionExpirationTimeInSeconds;
            MinimumBidIncrement = site.MinimumBidIncrement;
            Id = site.SiteEntityId;
            alarm = alarmClock.InstantiateAlarm(5 * 60 * 1000);
            alarm.RingingEvent += deleteExpiredSessions;
        }

        private void deleteExpiredSessions()
        {
            using (var c = new MyDb(connectionString))
            {
                var sessionExpired = c.SessionEntities.Where(s => s.SiteId == Id && s.ValidUntil < Now()).ToList();
                foreach (var s in sessionExpired)
                {
                    c.SessionEntities.Remove(s);
                }
                c.SaveChanges();
            }
            alarm = alarmClock.InstantiateAlarm(5 * 60 * 1000);
        }


        public void CreateUser(string username, string password)
        {
            if (username == null || password == null) throw new AuctionSiteArgumentNullException();
            if (username.Length < DomainConstraints.MinUserName || username.Length > DomainConstraints.MaxUserName ||
                password.Length < DomainConstraints.MinUserPassword) throw new AuctionSiteArgumentException();

            using (var c = new MyDb(connectionString))
            {
                var site = c.SiteEntities.SingleOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site not exist");

                if (c.UserEntities.SingleOrDefault(a => a.Username == username && a.SiteEntityId == Id) == null)
                {
                    c.UserEntities.Add(new UserEntity(username, HashPassword(password), Id));
                }
                else
                {
                    throw new AuctionSiteNameAlreadyInUseException(username, "alredy exist");
                }
                c.SaveChanges();
            }

        }

        public ISession? Login(string username, string password)
        {
            if (username == null || password == null) throw new AuctionSiteArgumentNullException();
            if (username.Length < DomainConstraints.MinUserName || username.Length > DomainConstraints.MaxUserName ||
                password.Length < DomainConstraints.MinUserPassword) throw new AuctionSiteArgumentException();
            using (var c = new MyDb(connectionString))
            {
                var site = c.SiteEntities.SingleOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site not exist");

                var user = c.UserEntities.SingleOrDefault(a => a.Username == username && a.SiteEntityId == Id);
                if (user == null) return null;

                if (CheckPassword(password, user.Password))
                {
                    var session =
                        c.SessionEntities.SingleOrDefault(s => s.UserId == user.UserEntityId && s.ValidUntil > Now());
                    if (session == null)
                    {
                        var newSessionEntity = new SessionEntity(username + Name + Now(),
                            Now().AddSeconds(SessionExpirationInSeconds),
                            user.UserEntityId, Id);
                        c.SessionEntities.Add(newSessionEntity);
                        c.SaveChanges();
                        return new Session(newSessionEntity, new User(user, this, connectionString), this,
                            connectionString);
                    }
                    else
                    {
                        return new Session(session, new User(user, this, connectionString), this, connectionString);
                    }
                }
                else return null;

            }


        }

        public static string HashPassword(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }


        public static bool CheckPassword(string enteredPassword, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, 10000);
            byte[] newHash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != newHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        public DateTime Now()
        {
            return alarmClock.Now;
        }

        public void Delete()
        {
            using (var c = new MyDb(connectionString))
            {
                var site = c.SiteEntities.SingleOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site not exist");

                var users = ToyGetUsers();
                foreach (var user in users)
                {
                    user.Delete();
                }

                c.Remove(site);
                c.SaveChanges();
            }
        }

        public IEnumerable<IUser> ToyGetUsers()
        {
            List<UserEntity>? users;
            var site = new MyDb(connectionString).SiteEntities.FirstOrDefault(s => s.SiteEntityId == Id);
            if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
            var allUsers = new MyDb(connectionString).UserEntities.Where(u => u.SiteEntityId == Id).ToList();
            return ToyGetUsersNoEx(allUsers);
        }
        IEnumerable<IUser> ToyGetUsersNoEx(List<UserEntity> allUsers)
        {
            foreach (var user in allUsers)
            {
                yield return new User(user, this, connectionString);
            }
        }


        public IEnumerable<ISession> ToyGetSessions()
        {
            List<SessionEntity> allUSession;
            using (var c = new MyDb(connectionString))
            {
                var site = c.SiteEntities.SingleOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
                allUSession = c.SessionEntities.Include(s => s.UserEntity).Where(u => u.SiteId == Id && u.ValidUntil > Now()).ToList();
 
            }
            foreach (var session in allUSession)
            {

                yield return new Session(session, new User(session.UserEntity, this, connectionString), this,
                    connectionString);
            }
        }

        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded)
        {
            List<AuctionEntity> allAuctions;
            using (var c = new MyDb(connectionString))
            {
                var site = c.SiteEntities.SingleOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
                if (onlyNotEnded)
                {
                     allAuctions = c.AuctionEntities.Include(a => a.Owner).Where(a => a.SiteEntityId == Id && a.EndsOn > Now()).ToList();
                }
                else
                {
                    allAuctions = c.AuctionEntities.Include(a => a.Owner).Where(a => a.SiteEntityId == Id).ToList();
                }
            }
            return ToyGetAuctionsNoEx(allAuctions);
        }
        IEnumerable<IAuction> ToyGetAuctionsNoEx(List<AuctionEntity> allAuctions)
        {
            foreach (var auction in allAuctions)
            {
                yield return new Auction(auction, this, connectionString);
            }
        }
    }
}
