using System;

namespace ActiveUsersDataCollector.Dto
{
    public class Plugin_GetActiveUsersOutputDto
    {
        public int? Id { get; set; }

        public int ActorId { get; set; }

        public string Login { get; set; }

        public string FullName { get; set; }

        public string Host { get; set; }

        public string ComputerName { get; set; }

        public DateTime? LastLoginDateTime { get; set; }
    }
}
