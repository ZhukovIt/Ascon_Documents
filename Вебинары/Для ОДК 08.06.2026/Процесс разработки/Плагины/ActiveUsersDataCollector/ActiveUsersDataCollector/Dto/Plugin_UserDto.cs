using Ascon.Plm.Common.Mapping;

namespace ActiveUsersDataCollector.Dto
{
    public class Plugin_UserDto
    {
        [Column("_ID")]
        public int ActorId { get; set; }

        [Column("_NAME")]
        public string Login { get; set; }

        [Column("_FULLNAME")]
        public string FullName { get; set; }
    }
}
