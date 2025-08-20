using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services
{
    /// <summary>
    /// Advanced gaming and virtual world service for blockchain gaming
    /// Supports NFT assets, tournaments, achievements, and virtual economies
    /// </summary>
    [DisplayName("GameContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Advanced gaming and virtual world service")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class GameContract : SmartContract, IServiceContract
    {
        #region Constants
        private const string SERVICE_NAME = "Game";
        private const byte GAME_PREFIX = 0x47; // 'G'
        private const byte PLAYERS_PREFIX = 0x50;
        private const byte ASSETS_PREFIX = 0x41;
        private const byte TOURNAMENTS_PREFIX = 0x54;
        private const byte ACHIEVEMENTS_PREFIX = 0x41;
        private const byte GUILDS_PREFIX = 0x47;
        #endregion

        #region Events
        [DisplayName("PlayerRegistered")]
        public static event Action<string, UInt160, string, BigInteger> OnPlayerRegistered;

        [DisplayName("GameAssetMinted")]
        public static event Action<string, UInt160, byte, string> OnGameAssetMinted;

        [DisplayName("TournamentCreated")]
        public static event Action<string, UInt160, BigInteger, BigInteger> OnTournamentCreated;

        [DisplayName("AchievementUnlocked")]
        public static event Action<string, string, UInt160, BigInteger> OnAchievementUnlocked;

        [DisplayName("GuildCreated")]
        public static event Action<string, UInt160, string, BigInteger> OnGuildCreated;

        [DisplayName("GameError")]
        public static event Action<string, string> OnGameError;
        #endregion

        #region Data Structures
        public enum AssetType : byte
        {
            Character = 0,
            Weapon = 1,
            Armor = 2,
            Consumable = 3,
            Land = 4,
            Building = 5,
            Vehicle = 6,
            Pet = 7
        }

        public enum AssetRarity : byte
        {
            Common = 0,
            Uncommon = 1,
            Rare = 2,
            Epic = 3,
            Legendary = 4,
            Mythic = 5
        }

        public enum TournamentStatus : byte
        {
            Registration = 0,
            Active = 1,
            Completed = 2,
            Cancelled = 3,
            Paused = 4
        }

        public enum AchievementType : byte
        {
            Combat = 0,
            Exploration = 1,
            Social = 2,
            Economic = 3,
            Collection = 4,
            Skill = 5,
            Special = 6
        }

        public class Player
        {
            public string Id;
            public UInt160 Owner;
            public string Username;
            public BigInteger Level;
            public BigInteger Experience;
            public BigInteger PlayTime;
            public BigInteger RegisteredAt;
            public string[] OwnedAssets;
            public string[] Achievements;
            public string GuildId;
            public BigInteger Reputation;
            public bool IsActive;
            public string Metadata;
        }

        public class GameAsset
        {
            public string Id;
            public UInt160 Owner;
            public string Name;
            public AssetType Type;
            public AssetRarity Rarity;
            public BigInteger Level;
            public BigInteger[] Stats;
            public string[] Attributes;
            public BigInteger CreatedAt;
            public BigInteger LastUsed;
            public bool IsEquipped;
            public bool IsTradeable;
            public BigInteger MarketValue;
            public string Metadata;
        }

        public class Tournament
        {
            public string Id;
            public UInt160 Organizer;
            public string Name;
            public string GameMode;
            public BigInteger EntryFee;
            public BigInteger PrizePool;
            public BigInteger MaxParticipants;
            public BigInteger CurrentParticipants;
            public BigInteger StartTime;
            public BigInteger EndTime;
            public TournamentStatus Status;
            public string[] Participants;
            public string[] Winners;
            public string Rules;
        }

        public class Achievement
        {
            public string Id;
            public string Name;
            public string Description;
            public AchievementType Type;
            public BigInteger Points;
            public string[] Requirements;
            public BigInteger Difficulty;
            public bool IsSecret;
            public string Reward;
            public BigInteger UnlockedBy;
            public string Icon;
        }

        public class Guild
        {
            public string Id;
            public UInt160 Leader;
            public string Name;
            public string Description;
            public BigInteger CreatedAt;
            public BigInteger MaxMembers;
            public BigInteger CurrentMembers;
            public string[] Members;
            public BigInteger Level;
            public BigInteger Experience;
            public string[] Achievements;
            public bool IsPublic;
            public BigInteger Treasury;
        }

        public class GameSession
        {
            public string Id;
            public string PlayerId;
            public BigInteger StartTime;
            public BigInteger EndTime;
            public BigInteger Score;
            public BigInteger ExperienceGained;
            public string[] AssetsUsed;
            public string[] AchievementsUnlocked;
            public bool IsCompleted;
            public string GameMode;
        }
        #endregion

        #region Storage Keys
        private static StorageKey PlayerKey(string id) => new byte[] { PLAYERS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey AssetKey(string id) => new byte[] { ASSETS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey TournamentKey(string id) => new byte[] { TOURNAMENTS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey AchievementKey(string id) => new byte[] { ACHIEVEMENTS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey GuildKey(string id) => new byte[] { GUILDS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        #endregion

        #region IServiceContract Implementation
        public static string GetServiceName() => SERVICE_NAME;

        public static string GetServiceVersion() => "1.0.0";

        public static string[] GetServiceMethods() => new string[]
        {
            "RegisterPlayer",
            "MintGameAsset",
            "CreateTournament",
            "JoinTournament",
            "UnlockAchievement",
            "CreateGuild",
            "JoinGuild",
            "TradeAsset",
            "GetPlayerStats"
        };

        public static bool IsServiceActive() => true;

        protected static void ValidateAccess()
        {
            if (!Runtime.CheckWitness(Runtime.CallingScriptHash))
                throw new InvalidOperationException("Unauthorized access");
        }

        public static object ExecuteServiceOperation(string method, object[] args)
        {
            return ExecuteServiceOperation<object>(method, args);
        }

        protected static T ExecuteServiceOperation<T>(string method, object[] args)
        {
            ValidateAccess();
            
            switch (method)
            {
                case "RegisterPlayer":
                    return (T)(object)RegisterPlayer((string)args[0]);
                case "MintGameAsset":
                    return (T)(object)MintGameAsset((string)args[0], (byte)args[1], (byte)args[2], (BigInteger[])args[3], (string[])args[4], (bool)args[5]);
                case "CreateTournament":
                    return (T)(object)CreateTournament((string)args[0], (string)args[1], (BigInteger)args[2], (BigInteger)args[3], (BigInteger)args[4], (BigInteger)args[5], (string)args[6]);
                case "JoinTournament":
                    return (T)(object)JoinTournament((string)args[0], (string)args[1]);
                case "UnlockAchievement":
                    return (T)(object)UnlockAchievement((string)args[0], (string)args[1]);
                case "CreateGuild":
                    return (T)(object)CreateGuild((string)args[0], (string)args[1], (BigInteger)args[2], (bool)args[3]);
                case "JoinGuild":
                    return (T)(object)JoinGuild((string)args[0], (string)args[1]);
                case "TradeAsset":
                    return (T)(object)TradeAsset((string)args[0], (UInt160)args[1], (BigInteger)args[2]);
                case "GetPlayerStats":
                    return (T)(object)GetPlayerStats((string)args[0]);
                default:
                    throw new InvalidOperationException($"Unknown method: {method}");
            }
        }
        #endregion

        #region Player Management
        /// <summary>
        /// Register a new player
        /// </summary>
        public static string RegisterPlayer(string username)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Username required");

            try
            {
                var playerId = GenerateId("PLY");
                var player = new Player
                {
                    Id = playerId,
                    Owner = Runtime.CallingScriptHash,
                    Username = username,
                    Level = 1,
                    Experience = 0,
                    PlayTime = 0,
                    RegisteredAt = Runtime.Time,
                    OwnedAssets = new string[0],
                    Achievements = new string[0],
                    GuildId = "",
                    Reputation = 100,
                    IsActive = true,
                    Metadata = ""
                };

                Storage.Put(Storage.CurrentContext, PlayerKey(playerId), StdLib.Serialize(player));
                OnPlayerRegistered(playerId, Runtime.CallingScriptHash, username, Runtime.Time);

                return playerId;
            }
            catch (Exception ex)
            {
                OnGameError("RegisterPlayer", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get player statistics
        /// </summary>
        public static Player GetPlayerStats(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentException("Player ID required");

            var data = Storage.Get(Storage.CurrentContext, PlayerKey(playerId));
            if (data == null) return null;

            return (Player)StdLib.Deserialize(data);
        }
        #endregion

        #region Asset Management
        /// <summary>
        /// Mint a new game asset (NFT)
        /// </summary>
        public static string MintGameAsset(string name, byte assetType, byte rarity, BigInteger[] stats, string[] attributes, bool isTradeable)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Asset name required");
            if (!Enum.IsDefined(typeof(AssetType), assetType)) throw new ArgumentException("Invalid asset type");
            if (!Enum.IsDefined(typeof(AssetRarity), rarity)) throw new ArgumentException("Invalid rarity");

            try
            {
                var assetId = GenerateId("AST");
                var asset = new GameAsset
                {
                    Id = assetId,
                    Owner = Runtime.CallingScriptHash,
                    Name = name,
                    Type = (AssetType)assetType,
                    Rarity = (AssetRarity)rarity,
                    Level = 1,
                    Stats = stats ?? new BigInteger[0],
                    Attributes = attributes ?? new string[0],
                    CreatedAt = Runtime.Time,
                    LastUsed = 0,
                    IsEquipped = false,
                    IsTradeable = isTradeable,
                    MarketValue = CalculateAssetValue((AssetRarity)rarity, stats),
                    Metadata = ""
                };

                Storage.Put(Storage.CurrentContext, AssetKey(assetId), StdLib.Serialize(asset));
                OnGameAssetMinted(assetId, Runtime.CallingScriptHash, assetType, name);

                return assetId;
            }
            catch (Exception ex)
            {
                OnGameError("MintGameAsset", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Trade a game asset
        /// </summary>
        public static bool TradeAsset(string assetId, UInt160 newOwner, BigInteger price)
        {
            if (string.IsNullOrEmpty(assetId)) throw new ArgumentException("Asset ID required");
            if (newOwner == UInt160.Zero) throw new ArgumentException("New owner required");

            var assetData = Storage.Get(Storage.CurrentContext, AssetKey(assetId));
            if (assetData == null) throw new InvalidOperationException("Asset not found");

            var asset = (GameAsset)StdLib.Deserialize(assetData);
            if (asset.Owner != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not asset owner");
            if (!asset.IsTradeable) throw new InvalidOperationException("Asset not tradeable");

            try
            {
                asset.Owner = newOwner;
                asset.MarketValue = price;
                Storage.Put(Storage.CurrentContext, AssetKey(assetId), StdLib.Serialize(asset));

                return true;
            }
            catch (Exception ex)
            {
                OnGameError("TradeAsset", ex.Message);
                return false;
            }
        }
        #endregion

        #region Tournament Management
        /// <summary>
        /// Create a new tournament
        /// </summary>
        public static string CreateTournament(string name, string gameMode, BigInteger entryFee, BigInteger prizePool, BigInteger maxParticipants, BigInteger startTime, string rules)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Tournament name required");
            if (maxParticipants <= 0) throw new ArgumentException("Max participants must be positive");
            if (startTime <= Runtime.Time) throw new ArgumentException("Start time must be in the future");

            try
            {
                var tournamentId = GenerateId("TRN");
                var tournament = new Tournament
                {
                    Id = tournamentId,
                    Organizer = Runtime.CallingScriptHash,
                    Name = name,
                    GameMode = gameMode ?? "",
                    EntryFee = entryFee,
                    PrizePool = prizePool,
                    MaxParticipants = maxParticipants,
                    CurrentParticipants = 0,
                    StartTime = startTime,
                    EndTime = startTime + 86400, // Default 24 hours
                    Status = TournamentStatus.Registration,
                    Participants = new string[0],
                    Winners = new string[0],
                    Rules = rules ?? ""
                };

                Storage.Put(Storage.CurrentContext, TournamentKey(tournamentId), StdLib.Serialize(tournament));
                OnTournamentCreated(tournamentId, Runtime.CallingScriptHash, prizePool, startTime);

                return tournamentId;
            }
            catch (Exception ex)
            {
                OnGameError("CreateTournament", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Join a tournament
        /// </summary>
        public static bool JoinTournament(string tournamentId, string playerId)
        {
            if (string.IsNullOrEmpty(tournamentId)) throw new ArgumentException("Tournament ID required");
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentException("Player ID required");

            var tournamentData = Storage.Get(Storage.CurrentContext, TournamentKey(tournamentId));
            if (tournamentData == null) throw new InvalidOperationException("Tournament not found");

            var tournament = (Tournament)StdLib.Deserialize(tournamentData);
            if (tournament.Status != TournamentStatus.Registration) throw new InvalidOperationException("Registration closed");
            if (tournament.CurrentParticipants >= tournament.MaxParticipants) throw new InvalidOperationException("Tournament full");

            try
            {
                tournament.Participants = AddToArray(tournament.Participants, playerId);
                tournament.CurrentParticipants += 1;

                Storage.Put(Storage.CurrentContext, TournamentKey(tournamentId), StdLib.Serialize(tournament));
                return true;
            }
            catch (Exception ex)
            {
                OnGameError("JoinTournament", ex.Message);
                return false;
            }
        }
        #endregion

        #region Achievement System
        /// <summary>
        /// Unlock an achievement for a player
        /// </summary>
        public static bool UnlockAchievement(string playerId, string achievementId)
        {
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentException("Player ID required");
            if (string.IsNullOrEmpty(achievementId)) throw new ArgumentException("Achievement ID required");

            var playerData = Storage.Get(Storage.CurrentContext, PlayerKey(playerId));
            var achievementData = Storage.Get(Storage.CurrentContext, AchievementKey(achievementId));
            
            if (playerData == null) throw new InvalidOperationException("Player not found");
            if (achievementData == null) throw new InvalidOperationException("Achievement not found");

            var player = (Player)StdLib.Deserialize(playerData);
            var achievement = (Achievement)StdLib.Deserialize(achievementData);

            // Check if already unlocked
            if (HasAchievement(player.Achievements, achievementId)) return false;

            try
            {
                player.Achievements = AddToArray(player.Achievements, achievementId);
                player.Experience += achievement.Points;
                
                // Level up check
                var newLevel = CalculateLevel(player.Experience);
                if (newLevel > player.Level)
                {
                    player.Level = newLevel;
                }

                Storage.Put(Storage.CurrentContext, PlayerKey(playerId), StdLib.Serialize(player));
                OnAchievementUnlocked(achievementId, playerId, Runtime.CallingScriptHash, achievement.Points);

                return true;
            }
            catch (Exception ex)
            {
                OnGameError("UnlockAchievement", ex.Message);
                return false;
            }
        }
        #endregion

        #region Guild Management
        /// <summary>
        /// Create a new guild
        /// </summary>
        public static string CreateGuild(string name, string description, BigInteger maxMembers, bool isPublic)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Guild name required");
            if (maxMembers <= 0) throw new ArgumentException("Max members must be positive");

            try
            {
                var guildId = GenerateId("GLD");
                var guild = new Guild
                {
                    Id = guildId,
                    Leader = Runtime.CallingScriptHash,
                    Name = name,
                    Description = description ?? "",
                    CreatedAt = Runtime.Time,
                    MaxMembers = maxMembers,
                    CurrentMembers = 1,
                    Members = new string[] { Runtime.CallingScriptHash.ToString() },
                    Level = 1,
                    Experience = 0,
                    Achievements = new string[0],
                    IsPublic = isPublic,
                    Treasury = 0
                };

                Storage.Put(Storage.CurrentContext, GuildKey(guildId), StdLib.Serialize(guild));
                OnGuildCreated(guildId, Runtime.CallingScriptHash, name, Runtime.Time);

                return guildId;
            }
            catch (Exception ex)
            {
                OnGameError("CreateGuild", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Join a guild
        /// </summary>
        public static bool JoinGuild(string guildId, string playerId)
        {
            if (string.IsNullOrEmpty(guildId)) throw new ArgumentException("Guild ID required");
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentException("Player ID required");

            var guildData = Storage.Get(Storage.CurrentContext, GuildKey(guildId));
            if (guildData == null) throw new InvalidOperationException("Guild not found");

            var guild = (Guild)StdLib.Deserialize(guildData);
            if (guild.CurrentMembers >= guild.MaxMembers) throw new InvalidOperationException("Guild full");

            try
            {
                guild.Members = AddToArray(guild.Members, playerId);
                guild.CurrentMembers += 1;

                Storage.Put(Storage.CurrentContext, GuildKey(guildId), StdLib.Serialize(guild));
                return true;
            }
            catch (Exception ex)
            {
                OnGameError("JoinGuild", ex.Message);
                return false;
            }
        }
        #endregion

        #region Utility Methods
        private static string GenerateId(string prefix)
        {
            var timestamp = Runtime.Time;
            var random = Runtime.GetRandom();
            return $"{prefix}_{timestamp}_{random}";
        }

        private static BigInteger CalculateAssetValue(AssetRarity rarity, BigInteger[] stats)
        {
            BigInteger baseValue = 1000;
            BigInteger rarityMultiplier = (BigInteger)rarity + 1;
            BigInteger statsBonus = 0;

            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    statsBonus += stat;
                }
            }

            return baseValue * rarityMultiplier + statsBonus * 10;
        }

        private static BigInteger CalculateLevel(BigInteger experience)
        {
            // Simple level calculation: level = sqrt(experience / 100)
            return (BigInteger)Math.Sqrt((double)(experience / 100)) + 1;
        }

        private static bool HasAchievement(string[] achievements, string achievementId)
        {
            if (achievements == null) return false;
            
            foreach (var achievement in achievements)
            {
                if (achievement == achievementId) return true;
            }
            return false;
        }

        private static string[] AddToArray(string[] array, string item)
        {
            var newArray = new string[array.Length + 1];
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }
            newArray[array.Length] = item;
            return newArray;
        }
        #endregion

        #region Administrative Methods
        /// <summary>
        /// Get gaming service statistics
        /// </summary>
        public static Map<string, BigInteger> GetGameStats()
        {
            var stats = new Map<string, BigInteger>();
            stats["total_players"] = GetTotalPlayers();
            stats["total_assets"] = GetTotalAssets();
            stats["total_tournaments"] = GetTotalTournaments();
            stats["total_achievements"] = GetTotalAchievements();
            stats["total_guilds"] = GetTotalGuilds();
            return stats;
        }

        private static BigInteger GetTotalPlayers()
        {
            return Storage.Get(Storage.CurrentContext, "total_players")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalAssets()
        {
            return Storage.Get(Storage.CurrentContext, "total_assets")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalTournaments()
        {
            return Storage.Get(Storage.CurrentContext, "total_tournaments")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalAchievements()
        {
            return Storage.Get(Storage.CurrentContext, "total_achievements")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalGuilds()
        {
            return Storage.Get(Storage.CurrentContext, "total_guilds")?.ToBigInteger() ?? 0;
        }
        #endregion
    }
}