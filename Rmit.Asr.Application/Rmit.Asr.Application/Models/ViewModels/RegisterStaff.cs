using Rmit.Asr.Application.ValidationAttributes;

namespace Rmit.Asr.Application.Models.ViewModels
{
    public class RegisterStaff : Staff
    {
        /// <summary>
        /// Staff ID applied with staff ID validation.
        /// </summary>
        [StaffId]
        public override string Id { get; set; }
    }
}