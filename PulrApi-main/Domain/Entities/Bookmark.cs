namespace Core.Domain.Entities;

public class Bookmark : EntityBase
{
    public int PostId { get; set; }
    public Post Post { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
}