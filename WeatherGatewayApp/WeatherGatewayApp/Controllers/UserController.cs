using Microsoft.AspNetCore.Mvc;
using WeatherGatewayApp.Application.Contracts;
using WeatherGatewayApp.Application.Users;

namespace WeatherProviderApp.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly IUserAppService appService;

        public UserController(IUserAppService appService)
        {
            this.appService = appService;
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Post(UserPostDto dto)
        {
            var user = await appService.PostAsync(dto);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet]
        public async Task<ActionResult<UserDto>> Get([FromQuery]UserGetDto dto)
        {
            var user = await appService.GetAsync(dto.Email, dto.Password);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            appService.DeleteAsync(id);
            return Ok();
        }
    }
}
