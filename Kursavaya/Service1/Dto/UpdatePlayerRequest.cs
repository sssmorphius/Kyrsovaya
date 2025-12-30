namespace AuthService.Dto
{
    public class UpdatePlayerTagRequest
    {
        public string PlayerTag { get; set; } = default!;
    }

    public class UpdateAvatarRequest
    {
        public string AvatarUrl { get; set; } = default!;
    }
}
