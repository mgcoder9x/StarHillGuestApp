namespace FresherDev.HMS.EntityFramework
{
    public interface IPaginationInput
    {
        int Page { get; set; }

        int PageSize { get; set; }        

        string? OrderColumn { get; set; }

        OrderDirection Direction { get; set; }
    }    
}
