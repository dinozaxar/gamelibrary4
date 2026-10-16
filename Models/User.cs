using System;
using System.Collections.Generic;

namespace gamelibrary4.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Login { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<string> FavoriteGames { get; set; } = new List<string>();
        public string AvatarPath { get; set; } = "";
    }
}