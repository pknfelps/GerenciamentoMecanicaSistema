using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Service.Interface;
using Service.Interface.Dto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service
{
    public class AuthenticationService(IConfiguration configuration, IUsuarioService usuarioService) : IAuthenticationService
    {
        private IConfiguration Configuration { get; set; } = configuration;
        private IUsuarioService UsuarioService { get; set; } = usuarioService;

        public async Task<string> Login(UsuarioDto usuarioDto)
        {
            var usuario = await UsuarioService.GetUsuario(usuarioDto);

            if (usuario == null)
                return string.Empty;

            if (!usuarioDto.Equals(usuario))
                return string.Empty;

            SymmetricSecurityKey secretKey = new(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"] ?? string.Empty));
            var issuer = Configuration["Jwt:Issuer"];
            var audience = Configuration["Jwt:Audience"];

            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenOptions = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims:
                [
                    new Claim(type: ClaimTypes.Name, usuario.Nome),
                    new Claim(type: ClaimTypes.Role, usuario.Cargo)
                ],
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signinCredentials);

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
    }
}
