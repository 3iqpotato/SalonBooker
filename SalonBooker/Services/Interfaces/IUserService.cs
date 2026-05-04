namespace SalonBooker.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> IsBarberAsync(string userId);
        Task<string?> GetCannotBookReasonAsync(string userId);
    }
}