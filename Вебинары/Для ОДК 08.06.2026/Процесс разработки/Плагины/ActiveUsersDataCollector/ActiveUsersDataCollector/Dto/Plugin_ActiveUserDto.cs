using Ascon.Plm.Common.Mapping;
using System;

namespace ActiveUsersDataCollector.Dto
{
    public class Plugin_ActiveUserDto
    {
        [Column("_ID_USER")]
        public int Id { get; set; }

        [Column("_NAME")]
        public string Login { get; set; }

        [Column("_HOST")]
        public string Host { get; set; }

        [Column("_COMP_NAME")]
        public string ComputerName { get; set; }

        [Column("_LAST_LOGIN_DATE")]
        public DateTime LastLoginDateTime { get; set; }
    }
}
