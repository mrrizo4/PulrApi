namespace Core.Application.Mediatr.Bookmarks.Queries;

public class BookmarkDto
{
    public int Count { get; set; }
    public string Action { get; set; }
    public bool BookmarkedByMe { get; set; }
    public bool IsMyStyle { get; set; }
    public string PostUid { get; internal set; }
}