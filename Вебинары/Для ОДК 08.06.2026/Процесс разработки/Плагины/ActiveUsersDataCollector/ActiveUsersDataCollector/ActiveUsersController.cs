using ActiveUsersDataCollector.Dto;
using Ascon.Plm.AppServer.Contracts;
using Ascon.Plm.AppServer.Contracts.WorkFlow;
using Ascon.Plm.AppServer.WebApi.Controllers;
using Ascon.Plm.AppServer.WebApi.Extensions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActiveUsersDataCollector
{
    [ApiVersion("4.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ActiveUsersController : ApiController
    {
        private readonly IOrgStructureContract _orgStructureContract;
        private readonly IDbAdministratorContract _dbAdministratorContract;

        public ActiveUsersController(IOrgStructureContract orgStructureContract, IDbAdministratorContract dbAdministratorContract)
        {
            _orgStructureContract = orgStructureContract;
            _dbAdministratorContract = dbAdministratorContract;
        }

        [HttpGet]
        public IActionResult GetActiveUsers()
        {
            var users = _orgStructureContract
                .GetUserList()
                .ReadToEnumerable<Plugin_UserDto>()
                .ToList();

            var activeUsers = new Dictionary<string, Plugin_ActiveUserDto>();
            var activity = _dbAdministratorContract.GetActivity().ReadToEnumerable<Plugin_ActiveUserDto>();
            foreach (var activeUser in activity)
            {
                if (!activeUsers.ContainsKey(activeUser.Login))
                {
                    activeUsers.Add(activeUser.Login, activeUser);
                }
                else
                {
                    DateTime currentUserLastLoginDateTime = activeUsers[activeUser.Login].LastLoginDateTime;
                    if (activeUser.LastLoginDateTime > currentUserLastLoginDateTime)
                    {
                        activeUsers[activeUser.Login] = activeUser;
                    }
                }
            }

            var dtos = new List<Plugin_GetActiveUsersOutputDto>();
            foreach (var user in users)
            {
                var dto = new Plugin_GetActiveUsersOutputDto()
                {
                    ActorId = user.ActorId,
                    Login = user.Login,
                    FullName = user.FullName
                };

                if (activeUsers.ContainsKey(user.Login))
                {
                    var activeUser = activeUsers[user.Login];

                    dto.Id = activeUser.Id;
                    dto.Host = activeUser.Host;
                    dto.ComputerName = activeUser.ComputerName;
                    dto.LastLoginDateTime = activeUser.LastLoginDateTime;
                }

                dtos.Add(dto);
            }

            return Ok(dtos);
        }
    }
}
