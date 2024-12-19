namespace proiect_daw.Models
{
    public class FollowRequest
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public bool PendingApproval { get; set; }
        public virtual ApplicationUser Sender { get; set; } = null!;
        public virtual ApplicationUser Receiver { get; set; } = null!;
    }
}
