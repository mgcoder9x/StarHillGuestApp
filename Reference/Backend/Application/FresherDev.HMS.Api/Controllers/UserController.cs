using FresherDev.HMS.Common;
using FresherDev.HMS.Core.Users;
using FresherDev.HMS.EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace FresherDev.HMS.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IQueryUserUseCase queryUserUseCase;
        private readonly IAddUserUseCase addUserUseCase;
        private readonly IUpdateUserUseCase updateUserUseCase;
        private readonly IDeleteUserUseCase deleteUserUseCase;

        public UserController(ILogger<UserController> logger,
            IQueryUserUseCase queryUserUseCase,
            IAddUserUseCase addUserUseCase,
            IUpdateUserUseCase updateUserUseCase,
            IDeleteUserUseCase deleteUserUseCase)
        {
            _logger = logger;
            this.queryUserUseCase = queryUserUseCase;
            this.addUserUseCase = addUserUseCase;
            this.updateUserUseCase = updateUserUseCase;
            this.deleteUserUseCase = deleteUserUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUserAsync()
        {
            var users = await queryUserUseCase.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("paging")]
        public async Task<IActionResult> GetAllUserAsync([FromQuery] PaginationInput input, [FromQuery] QueryUserCriteria criteria)
        {
            var users = await queryUserUseCase.GetUserPaginationAsync(input, criteria);
            return Ok(users);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserAsync(Guid userId)
        {
            var response = await queryUserUseCase.GetUserAsync(userId);
            return response.ToActionResult();
        }

        [HttpPost("transaction")]
        public async Task<IActionResult> AddTransactionAsync()
        {
            await this.addUserUseCase.AddUserTransactionAsync();
            return Ok("Add transaction success");
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromQuery] string username)
        {
            await this.addUserUseCase.AddUserAsync(username);
            return Ok("Add user success: " + username);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync([FromQuery] string username, [FromQuery] string lastName)
        {
            await this.updateUserUseCase.UpdateUserAsync(username, lastName);
            return Ok("Update user success: " + username);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAsync([FromQuery] string username)
        {
            await this.deleteUserUseCase.DeleteUserAsync(username);
            return Ok();
        }

        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllAsync()
        {
            await this.deleteUserUseCase.DeleteAllUsersAsync();
            return Ok();
        }
    }
}