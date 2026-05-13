using System.ComponentModel.DataAnnotations.Schema;

namespace StructureComposition
{
    public class GetAddressBook
    {
        /// <summary>
        /// Идентификатор сущности.
        /// </summary>
        [Column("_ID")]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор родительской сущности.
        /// </summary>
        [Column("_PARENT")]
        public int Parent { get; set; }

        /// <summary>
        /// Название сущности.
        /// </summary>
        [Column("_NAME")]
        public string Name { get; set; }

        /// <summary>
        /// Тип сущности организационной структуры.
        /// </summary>
        [Column("_TYPE")]
        public OrgStructureEntityTypes Type { get; set; }

        /// <summary>
        /// Логин пользователя.
        /// </summary>
        [Column("_USERNAME")]
        public string UserName { get; set; }

        /// <summary>
        /// Полное имя пользователя.
        /// </summary>
        [Column("_FULLNAME")]
        public string FullName { get; set; }

        /// <summary>
        /// Адрес электронной почты пользователя.
        /// </summary>
        [Column("_EMAIL")]
        public string Email { get; set; }

        /// <summary>
        /// Является ли пользователь администратором текущей базы данных?<br></br>
        /// 0 - Пользователь не является администратором текущей базы данных;<br></br>
        /// 1 - Пользователь является администратором текущей базы данных.
        /// </summary>
        [Column("_ISADMIN")]
        public int IsAdmin { get; set; }

        /// <summary>
        /// Статус пользователя.
        /// </summary>
        [Column("_STATUS")]
        public UserStatuses Status { get; set; }

        /// <summary>
        /// Имеет ли сущность в своём составе дочерние сущности?<br></br>
        /// 0 - Сущность не имеет в своём составе дочерние сущности;<br></br>
        /// 1 - Сущность имеет в своём составе дочерние сущности.
        /// </summary>
        [Column("_HAS_CHILD")]
        public int HasChild { get; set; }

        /// <summary>
        /// Является ли должность руководящей?<br></br>
        /// 0 - Должность не является руководящей;<br></br>
        /// 1 - Должность является руководящей.
        /// </summary>
        [Column("_ISCHIEF")]
        public short IsChief { get; set; }

        /// <summary>
        /// Назначен ли сущности указанный характер работ?<br></br>
        /// 0 - Указанный характер работ не назначен сущности;<br></br>
        /// 1 - Указанный характер работ назначен сущности.
        /// </summary>
        [Column("_HAS_SIGN_ROLE")]
        public int HasSignRole { get; set; }

        /// <summary>
        /// Код должности.
        /// </summary>
        [Column("_CODE")]
        public string Code { get; set; }
    }

    public class CurrentUser
    {
        [Column("_ID")]
        public int? id { set; get; }

        [Column("_NAME")]
        public string username { set; get; }

        [Column("_FULLNAME")]
        public string fullname { set; get; }

        [Column("_PROFILE")]
        public string profile { set; get; }

        [Column("_ID_PROFILE")]
        public int idProfile { set; get; }

        [Column("_USERDIR")]
        public string UserDir { get; set; }

        [Column("_FILEDIR")]
        public string FileDir { get; set; }
    }


    /// <summary>
    /// Описывает свойства пользователя для указанного подразделения.
    /// </summary>
    public class UserFromUnit
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин пользователя.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Полное имя пользователя.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Замещает ли пользователь с назначенной должностью другую должность:
        /// <br/>false - Роль принадлежит указанному пользователю напрямую;
        /// <br/>true - Роль была получена для пользователя через его должность-заместителя.
        /// </summary>
        public bool IsDeputyPost { get; set; }

        /// <summary>
        /// Идентификатор должности пользователя.
        /// </summary>
        public int PostId { get; set; }
    }

    public class Report
    {
        /// <summary>
        /// Подразделение
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// Должность пользователя
        /// </summary>
        public string? PostName { get; set; }

        /// <summary>
        /// Роль, в которой выступает пользователь
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// Пользователь
        /// </summary>
        public string UserLogin { get; set; }

        /// <summary>
        /// Полное имя пользователя
        /// </summary>
        public string? UserFullName { get; set; }
    }

    /// <summary>
    /// Описывает свойства роли для указанной должности.
    /// </summary>
    public sealed class RoleFromPost
    {
        /// <summary>
        /// Идентификатор роли.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название роли.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Является ли роль администраторской?<br></br>
        /// false - Роль является обычной;<br></br>
        /// true - Роль является администраторской.
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Является ли роль системной?<br></br>
        /// false - Роль не является системной;<br></br>
        /// true - Роль является системной.
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// Роль была получена по замещению?<br></br>
        /// false - Роль принадлежит указанной должности напрямую;<br></br>
        /// true - Роль была получена для должности через её должность-заместителя.
        /// </summary>
        public bool IsDeputyRole { get; set; }
    }

    public class Unit
    {
        /// <summary>
        /// Id подразделения
        /// </summary>
        public int UnitId { get; set; }

        /// <summary>
        /// Полное имя подразделения
        /// </summary>
        public string UnitName { get; set; }
    }

    /// <summary>
    /// Описывает свойства должности.
    /// </summary>
    public sealed class Post
    {
        /// <summary>
        /// Идентификатор должности.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название должности.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Идентификатор родительского подразделения.
        /// </summary>
        public int UnitId { get; set; }

        /// <summary>
        /// Код должности.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Описание должности.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Является ли должность руководящей?<br></br>
        /// false - Должность не является руководящей;<br></br>
        /// true - Должность является руководящей.
        /// </summary>
        public bool IsManager { get; set; }

        /// <summary>
        /// Полный путь к должности в организационной структуре.
        /// </summary>
        public string FullPathToPost { get; set; }

        /// <summary>
        /// Можно ли для должности назначать должности-заместители?<br></br>
        /// false - Для должности нельзя назначать должности-заместители;<br></br>
        /// true - Для должности можно назначать должности-заместители.
        /// </summary>
        public bool CanHaveDeputies { get; set; }

        /// <summary>
        /// Уникальный глобальный идентификатор должности в системе синхронизации с ОСА.
        /// </summary>
        public Guid? SSOGuid { get; set; }
    }

    /// <summary>
    /// Тип сущности организационной структуры.
    /// </summary>
    public enum OrgStructureEntityTypes
    {
        /// <summary>
        /// Пользователь.
        /// </summary>
        User = 0,

        /// <summary>
        /// Должность.
        /// </summary>
        Post = 1,

        /// <summary>
        /// Подразделение.
        /// </summary>
        Unit = 2,

        /// <summary>
        /// Головное подразделение.
        /// </summary>
        HeadUnit = 3
    }

    /// <summary>
    /// Статусы пользователя.
    /// </summary>
    public enum UserStatuses
    {
        /// <summary>
        /// Доступен.
        /// </summary>
        Available = 0,

        /// <summary>
        /// Не доступен.
        /// </summary>
        NotAvailable = 1,

        /// <summary>
        /// Уволен.
        /// </summary>
        Dismissed = 2
    }
}
