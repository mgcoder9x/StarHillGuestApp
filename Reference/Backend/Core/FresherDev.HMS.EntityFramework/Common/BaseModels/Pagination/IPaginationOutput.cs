namespace FresherDev.HMS.EntityFramework
{
    public interface IPaginationOutput<TModel>
         where TModel : class
    {
        int Page { get; set; }

        int Total { get; set; }

        int PageSize { get; set; }

        IEnumerable<TModel> Items { get; set; }
    }
}
