using Domain.Interface.User;
using Domain.User;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.User
{
    public class CreateUserDto(string name, string password, string role)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Name { get; set; } = name;
        [Required]
        public string Password { get; set; } = password;
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Role { get; set; } = role;

        public virtual IUser ToDomain() => new Domain.User.User(Name, Password, Role);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var user = (CreateUserDto)obj;

            return Name == user.Name && Password == user.Password && Role == user.Role;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Password, Role);
        }
    }
}
