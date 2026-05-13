namespace StructureComposition
{
    /// <summary>
    /// Сервис для генерации отчетов о структуре изделия
    /// </summary>
    public class ReportService
    {
        private readonly LoodsmanApiClient _apiClient;

        public ReportService(LoodsmanApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<List<Report>> GenerateReportAsync()
        {
            //Структура отчёта
            var report = new List<Report>();

            var currentUser = await _apiClient.GetCurrentUserAsync();

            var table = await _apiClient.GetUserAddressBookAsync((int)currentUser.id);

            //список всех подразделений, в которых текущий пользователь назначен на должность
            var UnitList = table
                .Where(x => (int)x.Type == 2 || (int)x.Type == 3)
                .Select(x => new Unit()
                {
                    UnitId = x.Id,
                    UnitName = x.Name
                }
                )
                .ToList();

            //дополнительно получим все должности в системе
            var posts = await _apiClient.GetAllPostsAsync();

            //Выстраиваем список по каждому подразделению
            foreach (var unit in UnitList)
            {
                var unitUsers = await _apiClient.GetInfoAboutUsersAsync(unit.UnitId);

                //Проходимся по каждому пользователю в конкретном подразделении
                foreach (var user in unitUsers)
                {
                    var postName = posts.FirstOrDefault(x => x.Id == user.PostId)?.Name;

                    var rolesFromPost = await _apiClient.GetPostUserRoles(user.PostId);

                    //проходимся по каждой роли для текущей должности пользователя в данном подразделении.
                    foreach (var role in rolesFromPost)
                    {
                        report.Add(
                            new Report()
                            {
                                UnitName = unit.UnitName,
                                PostName = string.IsNullOrWhiteSpace(postName) ? null : postName,
                                RoleName = role.Name,
                                UserFullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName,
                                UserLogin = user.Login,

                            });
                    }
                }
            }
            return report;
        }
    }
}
